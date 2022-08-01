Imports System.Text
Imports System.Xml

Partial Public Class Plugin
    Private Class UpnpServer
        Public ReadOnly RootDevice As UpnpDevice
        Public ReadOnly HttpServer As HttpServer
        Public ReadOnly SsdpServer As SsdpServer
        Private wmcDescriptionData() As Byte
        Private defaultDescriptionData() As Byte

        Public Sub New(rootDevice As UpnpDevice)
            Me.RootDevice = rootDevice
            SsdpServer = New SsdpServer(Me)
            HttpServer = New HttpServer
            HttpServer.AddRoute("GET", "/description.xml", New HttpRouteDelegate(AddressOf GetDescription))
        End Sub

        Public Sub Start()
            wmcDescriptionData = GetDeviceDescription(True)
            defaultDescriptionData = GetDeviceDescription(False)
            HttpServer.Start()
            SsdpServer.Start()
        End Sub

        Public Sub [Stop]()
            SsdpServer.Stop()
            HttpServer.Stop()
        End Sub

        Public Sub Restart(includeHttpServer As Boolean)
            wmcDescriptionData = GetDeviceDescription(True)
            defaultDescriptionData = GetDeviceDescription(False)
            If includeHttpServer Then
                HttpServer.Stop()
                HttpServer.Start()
            End If
            SsdpServer.Restart()
        End Sub

        Private Function GetDeviceDescription(wmcCompat As Boolean) As Byte()
            Using stream As New IO.MemoryStream, _
                  writer As New XmlTextWriter(stream, New UTF8Encoding(False))
                writer.WriteRaw("<?xml version=""1.0"" encoding=""UTF-8""?>")
                writer.WriteStartElement("root")
                writer.WriteAttributeString("xmlns", "urn:schemas-upnp-org:device-1-0")
                writer.WriteAttributeString("xmlns:dlna", "urn:schemas-dlna-org:device-1-0")
                writer.WriteStartElement("specVersion")
                writer.WriteElementString("major", "1")
                writer.WriteElementString("minor", "0")
                writer.WriteEndElement()
                writer.WriteStartElement("device")
                RootDevice.WriteDescription(writer, wmcCompat)
                writer.WriteEndElement()
                writer.WriteEndElement()
                writer.Flush()
                Return stream.ToArray()
            End Using
        End Function

        Private Sub GetDescription(request As HttpRequest)
            Dim profile As StreamingProfile = Settings.GetStreamingProfile(request.Headers)
            Dim descriptionData() As Byte = If(profile.WmcCompatability, wmcDescriptionData, defaultDescriptionData)
            Dim response As HttpResponse = request.Response
            response.AddHeader(HttpHeader.ContentLength, descriptionData.Length.ToString())
            response.AddHeader(HttpHeader.ContentType, "text/xml; charset=""utf-8""")
            Using stream As New IO.MemoryStream(descriptionData)
                response.SendHeaders()
                stream.CopyTo(response.Stream)
            End Using
        End Sub
    End Class  ' UpnpServer
End Class
