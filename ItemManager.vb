Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Xml
Imports System.Collections.ObjectModel

Partial Public Class Plugin
    Private Class ItemManager
        Public SupportedMimeTypes() As String = Nothing
        Public DisablePcmTimeSeek As Boolean = False
        Private ReadOnly template As New TemplateNode(Nothing, Nothing, Nothing, Nothing, "object.container", Nothing)
        Private treeIsLoaded As Boolean = False
        Private tree As New FolderNode
        Private ReadOnly streamingProfile As StreamingProfile
        Private ReadOnly encodeSettings As New MediaSettings
        Private Shared musicFiles As List(Of String())
        Private Shared podcastFiles As List(Of String())
        Private Shared radioFiles As List(Of String())
        Private Shared audiobookFiles As List(Of String())
        Private Shared inboxFiles As List(Of String())
        Private Shared playlistRootStandard As FolderNode
        Private Shared playlistRootWmcCompat As FolderNode
        Private Shared ReadOnly fileLookup As New Dictionary(Of String, String())(StringComparer.OrdinalIgnoreCase)
        Private Shared ReadOnly itemManagerLookup As New Dictionary(Of String, ItemManager)(StringComparer.OrdinalIgnoreCase)
        Private Shared libraryIsDirty As Boolean = False
        Private Shared fileLookupLoaded As Boolean = False
        Private Shared ReadOnly queryFields() As Plugin.MetaDataType = DirectCast(New Integer() {MetaDataType.Url, MetaDataType.Category, MetaDataType.Artist, MetaDataType.ArtistPeople, MetaDataType.AlbumArtist, MetaDataType.Composer, MetaDataType.Conductor, MetaDataType.TrackTitle, MetaDataType.Album, MetaDataType.TrackNo, MetaDataType.DiscNo, MetaDataType.DiscCount, MetaDataType.YearOnly, MetaDataType.Genre, MetaDataType.Publisher, MetaDataType.Rating, -MetaDataType.Duration, -MetaDataType.FileSize, -MetaDataType.Bitrate, -MetaDataType.SampleRate, -MetaDataType.Channels, -MetaDataType.DateAdded, -MetaDataType.PlayCount, -MetaDataType.DateLastPlayed, -MetaDataType.ReplayGainTrack, 0}, Plugin.MetaDataType())
        Private Shared ReadOnly musicCategory As String = mbApiInterface.MB_GetLocalisation("Main.tree.Music", "Music")
        Private Shared ReadOnly podcastCategory As String = mbApiInterface.MB_GetLocalisation("Main.tree.Podcasts", "Podcasts")
        Private Shared ReadOnly radioCategory As String = mbApiInterface.MB_GetLocalisation("Main.tree.RaSt", "Radio")
        Private Shared ReadOnly audiobookCategory As String = mbApiInterface.MB_GetLocalisation("Main.tree.AuBo", "Audiobooks")
        Private Shared ReadOnly inboxCategory As String = mbApiInterface.MB_GetLocalisation("Main.tree.Inbox", "Inbox")
        Private Shared ReadOnly playlistsCategory As String = mbApiInterface.MB_GetLocalisation("Main.tree.Playlists", "Playlists")

        Public Shared Function GetItemManager(requestHeaders As Dictionary(Of String, String)) As ItemManager
            Dim profile As StreamingProfile = Settings.GetStreamingProfile(requestHeaders)
            SyncLock itemManagerLookup
                Dim itemManager As ItemManager
                If Not itemManagerLookup.TryGetValue(profile.ProfileName, itemManager) Then
                    itemManager = New ItemManager(profile)
                    itemManagerLookup.Add(profile.ProfileName, itemManager)
                End If
                Return itemManager
            End SyncLock
        End Function

        Private Sub New(profile As StreamingProfile)
            streamingProfile = profile
        End Sub

        Public Shared Sub SetLibraryDirty()
            SyncLock fileLookup
                libraryIsDirty = True
            End SyncLock
        End Sub

        Private Sub LoadLibrary()
            SyncLock fileLookup
                Dim loadRequired As Boolean
                If fileLookupLoaded Then
                    loadRequired = libraryIsDirty
                    libraryIsDirty = False
                Else
                    libraryIsDirty = False
                    loadRequired = True
                    fileLookupLoaded = True
                End If
                If loadRequired Then
                    Dim filenames() As String
                    mbApiInterface.Library_QueryFilesEx("domain=Music+AudioBooks+Inbox", filenames)
                    musicFiles = New List(Of String())
                    audiobookFiles = New List(Of String())
                    inboxFiles = New List(Of String())
                    Dim podcastSubs() As String
                    mbApiInterface.Podcasts_QuerySubscriptions("", podcastSubs)
                    podcastFiles = New List(Of String())
                    Dim radioUrls() As String
                    mbApiInterface.Library_QueryFilesEx("domain=Radio", radioUrls)
                    radioFiles = New List(Of String())
                    For index As Integer = 0 To filenames.Length - 1
                        Dim tags() As String = LoadFile(filenames(index))
                        If String.Compare(tags(MetaDataIndex.Category), inboxCategory, StringComparison.Ordinal) = 0 Then
                            inboxFiles.Add(tags)
                        ElseIf String.Compare(tags(MetaDataIndex.Category), audiobookCategory, StringComparison.Ordinal) = 0 Then
                            audiobookFiles.Add(tags)
                        Else
                            musicFiles.Add(tags)
                        End If
                    Next index
                    For index As Integer = 0 To podcastSubs.Length - 1
                        Dim subscription() As String
                        mbApiInterface.Podcasts_GetSubscription(podcastSubs(index), subscription)
                        If subscription(SubscriptionMetaDataType.DounloadedCount) <> "0" Then
                            Dim episodes() As String
                            mbApiInterface.Podcasts_GetSubscriptionEpisodes(podcastSubs(index), episodes)
                            For episodeIndex As Integer = 0 To episodes.Length - 1
                                Dim episode() As String
                                mbApiInterface.Podcasts_GetSubscriptionEpisode(subscription(SubscriptionMetaDataType.Id), episodeIndex, episode)
                                If episode(EpisodeMetaDataType.IsDownloaded) = "True" Then
                                    Dim tags() As String
                                    mbApiInterface.Library_GetFileTags(episodes(episodeIndex), queryFields, tags)
                                    Dim id As String = GetFileId(tags(MetaDataIndex.Url))
                                    If Not fileLookup.ContainsKey(id) Then
                                        fileLookup.Add(id, tags)
                                    End If
                                    podcastFiles.Add(tags)
                                End If
                            Next episodeIndex
                        End If
                    Next index
                    For index As Integer = 0 To radioUrls.Length - 1
                        Dim tags() As String
                        mbApiInterface.Library_GetFileTags(radioUrls(index), queryFields, tags)
                        Dim id As String = GetFileId(tags(MetaDataIndex.Url))
                        If Not fileLookup.ContainsKey(id) Then
                            fileLookup.Add(id, tags)
                        End If
                        radioFiles.Add(tags)
                    Next index
                    playlistRootStandard = LoadLibraryPlaylists(False)
                    playlistRootWmcCompat = LoadLibraryPlaylists(True)
                End If
                If Not treeIsLoaded Then
                    treeIsLoaded = True
                    Dim musicTree As New TemplateNode("1", "0", "1", musicCategory, "object.container", Nothing)
                    Dim podcastTree As New TemplateNode("114", "0", "114", podcastCategory, "object.container", New MetaDataIndex() {MetaDataIndex.Album}) With {
                        .Category = ContainerCategory.Podcast
                    }
                    Dim audiobooksTree As New TemplateNode("115", "0", "115", audiobookCategory, "object.container", Nothing) With {
                        .Category = ContainerCategory.Audiobook
                    }
                    Dim inboxTree As New TemplateNode("116", "0", "116", inboxCategory, "object.container.container", Nothing) With {
                        .Category = ContainerCategory.Inbox
                    }
                    Dim radioTree As New TemplateNode("117", "0", "117", radioCategory, "object.container.container", Nothing) With {
                        .Category = ContainerCategory.Radio
                    }
                    Dim playlistsTree As New TemplateNode("13", "0", "13", playlistsCategory, "object.container", New MetaDataIndex() {MetaDataIndex.Url}) With {
                        .Category = ContainerCategory.Playlist
                    }
                    Dim artistsTree As New TemplateNode("1_101", "1", "101", "Artists", "object.container", New MetaDataIndex() {MetaDataIndex.Artist})
                    Dim albumArtistsTree As New TemplateNode("1_102", "1", "102", "Album Artists", "object.container", New MetaDataIndex() {MetaDataIndex.AlbumArtist})
                    Dim composersTree As New TemplateNode("1_105", "1", "105", "Composers", "object.container", New MetaDataIndex() {MetaDataIndex.Composer})
                    Dim genresTree As New TemplateNode("1_103", "1", "103", "Genres", "object.container", New MetaDataIndex() {MetaDataIndex.Genre})
                    Dim yearsTree As New TemplateNode("1_104", "1", "104", "Years", "object.container", New MetaDataIndex() {MetaDataIndex.Year})
                    Dim albumTree As New TemplateNode("1_100", "1", "100", "Albums", "object.container", (New MetaDataIndex() {MetaDataIndex.Album}))
                    musicTree.ChildNodes = New TemplateNode() {albumTree, artistsTree, albumArtistsTree, composersTree, genresTree, yearsTree}
                    Dim artistAlbumsTree As New TemplateNode("1_101_121", "101", "121", Nothing, "object.container", New MetaDataIndex() {MetaDataIndex.Artist, MetaDataIndex.Album}) With {
                        .IncludeAllTracks = True
                    }
                    artistsTree.ChildNodes = New TemplateNode() {artistAlbumsTree}
                    Dim albumArtistAlbumsTree As New TemplateNode("1_102_131", "102", "131", Nothing, "object.container", New MetaDataIndex() {MetaDataIndex.AlbumArtist, MetaDataIndex.Album}) With {
                        .IncludeAllTracks = True
                    }
                    albumArtistsTree.ChildNodes = New TemplateNode() {albumArtistAlbumsTree}
                    Dim composerAlbumsTree As New TemplateNode("1_105_132", "105", "132", Nothing, "object.container", New MetaDataIndex() {MetaDataIndex.Composer, MetaDataIndex.AlbumArtistAndAlbum}) With {
                        .IncludeAllTracks = True,
                        .ChildNodes = New TemplateNode() {composersTree}
                    }
                    Dim genreAlbumsTree As New TemplateNode("1_103_140", "103", "140", Nothing, "object.container", New MetaDataIndex() {MetaDataIndex.Genre, MetaDataIndex.AlbumArtistAndAlbum}) With {
                        .IncludeAllTracks = True
                    }
                    genresTree.ChildNodes = New TemplateNode() {genreAlbumsTree}
                    Dim yearsAlbumsTree As New TemplateNode("1_104_150", "104", "150", Nothing, "object.container", New MetaDataIndex() {MetaDataIndex.Year, MetaDataIndex.AlbumArtistAndAlbum}) With {
                        .IncludeAllTracks = True
                    }
                    yearsTree.ChildNodes = New TemplateNode() {yearsAlbumsTree}
                    template.ChildNodes = New TemplateNode() {musicTree, podcastTree, audiobooksTree, radioTree, inboxTree, playlistsTree}
                    tree.Folders = New FolderNode() {New FolderNode(musicCategory), New FolderNode(podcastCategory, podcastFiles), New FolderNode(audiobookCategory, audiobookFiles), New FolderNode(radioCategory, radioFiles), New FolderNode(inboxCategory, inboxFiles), If(streamingProfile.WmcCompatability, playlistRootWmcCompat, playlistRootStandard)}
                End If
            End SyncLock
        End Sub

        Private Shared Function LoadFile(url As String) As String()
            Dim tags() As String
            mbApiInterface.Library_GetFileTags(url, queryFields, tags)
            Dim id As String = GetFileId(url)
            If Not fileLookup.ContainsKey(id) Then
                fileLookup.Add(id, tags)
            End If
            Return tags
        End Function

        Private Shared Function LoadLibraryPlaylists(wmcCompatability As Boolean) As FolderNode
            mbApiInterface.Playlist_QueryPlaylists()
            Dim playlists As New PlaylistFolderNode(playlistsCategory)
            Dim folder As PlaylistFolderNode
            Dim playlist As PlaylistFolderNode
            playlist = New PlaylistFolderNode("Now Playing") With {
                .Path = "NowPlaying"
            }
            playlists.Folders.Add(playlist)
            Do
                Dim playlistUrl As String = mbApiInterface.Playlist_QueryGetNextPlaylist()
                If playlistUrl Is Nothing Then
                    Exit Do
                End If
                If mbApiInterface.Playlist_GetType(playlistUrl) <> PlaylistFormat.Radio Then
                    folder = playlists
                    Dim values() As String
                    If wmcCompatability Then
                        values = New String() {mbApiInterface.Playlist_GetName(playlistUrl)}
                    Else
                        values = mbApiInterface.Playlist_GetName(playlistUrl).Split("\"c)
                        For index As Integer = 0 To values.Length - 2
                            Dim name As String = values(index)
                            Dim matched As Boolean = False
                            For index2 As Integer = 0 To folder.Folders.Count - 1
                                If String.Compare(folder.Folders(index2).Name, name, StringComparison.OrdinalIgnoreCase) = 0 Then
                                    matched = True
                                    folder = folder.Folders(index2)
                                    Exit For
                                End If
                            Next index2
                            If Not matched Then
                                folder.Folders.Add(New PlaylistFolderNode(name))
                            End If
                        Next index
                    End If
                    playlist = New PlaylistFolderNode(values(values.Length - 1)) With {
                        .Path = playlistUrl
                    }
                    folder.Folders.Add(playlist)
                End If
            Loop
            Dim playlistRoot As New FolderNode
            LoadPlaylistFolder(playlists, playlistRoot)
            Return playlistRoot
        End Function

        Private Shared Sub LoadPlaylistFolder(playlist As PlaylistFolderNode, ByRef folder As FolderNode)
            folder.Folders = New FolderNode(playlist.Folders.Count - 1) {}
            For index As Integer = 0 To folder.Folders.Length - 1
                LoadPlaylistFolder(playlist.Folders(index), folder.Folders(index))
            Next index
            folder.Path = playlist.Path
            folder.Name = playlist.Name
        End Sub

        Private Sub LoadNode(template As TemplateNode, ByRef folder As FolderNode, drillDown As Boolean)
            If drillDown Then
                Dim childNodes() As TemplateNode = template.ChildNodes
                If childNodes IsNot Nothing Then
                    For index As Integer = 0 To childNodes.Length - 1
                        LoadNode(childNodes(index), folder.Folders(index), False)
                    Next index
                End If
            End If
            If template.Category = ContainerCategory.Playlist Then
                ' already loaded
            ElseIf folder.Folders Is Nothing AndAlso (folder.ChildFiles Is Nothing OrElse (template.Fields IsNot Nothing AndAlso folder.Folders Is Nothing)) Then
                Dim headerOnly As Boolean
                If template.ChildNodes Is Nothing Then
                    headerOnly = False
                Else
                    headerOnly = True
                    For index As Integer = 0 To template.ChildNodes.Length - 1
                        If template.ChildNodes(index).Name Is Nothing Then
                            headerOnly = False
                            Exit For
                        End If
                    Next index
                End If
                If headerOnly Then
                    folder.Folders = New FolderNode(template.ChildNodes.Length - 1) {}
                    For index As Integer = 0 To template.ChildNodes.Length - 1
                        folder.Folders(index) = New FolderNode(template.ChildNodes(index).Name)
                    Next index
                Else
                    Dim sourceFiles As List(Of String()) = If(folder.ChildFiles, musicFiles)
                    Dim fields() As MetaDataIndex = template.Fields
                    Dim lastField As MetaDataIndex = fields(fields.Length - 1)
                    For fieldIndex As Integer = 0 To fields.Length - 1
                        If fields(fieldIndex) = MetaDataIndex.AlbumArtistAndAlbum Then
                            For index As Integer = 0 To sourceFiles.Count - 1
                                Dim file() As String = sourceFiles(index)
                                If file(MetaDataIndex.Album).Length = 0 Then
                                    file(MetaDataIndex.AlbumArtistAndAlbum) = ""
                                Else
                                    file(MetaDataIndex.AlbumArtistAndAlbum) = file(MetaDataIndex.AlbumArtist) & " - " & file(MetaDataIndex.Album)
                                End If
                            Next index
                            Exit For
                        End If
                    Next fieldIndex
                    Dim key As New StringBuilder(256)
                    Dim lookup As New Dictionary(Of String, List(Of String()))(StringComparer.OrdinalIgnoreCase)
                    Dim lookupFolders As New List(Of FolderNode)
                    Dim genreFieldIndex As Integer = -1
                    If fields.Length = 1 AndAlso fields(0) = MetaDataIndex.Genre Then
                        genreFieldIndex = 0
                    End If
                    For index As Integer = 0 To sourceFiles.Count - 1
                        Dim tags() As String = sourceFiles(index)
                        If genreFieldIndex = -1 OrElse tags(MetaDataIndex.Genre).IndexOf("; ", StringComparison.Ordinal) = -1 Then
                            Dim files As List(Of String())
                            If fields.Length = 1 Then
                                Dim folderName As String = tags(fields(0))
                                If folderName.Length = 0 Then
                                    GoTo skipFile
                                End If
                                If Not lookup.TryGetValue(folderName, files) Then
                                    files = New List(Of String())
                                    lookup.Add(folderName, files)
                                    lookupFolders.Add(New FolderNode(folderName, files))
                                End If
                            Else
                                key.Length = 0
                                For fieldIndex As Integer = 0 To fields.Length - 1
                                    If tags(fields(fieldIndex)).Length = 0 Then
                                        GoTo skipFile
                                    End If
                                    If fieldIndex > 0 Then
                                        key.Append(ChrW(0))
                                    End If
                                    key.Append(tags(fields(fieldIndex)))
                                Next fieldIndex
                                If Not lookup.TryGetValue(key.ToString(), files) Then
                                    files = New List(Of String())
                                    lookup.Add(key.ToString(), files)
                                    lookupFolders.Add(New FolderNode(tags(lastField), files))
                                End If
                            End If
                            files.Add(tags)
                        Else
                            ' split genres
                            Dim genres() As String = tags(MetaDataIndex.Genre).Split(New String() {"; "}, StringSplitOptions.None)
                            For genreIndex As Integer = 0 To genres.Length - 1
                                Dim folderName As String = genres(genreIndex)
                                Dim files As List(Of String())
                                If Not lookup.TryGetValue(folderName, files) Then
                                    files = New List(Of String())
                                    lookup.Add(folderName, files)
                                    lookupFolders.Add(New FolderNode(folderName, files))
                                End If
                                files.Add(tags)
                            Next genreIndex
                        End If
skipFile:
                    Next index
                    If (Not Settings.BucketNodes OrElse lookupFolders.Count <= Settings.BucketTrigger) OrElse lastField = MetaDataIndex.Year Then
                        Dim folders() As FolderNode = New FolderNode(lookupFolders.Count - If(template.IncludeAllTracks, 0, 1)) {}
                        Dim offset As Integer = 0
                        If template.IncludeAllTracks Then
                            folders(0) = New FolderNode("[All Tracks]", sourceFiles)
                            offset = 1
                        End If
                        Select Case lastField
                            Case MetaDataIndex.Artist, MetaDataIndex.AlbumArtist, MetaDataIndex.Album
                                lookupFolders.Sort(New FolderPrefixedNameComparer)
                            Case Else
                                lookupFolders.Sort(New FolderNameComparer)
                        End Select
                        For index As Integer = 0 To lookupFolders.Count - 1
                            folders(offset) = lookupFolders(index)
                            offset += 1
                        Next index
                        folder.Folders = folders
                    Else
                        CreateBucket(lookupFolders, sourceFiles, (lastField <> MetaDataIndex.Genre), template.IncludeAllTracks, folder)
                    End If
                End If
            End If
        End Sub

        Private Sub CreateBucket(sourceFolders As List(Of FolderNode), sourceFiles As List(Of String()), useNameComparer As Boolean, includeAllTracks As Boolean, ByRef folder As FolderNode)
            Dim bucket As New Dictionary(Of Char, List(Of FolderNode))
            For index As Integer = 0 To sourceFolders.Count - 1
                Dim c As Char = If(useNameComparer, GetNameBucket(sourceFolders(index).Name), GetBucket(sourceFolders(index).Name))
                Dim bucketFolders As List(Of FolderNode)
                If Not bucket.TryGetValue(c, bucketFolders) Then
                    bucketFolders = New List(Of FolderNode)
                    bucket.Add(c, bucketFolders)
                End If
                bucketFolders.Add(sourceFolders(index))
            Next index
            Dim letters As New List(Of String)
            For Each c As Char In bucket.Keys
                letters.Add(c)
            Next c
            letters.Sort(StringComparer.CurrentCultureIgnoreCase)
            Dim folders() As FolderNode = New FolderNode(letters.Count - If(includeAllTracks, 0, 1)) {}
            folder.IsBucket = True
            folder.Folders = folders
            Dim offset As Integer = 0
            If includeAllTracks Then
                folders(0) = New FolderNode("[All Tracks]", sourceFiles)
                offset = 1
            End If
            For Each letter As String In letters
                Dim folderFiles As New List(Of String())
                Dim bucketFolders As List(Of FolderNode) = bucket(letter.Chars(0))
                For Each bucketFolder As FolderNode In bucketFolders
                    folderFiles.AddRange(bucketFolder.ChildFiles)
                Next bucketFolder
                If useNameComparer Then
                    bucketFolders.Sort(New FolderPrefixedNameComparer)
                Else
                    bucketFolders.Sort(New FolderNameComparer)
                End If
                folders(offset) = New FolderNode(letter, bucketFolders.ToArray(), folderFiles)
                offset += 1
            Next letter
        End Sub

        Public Function TryGetFileInfo(objectId As String, ByRef url As String, ByRef duration As TimeSpan) As Boolean
            If Not fileLookupLoaded Then
                LoadLibrary()
            End If
            Dim tags() As String = Nothing
            SyncLock fileLookup
                If Not fileLookup.TryGetValue(objectId, tags) Then
                    Return False
                Else
                    url = tags(MetaDataIndex.Url)
                    Dim durationValue As Long
                    If Long.TryParse(tags(MetaDataIndex.Duration), durationValue) Then
                        duration = New TimeSpan(durationValue)
                    End If
                End If
            End SyncLock
            Return True
        End Function

        Public Function TryGetThumbnailFile(objectId As String, ByRef pictureUrl As String) As Boolean
            Dim tags() As String = Nothing
            SyncLock fileLookup
                If Not fileLookup.TryGetValue(objectId, tags) Then
                    Return False
                Else
                    Dim url As String = tags(MetaDataIndex.Url)
                    If url.EndsWith("#"c) Then
                        ' remove track from virtual url
                        url = url.Substring(0, url.LastIndexOf("#"c, url.Length - 2))
                    End If
                    pictureUrl = mbApiInterface.Library_GetArtworkUrl(url, -1)
                    If pictureUrl Is Nothing Then
                        Return False
                    End If
                    Return True
                End If
            End SyncLock
        End Function

        Public Sub Search(headers As Dictionary(Of String, String), containerId As String, searchCriteria As String, filter As String, startingIndex As Integer, requestedCount As Integer, sortCriteria As String, ByRef result As String, ByRef numberReturned As String, ByRef totalMatches As String)
            'Debug.WriteLine(containerId & ":" & searchCriteria & ",f=" & filter)
            If Settings.LogDebugInfo Then
                LogInformation("Search", "object=" & containerId & ",criteria=" & searchCriteria)
            End If
            LoadLibrary()
            If streamingProfile.WmcCompatability AndAlso (containerId = "F" OrElse searchCriteria = "upnp:class derivedfrom ""object.container.playlistContainer"" and @refID exists false") Then
                Browse(headers, "13", BrowseFlag.BrowseDirectChildren, filter, startingIndex, requestedCount, sortCriteria, result, numberReturned, totalMatches)
            ElseIf streamingProfile.WmcCompatability AndAlso (containerId = "5" OrElse containerId = "6" OrElse containerId = "7") Then
                LoadNode(template, tree, True)
                Browse(headers, If(containerId = "5", "1_103", If(containerId = "6", "1_101", "1_100")), BrowseFlag.BrowseDirectChildren, filter, startingIndex, requestedCount, sortCriteria, result, numberReturned, totalMatches)
            Else
                Dim host As String
                Dim hostUrl As String = If(Not headers.TryGetValue("host", host), PrimaryHostUrl, "http://" & host)
                Dim text As New StringBuilder(16384)
                Dim filterSet As HashSet(Of String) = Nothing
                If filter <> "*" Then
                    filterSet = New HashSet(Of String)(filter.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase)
                End If
                Dim xmlSettings As New XmlWriterSettings With {
                    .OmitXmlDeclaration = True,
                    .Indent = False
                }
                Using writer As XmlWriter = XmlWriter.Create(text, xmlSettings)
                    writer.WriteStartElement("DIDL-Lite", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/")
                    writer.WriteAttributeString("xmlns", "dc", Nothing, "http://purl.org/dc/elements/1.1/")
                    writer.WriteAttributeString("xmlns", "upnp", Nothing, "urn:schemas-upnp-org:metadata-1-0/upnp/")
                    'writer.WriteAttributeString("xmlns", "av", Nothing, "urn:schemas-sony-com:av")
                    writer.WriteAttributeString("xmlns", "pv", Nothing, "http://www.pv.com/pvns/")
                    Dim objectIds() As String = containerId.Split("_"c)
                    Dim matches As List(Of String()) = Nothing
                    If containerId = "0" Then
                        matches = musicFiles
                    Else
                        Dim node As TemplateNode
                        Dim folder As FolderNode
                        Dim lookupIdCount As Integer
                        If TryLocateNode(objectIds, node, folder, lookupIdCount) Then
                            matches = folder.ChildFiles
                        End If
                    End If
                    If matches Is Nothing Then
                        totalMatches = "0"
                        numberReturned = "0"
                    Else
                        If searchCriteria.StartsWith("("c) Then
                            Dim charIndex As Integer = searchCriteria.IndexOf(")"c)
                            Dim scope As String = searchCriteria.Substring(1, charIndex - 1)
                            If String.Compare(scope, "upnp:class derivedfrom ""object.item.audioItem.musicTrack""", StringComparison.OrdinalIgnoreCase) = 0 OrElse
                               String.Compare(scope, "upnp:class derivedfrom ""object.item.audioItem""", StringComparison.OrdinalIgnoreCase) = 0 OrElse
                               String.Compare(scope, "upnp:class derivedfrom ""object.container.album.musicAlbum""", StringComparison.OrdinalIgnoreCase) = 0 Then
                                ' leave matches as-is
                            Else
                                matches = New List(Of String())
                            End If
                            searchCriteria = searchCriteria.Substring(charIndex + 1).TrimStart()
                            If searchCriteria.StartsWith("and ", StringComparison.OrdinalIgnoreCase) Then
                                searchCriteria = searchCriteria.Substring(4).TrimStart()
                                Dim field As String = searchCriteria.Substring(0, searchCriteria.IndexOf(" "c))
                                searchCriteria = searchCriteria.Substring(field.Length).TrimStart()
                                Dim criteria As String = searchCriteria.Substring(0, searchCriteria.IndexOf(" "c))
                                searchCriteria = searchCriteria.Substring(criteria.Length).TrimStart()
                                If (String.Compare(criteria, "contains", StringComparison.OrdinalIgnoreCase) = 0 OrElse criteria = "=") AndAlso searchCriteria.StartsWith(""""c) AndAlso searchCriteria.EndsWith(""""c) Then
                                    searchCriteria = searchCriteria.Substring(1, searchCriteria.Length - 2)
                                End If
                            End If
                            'JRiver: (upnp:class derivedfrom "object.item.audioItem.musicTrack")
                            '        (upnp:class derivedfrom "object.item.videoItem")
                            'upnp:genre, upnp:album
                            '(upnp:class derivedfrom "object.item.audioItem.musicTrack") and dc:title contains "<The Search Term you entered>"
                            '(upnp:class = ""object.container.album.musicAlbum"") and (upnp:artist = ""Amplifier"")
                            'upnp:class derivedfrom "object.item.audioItem" and @refID exists false
                        End If
                        numberReturned = WriteAudioFilesDIDL(writer, hostUrl, filterSet, "object.item.audioItem.musicTrack", "0", matches, startingIndex, requestedCount).ToString()
                        totalMatches = matches.Count.ToString()
                    End If
                End Using
                result = text.ToString()
            End If
        End Sub

        Public Sub Browse(headers As Dictionary(Of String, String), objectId As String, browseType As BrowseFlag, filter As String, startingIndex As Integer, requestedCount As Integer, sortCriteria As String, ByRef result As String, ByRef numberReturned As String, ByRef totalMatches As String)
            'Debug.WriteLine(objectId & "," & browseType.ToString & "," & startingIndex & "," & requestedCount & "," & sortCriteria & "," & filter)
            If Settings.LogDebugInfo Then
                LogInformation("Browse", objectId & "," & browseType.ToString() & "," & startingIndex & "," & requestedCount & ",sort=" & sortCriteria)
            End If
            Dim host As String
            Dim hostUrl As String = If(Not headers.TryGetValue("host", host), "", "http://" & host)
            Dim text As New StringBuilder(16384)
            Dim filterSet As HashSet(Of String) = Nothing
            If filter <> "*" Then
                filterSet = New HashSet(Of String)(filter.Split(New Char() {","c}, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase)
            End If
            If Not fileLookupLoaded Then
                ' needed because WMP can load Playlists without starting from the music file root
                LoadLibrary()
            End If
            Dim xmlSettings As New XmlWriterSettings With {
                .OmitXmlDeclaration = True
            }
            Using writer As XmlWriter = XmlWriter.Create(text, xmlSettings)
                writer.WriteStartElement("DIDL-Lite", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/")
                writer.WriteAttributeString("xmlns", "dc", Nothing, "http://purl.org/dc/elements/1.1/")
                writer.WriteAttributeString("xmlns", "upnp", Nothing, "urn:schemas-upnp-org:metadata-1-0/upnp/")
                'writer.WriteAttributeString("xmlns", "av", Nothing, "urn:schemas-sony-com:av")
                writer.WriteAttributeString("xmlns", "pv", Nothing, "http://www.pv.com/pvns/")
                If browseType = BrowseFlag.BrowseMetadata Then
                    If objectId = "0" Then
                        LoadLibrary()
                        LoadNode(template, tree, True)
                        WriteContainerDIDL(writer, filterSet, "0", "-1", tree.Folders.Length.ToString(), "Root", "object.container", Nothing)
                        totalMatches = "1"
                        numberReturned = "1"
                    ElseIf objectId.Length < 4 OrElse objectId.IndexOf("_"c) <> -1 Then
                        Dim objectIds() As String = objectId.Split("_"c)
                        Dim node As TemplateNode
                        Dim folder As FolderNode
                        Dim lookupIdCount As Integer
                        Dim matched As Boolean = TryLocateNode(objectIds, node, folder, lookupIdCount)
                        If Not matched Then
                            totalMatches = "0"
                            numberReturned = "0"
                        Else
                            Dim containerClassOverride0 As String = Nothing
                            Dim containerClassOverride1 As String = Nothing
                            If node.Fields IsNot Nothing Then
                                Dim fieldIndex As Integer = node.Fields.Length - 1
                                If lookupIdCount = 1 Then
                                    If node.Fields.Length > 1 Then
                                        fieldIndex -= 1
                                    End If
                                Else
                                    Do While lookupIdCount >= 2
                                        Dim index As Integer
                                        If Not Integer.TryParse(objectIds(objectIds.Length - (lookupIdCount - 1)), index) OrElse index >= folder.Folders.Length Then
                                            LogInformation("Browse", "id count=" & lookupIdCount & ",index=" & index & ",folders=" & folder.Folders.Length)
                                            Throw New ArgumentException
                                        Else
                                            folder = folder.Folders(index)
                                        End If
                                        lookupIdCount -= 1
                                    Loop
                                End If
                                SetContainerClass(node, (folder.IsBucket AndAlso objectIds.Length <= 3), fieldIndex, containerClassOverride0, containerClassOverride1)
                            End If
                            Dim firstIndex As Boolean = (objectId.EndsWith("_0", StringComparison.Ordinal) AndAlso folder.Name = "[All Tracks]")
                            WriteContainerDIDL(writer, filterSet, objectId, "0", If(folder.Folders Is Nothing, If(folder.ChildFiles Is Nothing, 0, folder.ChildFiles.Count), folder.Folders.Length).ToString(), folder.Name, If(containerClassOverride0 Is Nothing, node.ContainerClass, If(firstIndex, containerClassOverride0, containerClassOverride1)), folder.ChildFiles)
                            totalMatches = "1"
                            numberReturned = "1"
                        End If
                    Else
                        Dim charIndex As Integer = objectId.IndexOf("."c)
                        If charIndex <> -1 Then
                            objectId = objectId.Substring(0, charIndex)
                        End If
                        SyncLock fileLookup
                            Dim tags() As String
                            If Not fileLookup.TryGetValue(objectId, tags) Then
                                LogInformation("Browse", "metadata lookup fail=" & objectId)
                                totalMatches = "0"
                                numberReturned = "0"
                            Else
                                WriteAudioFileDIDL(writer, hostUrl, filterSet, "0", tags, "object.item.audioItem.musicTrack")
                                totalMatches = "1"
                                numberReturned = "1"
                            End If
                        End SyncLock
                    End If
                ElseIf objectId = "0" Then
                    ' browse with children
                    If startingIndex = 0 Then
                        ' allow library to be reloaded if starting from root
                        LoadLibrary()
                        LoadNode(template, tree, True)
                    End If
                    numberReturned = WriteContainerItemsDIDL(writer, filterSet, objectId, template, tree, startingIndex, requestedCount).ToString()
                    totalMatches = tree.Folders.Length.ToString()
                Else
                    Dim objectIds() As String = objectId.Split("_"c)
                    Dim node As TemplateNode
                    Dim folder As FolderNode
                    Dim lookupIdCount As Integer
                    Dim matched As Boolean = TryLocateNode(objectIds, node, folder, lookupIdCount)
                    If Not matched Then
                        totalMatches = "0"
                        numberReturned = "0"
                    ElseIf node.Category = ContainerCategory.Audiobook Then
                        ' audiobooks
                        folder.ChildFiles.Sort(New AlbumFileComparer)
                        numberReturned = WriteAudioFilesDIDL(writer, hostUrl, filterSet, "object.item.audioItem.audioBook", objectId, folder.ChildFiles, startingIndex, requestedCount).ToString()
                        totalMatches = folder.ChildFiles.Count.ToString()
                    ElseIf node.Category = ContainerCategory.Inbox Then
                        ' inbox 
                        folder.ChildFiles.Sort(New AlbumFileComparer)
                        numberReturned = WriteAudioFilesDIDL(writer, hostUrl, filterSet, "object.item.audioItem.musicTrack", objectId, folder.ChildFiles, startingIndex, requestedCount).ToString()
                        totalMatches = folder.ChildFiles.Count.ToString()
                    ElseIf lookupIdCount = 0 OrElse ((node.Category = ContainerCategory.Playlist OrElse node.Category = ContainerCategory.Podcast) AndAlso lookupIdCount < 2) Then
                        numberReturned = WriteContainerItemsDIDL(writer, filterSet, objectId, node, folder, startingIndex, requestedCount).ToString()
                        totalMatches = folder.Folders.Length.ToString()
                    Else
                        Dim index As Integer
                        Do While lookupIdCount >= 2
                            If Not Integer.TryParse(objectIds(objectIds.Length - (lookupIdCount - 1)), index) OrElse index >= folder.Folders.Length Then
                                LogInformation("Browse", "id count=" & lookupIdCount & ",index=" & index & ",folders=" & folder.Folders.Length)
                                Throw New ArgumentException
                            Else
                                folder = folder.Folders(index)
                            End If
                            lookupIdCount -= 1
                        Loop
                        If folder.Folders IsNot Nothing AndAlso folder.Folders.Length > 0 Then
                            numberReturned = WriteContainerItemsDIDL(writer, filterSet, objectId, node, folder, startingIndex, requestedCount).ToString()
                            totalMatches = folder.Folders.Length.ToString()
                        ElseIf node.Category = ContainerCategory.Playlist Then
                            Dim files As List(Of String()) = folder.ChildFiles
                            If files Is Nothing Then
                                Dim playlistUrl As String = folder.Path
                                files = New List(Of String())
                                Dim filenames() As String = Nothing
                                If folder.Path = "NowPlaying" Then
                                    mbApiInterface.NowPlayingList_QueryFilesEx(Nothing, filenames)
                                Else
                                    mbApiInterface.Playlist_QueryFilesEx(playlistUrl, filenames)
                                End If
                                If filenames IsNot Nothing Then
                                    For filenameIndex As Integer = 0 To filenames.Count - 1
                                        files.Add(LoadFile(filenames(filenameIndex)))
                                    Next filenameIndex
                                End If
                                folder.ChildFiles = files
                            End If
                            numberReturned = WriteAudioFilesDIDL(writer, hostUrl, filterSet, "object.item.audioItem.musicTrack", objectId, files, startingIndex, requestedCount).ToString()
                            totalMatches = files.Count.ToString()
                        Else
                            Dim files As List(Of String()) = folder.ChildFiles
                            If Not node.IncludeAllTracks OrElse index > 0 Then
                                files.Sort(New AlbumFileComparer)
                            Else
                                files.Sort(New TrackNameFileComparer)
                            End If
                            numberReturned = WriteAudioFilesDIDL(writer, hostUrl, filterSet, "object.item.audioItem.musicTrack", objectId, files, startingIndex, requestedCount).ToString()
                            totalMatches = files.Count.ToString()
                        End If
                    End If
                End If
                writer.WriteEndElement()
            End Using
            result = text.ToString()
            'If Settings.LogDebugInfo Then
            '    LogInformation("Browse", "num=" & numberReturned & ",tot=" & totalMatches & ",res=" & result)
            'End If
            'Debug.WriteLine(result)
        End Sub

        Private Function TryLocateNode(objectIds() As String, ByRef node As TemplateNode, ByRef folder As FolderNode, ByRef lookupIdCount As Integer) As Boolean
            node = template
            folder = tree
            lookupIdCount = 0
            Dim matched As Boolean = False
            For index As Integer = 0 To objectIds.Length - 1
                matched = False
                If node.Fields Is Nothing Then
                    For childIndex As Integer = 0 To node.ChildNodes.Length - 1
                        If node.ChildNodes(childIndex).Id = objectIds(index) Then
                            matched = True
                            node = node.ChildNodes(childIndex)
                            folder = folder.Folders(childIndex)
                            Exit For
                        End If
                    Next childIndex
                Else
                    Dim offset As Integer
                    If Integer.TryParse(objectIds(index), offset) AndAlso offset < folder.Folders.Length Then
                        matched = True
                        If Not folder.IsBucket Then
                            ' use last node as first could be [All Tracks]
                            node = node.ChildNodes(node.ChildNodes.Count - 1)
                        End If
                        folder = folder.Folders(offset)
                    End If
                End If
                If Not matched Then
                    Exit For
                Else
                    LoadNode(node, folder, True)
                    If node.ChildNodes Is Nothing Then
                        lookupIdCount = (objectIds.Length - index)
                        Exit For
                    End If
                End If
            Next index
            Return matched
        End Function

        Private Function WriteContainerItemsDIDL(writer As XmlWriter, filterSet As HashSet(Of String), parentId As String, template As TemplateNode, folder As FolderNode, startingIndex As Integer, requestedCount As Integer) As Integer
            Dim folders() As FolderNode = folder.Folders
            If startingIndex >= folders.Length Then
                Return 0
            Else
                Dim endingIndex As Integer = startingIndex + requestedCount - 1
                If endingIndex >= folders.Length Then
                    endingIndex = folders.Length - 1
                End If
                Dim containerClassOverride0 As String = Nothing
                Dim containerClassOverride1 As String = Nothing
                If template.Fields IsNot Nothing Then
                    SetContainerClass(template, folder.IsBucket, template.Fields.Length - 1, containerClassOverride0, containerClassOverride1)
                End If
                For index As Integer = startingIndex To endingIndex
                    Dim containerClass As String = If(template.Category = ContainerCategory.Playlist AndAlso folder.Folders(index).Folders.Length > 0, "object.container", If(containerClassOverride0 Is Nothing, template.ChildNodes(index).ContainerClass, If(index = 0 AndAlso String.Compare(folder.Folders(0).Name, "[All Tracks]", StringComparison.Ordinal) = 0, containerClassOverride0, containerClassOverride1)))
                    WriteContainerDIDL(writer, filterSet, If(template.Fields IsNot Nothing, parentId & "_" & index, template.ChildNodes(index).Path), parentId, If(folders(index).Folders Is Nothing, If(folders(index).ChildFiles Is Nothing, 0, folders(index).ChildFiles.Count), folders(index).Folders.Length).ToString(), folders(index).Name, containerClass, folders(index).ChildFiles)
                Next index
                Return (endingIndex - startingIndex + 1)
            End If
        End Function

        Private Sub SetContainerClass(template As TemplateNode, isBucket As Boolean, fieldIndex As Integer, ByRef containerClassOverride0 As String, ByRef containerClassOverride1 As String)
            If isBucket Then
                containerClassOverride1 = "object.container"
            Else
                containerClassOverride1 = template.ContainerClass
                Dim field As MetaDataIndex = template.Fields(fieldIndex)
                Select Case field
                    Case MetaDataIndex.Album, MetaDataIndex.AlbumArtistAndAlbum
                        containerClassOverride1 = "object.container.album.musicAlbum"
                    Case MetaDataIndex.Artist, MetaDataIndex.AlbumArtist
                        containerClassOverride1 = "object.container.person.musicArtist"
                    Case MetaDataIndex.Genre
                        containerClassOverride1 = "object.container.genre.musicGenre"
                    Case MetaDataIndex.Url
                        containerClassOverride1 = "object.container.playlistContainer"
                End Select
            End If
            If Not template.IncludeAllTracks Then
                containerClassOverride0 = containerClassOverride1
            Else
                containerClassOverride0 = "object.container.musicContainer"
            End If
        End Sub

        Private Sub WriteContainerDIDL(writer As XmlWriter, filterSet As HashSet(Of String), id As String, parentID As String, childCount As String, title As String, containerClass As String, childFiles As List(Of String()))
            writer.WriteStartElement("container")
            writer.WriteAttributeString("id", id)
            writer.WriteAttributeString("restricted", "1")
            writer.WriteAttributeString("parentID", parentID)
            If containerClass <> "object.container.playlistContainer" AndAlso childCount.Length > 0 Then
                writer.WriteAttributeString("childCount", childCount)
            End If
            writer.WriteAttributeString("searchable", "1")
            writer.WriteElementString("dc", "title", Nothing, title)
            If containerClass = "object.container.album.musicAlbum" AndAlso childFiles IsNot Nothing AndAlso childFiles.Count > 0 Then
                Dim tags() As String = childFiles(0)
                Dim year As String = tags(MetaDataIndex.Year)
                If year.Length = 4 Then
                    writer.WriteElementString("dc", "date", Nothing, year & "-01-01")
                ElseIf year.Length > 4 Then
                    Dim yearValue As DateTime
                    If DateTime.TryParse(year, yearValue) Then
                        writer.WriteElementString("dc", "date", Nothing, yearValue.ToString("yyyy-MM-dd"))
                    End If
                End If
                writer.WriteStartElement("upnp", "artist", Nothing)
                writer.WriteAttributeString("role", "AlbumArtist")
                writer.WriteValue(tags(MetaDataIndex.AlbumArtist))
                writer.WriteEndElement()
                writer.WriteElementString("upnp", "albumArtist", Nothing, tags(MetaDataIndex.AlbumArtist))
                Dim composer As String = tags(MetaDataIndex.Composer)
                If composer.Length > 0 Then
                    writer.WriteStartElement("upnp", "author", Nothing)
                    writer.WriteAttributeString("role", "Composer")
                    writer.WriteValue(composer)
                    writer.WriteEndElement()
                End If
                writer.WriteElementString("upnp", "album", Nothing, tags(MetaDataIndex.Album))
                If tags(MetaDataIndex.Genre).Length > 0 Then
                    writer.WriteElementString("upnp", "genre", Nothing, tags(MetaDataIndex.Genre))
                End If
            End If
            writer.WriteElementString("upnp", "class", Nothing, containerClass)
            If filterSet IsNot Nothing AndAlso filterSet.Contains("av:mediaClass") Then   'writer.LookupPrefix("urn:schemas-sony-com:av") IsNot Nothing
                writer.WriteElementString("av", "mediaClass", "urn:schemas-sony-com:av", "M")
            End If
            writer.WriteEndElement()
        End Sub

        Private Function WriteAudioFilesDIDL(writer As XmlWriter, hostUrl As String, filterSet As HashSet(Of String), classType As String, parentId As String, files As List(Of String()), startingIndex As Integer, requestedCount As Integer) As Integer
            If startingIndex >= files.Count Then
                Return 0
            Else
                Dim endingIndex As Integer = startingIndex + requestedCount - 1
                If endingIndex >= files.Count Then
                    endingIndex = files.Count - 1
                End If
                For index As Integer = startingIndex To endingIndex
                    WriteAudioFileDIDL(writer, hostUrl, filterSet, parentId, files(index), classType)
                Next index
                Return (endingIndex - startingIndex + 1)
            End If
        End Function

        Public Function WriteAudioFileDIDL(writer As XmlWriter, hostUrl As String, url As String, streamHandle As Integer) As String
            Dim objectId As String = GetFileId(url)
            Dim tags() As String
            SyncLock fileLookup
                If Not fileLookup.TryGetValue(objectId, tags) Then
                    tags = LoadFile(url)
                End If
            End SyncLock
            writer.WriteStartElement("DIDL-Lite", "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/")
            writer.WriteAttributeString("xmlns", "dc", Nothing, "http://purl.org/dc/elements/1.1/")
            writer.WriteAttributeString("xmlns", "upnp", Nothing, "urn:schemas-upnp-org:metadata-1-0/upnp/")
            'writer.WriteAttributeString("xmlns", "av", Nothing, "urn:schemas-sony-com:av")
            writer.WriteAttributeString("xmlns", "pv", Nothing, "http://www.pv.com/pvns/")
            Dim httpUrl As String = WriteAudioFileDIDL(writer, hostUrl, Nothing, "0", tags, "object.item.audioItem.musicTrack", streamHandle, True)
            writer.WriteEndElement()
            Return httpUrl
        End Function

        Private Function WriteAudioFileDIDL(writer As XmlWriter, hostUrl As String, filterSet As HashSet(Of String), parentId As String, tags() As String, classType As String, Optional streamHandle As Integer = 0, Optional musicBeePlayToMode As Boolean = False) As String
            writer.WriteStartElement("item")
            Dim defaultHttpUrl As String = Nothing
            If musicBeePlayToMode AndAlso Settings.ContinuousOutput Then
                writer.WriteAttributeString("id", "continuousstream")
                writer.WriteAttributeString("restricted", "true")
                writer.WriteAttributeString("parentID", "0")
                writer.WriteElementString("upnp", "class", Nothing, classType)
                writer.WriteElementString("dc", "title", Nothing, "Continuous Stream")
                If filterSet Is Nothing OrElse filterSet.Contains("dc:creator") Then
                    writer.WriteElementString("dc", "creator", Nothing, "MusicBee")
                End If
                If filterSet Is Nothing OrElse filterSet.Contains("upnp:artist") Then
                    writer.WriteElementString("upnp", "artist", Nothing, "MusicBee")
                End If
                If filterSet Is Nothing OrElse filterSet.Any(Function(a) a.StartsWith("res")) Then
                    Dim forcedCodec As FileCodec = If(IsCodecSupported(FileCodec.Pcm), FileCodec.Pcm, FileCodec.Wave)
                    writer.WriteStartElement("res")
                    writer.WriteAttributeString("protocolInfo", String.Format("http-get:*:{0}:{1}", GetMime(forcedCodec, streamingProfile.TranscodeBitDepth), GetEncodeFeature(forcedCodec, True)))
                    Dim sampleRate As Integer = If(streamingProfile.TranscodeSampleRate = -1, If(44100 < streamingProfile.MinimumSampleRate, streamingProfile.MinimumSampleRate, 44100), streamingProfile.TranscodeSampleRate)
                    If filterSet Is Nothing OrElse filterSet.Contains("res@sampleFrequency") Then
                        writer.WriteAttributeString("sampleFrequency", sampleRate.ToString())
                    End If
                    If filterSet Is Nothing OrElse filterSet.Contains("res@bitsPerSample") Then
                        writer.WriteAttributeString("bitsPerSample", "16")
                    End If
                    If filterSet Is Nothing OrElse filterSet.Contains("res@nrAudioChannels") Then
                        writer.WriteAttributeString("nrAudioChannels", "2")
                    End If
                    If filterSet Is Nothing OrElse filterSet.Contains("res@bitrate") Then
                        writer.WriteAttributeString("bitrate", ((sampleRate * 2 * 16) \ 1000).ToString())
                    End If
                    defaultHttpUrl = String.Format("{0}/encode/continuousstream{1}.{2}", hostUrl, streamHandle.ToString(), GetMime(forcedCodec, 16).Substring(6))
                    writer.WriteValue(defaultHttpUrl)
                    writer.WriteEndElement()
                End If
            Else
                Dim url As String = tags(MetaDataIndex.Url)
                If url.EndsWith(".asx", StringComparison.OrdinalIgnoreCase) Then
                    url = mbApiInterface.Library_GetFileTag(url, Plugin.MetaDataType.Origin)
                End If
                Dim sourceFileExtension As String
                Dim sourceFileCodec As FileCodec
                Dim id As String = GetFileId(url)
                Dim isVirtualFile As Boolean = url.EndsWith("#"c)
                Dim isWebFile As Boolean = (url.IndexOf("://", StringComparison.Ordinal) <> -1)
                If isVirtualFile Then
                    ' remove track from virtual url
                    url = url.Substring(0, url.LastIndexOf("#"c, url.Length - 2))
                End If
                Dim charIndex As Integer = url.LastIndexOf("."c)
                If charIndex = -1 Then
                    sourceFileExtension = ""
                Else
                    sourceFileExtension = url.Substring(charIndex).ToLower()
                End If
                sourceFileCodec = GetCodec(sourceFileExtension)
                Dim channelCount As Integer
                Integer.TryParse(tags(MetaDataIndex.Channels), channelCount)
                Dim sampleRate As Integer
                Integer.TryParse(tags(MetaDataIndex.SampleRate), sampleRate)
                If sourceFileCodec = FileCodec.Unknown AndAlso streamHandle <> 0 Then
                    Bass.TryGetStreamInformation(streamHandle, sampleRate, channelCount, sourceFileCodec)
                End If
                Dim fileHasTrackGain As Boolean = Not String.IsNullOrEmpty(mbApiInterface.Library_GetFileProperty(url, FilePropertyType.ReplayGainTrack))
                Dim fileHasAlbumGain As Boolean = Not String.IsNullOrEmpty(mbApiInterface.Library_GetFileProperty(url, FilePropertyType.ReplayGainAlbum))
                Dim mbSoundEffectsActive As Boolean = (mbApiInterface.Player_GetDspEnabled() OrElse mbApiInterface.Player_GetEqualiserEnabled())
                Dim mbReplayGainActive As Boolean = (mbApiInterface.Player_GetReplayGainMode() <> ReplayGainMode.Off AndAlso (fileHasTrackGain OrElse fileHasAlbumGain))
                Dim forceEncode As Boolean
                If musicBeePlayToMode Then
                    ' force encoding so an equaliser/ DSP can be added mid-stream?
                    forceEncode = (mbSoundEffectsActive OrElse mbReplayGainActive OrElse isWebFile)
                Else
                    forceEncode = ((Settings.ServerEnableSoundEffects AndAlso mbSoundEffectsActive) OrElse (Settings.ServerReplayGainMode <> ReplayGainMode.Off AndAlso mbReplayGainActive))
                End If
                If isVirtualFile Then
                    forceEncode = True
                End If
                If sampleRate < streamingProfile.MinimumSampleRate Then
                    forceEncode = True
                    sampleRate = streamingProfile.MinimumSampleRate
                ElseIf sampleRate > streamingProfile.MaximumSampleRate Then
                    forceEncode = True
                    sampleRate = streamingProfile.MaximumSampleRate
                End If
                If streamingProfile.StereoOnly AndAlso channelCount <> 2 Then
                    forceEncode = True
                    channelCount = 2
                End If
                Dim forcedCodec As FileCodec = FileCodec.Unknown
                If forceEncode OrElse Not IsCodecSupported(sourceFileCodec) Then
                    forcedCodec = streamingProfile.TranscodeCodec
                ElseIf Settings.BandwidthConstrained AndAlso (".flac;.wv;.tak;.wav;.aiff;.pcm;".IndexOf(sourceFileExtension) <> -1 OrElse (String.Compare(sourceFileExtension, ".m4a", StringComparison.OrdinalIgnoreCase) = 0 AndAlso mbApiInterface.Library_GetFileProperty(url, FilePropertyType.Kind).StartsWith("ALAC ", StringComparison.OrdinalIgnoreCase))) Then
                    forceEncode = True
                    forcedCodec = streamingProfile.TranscodeCodec
                End If
                If forcedCodec = FileCodec.Pcm Then
                    If musicBeePlayToMode Then
                        forcedCodec = If(Not IsCodecSupported(FileCodec.Wave) OrElse (IsCodecSupported(FileCodec.Pcm) AndAlso tags(MetaDataIndex.Size) = "0"), FileCodec.Pcm, FileCodec.Wave)
                    End If
                End If
                'If musicBeePlayToMode Then
                '    LogInformation("Item", "transcode=" & forceEncode & " to=" & forcedCodec.ToString() & ",rgmode=" & mbApiInterface.Player_GetReplayGainMode().ToString() & ",gain=" & (fileHasTrackGain OrElse fileHasAlbumGain) & ",eq/dsp=" & mbSoundEffectsActive & ",samplerate=" & sampleRate & ",dev min=" & streamingProfile.MinimumSampleRate & ",dev max=" & streamingProfile.MaximumSampleRate & ",chans=" & channelCount & ",dev stereo=" & streamingProfile.StereoOnly & ",codec=" & sourceFileCodec.ToString & ",sup=" & IsCodecSupported(sourceFileCodec) & ",bw=" & Settings.BandwidthConstrained)
                'End If
                If forcedCodec = FileCodec.Unknown AndAlso streamHandle <> 0 Then
                    Bass.CloseStream(streamHandle)
                    streamHandle = 0
                End If
                writer.WriteAttributeString("id", id)
                writer.WriteAttributeString("restricted", "true")
                writer.WriteAttributeString("parentID", parentId)
                writer.WriteElementString("upnp", "class", Nothing, classType)
                writer.WriteElementString("dc", "title", Nothing, tags(MetaDataIndex.Title))
                If filterSet Is Nothing OrElse filterSet.Contains("dc:date") Then
                    Dim year As String = tags(MetaDataIndex.Year)
                    If year.Length = 4 Then
                        writer.WriteElementString("dc", "date", Nothing, year & "-01-01")
                    ElseIf year.Length > 4 Then
                        Dim yearValue As DateTime
                        If DateTime.TryParse(year, yearValue) Then
                            writer.WriteElementString("dc", "date", Nothing, yearValue.ToString("yyyy-MM-dd"))
                        End If
                    End If
                End If
                Dim displayArtist As String = tags(MetaDataIndex.Artist)
                If displayArtist.Length > 0 Then
                    If filterSet Is Nothing OrElse filterSet.Contains("dc:creator") Then
                        writer.WriteElementString("dc", "creator", Nothing, displayArtist)
                    End If
                    If filterSet Is Nothing OrElse filterSet.Contains("upnp:artist") Then
                        writer.WriteElementString("upnp", "artist", Nothing, displayArtist)
                        Dim artists() As String = tags(MetaDataIndex.ArtistPeople).Split(ChrW(0))
                        For index As Integer = 0 To artists.Length - 1
                            Dim artist As String = artists(index)
                            If artist.Length > 0 Then
                                Dim role As String = "Performer"
                                If artist.Chars(0) < " "c Then
                                    If AscW(artist.Chars(0)) = 4 Then
                                        role = "Remixer"
                                    End If
                                    artist = artist.Substring(1)
                                End If
                                If String.Compare(artist, displayArtist, StringComparison.OrdinalIgnoreCase) <> 0 Then
                                    writer.WriteStartElement("upnp", "artist", Nothing)
                                    writer.WriteAttributeString("role", role)
                                    writer.WriteValue(artist)
                                    writer.WriteEndElement()
                                End If
                            End If
                        Next index
                        Dim composer As String = tags(MetaDataIndex.Composer)
                        If composer.Length > 0 Then
                            writer.WriteStartElement("upnp", "author", Nothing)
                            writer.WriteAttributeString("role", "Composer")
                            writer.WriteValue(composer)
                            writer.WriteEndElement()
                        End If
                        Dim conductor As String = tags(MetaDataIndex.Conductor)
                        If conductor.Length > 0 Then
                            writer.WriteStartElement("upnp", "artist", Nothing)
                            writer.WriteAttributeString("role", "Conductor")
                            writer.WriteValue(conductor)
                            writer.WriteEndElement()
                        End If
                        writer.WriteStartElement("upnp", "artist", Nothing)
                        writer.WriteAttributeString("role", "AlbumArtist")
                        writer.WriteValue(tags(MetaDataIndex.AlbumArtist))
                        writer.WriteEndElement()
                        writer.WriteElementString("upnp", "albumArtist", Nothing, tags(MetaDataIndex.AlbumArtist))
                    End If
                End If
                Dim album As String = tags(MetaDataIndex.Album)
                If album.Length > 0 AndAlso (filterSet Is Nothing OrElse filterSet.Contains("upnp:album")) Then
                    writer.WriteElementString("upnp", "album", Nothing, album)
                End If
                Dim trackNumber As String = tags(MetaDataIndex.TrackNo)
                If trackNumber.Length > 0 AndAlso (filterSet Is Nothing OrElse filterSet.Contains("upnp:originalTrackNumber")) Then
                    writer.WriteElementString("upnp", "originalTrackNumber", Nothing, trackNumber)
                End If
                Dim discNumber As String = tags(MetaDataIndex.DiscNo)
                If discNumber.Length > 0 AndAlso (filterSet Is Nothing OrElse filterSet.Contains("upnp:originalDiscNumber")) Then
                    writer.WriteElementString("upnp", "originalDiscNumber", Nothing, discNumber)
                End If
                Dim discCount As String = tags(MetaDataIndex.DiscCount)
                If discCount.Length > 0 AndAlso (filterSet Is Nothing OrElse filterSet.Contains("upnp:originalDiscCount")) Then
                    writer.WriteElementString("upnp", "originalDiscCount", Nothing, discCount)
                End If
                Dim publisher As String = tags(MetaDataIndex.Publisher)
                If publisher.Length > 0 AndAlso (filterSet Is Nothing OrElse filterSet.Contains("upnp:publisher")) Then
                    writer.WriteElementString("upnp", "publisher", Nothing, publisher)
                End If
                Dim genre As String = tags(MetaDataIndex.Genre)
                If genre.Length > 0 AndAlso (filterSet Is Nothing OrElse filterSet.Contains("upnp:genre")) Then
                    writer.WriteElementString("upnp", "genre", Nothing, genre)
                End If
                Dim rating As Double
                If Double.TryParse(tags(MetaDataIndex.Rating), rating) AndAlso rating >= 0 AndAlso (filterSet Is Nothing OrElse filterSet.Contains("upnp:rating")) Then
                    writer.WriteElementString("upnp", "rating", Nothing, CInt(If(rating = 0, 1, rating * 20)).ToString())
                End If
                Dim dateAdded As String = tags(MetaDataIndex.DateAdded)
                If dateAdded <> "0" AndAlso (filterSet Is Nothing OrElse filterSet.Contains("pv:addedTime")) Then
                    Dim dateAddedValue As New DateTime(CLng(dateAdded))
                    writer.WriteElementString("pv", "addedTime", Nothing, dateAddedValue.ToString("yyyy-MM-dd'T'hh':'mm':'ss"))
                End If
                Dim dateLastPlayed As String = tags(MetaDataIndex.DateLastPlayed)
                If dateLastPlayed <> "0" AndAlso (filterSet Is Nothing OrElse filterSet.Contains("pv:lastPlayedTime")) Then
                    Dim dateLastPlayedValue As New DateTime(CLng(dateLastPlayed))
                    writer.WriteElementString("pv", "lastPlayedTime", Nothing, dateLastPlayedValue.ToString("yyyy-MM-dd'T'hh':'mm':'ss"))
                End If
                Dim playCount As String = tags(MetaDataIndex.PlayCount)
                If playCount <> "0" Then
                    If filterSet Is Nothing OrElse filterSet.Contains("pv:playcount") Then
                        writer.WriteElementString("pv", "playcount", Nothing, playCount)
                    End If
                    If filterSet Is Nothing OrElse filterSet.Contains("upnp:playbackCount") Then
                        writer.WriteElementString("upnp", "playbackCount", Nothing, playCount)
                    End If
                End If
                If (filterSet Is Nothing OrElse filterSet.Contains("upnp:albumArtURI")) AndAlso streamingProfile.PictureSize > 0 Then
                    If mbApiInterface.Library_GetArtworkUrl(url, -2) <> "0" Then
                        For Each size As String In New String() {"JPEG_TN", "JPEG_SM"}
                            If size = "JPEG_TN" OrElse streamingProfile.PictureSize = 160 Then
                                writer.WriteStartElement("upnp", "albumArtURI", Nothing)
                                writer.WriteAttributeString("dlna", "profileID", "urn:schemas-dlna-org:metadata-1-0/", size)
                                writer.WriteValue(String.Format("{0}/thumbnail/{1}.{2}", hostUrl, id, If(size = "JPEG_TN" AndAlso streamingProfile.PictureSize <> 160, streamingProfile.PictureSize.ToString("0000000"), size)))
                                writer.WriteEndElement()
                            End If
                        Next size
                    End If
                End If
                If filterSet Is Nothing OrElse filterSet.Any(Function(a) a.StartsWith("res")) Then
                    Dim duration As TimeSpan
                    Dim durationValue As Long
                    If Long.TryParse(tags(MetaDataIndex.Duration), durationValue) Then
                        duration = New TimeSpan(durationValue)
                    End If
                    If duration.Ticks = 0 AndAlso streamHandle <> 0 Then
                        Dim fileDuration As Double = Bass.GetDecodedDuration(streamHandle)
                        If fileDuration > 0 Then
                            ' some files (eg. aac files might not have a cached duration)
                            duration = New TimeSpan(CLng(fileDuration * TimeSpan.TicksPerSecond))
                        End If
                    End If
                    If forcedCodec = FileCodec.Unknown OrElse (forcedCodec = sourceFileCodec AndAlso Not forceEncode) Then
                        writer.WriteStartElement("res")
                        Dim mime As String = GetMime(sourceFileCodec, 16)
                        writer.WriteAttributeString("protocolInfo", String.Format("http-get:*:{0}:{1}", mime, GetFileFeature(sourceFileCodec, (duration.Ticks <= 0))))
                        If duration.Ticks > 0 Then
                            If filterSet Is Nothing OrElse filterSet.Contains("res@size") Then
                                writer.WriteAttributeString("size", tags(MetaDataIndex.Size))
                            End If
                            If filterSet Is Nothing OrElse filterSet.Contains("res@duration") Then
                                writer.WriteAttributeString("duration", duration.ToString("h':'mm':'ss"))
                            End If
                        End If
                        If filterSet Is Nothing OrElse filterSet.Contains("res@bitrate") Then
                            Dim bitRate As String = tags(MetaDataIndex.Bitrate)
                            Dim bitrateValue As Integer
                            If Integer.TryParse(bitRate, bitrateValue) Then
                                writer.WriteAttributeString("bitrate", ((bitrateValue * 1000) \ 8).ToString())
                            End If
                        End If
                        If filterSet Is Nothing OrElse filterSet.Contains("res@sampleFrequency") Then
                            writer.WriteAttributeString("sampleFrequency", tags(MetaDataIndex.SampleRate))
                        End If
                        If filterSet Is Nothing OrElse filterSet.Contains("res@nrAudioChannels") Then
                            writer.WriteAttributeString("nrAudioChannels", tags(MetaDataIndex.Channels))
                        End If
                        If isWebFile AndAlso Not musicBeePlayToMode Then
                            defaultHttpUrl = url
                        Else
                            defaultHttpUrl = String.Format("{0}/files/{1}.{2}", hostUrl, If(Not musicBeePlayToMode, id, id & "p"), mime.Substring(6))
                        End If
                        writer.WriteValue(defaultHttpUrl)
                        writer.WriteEndElement()
                    End If
                    Dim encodeSampleRate As Integer = If(streamingProfile.TranscodeSampleRate = -1, sampleRate, streamingProfile.TranscodeSampleRate)
                    For Each encoder As AudioEncoder In encodeSettings.Audio.Encoders
                        If (encoder.Codec = forcedCodec AndAlso (forcedCodec <> sourceFileCodec OrElse forceEncode)) OrElse (forcedCodec = FileCodec.Unknown AndAlso (((encoder.Codec = FileCodec.Pcm OrElse encoder.Codec = FileCodec.Wave) AndAlso Not Settings.BandwidthConstrained))) Then
                            writer.WriteStartElement("res")
                            writer.WriteAttributeString("protocolInfo", String.Format("http-get:*:{0}:{1}", GetMime(encoder.Codec, streamingProfile.TranscodeBitDepth), GetEncodeFeature(encoder.Codec, (duration.Ticks <= 0))))
                            Dim isPcmData As Boolean = (encoder.Codec = FileCodec.Pcm OrElse encoder.Codec = FileCodec.Wave)
                            If duration.Ticks > 0 Then
                                If filterSet Is Nothing OrElse filterSet.Contains("res@duration") Then
                                    writer.WriteAttributeString("duration", duration.ToString("h':'mm':'ss"))
                                End If
                                If streamHandle <> 0 AndAlso isPcmData AndAlso (filterSet Is Nothing OrElse filterSet.Contains("res@size")) Then
                                    Dim streamSampleRate As Integer
                                    Dim streamChannelCount As Integer
                                    Dim streamCodec As FileCodec
                                    If Bass.TryGetStreamInformation(streamHandle, streamSampleRate, streamChannelCount, streamCodec) Then
                                        Dim fileLength As Long = Bass.GetDecodedLength(streamHandle, duration.Ticks / TimeSpan.TicksPerSecond)
                                        If fileLength > 0 Then
                                            fileLength = (fileLength * channelCount) \ streamChannelCount
                                            fileLength = (fileLength * encodeSampleRate) \ streamSampleRate
                                            If streamingProfile.TranscodeBitDepth <> 24 Then
                                                fileLength \= 2
                                            Else
                                                fileLength = (fileLength * 3) \ 4
                                            End If
                                            If encoder.Codec = FileCodec.Wave Then
                                                fileLength += 44
                                            End If
                                            writer.WriteAttributeString("size", fileLength.ToString())
                                        End If
                                    End If
                                End If
                            End If
                            If filterSet Is Nothing OrElse filterSet.Contains("res@sampleFrequency") Then
                                writer.WriteAttributeString("sampleFrequency", encodeSampleRate.ToString())
                            End If
                            If filterSet Is Nothing OrElse filterSet.Contains("res@bitsPerSample") Then
                                writer.WriteAttributeString("bitsPerSample", If(Not isPcmData, "16", streamingProfile.TranscodeBitDepth.ToString()))
                            End If
                            If filterSet Is Nothing OrElse filterSet.Contains("res@nrAudioChannels") Then
                                writer.WriteAttributeString("nrAudioChannels", If(Not isPcmData, "2", channelCount.ToString()))
                            End If
                            If isPcmData AndAlso (filterSet Is Nothing OrElse filterSet.Contains("res@bitrate")) Then
                                writer.WriteAttributeString("bitrate", ((encodeSampleRate * channelCount * streamingProfile.TranscodeBitDepth) \ 8).ToString())
                            End If
                            Dim httpUrl As String = String.Format("{0}/encode/{1}{2}.{3}", hostUrl, id, streamHandle.ToString(), GetMime(encoder.Codec, streamingProfile.TranscodeBitDepth).Substring(6))
                            If defaultHttpUrl Is Nothing Then
                                defaultHttpUrl = httpUrl
                            End If
                            writer.WriteValue(httpUrl)
                            writer.WriteEndElement()
                        End If
                    Next encoder
                End If
            End If
            writer.WriteEndElement()
            Return defaultHttpUrl
        End Function

        Private Function IsCodecSupported(codec As FileCodec) As Boolean
            If codec = FileCodec.Unknown Then
                Return False
            ElseIf SupportedMimeTypes Is Nothing Then
                Return True
            Else
                Dim mimeTypes() As String = GetMimes(codec)
                For index As Integer = 0 To SupportedMimeTypes.Length - 1
                    For index2 As Integer = 0 To mimeTypes.Length - 1
                        If SupportedMimeTypes(index).StartsWith(mimeTypes(index2), StringComparison.OrdinalIgnoreCase) Then
                            Return True
                        End If
                    Next index2
                Next index
                Return False
            End If
        End Function

        Public Function GetCodec(extension As String) As FileCodec
            If String.IsNullOrEmpty(extension) Then
                Return FileCodec.Unknown
            Else
                Select Case extension
                    Case ".mp3"
                        Return FileCodec.Mp3
                    Case ".m4a", ".m4b", ".mp2", ".mp4"
                        Return FileCodec.Aac
                    Case ".aac"
                        Return FileCodec.AacNoContainer
                    Case ".wma"
                        Return FileCodec.Wma
                    Case ".opus"
                        Return FileCodec.Opus
                    Case ".ogg", ".oga"
                        Return FileCodec.Ogg
                    Case ".spx"
                        Return FileCodec.Spx
                    Case ".flac"
                        Return FileCodec.Flac
                    Case ".wv"
                        Return FileCodec.WavPack
                    Case ".tak"
                        Return FileCodec.Tak
                    Case ".mpc", ".mp+", ".mpp"
                        Return FileCodec.Mpc
                    Case ".wav"
                        Return FileCodec.Wave
                    Case ".aiff"
                        Return FileCodec.Aiff
                    Case ".pcm"
                        Return FileCodec.Pcm
                    Case Else
                        Return FileCodec.Unknown
                End Select
            End If
        End Function

        'Private Function GetExtension(codec As FileCodec) As String
        '    Select Case codec
        '        Case FileCodec.Mp3
        '            Return ".mp3"
        '        Case FileCodec.Aac, FileCodec.Alac
        '            Return ".m4a"
        '        Case FileCodec.AacNoContainer
        '            Return ".aac"
        '        Case FileCodec.Wma
        '            Return ".wma"
        '        Case FileCodec.Ogg
        '            Return ".ogg"
        '        Case FileCodec.Flac
        '            Return ".flac"
        '        Case FileCodec.WavPack
        '            Return ".wv"
        '        Case FileCodec.Wave
        '            Return ".wav"
        '        Case FileCodec.Tak
        '            Return ".tak"
        '        Case FileCodec.Mpc
        '            Return ".mpc"
        '        Case FileCodec.Aiff
        '            Return ".aiff"
        '        Case Else
        '            Return ".pcm"
        '    End Select
        'End Function

        Private Function GetMime(codec As FileCodec, bitDepth As Integer) As String
            Dim mimes() As String = GetMimes(codec)
            If codec = FileCodec.Pcm Then
                mimes = New String() {mimes(If(bitDepth <> 24, 0, 1))}
            End If
            If SupportedMimeTypes Is Nothing Then
                If mimes.Length > 0 Then
                    Return mimes(0)
                End If
                Return ""
            Else
                For index As Integer = 0 To mimes.Length - 1
                    For index2 As Integer = 0 To SupportedMimeTypes.Length - 1
                        If SupportedMimeTypes(index2).StartsWith(mimes(index), StringComparison.OrdinalIgnoreCase) Then
                            Return mimes(index)
                        End If
                    Next index2
                Next index
                Select Case codec
                    Case FileCodec.Wave
                        Return "audio/wav"
                    Case FileCodec.Pcm
                        Return "audio/L" & bitDepth
                    Case Else
                        Return ""
                End Select
            End If
        End Function

        Private Function GetMimes(codec As FileCodec) As String()
            Select Case codec
                Case FileCodec.Mp3
                    Return New String() {"audio/mpeg", "audio/mp3", "audio/x-mp3"}
                Case FileCodec.Aac, FileCodec.Alac
                    Return New String() {"audio/m4a", "audio/mp4"}
                Case FileCodec.AacNoContainer
                    Return New String() {"audio/aac", "audio/x-aac"}
                Case FileCodec.Wma
                    Return New String() {"audio/x-ms-wma", "audio/wma"}
                Case FileCodec.Ogg
                    Return New String() {"audio/x-ogg", "audio/ogg", "application/ogg"}
                Case FileCodec.Flac
                    Return New String() {"audio/x-flac", "audio/flac"}
                Case FileCodec.WavPack
                    Return New String() {"audio/x-wavpack", "audio/wavpack"}
                Case FileCodec.Wave
                    Return New String() {"audio/wav", "audio/x-wav"}
                Case FileCodec.Tak
                    Return New String() {"audio/x-tak", "audio/tak"}
                Case FileCodec.Mpc
                    Return New String() {"audio/x-musepack", "audio/musepack"}
                Case FileCodec.Aiff
                    Return New String() {"audio/x-aiff", "audio/aiff"}
                Case FileCodec.Pcm
                    Return New String() {"audio/L16", "audio/L24"}
                Case Else
                    Return New String() {}
            End Select
        End Function

        Private Function GetDlnaType(codec As FileCodec) As String
            Select Case codec
                Case FileCodec.Mp3
                    Return "DLNA.ORG_PN=MP3;"
                Case FileCodec.Aac, FileCodec.AacNoContainer
                    Return "DLNA.ORG_PN=AAC_ISO;"
                Case FileCodec.Wma
                    Return "DLNA.ORG_PN=WMABASE;"
                Case FileCodec.Pcm
                    Return "DLNA.ORG_PN=LPCM;"
                Case Else
                    Return String.Empty
            End Select
        End Function

        Public Function GetFileFeature(url As String, disableSeek As Boolean) As String
            Return GetFileFeature(GetCodec(url), disableSeek)
        End Function

        Private Function GetFileFeature(codec As FileCodec, disableSeek As Boolean) As String
            Return GetDlnaType(codec) & If(disableSeek, "DLNA.ORG_OP=00;", If(DisablePcmTimeSeek, "DLNA.ORG_OP=01;", "DLNA.ORG_OP=11;")) & "DLNA.ORG_CI=0;DLNA.ORG_FLAGS=01700000000000000000000000000000"
        End Function

        Public Function GetEncodeFeature(codec As FileCodec, disableSeek As Boolean) As String
            'DLNA.ORG_CI=1;
            Return GetDlnaType(codec) & If(disableSeek, "DLNA.ORG_OP=00;", If(codec = FileCodec.Pcm OrElse codec = FileCodec.Wave, If(DisablePcmTimeSeek, "DLNA.ORG_OP=01;", "DLNA.ORG_OP=11;"), "DLNA.ORG_OP=10;")) & "DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01700000000000000000000000000000"
        End Function

        Public Function GetContinuousStreamFeature(codec As FileCodec) As String
            Return GetDlnaType(codec) & "DLNA.ORG_OP=00;DLNA.ORG_CI=1;DLNA.ORG_FLAGS=01700000000000000000000000000000"
        End Function

        Private Shared Function GetFileId(url As String) As String
            Return (Hex(StringComparer.OrdinalIgnoreCase.GetHashCode(url)) & Hex(StringComparer.OrdinalIgnoreCase.GetHashCode(IO.Path.GetFileName(url)))).PadLeft(16, "0"c)
        End Function

        Private Shared Function GetBucket(name As String) As Char
            If name.Length = 0 Then
                Return "#"c
            ElseIf Not Char.IsLetter(name.Chars(0)) Then
                Return "#"c
            Else
                Return Char.ToUpper(name.Chars(0))
            End If
        End Function

        Private Shared Function GetNameBucket(name As String) As Char
            If name.Length = 0 Then
                Return "#"c
            ElseIf Not Char.IsLetter(name.Chars(0)) Then
                Return "#"c
            Else
                Return Char.ToUpper(name.Chars(0))
            End If
        End Function

        Private NotInheritable Class AlbumFileComparer
            Inherits Comparer(Of String())
            Public Overrides Function Compare(tags1() As String, tags2() As String) As Integer
                Dim result As Integer
                result = String.Compare(tags1(MetaDataIndex.Album), tags2(MetaDataIndex.Album), StringComparison.OrdinalIgnoreCase)
                If result <> 0 Then
                    Return result
                Else
                    result = String.Compare(tags1(MetaDataIndex.AlbumArtist), tags2(MetaDataIndex.AlbumArtist), StringComparison.OrdinalIgnoreCase)
                    If result <> 0 Then
                        Return result
                    Else
                        Dim discNo1 As Integer
                        If Not Integer.TryParse(tags1(MetaDataIndex.DiscNo), discNo1) Then
                            discNo1 = -1
                        End If
                        If discNo1 < 0 Then
                            result = String.Compare(tags1(MetaDataIndex.DiscNo), tags2(MetaDataIndex.DiscNo), StringComparison.Ordinal)
                            If result <> 0 Then
                                Return result
                            End If
                        End If
                        Dim discNo2 As Integer
                        If Not Integer.TryParse(tags2(MetaDataIndex.DiscNo), discNo2) Then
                            discNo2 = -1
                        End If
                        If discNo2 < 0 AndAlso discNo1 >= 0 Then
                            result = String.Compare(tags1(MetaDataIndex.DiscNo), tags2(MetaDataIndex.DiscNo), StringComparison.OrdinalIgnoreCase)
                            If result <> 0 Then
                                Return result
                            End If
                        End If
                        If discNo1 <> discNo2 Then
                            Return discNo1 - discNo2
                        End If
                        Dim trackNo1 As Integer
                        Dim trackNo2 As Integer
                        If Not Integer.TryParse(tags1(MetaDataIndex.TrackNo), trackNo1) Then
                            trackNo1 = -1
                        End If
                        If Not Integer.TryParse(tags2(MetaDataIndex.TrackNo), trackNo2) Then
                            trackNo2 = -1
                        End If
                        If trackNo1 > 0 AndAlso trackNo2 > 0 Then
                            Return trackNo1 - trackNo2
                        End If
                        If trackNo1 = trackNo2 Then
                            If trackNo1 <> 0 Then
                                Return 0
                            Else
                                Return String.Compare(tags1(MetaDataIndex.TrackNo), tags2(MetaDataIndex.TrackNo), StringComparison.Ordinal)
                            End If
                        ElseIf trackNo1 = 0 Then
                            Return -1
                        ElseIf trackNo2 = 0 Then
                            Return 1
                        Else
                            Return If(trackNo1 < trackNo2, -1, 1)
                        End If
                    End If
                End If
            End Function
        End Class  ' AlbumFileComparer

        Private NotInheritable Class TrackNameFileComparer
            Inherits Comparer(Of String())
            Public Overrides Function Compare(tags1() As String, tags2() As String) As Integer
                Return String.Compare(tags1(MetaDataIndex.Title), tags2(MetaDataIndex.Title), StringComparison.OrdinalIgnoreCase)
            End Function
        End Class  ' TrackNameFileComparer

        Private NotInheritable Class FolderNameComparer
            Inherits Comparer(Of FolderNode)
            Public Overrides Function Compare(node1 As FolderNode, node2 As FolderNode) As Integer
                Return String.Compare(node1.Name, node2.Name, StringComparison.OrdinalIgnoreCase)
            End Function
        End Class  ' FolderNameComparer

        Private NotInheritable Class FolderPrefixedNameComparer
            Inherits Comparer(Of FolderNode)
            Public Overrides Function Compare(node1 As FolderNode, node2 As FolderNode) As Integer
                Dim name1Changed As Boolean = False
                Dim name2Changed As Boolean = False
                Dim result As Integer = String.Compare(node1.Name, node2.Name, StringComparison.OrdinalIgnoreCase)
                If result <> 0 OrElse Not (name1Changed Xor name2Changed) Then
                    Return result
                Else
                    Return If(name1Changed, 1, -1)
                End If
            End Function
        End Class  ' FolderPrefixedNameComparer

        Private Enum MetaDataType As Byte
            Id = 1
            Url = 2
            FileKind = 4
            FileSize = 7
            Channels = 8
            SampleRate = 9
            Bitrate = 10
            DateModified = 11
            DateAdded = 12
            DateLastPlayed = 13
            PlayCount = 14
            DiscNo = 52
            DiscCount = 54
            Duration = 16
            Category = 42
            TrackTitle = 65
            Name = 65
            Album = 30
            Artist = 32
            ArtistPeople = 33
            AlbumArtist = 31
            Composer = 43
            Conductor = 45
            Genre = 59
            Publisher = 73
            Rating = 75
            TrackNo = 86
            TrackCount = 87
            Year = 88
            YearOnly = 35
            ReplayGainTrack = 94
            ReplayGainAlbum = 95
        End Enum  ' MetaDataType

        Private Enum MetaDataIndex
            None = -1
            Url = 0
            Category = 1
            Artist = 2
            ArtistPeople = 3
            AlbumArtist = 4
            Composer = 5
            Conductor = 6
            Title = 7
            Album = 8
            TrackNo = 9
            DiscNo = 10
            DiscCount = 11
            Year = 12
            Genre = 13
            Publisher = 14
            Rating = 15
            Duration = 16
            Size = 17
            Bitrate = 18
            SampleRate = 19
            Channels = 20
            DateAdded = 21
            PlayCount = 22
            DateLastPlayed = 23
            ReplayGainTrack = 24
            AlbumArtistAndAlbum = 25
        End Enum  ' MetaDataIndex

        Private Enum MediaType
            Image
            Audio
            Video
            Playlist
            Other
        End Enum  ' MediaType

        Private Class TemplateNode
            Public Path As String
            Public ParentId As String
            Public Id As String
            Public Name As String
            Public ContainerClass As String
            Public IncludeAllTracks As Boolean = False
            Public Fields() As MetaDataIndex
            Public Category As ContainerCategory = ContainerCategory.Music
            Public ChildNodes() As TemplateNode
            Public Sub New(path As String, parentId As String, id As String, name As String, containerClass As String, fields() As MetaDataIndex)
                Me.Path = path
                Me.ParentId = parentId
                Me.Id = id
                Me.Name = name
                Me.ContainerClass = containerClass
                Me.Fields = fields
            End Sub
        End Class  ' TemplateNode

        Private Class PlaylistFolderNode
            Public Path As String
            Public Name As String
            Public Folders As New List(Of PlaylistFolderNode)
            Public Sub New()
            End Sub
            Public Sub New(name As String)
                Me.Name = name
            End Sub
        End Class  ' PlaylistFolderNode

        Private Structure FolderNode
            Public Path As String
            Public Name As String
            Public Folders() As FolderNode
            Public ChildFiles As List(Of String())
            Public IsBucket As Boolean
            Public Sub New(name As String)
                Me.Name = name
            End Sub
            Public Sub New(name As String, files As List(Of String()))
                Me.Name = name
                Me.ChildFiles = files
            End Sub
            Public Sub New(name As String, folders() As FolderNode, files As List(Of String()))
                Me.Name = name
                Me.Folders = folders
                Me.ChildFiles = files
            End Sub
        End Structure  ' FolderNode

        Private Enum ContainerCategory
            Music = 0
            Audiobook = 1
            Inbox = 2
            Radio = 3
            Podcast = 4
            Playlist = 5
        End Enum  ' ContainerCategory

        Private Class MediaSettings
            Public ReadOnly Audio As MediaSettings

            Public Sub New()
                Audio = New MediaSettings(New AudioEncoder(FileCodec.Pcm), New AudioEncoder(FileCodec.Wave), New AudioEncoder(FileCodec.Mp3), New AudioEncoder(FileCodec.Aac), New AudioEncoder(FileCodec.Ogg))
            End Sub

            Public Class MediaSettings
                Public ReadOnly Encoders As ReadOnlyCollection(Of Encoder)

                Public Sub New(ParamArray encoder() As Encoder)
                    Me.Encoders = New ReadOnlyCollection(Of Encoder)(encoder)
                End Sub
            End Class  ' MediaSettingsImage
        End Class  ' MediaSettings
    End Class  ' ItemManager
End Class