Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Xml
Imports System.Runtime.InteropServices
Imports System.Net
Imports System.Net.Sockets

Partial Public Class Plugin
    Private Shared ReadOnly renderingDevices As New List(Of MediaRendererDevice)
    Private Shared activeRenderingDevice As MediaRendererDevice

    Private Class ControlPointManager
        Private listenerThreads() As Thread = Nothing
        Private listenerSockets() As Socket = Nothing
        Private started As Boolean = False
        Private ReadOnly resourceLock As New Object
        Private shutdownSearchTimer As Timer = Nothing

        Public Sub Dispose()
            [Stop]()
        End Sub

        Public Sub Start()
            Try
                ''server.HttpServer.AddRoute("NOTIFY", "/eventSub", New HttpRouteDelegate(AddressOf ProcessEventSubscription))
                If Settings.EnablePlayToDevice Then
                    ExecuteSearchForRenderingDevices()
                End If
            Catch ex As Exception
                LogError(ex, "ControlPointManager:Start")
            End Try
        End Sub

        Public Sub [Stop]()
            AudioEncoder.StopEncode()
            ShutdownSearchThreads(Nothing)
            started = False
            SyncLock renderingDevices
                For Each device As MediaRendererDevice In renderingDevices
                    device.Dispose()
                Next device
                renderingDevices.Clear()
                activeRenderingDevice = Nothing
            End SyncLock
        End Sub

        Public Sub Restart()
            If activeRenderingDevice IsNot Nothing Then
                activeRenderingDevice.StopPlayback(True)
            End If
            If Not Settings.EnablePlayToDevice Then
                [Stop]()
            ElseIf Not started Then
                ExecuteSearchForRenderingDevices()
            End If
        End Sub

        Private Sub ExecuteSearchForRenderingDevices()
            listenerThreads = New Thread(hostAddresses.Length - 1) {}
            listenerSockets = New Socket(hostAddresses.Length - 1) {}
            Dim broadcastGroup As IPAddress = IPAddress.Parse("239.255.255.250")
            Dim remoteEndPoint As New IPEndPoint(broadcastGroup, 1900)
            Dim request() As Byte = Encoding.UTF8.GetBytes("M-SEARCH * HTTP/1.1" & ControlChars.CrLf & "HOST: 239.255.255.250:1900" & ControlChars.CrLf & "ST:urn:schemas-upnp-org:device:MediaRenderer:1" & ControlChars.CrLf & "MAN: ""ssdp:discover""" & ControlChars.CrLf & "MX: 1" & ControlChars.CrLf & ControlChars.CrLf)
            For index As Integer = 0 To hostAddresses.Length - 1
                Dim address As IPAddress = hostAddresses(index)
                Dim listenerSocket As New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, True)
                listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, True)
                listenerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, New MulticastOption(broadcastGroup, address))
                listenerSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2)
                listenerSocket.Bind(New IPEndPoint(address, 0))
                listenerSockets(index) = listenerSocket
                Dim thread As New Thread(New ParameterizedThreadStart(AddressOf Listen)) With {
                    .IsBackground = True,
                    .Priority = ThreadPriority.BelowNormal
                }
                listenerThreads(index) = thread
                thread.Start(listenerSocket)
                listenerSocket.SendTo(request, remoteEndPoint)
            Next index
            started = True
            shutdownSearchTimer = New Timer(AddressOf ShutdownSearchThreads, Nothing, 30000, Timeout.Infinite)
        End Sub

        Private Sub ShutdownSearchThreads(state As Object)
            Try
                SyncLock resourceLock
                    If shutdownSearchTimer IsNot Nothing Then
                        shutdownSearchTimer.Dispose()
                        shutdownSearchTimer = Nothing
                    End If
                    If listenerSockets IsNot Nothing Then
                        For index As Integer = 0 To listenerSockets.Length - 1
                            If listenerSockets(index) IsNot Nothing Then
                                Try
                                    closesocket(listenerSockets(index).Handle)
                                    listenerSockets(index).Close()
                                    listenerThreads(index).Join()
                                Catch
                                End Try
                            End If
                        Next index
                        listenerSockets = Nothing
                        listenerThreads = Nothing
                    End If
                End SyncLock
            Catch
            End Try
        End Sub

        Private Sub Listen(parameters As Object)
            Dim socket As Socket = DirectCast(parameters, Socket)
            Dim buffer As Byte() = New Byte(1023) {}
            Dim length As Integer
            Do
                Dim receivePoint As EndPoint = New IPEndPoint(IPAddress.Any, 0)
                Try
                    length = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, receivePoint)
                Catch
                    Exit Do
                End Try
                Try
                    If TypeOf receivePoint Is IPEndPoint AndAlso DirectCast(receivePoint, IPEndPoint).Port = Settings.ServerPort Then
                        ' ignore
                    Else
                        ProcessMessageHeader(receivePoint, Encoding.ASCII.GetString(buffer, 0, length).Split(New String() {ControlChars.CrLf}, StringSplitOptions.RemoveEmptyEntries), "st:")
                    End If
                Catch ex As Exception
                    LogError(ex, "ControlListen")
                End Try
            Loop
        End Sub

        Public Sub ProcessMessageHeader(receivingSocket As EndPoint, header() As String, serviceKey As String)
            If Settings.EnablePlayToDevice Then
                Dim deviceLocationUrl As Uri = Nothing
                Dim isMediaRenderer As Boolean = False
                Dim notificationSubType As String = If(serviceKey <> "st:", Nothing, "ssdp:alive")
                Dim udn As String = Nothing
                For index As Integer = 0 To header.Length - 1
                    If header(index).StartsWith("location:", StringComparison.OrdinalIgnoreCase) Then
                        deviceLocationUrl = New Uri(header(index).Substring(9).Trim())
                    ElseIf header(index).StartsWith("usn:", StringComparison.OrdinalIgnoreCase) Then
                        Dim usn As String = header(index).Substring(4).Trim()
                        If Not usn.EndsWith("::urn:schemas-upnp-org:device:MediaRenderer:1", StringComparison.OrdinalIgnoreCase) AndAlso Not usn.EndsWith("::urn:schemas-upnp-org:device:MediaRenderer:2", StringComparison.OrdinalIgnoreCase) Then
                            udn = Nothing
                        Else
                            udn = usn.Substring(0, usn.Length - 45)
                        End If
                    ElseIf header(index).StartsWith("nts:", StringComparison.OrdinalIgnoreCase) Then
                        notificationSubType = header(index).Substring(4).Trim()
                    ElseIf header(index).StartsWith(serviceKey, StringComparison.OrdinalIgnoreCase) Then
                        isMediaRenderer = (String.Compare(header(index).Substring(3).Trim(), "urn:schemas-upnp-org:device:MediaRenderer:1", StringComparison.OrdinalIgnoreCase) = 0 OrElse String.Compare(header(index).Substring(3).Trim(), "urn:schemas-upnp-org:device:MediaRenderer:2", StringComparison.OrdinalIgnoreCase) = 0)
                    End If
                Next index
                Dim receivingAddress As String = "unknown address"
                If Settings.LogDebugInfo AndAlso TypeOf receivingSocket Is IPEndPoint Then
                    receivingAddress = DirectCast(receivingSocket, IPEndPoint).Address.ToString()
                End If

                'If Settings.LogDebugInfo Then
                '    Dim headerText As New StringBuilder(512)
                '    Try
                '        If Not TypeOf receivingSocket Is IPEndPoint Then
                '            headerText.Append("unknown remote point")
                '        Else
                '            headerText.Append(DirectCast(receivingSocket, IPEndPoint).Address.ToString())
                '        End If
                '    Catch
                '    End Try
                '    headerText.Append("; key=")
                '    headerText.Append(serviceKey)
                '    headerText.Append("; ")
                '    For index As Integer = 0 To header.Length - 1
                '        headerText.Append(header(index))
                '        headerText.Append("; ")
                '    Next index
                '    headerText.Append("nts=")
                '    headerText.Append(If(notificationSubType Is Nothing, "null", notificationSubType))
                '    headerText.Append("; udn=")
                '    headerText.Append(If(udn Is Nothing, "null", udn))
                '    LogInformation("ProcessMessageHeader", headerText.ToString())
                'End If
                If udn IsNot Nothing Then
                    Dim devicesChanged As Boolean = False
                    SyncLock renderingDevices
                        Dim deviceIndex As Integer = -1
                        For index As Integer = 0 To renderingDevices.Count - 1
                            If renderingDevices(index).Udn.Contains(udn) Then
                                deviceIndex = index
                                Exit For
                            End If
                        Next index
                        If notificationSubType = "ssdp:alive" Then
                            If deviceIndex = -1 AndAlso isMediaRenderer AndAlso deviceLocationUrl IsNot Nothing Then
                                Dim device As New MediaRendererDevice(deviceLocationUrl)
                                If device.IsValidRenderer Then
                                    devicesChanged = True
                                    renderingDevices.Add(device)
                                End If
                                LogInformation("ProcessMessage " & receivingAddress, "device '" & udn & ":" & device.FriendlyName & "',valid=" & device.IsValidRenderer)
                            End If
                        ElseIf notificationSubType = "ssdp:byebye" AndAlso deviceIndex <> -1 Then
                            devicesChanged = True
                            If activeRenderingDevice IsNot Nothing AndAlso activeRenderingDevice.Udn.Contains(udn) Then
                                activeRenderingDevice = Nothing
                            End If
                            Dim device As MediaRendererDevice = renderingDevices(deviceIndex)
                            device.Activate(False)
                            renderingDevices.RemoveAt(deviceIndex)
                            LogInformation("ProcessMessage " & receivingAddress, "device '" & udn & ":" & device.FriendlyName & "' disconnected")
                        End If
                    End SyncLock
                    If devicesChanged Then
                        mbApiInterface.MB_SendNotification(CallbackType.RenderingDevicesChanged)
                    End If
                End If
            End If
        End Sub

        ''Private Sub ProcessEventSubscription(request As HttpRequest)
        ''    Dim sid As String
        ''    If request.Headers.TryGetValue("SID", sid) Then
        ''        SyncLock renderingDevices
        ''            Try
        ''                If activeRenderingDevice IsNot Nothing Then
        ''                    activeRenderingDevice.ProcessEventSubscription(sid, request)
        ''                End If
        ''            Catch
        ''            End Try
        ''        End SyncLock
        ''    End If
        ''    Dim response As HttpResponse = request.Response
        ''    response.StateCode = 200
        ''    response.SendHeaders()
        ''End Sub

        <DllImport("ws2_32.dll", CharSet:=CharSet.Unicode)> _
        Private Shared Function closesocket(socketHandle As IntPtr) As Integer
        End Function
    End Class  ' ControlPointManager

    Private Class MediaRendererDevice
        Public ReadOnly FriendlyName As String
        Public ReadOnly Udn As New HashSet(Of String)(StringComparer.Ordinal)
        Private isActive As Boolean = False
        Private ReadOnly modelDescription As String = ""
        Private ReadOnly supportSetNextInQueue As Boolean = False
        'Private supportPlayPause As Boolean = False
        'Private supportPlayNext As Boolean = False
        'Private supportPlayPrev As Boolean = False
        'Private supportPlaySeek As Boolean = False
        ''Private avTransportEventSid As String
        ''Private avTransportEventTimeout As Integer
        ''Private renderingControlEventSid As String
        ''Private renderingControlEventTimeout As Integer
        Private deviceMinVolume As UInteger = 0
        Private deviceMaxVolume As UInteger = 100
        Private currentVolume As UInteger
        Private currentMute As Boolean
        Private currentErrorCount As Integer = 0
        Private currentPlayState As PlayState = PlayState.Undefined
        Private currentPlaySpeed As Integer = 1
        'Private currentTransportStatus As String
        Private currentPlayStartTimeEstimated As Boolean
        Private currentPlayStartTicks As Long
        Private currentTrackDurationTicks As Long
        Private currentPlayPositionMs As Integer
        'Private currentPlayUrl As String
        'Private nextPlayUrl As String
        Private lastUserInitiatedStop As Long = 0
        Private queueNextTrackPending As Boolean = False
        'Private queueNextFailedCount As Integer = 0
        Private lastServerHeader As String
        Private directory As ItemManager = Nothing
        Private streamingProfile As StreamingProfile = Nothing
        Private ReadOnly resourceLock As New Object
        Private playToMode As Boolean = False
        Private statusTimerInterval As Integer = 500
        'Private ReadOnly deviceLocationUrl As Uri
        Private ReadOnly connectionManagerUrl As Uri
        'Private ReadOnly connectionManagerEventUrl As Uri
        Private ReadOnly renderingControlUrl As Uri
        Private ReadOnly renderingControlSCPDUri As Uri
        Private ReadOnly renderingControlEventUrl As Uri
        ''Private ReadOnly renderingControlNotifyTimer As New Timer(AddressOf OnRenderingControlResubscribe, Nothing, Timeout.Infinite, Timeout.Infinite)
        Private ReadOnly avTransportControlUrl As Uri
        Private ReadOnly avTransportSCPDUri As Uri
        Private ReadOnly avTransportEventUrl As Uri
        ''Private ReadOnly avTransportNotifyTimer As New Timer(AddressOf OnAvTransportResubscribe, Nothing, Timeout.Infinite, Timeout.Infinite)
        Private ReadOnly avTransportStatusTimer As New Timer(AddressOf OnAvTransportStatusCheck, Nothing, Timeout.Infinite, Timeout.Infinite)
        Private Shared ReadOnly userAgent As String = "User-Agent: MusicBee UPnP Plugin" & ControlChars.CrLf

        Public Sub New(deviceLocationUrl As Uri)
            'Me.deviceLocationUrl = deviceLocationUrl
            Dim xml As String = GetXmlDocument(deviceLocationUrl)
            For retry As Integer = 1 To 2
                Try
                    If retry = 2 Then
                        xml = GetCleanedXml(xml)
                    End If
                    'LogInformation("NewMediaRendererDevice:" & deviceLocationUrl.ToString(), xml)
                    Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
                        Do While reader.Read()
                            If reader.NodeType = XmlNodeType.Element Then
                                Dim name As String = reader.Name
                                If String.Compare(name, "friendlyName", StringComparison.OrdinalIgnoreCase) = 0 Then
                                    If FriendlyName IsNot Nothing Then
                                        reader.ReadString()
                                    Else
                                        FriendlyName = reader.ReadString()
                                    End If
                                ElseIf String.Compare(name, "modelDescription", StringComparison.OrdinalIgnoreCase) = 0 Then
                                    If modelDescription IsNot Nothing Then
                                        reader.ReadString()
                                    Else
                                        modelDescription = reader.ReadString()
                                    End If
                                ElseIf String.Compare(name, "UDN", StringComparison.OrdinalIgnoreCase) = 0 Then
                                    Udn.Add(reader.ReadString())
                                ElseIf String.Compare(name, "service", StringComparison.OrdinalIgnoreCase) = 0 Then
                                    Using serviceReader As XmlReader = reader.ReadSubtree()
                                        Dim serviceType As String
                                        Do While serviceReader.Read()
                                            If serviceReader.NodeType = XmlNodeType.Element Then
                                                name = serviceReader.Name
                                                If String.Compare(name, "serviceType", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                    serviceType = serviceReader.ReadString()
                                                ElseIf serviceType IsNot Nothing Then
                                                    If String.Compare(name, "SCPDURL", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                        ':1
                                                        If serviceType.StartsWith("urn:schemas-upnp-org:service:AVTransport:", StringComparison.OrdinalIgnoreCase) Then
                                                            avTransportSCPDUri = New Uri(deviceLocationUrl, serviceReader.ReadString())
                                                        ElseIf serviceType.StartsWith("urn:schemas-upnp-org:service:RenderingControl:", StringComparison.OrdinalIgnoreCase) Then
                                                            renderingControlSCPDUri = New Uri(deviceLocationUrl, serviceReader.ReadString())
                                                        End If
                                                    ElseIf String.Compare(name, "controlURL", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                        ':1
                                                        If serviceType.StartsWith("urn:schemas-upnp-org:service:ConnectionManager:", StringComparison.OrdinalIgnoreCase) Then
                                                            connectionManagerUrl = New Uri(deviceLocationUrl, serviceReader.ReadString())
                                                        ElseIf serviceType.StartsWith("urn:schemas-upnp-org:service:AVTransport:", StringComparison.OrdinalIgnoreCase) Then
                                                            avTransportControlUrl = New Uri(deviceLocationUrl, serviceReader.ReadString())
                                                        ElseIf serviceType.StartsWith("urn:schemas-upnp-org:service:RenderingControl:", StringComparison.OrdinalIgnoreCase) Then
                                                            renderingControlUrl = New Uri(deviceLocationUrl, serviceReader.ReadString())
                                                        End If
                                                    ElseIf String.Compare(name, "eventSubURL", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                        ':1
                                                        If serviceType.StartsWith("urn:schemas-upnp-org:service:AVTransport:", StringComparison.OrdinalIgnoreCase) Then
                                                            avTransportEventUrl = New Uri(deviceLocationUrl, serviceReader.ReadString())
                                                        ElseIf serviceType.StartsWith("urn:schemas-upnp-org:service:RenderingControl:", StringComparison.OrdinalIgnoreCase) Then
                                                            renderingControlEventUrl = New Uri(deviceLocationUrl, serviceReader.ReadString())
                                                        End If
                                                    End If
                                                End If
                                            End If
                                        Loop
                                    End Using
                                End If
                            End If
                        Loop
                    End Using
                    Exit For
                Catch ex As XmlException
                    If retry = 2 Then
                        Throw LogError(ex, "NewMediaRendererDevice:" & deviceLocationUrl.ToString(), xml)
                    End If
                End Try
            Next retry
        End Sub

        Public Sub Dispose()
            If isActive Then
                StopPlayback()
                If mbApiInterface.Player_GetPlayState() <> Plugin.PlayState.Stopped Then
                    mbApiInterface.Player_Stop()
                End If
                Activate(False)
            End If
            ''avTransportNotifyTimer.Dispose()
            ''renderingControlNotifyTimer.Dispose()
        End Sub

        Public ReadOnly Property IsValidRenderer() As Boolean
            Get
                Return (avTransportSCPDUri IsNot Nothing AndAlso avTransportEventUrl IsNot Nothing AndAlso connectionManagerUrl IsNot Nothing AndAlso renderingControlEventUrl IsNot Nothing)
            End Get
        End Property

        Public Function Activate(active As Boolean) As Boolean
            Dim xml As String = Nothing
            Try
                If active Then
                    If isActive Then
                        Activate(False)
                    End If
                    If directory Is Nothing Then
                        Dim requestHeaders As New Dictionary(Of String, String) From {
                            {"User-Agent", modelDescription & "|" & lastServerHeader}
                        }
                        streamingProfile = Settings.GetStreamingProfile(requestHeaders)
                        directory = ItemManager.GetItemManager(requestHeaders)
                        If lastServerHeader IsNot Nothing AndAlso lastServerHeader.IndexOf("Platinum", StringComparison.OrdinalIgnoreCase) <> -1 Then
                            directory.DisablePcmTimeSeek = True
                        End If
                    End If
                    xml = GetXmlDocument(avTransportSCPDUri)
                    For retry As Integer = 1 To 2
                        Try
                            If retry = 2 Then
                                xml = GetCleanedXml(xml)
                            End If
                            Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
                                Do While reader.Read()
                                    If reader.NodeType = XmlNodeType.Element AndAlso String.Compare(reader.Name, "Action", StringComparison.OrdinalIgnoreCase) = 0 Then
                                        Using actionReader As XmlReader = reader.ReadSubtree()
                                            Do While actionReader.Read()
                                                If actionReader.NodeType = XmlNodeType.Element AndAlso String.Compare(actionReader.Name, "name", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                    Dim name As String = actionReader.ReadString()
                                                    If String.Compare(name, "SetNextAVTransportURI", StringComparison.OrdinalIgnoreCase) = 0 AndAlso Settings.EnablePlayToSetNext Then
                                                        'supportSetNextInQueue = True
                                                        'ElseIf String.Compare(name, "Pause", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                        '    supportPlayPause = True
                                                        'ElseIf String.Compare(name, "Next", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                        '    supportPlayNext = True
                                                        'ElseIf String.Compare(name, "Previous", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                        '    supportPlayPrev = True
                                                        'ElseIf String.Compare(name, "Seek", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                        '    supportPlaySeek = True
                                                    End If
                                                End If
                                            Loop
                                        End Using
                                    End If
                                Loop
                            End Using
                            Exit For
                        Catch ex As XmlException
                            If retry = 2 Then Throw
                        End Try
                    Next retry
                    Try
                        xml = GetXmlDocument(renderingControlSCPDUri)
                        For retry As Integer = 1 To 2
                            Try
                                If retry = 2 Then
                                    xml = GetCleanedXml(xml)
                                End If
                                Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
                                    Do While reader.Read()
                                        If reader.NodeType = XmlNodeType.Element AndAlso String.Compare(reader.Name, "stateVariable", StringComparison.OrdinalIgnoreCase) = 0 Then
                                            Dim isVolumeNode As Boolean = False
                                            Using variableReader As XmlReader = reader.ReadSubtree()
                                                Do While variableReader.Read()
                                                    If variableReader.NodeType = XmlNodeType.Element Then
                                                        If String.Compare(variableReader.Name, "name", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                            If String.Compare(variableReader.ReadString(), "Volume", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                                isVolumeNode = True
                                                            End If
                                                        ElseIf isVolumeNode Then
                                                            If String.Compare(variableReader.Name, "minimum", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                                UInteger.TryParse(variableReader.ReadString(), deviceMinVolume)
                                                            ElseIf String.Compare(variableReader.Name, "maximum", StringComparison.OrdinalIgnoreCase) = 0 Then
                                                                UInteger.TryParse(variableReader.ReadString(), deviceMaxVolume)
                                                            End If
                                                        End If
                                                    End If
                                                Loop
                                            End Using
                                            If isVolumeNode Then
                                                Exit Do
                                            End If
                                        End If
                                    Loop
                                End Using
                                Exit For
                            Catch ex As XmlException
                                If retry = 2 Then Throw
                            End Try
                        Next retry
                    Catch ex As Exception
                        LogError(ex, "Activate:GetVolumeRange")
                    End Try
                    Dim values() As String
                    Using reader As New XmlTextReader(PostSoapRequest(connectionManagerUrl, "GetProtocolInfo", "urn:schemas-upnp-org:service:ConnectionManager:1"), XmlNodeType.Document, Nothing)
                        Do While reader.Read()
                            If reader.NodeType = XmlNodeType.Element AndAlso String.Compare(reader.Name, "Sink", StringComparison.OrdinalIgnoreCase) = 0 Then
                                Dim mimeTypeList As String = reader.ReadString()
                                LogInformation("Activate", FriendlyName & ":" & mimeTypeList)
                                values = mimeTypeList.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries)
                                Dim supportedMimeTypes() As String = New String(values.Length - 1) {}
                                For index As Integer = 0 To values.Length - 1
                                    Dim sections() As String = values(index).Split(":"c)
                                    If sections.Length <= 2 Then
                                        supportedMimeTypes(index) = "error"
                                        LogInformation("Activate", "invalid mime type: " & values(index))
                                    Else
                                        supportedMimeTypes(index) = sections(2)
                                    End If
                                Next index
                                directory.SupportedMimeTypes = supportedMimeTypes
                                Exit Do
                            End If
                        Loop
                    End Using
                    ''values = SubscribeSoapRequest(avTransportEventUrl, primaryHostAddress & "/eventSub", Nothing)
                    ''avTransportEventSid = values(0)
                    ''If Integer.TryParse(values(1), avTransportEventTimeout) Then
                    ''    avTransportEventTimeout *= 1000
                    ''Else
                    ''    avTransportEventTimeout = Timeout.Infinite
                    ''End If
                    ''avTransportNotifyTimer.Change(avTransportEventTimeout, avTransportEventTimeout)
                    ''values = SubscribeSoapRequest(renderingControlEventUrl, PrimaryHostUrl & "/eventSub", Nothing)
                    ''renderingControlEventSid = values(0)
                    ''If Integer.TryParse(values(1), renderingControlEventTimeout) Then
                    ''    renderingControlEventTimeout *= 1000
                    ''Else
                    ''    renderingControlEventTimeout = Timeout.Infinite
                    ''End If
                    ''renderingControlNotifyTimer.Change(renderingControlEventTimeout, renderingControlEventTimeout)
                    StopPlayback()
                    GetPlayStateInformation()
                    InitialiseVolume()
                    isActive = True
                    Return (directory.SupportedMimeTypes IsNot Nothing)
                ElseIf isActive Then
                    isActive = False
                    playToMode = False
                    avTransportStatusTimer.Change(Timeout.Infinite, Timeout.Infinite)
                    If currentPlayState <> PlayState.Stopped Then
                        currentPlayState = PlayState.Stopped
                        mbApiInterface.Player_Stop()
                    End If
                    ''avTransportNotifyTimer.Change(Timeout.Infinite, Timeout.Infinite)
                    ''UnsubscribeSoapRequest(avTransportEventUrl, avTransportEventSid)
                    ''avTransportEventSid = Nothing
                    ''renderingControlNotifyTimer.Change(Timeout.Infinite, Timeout.Infinite)
                    ''UnsubscribeSoapRequest(renderingControlEventUrl, renderingControlEventSid)
                    ''renderingControlEventSid = Nothing
                End If
                Return True
            Catch ex As XmlException
                LogError(ex, "Activate:" & active & ":" & avTransportSCPDUri.ToString(), xml)
                Return False
            Catch ex As Exception
                LogError(ex, "Activate:" & active)
                Return False
            End Try
        End Function

        ''Public Sub ProcessEventSubscription(sid As String, request As HttpRequest)
        ''    If isActive AndAlso String.Compare(sid, renderingControlEventSid, StringComparison.OrdinalIgnoreCase) = 0 Then   '' OrElse String.Compare(sid, avTransportEventSid, StringComparison.OrdinalIgnoreCase) = 0) Then
        ''        Using stream As IO.MemoryStream = request.GetContent(), _
        ''              reader As New XmlTextReader(stream)
        ''            Do While reader.Read()
        ''                If reader.NodeType = XmlNodeType.Element AndAlso String.Compare(reader.Name, "LastChange", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                    Dim xml As String = reader.ReadString()
        ''                    If xml.IndexOf("VolumeDB ", StringComparison.OrdinalIgnoreCase) <> -1 OrElse xml.IndexOf("Mute ", StringComparison.OrdinalIgnoreCase) <> -1 Then
        ''                        'LogInformation("Volume", xml)
        ''                        Using lastChangeReader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
        ''                            Do While lastChangeReader.Read()
        ''                                If lastChangeReader.NodeType = XmlNodeType.Element Then
        ''                                    If String.Compare(lastChangeReader.Name, "VolumeDB", StringComparison.OrdinalIgnoreCase) = 0 AndAlso String.Compare(lastChangeReader.GetAttribute(0), "Master", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                                        Dim value As Integer
        ''                                        If Integer.TryParse(lastChangeReader.GetAttribute("val"), value) Then
        ''                                            currentVolumeDb = value / 256.0F
        ''                                            Debug.WriteLine("cur vol event=" & currentVolumeDb)
        ''                                            Dim newVolume As Single = (currentVolumeDb - deviceMinVolumeDb) / (deviceMaxVolumeDb - deviceMinVolumeDb)
        ''                                            mbApiInterface.Player_SetVolume(newVolume)
        ''                                        End If
        ''                                    ElseIf String.Compare(lastChangeReader.Name, "Mute", StringComparison.OrdinalIgnoreCase) = 0 AndAlso String.Compare(lastChangeReader.GetAttribute(0), "Master", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                                        currentMute = (lastChangeReader.GetAttribute("val") = "1")
        ''                                        mbApiInterface.Player_SetMute(currentMute)
        ''                                    End If
        ''                                    ''    If String.Compare(lastChangeReader.Name, "TransportState", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                                    ''        Dim syncPlayState As Boolean = False
        ''                                    ''        SyncLock resourceLock
        ''                                    ''            Dim oldPlayState As PlayState = currentPlayState
        ''                                    ''            Select Case lastChangeReader.GetAttribute("val")
        ''                                    ''                Case "STOPPED"
        ''                                    ''                    currentPlayState = PlayState.Stopped
        ''                                    ''                Case "PLAYING"
        ''                                    ''                    currentPlayState = PlayState.Playing
        ''                                    ''                Case "PAUSED_PLAYBACK"
        ''                                    ''                    currentPlayState = PlayState.Paused
        ''                                    ''                Case "TRANSITIONING"
        ''                                    ''                    currentPlayState = PlayState.Loading
        ''                                    ''            End Select
        ''                                    ''            Debug.WriteLine(Now.ToString() & ":event=" & currentPlayState.ToString & ",was=" & oldPlayState.ToString())
        ''                                    ''            If currentPlayState <> oldPlayState Then
        ''                                    ''                syncPlayState = ProcessNewPlayState(oldPlayState)
        ''                                    ''            End If
        ''                                    ''        End SyncLock
        ''                                    ''        If syncPlayState Then
        ''                                    ''            SyncNewPlayState()
        ''                                    ''        End If
        ''                                    ''    ElseIf String.Compare(lastChangeReader.Name, "TransportStatus", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                                    ''        currentTransportStatus = lastChangeReader.ReadString()
        ''                                    ''    Else
        ''                                    ''        'CurrentPlayMode
        ''                                    ''        'CurrentTrackURI
        ''                                    ''        'CurrentTrackDuration val=
        ''                                    ''        'CurrentMediaDuration val=
        ''                                    ''        'TransportState
        ''                                    ''        'CurrentTrackMetaData val=
        ''                                    ''        'CurrentTrack val="0"
        ''                                    ''        'NextAVTransportURI val="NOT_IMPLEMENTED"
        ''                                    ''        'NumberOfTracks val="0"
        ''                                    ''        'TransportStatus val="OK"
        ''                                    ''        'TransportPlaySpeed val="1"
        ''                                    ''        'Volume Channel="Master" val="65"
        ''                                    ''        'Mute Channel="Master" val="0"
        ''                                    ''        'VolumeDB Channel="Master" val="-1578"
        ''                                    ''    End If
        ''                                End If
        ''                            Loop
        ''                        End Using
        ''                    End If
        ''                    Exit Do
        ''                End If
        ''            Loop
        ''        End Using
        ''    End If
        ''End Sub

        Private Function ProcessNewPlayState(oldPlayState As PlayState) As Boolean
            If Not playToMode Then
                Return False
            End If
            Dim playNextTrack As Boolean = False
            Select Case currentPlayState
                Case PlayState.Stopped
                    avTransportStatusTimer.Change(Timeout.Infinite, Timeout.Infinite)
                    currentPlayStartTimeEstimated = True
                    currentPlayPositionMs = 0
                    Dim stopInvokedByUser As Boolean = ((DateTime.UtcNow.Ticks - lastUserInitiatedStop) < 5000 * TimeSpan.TicksPerMillisecond)
                    lastUserInitiatedStop = 0
                    queueNextTrackPending = False
                    If (oldPlayState = PlayState.Playing OrElse oldPlayState = PlayState.Loading) AndAlso Not supportSetNextInQueue AndAlso Not stopInvokedByUser Then
                        currentPlayStartTicks = Long.MaxValue
                        If mbApiInterface.Player_PlayNextTrack() Then
                            Return False
                        End If
                    End If
                    Return True
                Case PlayState.Playing
                    If currentPlayStartTimeEstimated Then
                        currentPlayPositionMs = 0
                        currentPlayStartTicks = DateTime.UtcNow.Ticks
                        currentPlayStartTimeEstimated = False
                    Else
                        currentPlayStartTicks = DateTime.UtcNow.Ticks - currentPlayPositionMs * TimeSpan.TicksPerMillisecond
                    End If
                    avTransportStatusTimer.Change(0, statusTimerInterval)
                    Return True
                Case PlayState.Paused
                    currentPlayPositionMs = CInt((DateTime.UtcNow.Ticks - currentPlayStartTicks) \ TimeSpan.TicksPerMillisecond)
                    avTransportStatusTimer.Change(Timeout.Infinite, Timeout.Infinite)
                    Return True
            End Select
            Return False
        End Function

        Private Sub SyncNewPlayState()
            Dim mbPlayState As PlayState = mbApiInterface.Player_GetPlayState()
            If currentPlayState <> mbPlayState Then
                LogInformation("SyncNewPlayState", currentPlayState.ToString & ",mb=" & mbPlayState.ToString())
                Select Case currentPlayState
                    Case PlayState.Stopped
                        If mbPlayState <> PlayState.Loading Then
                            mbApiInterface.Player_Stop()
                        End If
                    Case PlayState.Paused
                        If mbPlayState = PlayState.Playing Then
                            mbApiInterface.Player_PlayPause()
                        End If
                    Case PlayState.Playing
                        If mbPlayState = PlayState.Paused Then
                            mbApiInterface.Player_PlayPause()
                        End If
                End Select
            End If
        End Sub

        Public ReadOnly Property PlayState() As PlayState
            Get
                Return currentPlayState
            End Get
        End Property

        Public ReadOnly Property PlayPositionMs() As Integer
            Get
                Select Case currentPlayState
                    Case PlayState.Playing, Plugin.PlayState.Loading
                        If currentPlayStartTimeEstimated Then
                            Return currentPlayPositionMs
                        Else
                            Return CInt((DateTime.UtcNow.Ticks - currentPlayStartTicks) \ TimeSpan.TicksPerMillisecond)
                        End If
                    Case PlayState.Paused
                        Return currentPlayPositionMs
                End Select
                Return 0
            End Get
        End Property

        Private Function GetPlayStateInformation() As Exception
            Dim xml As String = Nothing
            Try
                xml = PostSoapRequest(avTransportControlUrl, "GetTransportInfo", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID"}, New String() {"0"})
                Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
                    Do While reader.Read()
                        If reader.NodeType = XmlNodeType.Element Then
                            If String.Compare(reader.Name, "CurrentTransportState", StringComparison.OrdinalIgnoreCase) = 0 Then
                                Dim value As String = reader.ReadString()
                                If value IsNot Nothing Then
                                    Select Case value.ToUpper()
                                        Case "STOPPED", "NO_MEDIA_PRESENT"
                                            currentPlayState = PlayState.Stopped
                                        Case "PLAYING"
                                            currentPlayState = PlayState.Playing
                                        Case "PAUSED_PLAYBACK"
                                            currentPlayState = PlayState.Paused
                                        Case "TRANSITIONING"
                                            currentPlayState = PlayState.Loading
                                    End Select
                                End If
                                'ElseIf String.Compare(reader.Name, "CurrentTransportStatus", StringComparison.OrdinalIgnoreCase) = 0 Then
                                '    currentTransportStatus = reader.ReadString()
                            ElseIf String.Compare(reader.Name, "CurrentSpeed", StringComparison.OrdinalIgnoreCase) = 0 Then
                                Integer.TryParse(reader.ReadString(), currentPlaySpeed)
                            End If
                        End If
                    Loop
                End Using
                Return Nothing
            Catch ex As XmlException
                Return LogError(ex, "GetTransportInfo", xml)
            Catch ex As Exception
                Return ex
            End Try
        End Function

        Private Function GetPlayPositionInformation() As Boolean
            Dim xml As String = Nothing
            Try
                xml = PostSoapRequest(avTransportControlUrl, "GetPositionInfo", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID"}, New String() {"0"})
                Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
                    Do While reader.Read()
                        If reader.NodeType = XmlNodeType.Element Then
                            If String.Compare(reader.Name, "TrackURI", StringComparison.OrdinalIgnoreCase) = 0 Then
                                'currentPlayUrl = reader.ReadString()
                            ElseIf String.Compare(reader.Name, "RelTime", StringComparison.OrdinalIgnoreCase) = 0 Then
                                'can be: NOT_IMPLEMENTED
                                Dim value As TimeSpan
                                If Not TimeSpan.TryParse(reader.ReadString(), value) Then
                                    currentPlayPositionMs = 0
                                Else
                                    currentPlayPositionMs = CInt(value.Ticks \ TimeSpan.TicksPerMillisecond)
                                End If
                            Else
                                'Track
                                'TrackDuration
                                'TrackMetaData
                            End If
                        End If
                    Loop
                End Using
                Return True
            Catch ex As Exception
                LogError(ex, "GetPositionInfo", xml)
            End Try
            Return False
        End Function

        Private Sub InitialiseVolume()
            Dim xml As String = Nothing
            Try
                xml = PostSoapRequest(renderingControlUrl, "GetMute", "urn:schemas-upnp-org:service:RenderingControl:1", New String() {"InstanceID", "Channel"}, New String() {"0", "Master"})
                Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
                    Do While reader.Read()
                        If reader.NodeType = XmlNodeType.Element Then
                            If String.Compare(reader.Name, "CurrentMute", StringComparison.OrdinalIgnoreCase) = 0 Then
                                currentMute = (reader.ReadString() = "1")
                                mbApiInterface.Player_SetMute(currentMute)
                                Exit Do
                            End If
                        End If
                    Loop
                End Using
            Catch ex As Exception
                LogError(ex, "SetMusicBeeMute", xml)
            End Try
            Try
                xml = PostSoapRequest(renderingControlUrl, "GetVolume", "urn:schemas-upnp-org:service:RenderingControl:1", New String() {"InstanceID", "Channel"}, New String() {"0", "Master"})
                Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
                    Do While reader.Read()
                        If reader.NodeType = XmlNodeType.Element Then
                            Dim value As UInteger
                            If String.Compare(reader.Name, "CurrentVolume", StringComparison.OrdinalIgnoreCase) = 0 Then
                                Dim volumeText As String = reader.ReadString()
                                If Not UInteger.TryParse(volumeText, value) Then
                                    LogInformation("SetMusicBeeVolume", "invalid volume=" & volumeText)
                                Else
                                    currentVolume = value
                                    mbApiInterface.Player_SetVolume(CSng(currentVolume / deviceMaxVolume))
                                End If
                                Exit Do
                            End If
                        End If
                    Loop
                End Using
            Catch ex As Exception
                LogError(ex, "SetMusicBeeVolume", xml)
            End Try
        End Sub

        Public Sub SetVolume(value As Single)
            Try
                Dim newVolume As UInteger = CUInt(value * deviceMaxVolume)
                If newVolume <> currentVolume Then
                    Dim statusCode As String = PostSoapRequest(renderingControlUrl, "SetVolume", "urn:schemas-upnp-org:service:RenderingControl:1", New String() {"InstanceID", "Channel", "DesiredVolume"}, New String() {"0", "Master", newVolume.ToString()}, True)
                    If statusCode = "200" Then
                        currentVolume = newVolume
                    Else
                        LogInformation("SetVolume", "status=" & statusCode)
                    End If
                End If
            Catch ex As Exception
                LogError(ex, "SetVolume")
            End Try
        End Sub

        Public Sub SetMute(value As Boolean)
            If value <> currentMute Then
                Try
                    Dim statusCode As String = PostSoapRequest(renderingControlUrl, "SetMute", "urn:schemas-upnp-org:service:RenderingControl:1", New String() {"InstanceID", "Channel", "DesiredMute"}, New String() {"0", "Master", If(Not value, "0", "1")}, True)
                    If statusCode = "200" Then
                        currentMute = value
                    Else
                        LogInformation("SetMute", "status=" & statusCode)
                    End If
                Catch ex As Exception
                    LogError(ex, "SetMute")
                End Try
            End If
        End Sub

        ''Private Sub GetMediaInfo()
        ''    ' needed for playlists - only updates when the entire playlist is done
        ''    Try
        ''        Dim xml As String = PostSoapRequest(avTransportControlUrl, "GetMediaInfo", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID"}, New String() {"0"})
        ''        Using reader As New XmlTextReader(xml, XmlNodeType.Document, Nothing)
        ''            If reader.NodeType = XmlNodeType.Element Then
        ''                'If String.Compare(reader.Name, "NrTracks", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                'ElseIf String.Compare(reader.Name, "MediaDuration", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                'ElseIf String.Compare(reader.Name, "CurrentURI", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                'ElseIf String.Compare(reader.Name, "CurrentURIMetaData", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                'ElseIf String.Compare(reader.Name, "NextURIMetaData", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                If String.Compare(reader.Name, "NextURI", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                    nextPlayUrl = reader.ReadString()
        ''                    If String.Compare(nextPlayUrl, "NOT_IMPLEMENTED", StringComparison.OrdinalIgnoreCase) = 0 Then
        ''                        nextPlayUrl = ""
        ''                    End If
        ''                End If
        ''            End If
        ''        End Using
        ''    Catch ex As Exception
        ''        LogError(ex, "GetMediaInfo")
        ''    End Try
        ''End Sub

        Public Function GetContinuousStreamingSampleRate() As Integer
            Return If(streamingProfile Is Nothing, 44100, If(streamingProfile.TranscodeSampleRate = -1, If(44100 < streamingProfile.MinimumSampleRate, streamingProfile.MinimumSampleRate, 44100), streamingProfile.TranscodeSampleRate))
        End Function

        Public Function GetContinuousStreamingBitDepth() As Integer
            Return If(streamingProfile Is Nothing, 16, streamingProfile.TranscodeBitDepth)
        End Function

        Public Function PlayToDevice(url As String, streamHandle As Integer) As Boolean
            Try
                avTransportStatusTimer.Change(Timeout.Infinite, Timeout.Infinite)
                Dim sourceUrl As String
                If String.IsNullOrEmpty(url) Then
                    sourceUrl = "stream"
                    currentTrackDurationTicks = Long.MaxValue
                Else
                    sourceUrl = url
                    Long.TryParse(mbApiInterface.Library_GetFileProperty(url, DirectCast(-FilePropertyType.Duration, FilePropertyType)), currentTrackDurationTicks)
                End If
                Dim metadata As String
                Try
                    Using bufferStream As New IO.MemoryStream, writer As New XmlTextWriter(bufferStream, New UTF8Encoding(False))
                        url = directory.WriteAudioFileDIDL(writer, PrimaryHostUrl, url, streamHandle)
                        writer.Flush()
                        metadata = Encoding.UTF8.GetString(bufferStream.GetBuffer(), 0, CInt(bufferStream.Length))
                    End Using
                Finally
                    LogInformation("Play", sourceUrl & " (" & url & ")")
                End Try
                currentPlayStartTimeEstimated = True
                playToMode = True
                Dim statusCode As String = PostSoapRequest(avTransportControlUrl, "SetAVTransportURI", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID", "CurrentURI", "CurrentURIMetaData"}, New String() {"0", url, metadata}, True)
                If statusCode <> "200" Then
                    LogInformation("Play:SetAVTransportURI", "status=" & statusCode & ",url=" & url)
                    Return False
                End If
                statusCode = PostSoapRequest(avTransportControlUrl, "Play", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID", "Speed"}, New String() {"0", "1"}, True)
                If statusCode = "200" Then
                    SyncLock resourceLock
                        If currentPlayStartTimeEstimated Then
                            currentPlayStartTicks = DateTime.UtcNow.Ticks
                        End If
                    End SyncLock
                    avTransportStatusTimer.Change(100, statusTimerInterval)
                    Return True
                Else
                    LogInformation("Play", "status=" & statusCode & ",url=" & url)
                End If
            Catch ex As Exception
                LogError(ex, "Play")
            End Try
            Return False
        End Function

        Public Function QueueNext(url As String) As Boolean
            If supportSetNextInQueue Then
                ''Try
                ''    Dim metadata As String
                ''    Using bufferStream As New IO.MemoryStream, _
                ''          writer As New XmlTextWriter(bufferStream, New UTF8Encoding(False))
                ''        url = directory.WriteAudioFileDIDL(writer, PrimaryHostUrl, url, 0)
                ''        writer.Flush()
                ''        metadata = Encoding.UTF8.GetString(bufferStream.GetBuffer(), 0, CInt(bufferStream.Length))
                ''    End Using
                ''    Dim statusCode As String = PostSoapRequest(avTransportControlUrl, "SetNextAVTransportURI", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID", "NextURI", "NextURIMetaData"}, New String() {"0", url, metadata}, True)
                ''    If statusCode <> "200" Then
                ''        If queueNextFailedCount > 3 Then
                ''            supportSetNextInQueue = False
                ''        End If
                ''        queueNextFailedCount += 1
                ''    Else
                ''        queueNextFailedCount = Integer.MinValue
                ''        ''GetMediaInfo()
                ''        If String.IsNullOrEmpty(nextPlayUrl) Then
                ''            supportSetNextInQueue = False
                ''        Else
                ''            queueNextTrackPending = True
                ''            Return True
                ''        End If
                ''    End If
                ''Catch ex As Exception
                ''    LogError(ex, "QueueNext")
                ''End Try
            End If
            Return False
        End Function

        Public Sub PausePlayback()
            If currentPlayState = PlayState.Playing Then
                Try
                    avTransportStatusTimer.Change(Timeout.Infinite, Timeout.Infinite)
                    currentPlayPositionMs = CInt((DateTime.UtcNow.Ticks - currentPlayStartTicks) \ TimeSpan.TicksPerMillisecond)
                    Dim statusCode As String = PostSoapRequest(avTransportControlUrl, "Pause", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID"}, New String() {"0"}, True)
                    If statusCode <> "200" Then
                        LogInformation("Pause", "status=" & statusCode)
                    End If
                    GetPlayStateInformation()
                Catch ex As Exception
                    LogError(ex, "Pause")
                End Try
            End If
        End Sub

        Public Sub ResumePlayback()
            If currentPlayState = PlayState.Paused Then
                Try
                    Dim statusCode As String = PostSoapRequest(avTransportControlUrl, "Play", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID", "Speed"}, New String() {"0", "1"}, True)
                    currentPlayStartTicks = DateTime.UtcNow.Ticks - currentPlayPositionMs * TimeSpan.TicksPerMillisecond
                    If statusCode <> "200" Then
                        LogInformation("Resume", "status=" & statusCode)
                    End If
                    GetPlayStateInformation()
                    avTransportStatusTimer.Change(0, statusTimerInterval)
                Catch ex As Exception
                    LogError(ex, "Resume")
                End Try
            End If
        End Sub

        Public Sub StopPlayback(Optional raiseNotification As Boolean = False)
            avTransportStatusTimer.Change(Timeout.Infinite, Timeout.Infinite)
            playToMode = False
            If currentPlayState <> PlayState.Stopped Then
                Try
                    lastUserInitiatedStop = DateTime.UtcNow.Ticks
                    queueNextTrackPending = False
                    PostSoapRequest(avTransportControlUrl, "Stop", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID"}, New String() {"0"}, True, True)
                    currentPlayPositionMs = 0
                    If raiseNotification Then
                        currentPlayState = Plugin.PlayState.Stopped
                        SyncNewPlayState()
                    End If
                Catch ex As Exception
                    If isActive Then
                        LogError(ex, "Stop")
                    End If
                End Try
            End If
        End Sub

        Public Function Seek(positionMs As Integer) As Boolean
            Try
                Dim value As New TimeSpan(positionMs * TimeSpan.TicksPerMillisecond)
                LogInformation("Seek", "goto=" & positionMs.ToString())
                Dim statusCode As String = PostSoapRequest(avTransportControlUrl, "Seek", "urn:schemas-upnp-org:service:AVTransport:1", New String() {"InstanceID", "Unit", "Target"}, New String() {"0", "REL_TIME", value.ToString("h':'mm':'ss")}, True)
                If statusCode = "200" Then
                    SyncLock resourceLock
                        If GetPlayPositionInformation() Then
                            currentPlayStartTicks = DateTime.UtcNow.Ticks - (currentPlayPositionMs * TimeSpan.TicksPerMillisecond)
                        Else
                            currentPlayStartTicks = DateTime.UtcNow.Ticks - value.Ticks
                        End If
                    End SyncLock
                    LogInformation("Seek", "pos=" & currentPlayPositionMs.ToString())
                    Return True
                Else
                    LogInformation("Seek", "status=" & statusCode)
                End If
            Catch
            End Try
            Return False
        End Function

        Private Sub OnAvTransportStatusCheck(parameters As Object)
            Try
                Dim syncPlayState As Boolean = False
                SyncLock resourceLock
                    Dim oldPlayState As PlayState = currentPlayState
                    Dim ex As Exception = GetPlayStateInformation()
                    If ex Is Nothing Then
                        currentErrorCount = 0
                    ElseIf TypeOf ex Is SocketException AndAlso DirectCast(ex, SocketException).SocketErrorCode = SocketError.ConnectionRefused Then
                        currentErrorCount += 1
                        If currentErrorCount = 3 Then
                            LogInformation("StateTimer", "3 connection refusals")
                            statusTimerInterval = 750
                            avTransportStatusTimer.Change(0, statusTimerInterval)
                        ElseIf currentErrorCount = 6 Then
                            LogInformation("StateTimer", "6 connection refusals")
                            statusTimerInterval = 1000
                            avTransportStatusTimer.Change(0, statusTimerInterval)
                        ElseIf currentErrorCount >= 10 Then
                            currentPlayState = PlayState.Stopped
                            LogError(ex, "StateTimer", "10 connection refusals")
                        End If
                    Else
                        LogError(ex, "StateTimer")
                    End If
                    If currentPlayState <> oldPlayState Then
                        LogInformation("StateTimer", currentPlayState.ToString & ",old=" & oldPlayState.ToString())
                        syncPlayState = ProcessNewPlayState(oldPlayState)
                    End If
                End SyncLock
                If syncPlayState Then
                    SyncNewPlayState()
                End If
                If currentPlayStartTimeEstimated OrElse queueNextTrackPending Then
                    GetPlayPositionInformation()
                    'If queueNextTrackPending AndAlso String.Compare(currentPlayUrl, nextPlayUrl, StringComparison.OrdinalIgnoreCase) = 0 AndAlso currentPlayState = PlayState.Playing Then
                    'End If
                End If
            Catch
            End Try
        End Sub

        ''Private Sub OnAvTransportResubscribe(parameters As Object)
        ''    Try
        ''        Dim values() As String = SubscribeSoapRequest(avTransportEventUrl, Nothing, avTransportEventSid)
        ''        avTransportEventSid = values(0)
        ''        If Integer.TryParse(values(1), avTransportEventTimeout) Then
        ''            avTransportEventTimeout *= 1000
        ''        Else
        ''            avTransportEventTimeout = Timeout.Infinite
        ''        End If
        ''    Catch
        ''    End Try
        ''End Sub

        ''Private Sub OnRenderingControlResubscribe(parameters As Object)
        ''    Try
        ''        Dim values() As String = SubscribeSoapRequest(renderingControlEventUrl, Nothing, renderingControlEventSid)
        ''        renderingControlEventSid = values(0)
        ''        If Integer.TryParse(values(1), renderingControlEventTimeout) Then
        ''            renderingControlEventTimeout *= 1000
        ''        Else
        ''            renderingControlEventTimeout = Timeout.Infinite
        ''        End If
        ''    Catch ex As Exception
        ''        LogError(ex, "RenderingControlResubscribe")
        ''    End Try
        ''End Sub

        ''Private Function SubscribeSoapRequest(url As Uri, callback As String, sid As String) As String()
        ''    Dim timeout As String = Nothing
        ''    Dim header As New StringBuilder(1024)
        ''    header.Append("SUBSCRIBE ")
        ''    header.Append(url.PathAndQuery)
        ''    header.Append(" HTTP/1.1")
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append("Host: ")
        ''    header.Append(url.Authority)
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append(userAgent)
        ''    If callback IsNot Nothing Then
        ''        header.Append("Callback: <")
        ''        header.Append(callback)
        ''        header.Append(">"c)
        ''        header.Append(ControlChars.CrLf)
        ''        header.Append("Nt: upnp:event")
        ''        header.Append(ControlChars.CrLf)
        ''    Else
        ''        ' may need to remove the  uuid:xxxxxxxxx from the sid for some renderers
        ''        header.Append("SID: ")
        ''        header.Append(sid)
        ''        header.Append(ControlChars.CrLf)
        ''    End If
        ''    header.Append("Timeout: Second-1800")
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append("Content-Length: 0")
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append(ControlChars.CrLf)
        ''    Dim headerData() As Byte = Encoding.ASCII.GetBytes(header.ToString())
        ''    Using socket As New TcpClient(url.DnsSafeHost, url.Port), _
        ''          socketStream As NetworkStream = socket.GetStream()
        ''        socketStream.Write(headerData, 0, headerData.Length)
        ''        socketStream.Flush()
        ''        Using bufferStream As New IO.MemoryStream
        ''            Dim line As String
        ''            Do
        ''                Dim b As Byte = CByte(socketStream.ReadByte())
        ''                If b = 13 Then
        ''                    ' ignore
        ''                ElseIf b = 10 Then
        ''                    If bufferStream.Position = 0 Then
        ''                        Exit Do
        ''                    End If
        ''                    line = Encoding.ASCII.GetString(bufferStream.GetBuffer(), 0, CInt(bufferStream.Position))
        ''                    If line.StartsWith("SID:", StringComparison.OrdinalIgnoreCase) Then
        ''                        sid = line.Substring(4).Trim()
        ''                    ElseIf line.StartsWith("TIMEOUT:", StringComparison.OrdinalIgnoreCase) Then
        ''                        timeout = line.Substring(8).Trim()
        ''                        If timeout.StartsWith("Second-", StringComparison.OrdinalIgnoreCase) Then
        ''                            timeout = timeout.Substring(7)
        ''                        End If
        ''                    End If
        ''                    bufferStream.Position = 0
        ''                Else
        ''                    bufferStream.WriteByte(b)
        ''                End If
        ''            Loop
        ''        End Using
        ''        socketStream.Close()
        ''        socket.Close()
        ''    End Using
        ''    Return New String() {sid, timeout}
        ''End Function

        ''Private Sub UnsubscribeSoapRequest(url As Uri, sid As String)
        ''    Dim timeout As String = Nothing
        ''    Dim header As New StringBuilder(1024)
        ''    header.Append("UNSUBSCRIBE ")
        ''    header.Append(url.PathAndQuery)
        ''    header.Append(" HTTP/1.1")
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append("Host: ")
        ''    header.Append(url.Authority)
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append(userAgent)
        ''    header.Append("SID: ")
        ''    header.Append(sid)
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append("Content-Length: 0")
        ''    header.Append(ControlChars.CrLf)
        ''    header.Append(ControlChars.CrLf)
        ''    Dim headerData() As Byte = Encoding.ASCII.GetBytes(header.ToString())
        ''    Using socket As New TcpClient(url.DnsSafeHost, url.Port), _
        ''          socketStream As NetworkStream = socket.GetStream()
        ''        socketStream.Write(headerData, 0, headerData.Length)
        ''        socketStream.Flush()
        ''        Dim charCount As Integer = 0
        ''        Do
        ''            Dim b As Byte = CByte(socketStream.ReadByte())
        ''            If b = 13 Then
        ''                ' ignore
        ''            ElseIf b = 10 Then
        ''                If charCount = 0 Then
        ''                    Exit Do
        ''                End If
        ''                charCount = 0
        ''            Else
        ''                charCount += 1
        ''            End If
        ''        Loop
        ''        socketStream.Close()
        ''        socket.Close()
        ''    End Using
        ''End Sub

        Private Function PostSoapRequest(url As Uri, action As String, service As String, Optional parameters() As String = Nothing, Optional arguments() As String = Nothing, Optional returnStatusCode As Boolean = False, Optional ignoreError As Boolean = False) As String
            Using bufferStream As New IO.MemoryStream, soapWriter As New XmlTextWriter(bufferStream, New UTF8Encoding(False))
                soapWriter.WriteRaw("<?xml version=""1.0"" encoding=""UTF-8""?>")
                soapWriter.WriteStartElement("s", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/")
                soapWriter.WriteAttributeString("s", "encodingStyle", Nothing, "http://schemas.xmlsoap.org/soap/encoding/")
                soapWriter.WriteStartElement("s", "Body", Nothing)
                soapWriter.WriteStartElement("u", action, service)
                If arguments IsNot Nothing Then
                    For index As Integer = 0 To arguments.Length - 1
                        soapWriter.WriteElementString(parameters(index), arguments(index))
                    Next index
                End If
                soapWriter.WriteEndElement()
                soapWriter.WriteEndElement()
                soapWriter.WriteEndElement()
                soapWriter.Flush()
                Dim header As New StringBuilder(1024)
                Dim headerData() As Byte
                Dim isMPost As Boolean = False
                Dim createHeader As MethodInvoker =
                    Sub()
                        header.Append(If(Not isMPost, "POST ", "M-POST "))
                        header.Append(url.PathAndQuery)
                        header.Append(" HTTP/1.1")
                        header.Append(ControlChars.CrLf)
                        header.Append("Host: ")
                        header.Append(url.Authority)
                        header.Append(ControlChars.CrLf)
                        header.Append(userAgent)
                        header.Append("Content-Type: text/xml; charset=""utf-8""")
                        header.Append(ControlChars.CrLf)
                        If Not isMPost Then
                            header.Append("SOAPAction: """)
                        Else
                            header.Append("MAN: ""http://schemas.xmlsoap.org/soap/envelope/""; ns=01")
                            header.Append("01-SOAPAction: """)
                        End If
                        header.Append(service)
                        header.Append("#"c)
                        header.Append(action)
                        header.Append(""""c)
                        header.Append(ControlChars.CrLf)
                        header.Append("Content-Length: ")
                        header.Append(bufferStream.Length.ToString())
                        header.Append(ControlChars.CrLf)
                        header.Append(ControlChars.CrLf)
                        headerData = Encoding.ASCII.GetBytes(header.ToString())
                    End Sub
                createHeader()
                Dim statusCode As Integer = 0
                Dim sendData() As Byte = Nothing
                Dim buffer() As Byte
                Using socket As New TcpClient(url.DnsSafeHost, url.Port),
                      socketStream As NetworkStream = socket.GetStream()
                    For attempt As Integer = 1 To 2
                        socketStream.Write(headerData, 0, headerData.Length)
                        bufferStream.Position = 0
                        bufferStream.CopyTo(socketStream)
                        If Settings.LogDebugInfo Then
                            sendData = bufferStream.ToArray()
                        End If
                        socketStream.Flush()
                        Dim contentLength As Integer = 0
                        bufferStream.Position = 0
                        Dim firstLine As Boolean = True
                        Dim isChunked As Boolean = False
                        Do
                            Dim b As Integer = socketStream.ReadByte()
                            '' changed to included end of stream error
                            If b = -1 Then
                                Throw New HttpException(400, "Unexpected end of stream")
                            ElseIf b = 13 Then
                                ' ignore
                            ElseIf b = 10 Then
                                If bufferStream.Position = 0 Then
                                    Exit Do
                                End If
                                Dim line As String = Encoding.ASCII.GetString(bufferStream.GetBuffer(), 0, CInt(bufferStream.Position))
                                If firstLine Then
                                    If line.Length > 12 Then
                                        Integer.TryParse(line.Substring(9, 3), statusCode)
                                    End If
                                    firstLine = False
                                ElseIf line.StartsWith("content-length:", StringComparison.OrdinalIgnoreCase) Then
                                    Integer.TryParse(line.Substring(15), contentLength)
                                ElseIf line.StartsWith("transfer-encoding:", StringComparison.OrdinalIgnoreCase) AndAlso line.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) <> -1 Then
                                    isChunked = True
                                End If
                                bufferStream.Position = 0
                            Else
                                bufferStream.WriteByte(CByte(b))
                            End If
                        Loop
                        If isChunked Then
                            buffer = ReadChuckedStream(socketStream)
                        Else
                            buffer = New Byte(contentLength - 1) {}
                            Dim offset As Integer = 0
                            Do While offset < buffer.Length
                                Dim count As Integer = socketStream.Read(buffer, offset, buffer.Length - offset)
                                '' changed to included end of stream error
                                If count <= 0 Then
                                    Throw New HttpException(400, "Unexpected end of stream")
                                End If
                                offset += count
                            Loop
                        End If
                        If statusCode <> 200 AndAlso Not ignoreError Then
                            LogInformation("PostSoapRequest", statusCode & ",send=" & Encoding.UTF8.GetString(headerData) & Encoding.UTF8.GetString(sendData))
                        End If
                        If statusCode <> 405 Then
                            Exit For
                        Else
                            isMPost = True
                            header.Length = 0
                            createHeader()
                        End If
                    Next attempt
                    socketStream.Close()
                    socket.Close()
                    If statusCode <> 200 AndAlso Not ignoreError Then
                        Dim functionParameters As String = ""
                        If arguments IsNot Nothing Then
                            For index As Integer = 0 To arguments.Length - 1
                                If index > 0 Then functionParameters &= ","
                                functionParameters &= parameters(index) & "=" & arguments(index)
                            Next index
                        End If
                        LogInformation("SoapRequest" & ":" & action & ":" & service & ":" & functionParameters, Encoding.UTF8.GetString(buffer))
                    End If
                    If returnStatusCode Then
                        Return statusCode.ToString()
                    Else
                        Return Encoding.UTF8.GetString(buffer)
                    End If
                End Using
            End Using
        End Function

        Private Function GetXmlDocument(url As Uri) As String
            Dim header As New StringBuilder(1024)
            header.Append("GET ")
            header.Append(url.PathAndQuery)
            header.Append(" HTTP/1.1")
            header.Append(ControlChars.CrLf)
            header.Append("Host: ")
            header.Append(url.Authority)
            header.Append(ControlChars.CrLf)
            header.Append(userAgent)
            header.Append("Accept: text/xml")
            header.Append(ControlChars.CrLf)
            header.Append("Content-Length: 0")
            header.Append(ControlChars.CrLf)
            header.Append(ControlChars.CrLf)
            Dim headerData() As Byte = Encoding.ASCII.GetBytes(header.ToString())
            Using socket As New TcpClient(url.DnsSafeHost, url.Port), socketStream As NetworkStream = socket.GetStream()
                socketStream.Write(headerData, 0, headerData.Length)
                socketStream.Flush()
                Dim contentLength As Integer = 0
                Dim line As String
                Dim isChunked As Boolean = False
                Using bufferStream As New IO.MemoryStream
                    Do
                        Dim b As Integer = socketStream.ReadByte()
                        '' changed to included end of stream error
                        If b = -1 Then
                            Exit Do
                        ElseIf b = 13 Then
                            ' ignore
                        ElseIf b = 10 Then
                            If bufferStream.Position = 0 Then
                                Exit Do
                            End If
                            line = Encoding.ASCII.GetString(bufferStream.GetBuffer(), 0, CInt(bufferStream.Position))
                            If line.StartsWith("server:", StringComparison.OrdinalIgnoreCase) Then
                                lastServerHeader = line.Substring(7).Trim()
                            ElseIf line.StartsWith("content-length:", StringComparison.OrdinalIgnoreCase) Then
                                Integer.TryParse(line.Substring(15), contentLength)
                            ElseIf line.StartsWith("transfer-encoding:", StringComparison.OrdinalIgnoreCase) AndAlso line.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) <> -1 Then
                                isChunked = True
                            End If
                            bufferStream.Position = 0
                        Else
                            bufferStream.WriteByte(CByte(b))
                        End If
                    Loop
                End Using
                Dim buffer() As Byte
                If isChunked Then
                    buffer = ReadChuckedStream(socketStream)
                Else
                    buffer = New Byte(contentLength - 1) {}
                    Dim offset As Integer = 0
                    Do While offset < buffer.Length
                        Dim count As Integer = socketStream.Read(buffer, offset, buffer.Length - offset)
                        '' changed to included end of stream error
                        If count <= 0 Then
                            Throw New HttpException(400, "Unexpected end of stream")
                        End If
                        offset += count
                    Loop
                End If
                socketStream.Close()
                socket.Close()
                Return Encoding.UTF8.GetString(buffer)
            End Using
        End Function

        Private Function ReadChuckedStream(socketStream As NetworkStream) As Byte()
            Using bufferStream As New IO.MemoryStream(4096)
                Dim buffer() As Byte = Nothing
                Dim b As Integer
                Dim chunckHeader As New StringBuilder(6)
                Do
                    Dim chunckSize As Integer = 0
                    chunckHeader.Length = 0
                    Dim ignoreRemainder As Boolean = False
                    Const semicolon As Integer = AscW(";"c)
                    Do
                        b = socketStream.ReadByte()
                        '' changed to included -1 end of stream error
                        If b = -1 Then
                            Throw New HttpException(400, "Unexpected end of stream")
                        ElseIf b = 10 Then
                            If Not Integer.TryParse(chunckHeader.ToString(), Globalization.NumberStyles.HexNumber, Nothing, chunckSize) Then
                                Throw New HttpException(400, "Invalid chunk size")
                            End If
                            Exit Do
                        ElseIf ignoreRemainder Then
                            ' ignore
                        ElseIf b = semicolon Then
                            ignoreRemainder = True
                        ElseIf b <> 13 Then
                            chunckHeader.Append(ChrW(b))
                        End If
                    Loop
                    If chunckSize = 0 Then
                        Exit Do
                    End If
                    If buffer Is Nothing OrElse chunckSize > buffer.Length Then
                        buffer = New Byte(chunckSize - 1) {}
                    End If
                    Dim remainingCount As Integer = chunckSize
                    Do While remainingCount > 0
                        Dim readCount As Integer = socketStream.Read(buffer, 0, remainingCount)
                        If readCount <= 0 Then
                            Throw New HttpException(400, "Unexpected end of stream")
                        End If
                        remainingCount -= readCount
                        bufferStream.Write(buffer, 0, readCount)
                    Loop
                    Do
                        b = socketStream.ReadByte()
                        '' changed to included -1 end of stream error
                        If b = -1 Then
                            Exit Do
                        End If
                    Loop Until b = 10
                Loop Until b = -1
                Return bufferStream.ToArray()
            End Using
        End Function

        Private Function GetCleanedXml(xml As String) As String
            Dim rootStartPos As Integer = xml.IndexOf("<root", StringComparison.OrdinalIgnoreCase)
            If rootStartPos = -1 Then
                Throw New IO.InvalidDataException
            End If
            Dim startPos As Integer = rootStartPos + 1
            Dim rootEndPos As Integer = xml.IndexOf(">"c, startPos)
            Dim idEndPos As Integer
            Dim existingPrefixes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Do
                Dim prefixStartPos As Integer = xml.IndexOf(" xmlns:", startPos, rootEndPos - startPos, StringComparison.OrdinalIgnoreCase)
                If prefixStartPos = -1 Then
                    Exit Do
                End If
                prefixStartPos += 7
                Dim prefixEndPos As Integer = xml.IndexOf("="c, prefixStartPos, rootEndPos - prefixStartPos)
                If prefixEndPos = -1 Then
                    Exit Do
                End If
                existingPrefixes.Add(xml.Substring(prefixStartPos, prefixEndPos - prefixStartPos))
                startPos = prefixEndPos + 1
            Loop
            startPos = rootEndPos + 1
            Dim endPos As Integer
            Dim missingPrefixes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Do While startPos < xml.Length
                startPos = xml.IndexOf("<"c, startPos)
                If startPos = -1 Then Exit Do
                endPos = xml.IndexOf(">"c, startPos + 1)
                If endPos = -1 Then Exit Do
                If xml.Chars(startPos + 1) <> "/"c Then
                    startPos += 1
                    idEndPos = xml.IndexOfAny(New Char() {":"c, " "c}, startPos, endPos - startPos)
                    If idEndPos <> -1 AndAlso xml.Chars(idEndPos) = ":"c Then
                        If xml.IndexOf(" xmlns:", startPos, endPos - startPos, StringComparison.OrdinalIgnoreCase) = -1 Then
                            Dim id As String = xml.Substring(startPos, idEndPos - startPos)
                            If Not existingPrefixes.Contains(id) Then
                                missingPrefixes.Add(id)
                            End If
                        End If
                    End If
                End If
                startPos = endPos + 1
            Loop
            If missingPrefixes.Count > 0 Then
                Dim rootElement As String = xml.Substring(rootStartPos, rootEndPos - rootStartPos)
                For Each prefix As String In missingPrefixes
                    rootElement &= " xmlns:" & prefix & "=""http://" & prefix & ".com/"""
                Next prefix
                rootElement &= ">"
                xml = xml.Substring(0, rootStartPos) & rootElement & xml.Substring(rootEndPos + 1)
            End If
            startPos = rootStartPos
            ' remove invalid "&" in xml
            Do
                startPos = xml.IndexOf("&"c, startPos)
                If startPos = -1 Then Exit Do
                '' changed startPos to startPos+1
                endPos = xml.IndexOfAny(New Char() {";"c, "<"c, ">"c, "&"c, " "c}, startPos + 1)
                If endPos <> -1 AndAlso xml.Chars(endPos) = ";"c Then
                    startPos = endPos
                Else
                    xml = xml.Remove(startPos, 1)
                End If
            Loop
            Return xml
        End Function
    End Class  ' MediaRendererDevice
End Class