Imports System.Text
Imports System.Xml

Partial Public Class Plugin
    <UpnpServiceVariable("A_ARG_TYPE_BrowseFlag", "string", False, "BrowseMetadata", "BrowseDirectChildren")> _
    <UpnpServiceVariable("ContainerUpdateIDs", "string", True)> _
    <UpnpServiceVariable("SystemUpdateID", "ui4", True)> _
    <UpnpServiceVariable("A_ARG_TYPE_Count", "ui4", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_SortCriteria", "string", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_SearchCriteria", "string", False)> _
    <UpnpServiceVariable("SortCapabilities", "string", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_Index", "ui4", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_ObjectID", "string", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_UpdateID", "ui4", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_Result", "string", False)> _
    <UpnpServiceVariable("SearchCapabilities", "string", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_Filter", "string", False)> _
    <UpnpServiceVariable("A_ARG_TYPE_Featurelist", "string", False)> _
    Private NotInheritable Class ContentDirectoryService
        Inherits UpnpService

        Public Sub New(server As UpnpServer)
            MyBase.New(server, "urn:schemas-upnp-org:service:ContentDirectory:1", "urn:upnp-org:serviceId:ContentDirectory", "/ContentDirectory.control", "/ContentDirectory.event", "/ContentDirectory.xml")
        End Sub

        Protected Overrides Sub WriteEventProperty(writer As XmlWriter)
            writer.WriteStartElement("e", "property", Nothing)
            writer.WriteElementString("SystemUpdateID", "0")
            writer.WriteEndElement()
        End Sub

        <UpnpServiceArgument(0, "SearchCaps", "SearchCapabilities")> _
        Private Sub GetSearchCapabilities(request As HttpRequest)
            'dc:title,dc:creator,upnp:artist,upnp:genre,upnp:album,dc:date,upnp:originalTrackNumber,upnp:class,@id,@refID,@protocolInfo
            '@id,@refID,dc:title,upnp:class,upnp:genre,upnp:artist,upnp:author,upnp:author@role,upnp:album,dc:creator,res@size,res@duration,res@protocolInfo,res@protection,dc:publisher,dc:language,upnp:originalTrackNumber,dc:date,upnp:producer,upnp:rating,upnp:actor,upnp:director,upnp:toc,dc:description,microsoft:userRatingInStars,microsoft:userEffectiveRatingInStars,microsoft:userRating,microsoft:userEffectiveRating,microsoft:serviceProvider,microsoft:artistAlbumArtist,microsoft:artistPerformer,microsoft:artistConductor,microsoft:authorComposer,microsoft:authorOriginalLyricist,microsoft:authorWriter,upnp:userAnnotation,upnp:channelName,upnp:longDescription,upnp:programTitle
            request.Response.SendSoapHeadersBody(request, "")
        End Sub

        <UpnpServiceArgument(0, "SortCaps", "SortCapabilities")> _
        Private Sub GetSortCapabilities(request As HttpRequest)
            'dc:title,upnp:album,dc:creator,upnp:artist,upnp:albumArtist,upnp:genre
            request.Response.SendSoapHeadersBody(request, "")
        End Sub

        <UpnpServiceArgument(0, "Id", "SystemUpdateID")> _
        Private Sub GetSystemUpdateID(request As HttpRequest)
            request.Response.SendSoapHeadersBody(request, "0")
        End Sub

        <UpnpServiceArgument(0, "Result", "A_ARG_TYPE_Result")> _
        <UpnpServiceArgument(1, "NumberReturned", "A_ARG_TYPE_Count")> _
        <UpnpServiceArgument(2, "TotalMatches", "A_ARG_TYPE_Count")> _
        <UpnpServiceArgument(3, "UpdateID", "A_ARG_TYPE_UpdateID")> _
        Private Sub Browse(request As HttpRequest, _
                <UpnpServiceArgument("A_ARG_TYPE_ObjectID")> ObjectID As String, _
                <UpnpServiceArgument("A_ARG_TYPE_BrowseFlag")> BrowseFlag As String, _
                <UpnpServiceArgument("A_ARG_TYPE_Filter")> Filter As String, _
                <UpnpServiceArgument("A_ARG_TYPE_Index")> StartingIndex As String, _
                <UpnpServiceArgument("A_ARG_TYPE_Count")> RequestedCount As String, _
                <UpnpServiceArgument("A_ARG_TYPE_SortCriteria")> SortCriteria As String)
            Dim result As String = Nothing
            Dim numberReturned As String = Nothing
            Dim totalMatches As String = Nothing
            Dim startIndexValue As UInteger
            Dim requestCountValue As UInteger
            Dim browseFlagValue As BrowseFlag
            If Not UInteger.TryParse(StartingIndex, startIndexValue) OrElse Not UInteger.TryParse(RequestedCount, requestCountValue) OrElse Not [Enum].TryParse(BrowseFlag, True, browseFlagValue) Then
                LogInformation("RegisterDevice", "Invalid Browse Args:" & StartingIndex & "," & RequestedCount & "," & BrowseFlag.ToString())
                Throw New SoapException(402, "Invalid Args")
            End If
            Dim directory As ItemManager = ItemManager.GetItemManager(request.Headers)
            directory.Browse(request.Headers, ObjectID, browseFlagValue, Filter, CInt(startIndexValue), If(requestCountValue > Integer.MaxValue, Integer.MaxValue, CInt(requestCountValue)), SortCriteria, result, numberReturned, totalMatches)
            request.Response.SendSoapHeadersBody(request, result, numberReturned, totalMatches, "0")
        End Sub

        <UpnpServiceArgument(0, "Result", "A_ARG_TYPE_Result")> _
        <UpnpServiceArgument(1, "NumberReturned", "A_ARG_TYPE_Count")> _
        <UpnpServiceArgument(2, "TotalMatches", "A_ARG_TYPE_Count")> _
        <UpnpServiceArgument(3, "UpdateID", "A_ARG_TYPE_UpdateID")> _
        Private Sub Search(request As HttpRequest, _
                <UpnpServiceArgument("A_ARG_TYPE_ObjectID")> ContainerID As String, _
                <UpnpServiceArgument("A_ARG_TYPE_SearchCriteria")> SearchCriteria As String, _
                <UpnpServiceArgument("A_ARG_TYPE_Filter")> Filter As String, _
                <UpnpServiceArgument("A_ARG_TYPE_Index")> StartingIndex As String, _
                <UpnpServiceArgument("A_ARG_TYPE_Count")> RequestedCount As String, _
                <UpnpServiceArgument("A_ARG_TYPE_SortCriteria")> SortCriteria As String)
            Dim startIndexValue As UInteger
            Dim requestCountValue As UInteger
            If Not UInteger.TryParse(StartingIndex, startIndexValue) OrElse Not UInteger.TryParse(RequestedCount, requestCountValue) Then
                LogInformation("Search", "Invalid Args: " & StartingIndex & "," & RequestedCount)
                Throw New SoapException(402, "Invalid Args")
            End If
            Dim directory As ItemManager = ItemManager.GetItemManager(request.Headers)
            Dim result As String = Nothing
            Dim numberReturned As String = Nothing
            Dim totalMatches As String = Nothing
            directory.Search(request.Headers, ContainerID, SearchCriteria, Filter, CInt(startIndexValue), If(requestCountValue > Integer.MaxValue, Integer.MaxValue, CInt(requestCountValue)), SortCriteria, result, numberReturned, totalMatches)
            request.Response.SendSoapHeadersBody(request, result, numberReturned, totalMatches, "0")
        End Sub

        <UpnpServiceArgument(0, "FeatureList", "A_ARG_TYPE_Featurelist")> _
        Private Sub X_GetFeatureList(request As HttpRequest)
            Dim text As New StringBuilder(1024)
            Dim xmlSettings As New XmlWriterSettings With {
                .OmitXmlDeclaration = True
            }
            Using writer As XmlWriter = XmlWriter.Create(text, xmlSettings)
                writer.WriteStartElement("Features", "urn:schemas-upnp-org:av:avs")
                writer.WriteAttributeString("xmlns", "xsi", Nothing, "http://www.w3.org/2001/XMLSchema-instance")
                writer.WriteAttributeString("xsi", "schemaLocation", Nothing, "urn:schemas-upnp-org:av:avs http://www.upnp.org/schemas/av/avs.xsd")
                writer.WriteStartElement("Feature")
                writer.WriteAttributeString("name", "samsung.com_BASICVIEW")
                writer.WriteAttributeString("version", "1")
                writer.WriteStartElement("container")
                writer.WriteAttributeString("id", "1")
                writer.WriteAttributeString("type", "object.item.audioItem")
                writer.WriteEndElement()
                writer.WriteStartElement("container")
                writer.WriteAttributeString("id", "2")
                writer.WriteAttributeString("type", "object.item.videoItem")
                writer.WriteEndElement()
                writer.WriteStartElement("container")
                writer.WriteAttributeString("id", "3")
                writer.WriteAttributeString("type", "object.item.imageItem")
                writer.WriteEndElement()
                writer.WriteEndElement()
                writer.WriteEndElement()
            End Using
            request.Response.SendSoapHeadersBody(request, text.ToString())
        End Sub
    End Class  ' ContentDirectoryService

    Private Enum BrowseFlag
        BrowseMetadata
        BrowseDirectChildren
    End Enum  ' BrowseFlag
End Class
