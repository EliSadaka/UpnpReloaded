Imports System.Xml

Partial Public Class Plugin
    <UpnpServiceVariable("SourceProtocolInfo", "string", True)> _
    <UpnpServiceVariable("SinkProtocolInfo", "string", True)> _
    <UpnpServiceVariable("CurrentConnectionIDs", "string", True)> _
    <UpnpServiceVariable("A_ARG_TYPE_ConnectionStatus", "string", False, "OK", "ContentFormatMismatch", "InsufficientBandwidth", "UnreliableChannel", "Unknown")> _
    <UpnpServiceVariable("A_ARG_TYPE_ConnectionManager", "string", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_Direction", "string", False, "Input", "Output")> _
    <UpnpServiceVariable("A_ARG_TYPE_ProtocolInfo", "string", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_ConnectionID", "i4", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_AVTransportID", "i4", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_RcsID", "i4", False)> _
    Private NotInheritable Class ConnectionManagerService
        Inherits UpnpService
        Private Const sourceProtocolInfo As String = "http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_TN,http-get:*:image/jpeg:DLNA.ORG_PN=JPEG_SM,http-get:*:audio/L16:DLNA.ORG_PN=LPCM,http-get:*:audio/L24:DLNA.ORG_PN=LPCM,http-get:*:audio/x-ms-wma:DLNA.ORG_PN=WMABASE,http-get:*:audio/mpeg:DLNA.ORG_PN=MP3,http-get:*:audio/mp3:DLNA.ORG_PN=MP3,http-get:*:audio/x-mp3:DLNA.ORG_PN=MP3,http-get:*:audio/m4a:DLNA.ORG_PN=AAC_ISO,http-get:*:audio/aac:DLNA.ORG_PN=AAC_ISO,http-get:*:audio/x-aac:DLNA.ORG_PN=AAC_ISO,http-get:*:audio/wav:*,http-get:*:audio/x-wav:*,http-get:*:audio/x-flac:*,http-get:*:audio/flac:*,http-get:*:audio/x-ogg:*,http-get:*:audio/ogg:*,http-get:*:audio/x-wavpack:*,http-get:*:audio/musepack:*,http-get:*:audio/x-musepack:*"

        Public Sub New(server As UpnpServer)
            MyBase.New(server, "urn:schemas-upnp-org:service:ConnectionManager:1", "urn:upnp-org:serviceId:ConnectionManager", "/ConnectionManager.control", "/ConnectionManager.event", "/ConnectionManager.xml")
        End Sub

        Protected Overrides Sub WriteEventProperty(writer As XmlWriter)
            writer.WriteStartElement("e", "property", Nothing)
            writer.WriteElementString("SourceProtocolInfo", sourceProtocolInfo)
            writer.WriteEndElement()
            writer.WriteStartElement("e", "property", Nothing)
            writer.WriteElementString("SinkProtocolInfo", "")
            writer.WriteEndElement()
            writer.WriteStartElement("e", "property", Nothing)
            writer.WriteElementString("CurrentConnectionIDs", "0")
            writer.WriteEndElement()
        End Sub

        <UpnpServiceArgument(0, "Source", "SourceProtocolInfo")> _
        <UpnpServiceArgument(1, "Sink", "SinkProtocolInfo")> _
        Private Sub GetProtocolInfo(request As HttpRequest)
            request.Response.SendSoapHeadersBody(request, sourceProtocolInfo, "")
        End Sub

        <UpnpServiceArgument(0, "ConnectionIDs", "CurrentConnectionIDs")> _
        Private Sub GetCurrentConnectionIDs(request As HttpRequest)
            request.Response.SendSoapHeadersBody(request, "0")
        End Sub

        <UpnpServiceArgument(0, "RcsID", "A_ARG_TYPE_RcsID")> _
        <UpnpServiceArgument(1, "AVTransportID", "A_ARG_TYPE_AVTransportID")> _
        <UpnpServiceArgument(2, "ProtocolInfo", "A_ARG_TYPE_ProtocolInfo")> _
        <UpnpServiceArgument(3, "PeerConnectionManager", "A_ARG_TYPE_ConnectionManager")> _
        <UpnpServiceArgument(4, "PeerConnectionID", "A_ARG_TYPE_ConnectionID")> _
        <UpnpServiceArgument(5, "Direction", "A_ARG_TYPE_Direction")> _
        <UpnpServiceArgument(6, "Status", "A_ARG_TYPE_ConnectionStatus")> _
        Private Sub GetCurrentConnectionInfo(request As HttpRequest, <UpnpServiceArgument("A_ARG_TYPE_ConnectionID")> ConnectionID As String)
            If ConnectionID <> "0" Then
                LogInformation("GetCurrentConnectionInfo", "Invalid Connection Id:" & ConnectionID)
                'Throw New SoapException(402, "Invalid Args")
            End If
            request.Response.SendSoapHeadersBody(request, "-1", "-1", "", "", "-1", "Output", "OK")
        End Sub
    End Class  ' ConnectionManagerService
End Class