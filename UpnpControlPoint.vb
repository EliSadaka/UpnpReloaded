Imports System.Text
Imports UPNPLib
Imports System.Xml
Imports System.IO
Imports System.Net
Imports System.Net.Sockets

Partial Public Class Plugin
    Private Class UpnpControlPoint
        Public Shared Sub StartPortForwarding()
            Task.Factory.StartNew(AddressOf ExecuteStartPortForwarding, True)
        End Sub

        Public Shared Sub StopPortForwarding()
            Task.Factory.StartNew(AddressOf ExecuteStartPortForwarding, False)
        End Sub

        Private Shared Sub ExecuteStartPortForwarding(parameters As Object)
            Dim forward As Boolean = DirectCast(parameters, Boolean)
            Dim port As Integer = Settings.ServerPort
            Try
                Dim routerFound As Boolean = False
                Dim finder As New UPnPDeviceFinder
                Dim devicesRoot As UPnPDevices = finder.FindByType("upnp:rootdevice", 0)
                Dim wanDevices As New List(Of UPNPLib.UPnPDevice)
                For Each device As UPNPLib.UPnPDevice In GetAllDevices(devicesRoot.OfType(Of UPNPLib.UPnPDevice)())
                    If String.Compare(device.Type, "urn:schemas-upnp-org:device:WANConnectionDevice:1", StringComparison.OrdinalIgnoreCase) = 0 OrElse String.Compare(device.Type, "urn:schemas-upnp-org:device:WANConnectionDevice:2", StringComparison.OrdinalIgnoreCase) = 0 Then
                        wanDevices.Add(device)
                    End If
                Next device
                Dim service As UPNPLib.UPnPService
                For Each device As UPNPLib.UPnPDevice In wanDevices
                    service = GetService(device, "urn:schemas-upnp-org:service:WANIPConnection:1")
                    If service Is Nothing Then
                        service = GetService(device, "urn:schemas-upnp-org:service:WANPPPConnection:1")
                    End If
                    If service Is Nothing Then
                        service = GetService(device, "urn:schemas-upnp-org:service:WANIPConnection:2")
                    End If
                    If service Is Nothing Then
                        service = GetService(device, "urn:schemas-upnp-org:service:WANPPPConnection:2")
                    End If
                    If service IsNot Nothing Then
                        Try
                            If forward Then
                                Dim documentUrl As New Uri(DirectCast(device, IUPnPDeviceDocumentAccess).GetDocumentURL())
                                Dim client As New TcpClient
                                client.Connect(documentUrl.Host, documentUrl.Port)
                                Dim localEndPoint As String = DirectCast(client.Client.LocalEndPoint, IPEndPoint).Address.ToString()
                                client.Close()
                                Dim inArgs() As Object = New Object() {"", port, "TCP", port, localEndPoint, True, "MusicBee", 0}
                                Dim outArgs As Object = Nothing
                                service.InvokeAction("AddPortMapping", inArgs, outArgs)
                                inArgs = New Object() {"", port, "TCP"}
                                outArgs = Nothing
                                service.InvokeAction("GetSpecificPortMappingEntry", inArgs, outArgs)
                                If DirectCast(DirectCast(outArgs, Object())(1), String) = localEndPoint Then
                                    routerFound = True
                                End If
                            Else
                                Dim inArgs() As Object = New Object() {"", port, "TCP"}
                                Dim outArgs As Object = Nothing
                                service.InvokeAction("DeletePortMapping", inArgs, outArgs)
                            End If
                        Catch
                        End Try
                    End If
                Next device
                If forward AndAlso Not routerFound Then
                    LogInformation("StartPortForwarding", "UPnP router not found")
                End If
            Catch ex As Exception
                LogError(ex, "StartPortForwarding")
            End Try
        End Sub

        Private Shared Function GetService(device As UPNPLib.UPnPDevice, serviceType As String) As UPNPLib.UPnPService
            For Each service As UPNPLib.UPnPService In device.Services
                If String.Compare(service.ServiceTypeIdentifier, serviceType, StringComparison.OrdinalIgnoreCase) = 0 Then
                    Return service
                End If
            Next service
            Return Nothing
        End Function

        Private Shared Function GetAllDevices(rootDevices As IEnumerable(Of UPNPLib.UPnPDevice)) As IEnumerable(Of UPNPLib.UPnPDevice)
            Dim result As IEnumerable(Of UPNPLib.UPnPDevice) = rootDevices
            For Each device As UPNPLib.UPnPDevice In rootDevices
                Dim name As String = device.FriendlyName
                If device.HasChildren Then
                    result = result.Concat(GetAllDevices(device.Children.OfType(Of UPNPLib.UPnPDevice)()))
                End If
            Next device
            Return result
        End Function
    End Class  ' UpnpControlPoint
End Class
