Imports System.Text
Imports System.Threading
Imports System.Net.Sockets
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Runtime.InteropServices

Partial Public Class Plugin
    Private Class SsdpServer
        Private ReadOnly upnpServer As UpnpServer
        Private listenerThreads() As Thread
        Private notifyTimers() As Timer
        Private sockets() As SocketSet
        Private ReadOnly rand As New Random
        Private Const maxAge As Integer = 1800

        Public Sub New(upnpServer As UpnpServer)
            Me.upnpServer = upnpServer
        End Sub

        Public Sub Start()
            sockets = New SocketSet(hostAddresses.Length - 1) {}
            listenerThreads = New Thread(hostAddresses.Length - 1) {}
            notifyTimers = New Timer(hostAddresses.Length - 1) {}
            Dim broadcastGroup As IPAddress = IPAddress.Parse("239.255.255.250")
            For index As Integer = 0 To sockets.Length - 1
                Dim socket As New SocketSet
                sockets(index) = socket
                Dim address As IPAddress = hostAddresses(index)
                socket.Address = address
                Dim listenerSocket As New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
                listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, True)
                listenerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2)
                listenerSocket.Bind(New IPEndPoint(address, 1900))
                listenerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, New MulticastOption(broadcastGroup, address))
                socket.ListenerSocket = listenerSocket
                If Settings.EnableContentAccess Then
                    Dim notifySocket As New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                    notifySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
                    notifySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, True)
                    notifySocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2)
                    notifySocket.Bind(New IPEndPoint(address, 1900))
                    socket.NotifySocket = notifySocket
                End If
                Dim thread As New Thread(New ParameterizedThreadStart(AddressOf ListenNotify)) With {
                    .IsBackground = True,
                    .Priority = ThreadPriority.BelowNormal
                }
                listenerThreads(index) = thread
                thread.Start(socket)
            Next index
            If Settings.EnableContentAccess Then
                Dim thread As New Thread(AddressOf StartSendDisconnectNotify) With {
                    .IsBackground = True
                }
                thread.Start()
            End If
        End Sub

        Private Sub StartSendDisconnectNotify()
            Try
                For index As Integer = 0 To sockets.Length - 1
                    Dim socket As SocketSet = sockets(index)
                    Try
                        SendNotify(socket.NotifySocket, socket.Address.ToString(), False)
                    Catch ex As Exception
                        LogError(ex, "SsdpServer:Start:SendNotify")
                    End Try
                    notifyTimers(index) = New Timer(New TimerCallback(AddressOf OnNotifyTimeout), socket, 1000, (maxAge - 10) * 1000)
                Next index
            Catch ex As Exception
                LogError(ex, "SsdpServer:Start")
            End Try
        End Sub

        Public Sub [Stop]()
            If sockets IsNot Nothing Then
                For index As Integer = 0 To sockets.Length - 1
                    If notifyTimers(index) IsNot Nothing Then
                        notifyTimers(index).Dispose()
                    End If
                    If sockets(index) IsNot Nothing Then
                        If sockets(index).ListenerSocket IsNot Nothing Then
                            closesocket(sockets(index).ListenerSocket.Handle)
                            sockets(index).ListenerSocket.Close()
                        End If
                    End If
                    If listenerThreads(index) IsNot Nothing Then
                        listenerThreads(index).Join()
                    End If
                Next index
                sockets = Nothing
                listenerThreads = Nothing
                notifyTimers = Nothing
            End If
        End Sub

        Public Sub Restart()
            [Stop]()
            Start()
        End Sub

        Private Sub ListenNotify(parameters As Object)
            Dim socket As SocketSet = DirectCast(parameters, SocketSet)
            Dim buffer As Byte() = New Byte(1023) {}
            Dim length As Integer
            Dim localEndPoint As String = socket.Address.ToString()
            Do
                Dim receivePoint As EndPoint = New IPEndPoint(0, 0)
                Try
                    length = socket.ListenerSocket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, receivePoint)
                Catch
                    Exit Do
                End Try
                Dim header As String() = Encoding.ASCII.GetString(buffer, 0, length).Split(New String() {ControlChars.CrLf}, StringSplitOptions.RemoveEmptyEntries)
                If header.Length = 0 OrElse (TypeOf receivePoint Is IPEndPoint AndAlso DirectCast(receivePoint, IPEndPoint).Port = Settings.ServerPort) Then
                    'ignore
                ElseIf Settings.EnableContentAccess AndAlso String.Compare(header(0), "M-SEARCH * HTTP/1.1", StringComparison.OrdinalIgnoreCase) = 0 Then
                    Try
                        Dim lookup As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                        For index As Integer = 1 To header.Length - 1
                            Dim keyValue As String() = header(index).Split(New Char() {":"c}, 2)
                            If keyValue.Length = 2 Then
                                lookup(keyValue(0).Trim()) = keyValue(1).Trim()
                            End If
                        Next index
                        Dim value As String = Nothing
                        If Not lookup.TryGetValue("MAN", value) OrElse String.Compare(value, """ssdp:discover""", StringComparison.OrdinalIgnoreCase) <> 0 Then
                            Continue Do
                        End If
                        Dim mx As Integer
                        If Not lookup.TryGetValue("MX", value) OrElse Not Integer.TryParse(value, mx) OrElse mx <= 0 Then
                            mx = 1
                        End If
                        Dim st As String = Nothing
                        If Not lookup.TryGetValue("ST", st) Then
                            Continue Do
                        End If
                        Dim usn As String = "uuid:" & upnpServer.RootDevice.Udn.ToString()
                        If String.Compare(st, "upnp:rootdevice", StringComparison.OrdinalIgnoreCase) = 0 OrElse String.Compare(st, upnpServer.RootDevice.DeviceType, StringComparison.OrdinalIgnoreCase) = 0 Then
                            ' ignore
                        ElseIf String.Compare(st, usn, StringComparison.OrdinalIgnoreCase) = 0 Then
                            st = usn
                        ElseIf String.Compare(st, "ssdp:all", StringComparison.OrdinalIgnoreCase) = 0 Then
                            Thread.Sleep(rand.Next(mx * 100))
                            SendResponseMessage(socket.ListenerSocket, receivePoint, localEndPoint, "upnp:rootdevice", usn)
                            SendResponseMessage(socket.ListenerSocket, receivePoint, localEndPoint, usn, usn)
                            SendResponseMessage(socket.ListenerSocket, receivePoint, localEndPoint, upnpServer.RootDevice.DeviceType, usn)
                            For Each service As UpnpService In upnpServer.RootDevice.Services
                                SendResponseMessage(socket.ListenerSocket, receivePoint, localEndPoint, service.ServiceType, usn)
                            Next service
                            Continue Do
                        ElseIf upnpServer.RootDevice.Services.FirstOrDefault(Function(a) String.Compare(st, a.ServiceType, StringComparison.OrdinalIgnoreCase) = 0) Is Nothing Then
                            Continue Do
                        End If
                        Thread.Sleep(rand.Next(mx * 100))
                        SendResponseMessage(socket.ListenerSocket, receivePoint, localEndPoint, st, usn)
                    Catch
                        Thread.Sleep(100)
                    End Try
                ElseIf String.Compare(header(0), "NOTIFY * HTTP/1.1", StringComparison.OrdinalIgnoreCase) = 0 Then
                    Do While controller Is Nothing
                        Thread.Sleep(50)
                        Thread.MemoryBarrier()
                    Loop
                    Try
                        controller.ProcessMessageHeader(receivePoint, header, "nt:")
                    Catch ex As Exception
                        LogError(ex, "ListenNotify")
                    End Try
                End If
            Loop
            If socket.NotifySocket IsNot Nothing Then
                Try
                    SendNotify(socket.NotifySocket, socket.Address.ToString(), False)
                Catch
                End Try
                Try
                    closesocket(socket.NotifySocket.Handle)
                    socket.NotifySocket.Close()
                    socket.NotifySocket = Nothing
                Catch
                End Try
            End If
        End Sub

        Private Sub OnNotifyTimeout(parameters As Object)
            Try
                Dim socket As SocketSet = DirectCast(parameters, SocketSet)
                SendNotify(socket.NotifySocket, socket.Address.ToString(), True)
            Catch ex As Exception
                LogError(ex, "SsdpServer:OnNotifyTimeout")
            End Try
        End Sub

        Private Sub SendNotify(socket As Socket, host As String, isAlive As Boolean)
            SendNotifyMessage(socket, host, "upnp:rootdevice", upnpServer.RootDevice.Udn.ToString(), isAlive)
            SendNotifyMessage(socket, host, upnpServer.RootDevice.Udn.ToString(), upnpServer.RootDevice.Udn.ToString(), isAlive)
            SendNotifyMessage(socket, host, upnpServer.RootDevice.DeviceType, upnpServer.RootDevice.Udn.ToString(), isAlive)
            For Each service As UpnpService In upnpServer.RootDevice.Services
                SendNotifyMessage(socket, host, service.ServiceType, upnpServer.RootDevice.Udn.ToString(), isAlive)
            Next service
        End Sub

        Private Sub SendNotifyMessage(socket As Socket, host As String, nt As String, usn As String, isAlive As Boolean)
            Try
                socket.SendTo(Encoding.ASCII.GetBytes(String.Format("NOTIFY * HTTP/1.1{10}HOST: 239.255.255.250:1900{10}CACHE-CONTROL: max-age = {0}{10}LOCATION: http://{1}:{2}/description.xml{10}NT: {3}{10}NTS: ssdp:{4}{10}SERVER: WindowsNT/{5}.{6} UPnP/1.1 MusicBee UPnP Plugin/{7}{10}USN: uuid:{8}{9}{10}{10}", maxAge, host, Settings.ServerPort, If(nt = usn, "uuid:" & nt, nt), If(isAlive, "alive", "byebye"), Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, musicBeePluginVersion, usn, If(nt = usn, "", "::" & nt), ControlChars.CrLf)), New IPEndPoint(IPAddress.Broadcast, 1900))
            Catch ex As Exception
                LogError(ex, "SsdpServer:SendNotifyMessage", "host=" & host & ",nt=" & nt & ",usn=" & usn & ",isalive=" & isAlive)
            End Try
            Thread.Sleep(50)
        End Sub

        Private Sub SendResponseMessage(socket As Socket, receivePoint As EndPoint, host As String, st As String, usn As String)
            Try
                socket.SendTo(Encoding.ASCII.GetBytes(String.Format("HTTP/1.1 200 OK{10}CACHE-CONTROL: max-age = {0}{10}DATE: {1}{10}EXT:{10}LOCATION: http://{2}:{3}/description.xml{10}SERVER: WindowsNT/{4}.{5} UPnP/1.1 MusicBee UPnP Plugin/{6}{10}ST: {7}{10}USN: {8}{9}{10}{10}", maxAge, DateTime.Now.ToString("r"), host, Settings.ServerPort, Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor, musicBeePluginVersion, st, usn, If(st = usn, "", "::" & st), ControlChars.CrLf)), receivePoint)
            Catch ex As Exception
                LogError(ex, "SsdpServer:SendResponseMessage", "host=" & host & ",st=" & st & ",usn=" & usn)
            End Try
        End Sub

        Private Class SocketSet
            Public Address As IPAddress
            Public ListenerSocket As Socket
            Public NotifySocket As Socket
        End Class  ' SocketSet

        <DllImport("ws2_32.dll", CharSet:=CharSet.Unicode)> _
        Private Shared Function closesocket(socketHandle As IntPtr) As Integer
        End Function
    End Class  ' SsdpServer
End Class
