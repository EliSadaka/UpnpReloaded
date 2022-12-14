Imports System.Runtime.InteropServices

Partial Public Class Plugin
    Public Const PluginInfoVersion As Short = 1
    Public Const MinInterfaceVersion As Short = 41
    Public Const MinApiRevision As Short = 53

    <StructLayout(LayoutKind.Sequential)>
    Public Structure MusicBeeApiInterface
        Public Shared Sub Initialise(ByRef mbApiInterface As MusicBeeApiInterface, apiInterfacePtr As IntPtr)
            CopyMemory(mbApiInterface, apiInterfacePtr, 4)
            If mbApiInterface.MusicBeeVersion = MusicBeeVersion.v2_0 Then
                ' MusicBee version 2.0 - Api methods > revision 25 are not available
                CopyMemory(mbApiInterface, apiInterfacePtr, 456)
            ElseIf mbApiInterface.MusicBeeVersion = MusicBeeVersion.v2_1 Then
                CopyMemory(mbApiInterface, apiInterfacePtr, 516)
            ElseIf mbApiInterface.MusicBeeVersion = MusicBeeVersion.v2_2 Then
                CopyMemory(mbApiInterface, apiInterfacePtr, 584)
            ElseIf mbApiInterface.MusicBeeVersion = MusicBeeVersion.v2_3 Then
                CopyMemory(mbApiInterface, apiInterfacePtr, 596)
            ElseIf mbApiInterface.MusicBeeVersion = MusicBeeVersion.v2_4 Then
                CopyMemory(mbApiInterface, apiInterfacePtr, 604)
            ElseIf mbApiInterface.MusicBeeVersion = MusicBeeVersion.v2_5 Then
                CopyMemory(mbApiInterface, apiInterfacePtr, 648)
            ElseIf mbApiInterface.MusicBeeVersion = MusicBeeVersion.v3_0 Then
                CopyMemory(mbApiInterface, apiInterfacePtr, 652)
            Else
                CopyMemory(mbApiInterface, apiInterfacePtr, Marshal.SizeOf(mbApiInterface))
            End If
        End Sub

        Public ReadOnly Property MusicBeeVersion() As MusicBeeVersion
            Get
                If ApiRevision <= 25 Then
                    Return MusicBeeVersion.v2_0
                ElseIf ApiRevision <= 30 Then
                    Return MusicBeeVersion.v2_1
                ElseIf ApiRevision <= 33 Then
                    Return MusicBeeVersion.v2_2
                ElseIf ApiRevision <= 38 Then
                    Return MusicBeeVersion.v2_3
                ElseIf ApiRevision <= 43 Then
                    Return MusicBeeVersion.v2_4
                ElseIf ApiRevision <= 47 Then
                    Return MusicBeeVersion.v2_5
                ElseIf ApiRevision <= 48 Then
                    Return MusicBeeVersion.v3_0
                Else
                    Return MusicBeeVersion.v3_1
                End If
            End Get
        End Property
        Public InterfaceVersion As Short
        Public ApiRevision As Short
        Public MB_ReleaseString As MB_ReleaseStringDelegate
        Public MB_Trace As MB_TraceDelegate
        Public Setting_GetPersistentStoragePath As Setting_GetPersistentStoragePathDelegate
        Public Setting_GetSkin As Setting_GetSkinDelegate
        Public Setting_GetSkinElementColour As Setting_GetSkinElementColourDelegate
        Public Setting_IsWindowBordersSkinned As Setting_IsWindowBordersSkinnedDelegate
        Public Library_GetFileProperty As Library_GetFilePropertyDelegate
        Public Library_GetFileTag As Library_GetFileTagDelegate
        Public Library_SetFileTag As Library_SetFileTagDelegate
        Public Library_CommitTagsToFile As Library_CommitTagsToFileDelegate
        Public Library_GetLyrics As Library_GetLyricsDelegate
        <Obsolete("Use Library_GetArtworkEx")>
        Public Library_GetArtwork As Library_GetArtworkDelegate
        Public Library_QueryFiles As Library_QueryFilesDelegate
        Public Library_QueryGetNextFile As Library_QueryGetNextFileDelegate
        Public Player_GetPosition As Player_GetPositionDelegate
        Public Player_SetPosition As Player_SetPositionDelegate
        Public Player_GetPlayState As Player_GetPlayStateDelegate
        Public Player_PlayPause As Player_ActionDelegate
        Public Player_Stop As Player_ActionDelegate
        Public Player_StopAfterCurrent As Player_ActionDelegate
        Public Player_PlayPreviousTrack As Player_ActionDelegate
        Public Player_PlayNextTrack As Player_ActionDelegate
        Public Player_StartAutoDj As Player_ActionDelegate
        Public Player_EndAutoDj As Player_ActionDelegate
        Public Player_GetVolume As Player_GetVolumeDelegate
        Public Player_SetVolume As Player_SetVolumeDelegate
        Public Player_GetMute As Player_GetMuteDelegate
        Public Player_SetMute As Player_SetMuteDelegate
        Public Player_GetShuffle As Player_GetShuffleDelegate
        Public Player_SetShuffle As Player_SetShuffleDelegate
        Public Player_GetRepeat As Player_GetRepeatDelegate
        Public Player_SetRepeat As Player_SetRepeatDelegate
        Public Player_GetEqualiserEnabled As Player_GetEqualiserEnabledDelegate
        Public Player_SetEqualiserEnabled As Player_SetEqualiserEnabledDelegate
        Public Player_GetDspEnabled As Player_GetDspEnabledDelegate
        Public Player_SetDspEnabled As Player_SetDspEnabledDelegate
        Public Player_GetScrobbleEnabled As Player_GetScrobbleEnabledDelegate
        Public Player_SetScrobbleEnabled As Player_SetScrobbleEnabledDelegate
        Public NowPlaying_GetFileUrl As NowPlaying_GetFileUrlDelegate
        Public NowPlaying_GetDuration As NowPlaying_GetDurationDelegate
        Public NowPlaying_GetFileProperty As NowPlaying_GetFilePropertyDelegate
        Public NowPlaying_GetFileTag As NowPlaying_GetFileTagDelegate
        Public NowPlaying_GetLyrics As NowPlaying_GetLyricsDelegate
        Public NowPlaying_GetArtwork As NowPlaying_GetArtworkDelegate
        Public NowPlayingList_Clear As NowPlayingList_ActionDelegate
        Public NowPlayingList_QueryFiles As Library_QueryFilesDelegate
        Public NowPlayingList_QueryGetNextFile As Library_QueryGetNextFileDelegate
        Public NowPlayingList_PlayNow As NowPlayingList_FileActionDelegate
        Public NowPlayingList_QueueNext As NowPlayingList_FileActionDelegate
        Public NowPlayingList_QueueLast As NowPlayingList_FileActionDelegate
        Public NowPlayingList_PlayLibraryShuffled As NowPlayingList_ActionDelegate
        Public Playlist_QueryPlaylists As Playlist_QueryPlaylistsDelegate
        Public Playlist_QueryGetNextPlaylist As Playlist_QueryGetNextPlaylistDelegate
        Public Playlist_GetType As Playlist_GetTypeDelegate
        Public Playlist_QueryFiles As Playlist_QueryFilesDelegate
        Public Playlist_QueryGetNextFile As Library_QueryGetNextFileDelegate
        Public MB_GetWindowHandle As MB_WindowHandleDelegate
        Public MB_RefreshPanels As MB_RefreshPanelsDelegate
        Public MB_SendNotification As MB_SendNotificationDelegate
        Public MB_AddMenuItem As MB_AddMenuItemDelegate
        Public Setting_GetFieldName As Setting_GetFieldNameDelegate
        <Obsolete("Use Library_QueryFilesEx", True)>
        Public Library_QueryGetAllFiles As Library_QueryGetAllFilesDelegate
        <Obsolete("Use NowPlayingList_QueryFilesEx", True)>
        Public NowPlayingList_QueryGetAllFiles As Library_QueryGetAllFilesDelegate
        <Obsolete("Use Playlist_QueryFilesEx", True)>
        Public Playlist_QueryGetAllFiles As Library_QueryGetAllFilesDelegate
        Public MB_CreateBackgroundTask As MB_CreateBackgroundTaskDelegate
        Public MB_SetBackgroundTaskMessage As MB_SetBackgroundTaskMessageDelegate
        Public MB_RegisterCommand As MB_RegisterCommandDelegate
        Public Setting_GetDefaultFont As Setting_GetDefaultFontDelegate
        Public Player_GetShowTimeRemaining As Player_GetShowTimeRemainingDelegate
        Public NowPlayingList_GetCurrentIndex As NowPlayingList_GetCurrentIndexDelegate
        Public NowPlayingList_GetListFileUrl As NowPlayingList_GetFileUrlDelegate
        Public NowPlayingList_GetFileProperty As NowPlayingList_GetFilePropertyDelegate
        Public NowPlayingList_GetFileTag As NowPlayingList_GetFileTagDelegate
        Public NowPlaying_GetSpectrumData As NowPlaying_GetSpectrumDataDelegate
        Public NowPlaying_GetSoundGraph As NowPlaying_GetSoundGraphDelegate
        Public MB_GetPanelBounds As MB_GetPanelBoundsDelegate
        Public MB_AddPanel As MB_AddPanelDelegate
        Public MB_RemovePanel As MB_RemovePanelDelegate
        Public MB_GetLocalisation As MB_GetLocalisationDelegate
        Public NowPlayingList_IsAnyPriorTracks As NowPlayingList_IsAnyPriorTracksDelegate
        Public NowPlayingList_IsAnyFollowingTracks As NowPlayingList_IsAnyFollowingTracksDelegate
        Public Player_ShowEqualiser As Player_ShowEqualiserDelegate
        Public Player_GetAutoDjEnabled As Player_GetAutoDjEnabledDelegate
        Public Player_GetStopAfterCurrentEnabled As Player_GetStopAfterCurrentEnabledDelegate
        Public Player_GetCrossfade As Player_GetCrossfadeDelegate
        Public Player_SetCrossfade As Player_SetCrossfadeDelegate
        Public Player_GetReplayGainMode As Player_GetReplayGainModeDelegate
        Public Player_SetReplayGainMode As Player_SetReplayGainModeDelegate
        Public Player_QueueRandomTracks As Player_QueueRandomTracksDelegate
        Public Setting_GetDataType As Setting_GetDataTypeDelegate
        Public NowPlayingList_GetNextIndex As NowPlayingList_GetNextIndexDelegate
        Public NowPlaying_GetArtistPicture As NowPlaying_GetArtistPictureDelegate
        Public NowPlaying_GetDownloadedArtwork As NowPlaying_GetArtworkDelegate
        ' api version 16
        Public MB_ShowNowPlayingAssistant As MB_ShowNowPlayingAssistantDelegate
        ' api version 17
        Public NowPlaying_GetDownloadedLyrics As NowPlaying_GetLyricsDelegate
        ' api version 18
        Public Player_GetShowRatingTrack As Player_GetShowRatingTrackDelegate
        Public Player_GetShowRatingLove As Player_GetShowRatingLoveDelegate
        ' api version 19
        Public MB_CreateParameterisedBackgroundTask As MB_CreateParameterisedBackgroundTaskDelegate
        Public Setting_GetLastFmUserId As Setting_GetLastFmUserIdDelegate
        Public Playlist_GetName As Playlist_GetNameDelegate
        Public Playlist_CreatePlaylist As Playlist_CreatePlaylistDelegate
        Public Playlist_SetFiles As Playlist_SetFilesDelegate
        Public Library_QuerySimilarArtists As Library_QuerySimilarArtistsDelegate
        Public Library_QueryLookupTable As Library_QueryLookupTableDelegate
        Public Library_QueryGetLookupTableValue As Library_QueryGetLookupTableValueDelegate
        Public NowPlayingList_QueueFilesNext As NowPlayingList_FilesActionDelegate
        Public NowPlayingList_QueueFilesLast As NowPlayingList_FilesActionDelegate
        ' api version 20
        Public Setting_GetWebProxy As Setting_GetWebProxyDelegate
        ' api version 21
        Public NowPlayingList_RemoveAt As NowPlayingList_RemoveAtDelegate
        ' api version 22
        Public Playlist_RemoveAt As Playlist_RemoveAtDelegate
        ' api version 23
        Public MB_SetPanelScrollableArea As MB_SetPanelScrollableAreaDelegate
        ' api version 24
        Public MB_InvokeCommand As MB_InvokeCommandDelegate
        Public MB_OpenFilterInTab As MB_OpenFilterInTabDelegate
        ' api version 25
        Public MB_SetWindowSize As MB_SetWindowSizeDelegate
        Public Library_GetArtistPicture As Library_GetArtistPictureDelegate
        Public Pending_GetFileUrl As Pending_GetFileUrlDelegate
        Public Pending_GetFileProperty As Pending_GetFilePropertyDelegate
        Public Pending_GetFileTag As Pending_GetFileTagDelegate
        ' api version 26
        Public Player_GetButtonEnabled As Player_GetButtonEnabledDelegate
        ' api version 27
        Public NowPlayingList_MoveFiles As NowPlayingList_MoveFilesDelegate
        ' api version 28
        Public Library_GetArtworkUrl As Library_GetArtworkDelegate
        Public Library_GetArtistPictureThumb As Library_GetArtistPictureThumbDelegate
        Public NowPlaying_GetArtworkUrl As NowPlaying_GetArtworkDelegate
        Public NowPlaying_GetDownloadedArtworkUrl As NowPlaying_GetArtworkDelegate
        Public NowPlaying_GetArtistPictureThumb As NowPlaying_GetArtistPictureThumbDelegate
        ' api version 29
        Public Playlist_IsInList As Playlist_IsInListDelegate
        ' api version 30
        Public Library_GetArtistPictureUrls As Library_GetArtistPictureUrlsDelegate
        Public NowPlaying_GetArtistPictureUrls As NowPlaying_GetArtistPictureUrlsDelegate
        ' api version 31
        Public Playlist_AddFiles As Playlist_AddFilesDelegate
        ' api version 32
        Public Sync_FileStart As Sync_FileStartDelegate
        Public Sync_FileEnd As Sync_FileEndDelegate
        ' api version 33
        Public Library_QueryFilesEx As Library_QueryFilesExDelegate
        Public NowPlayingList_QueryFilesEx As Library_QueryFilesExDelegate
        Public Playlist_QueryFilesEx As Playlist_QueryFilesExDelegate
        Public Playlist_MoveFiles As Playlist_MoveFilesDelegate
        Public Playlist_PlayNow As Playlist_PlayNowDelegate
        Public NowPlaying_IsSoundtrack As NowPlaying_IsSoundtrackDelegate
        Public NowPlaying_GetSoundtrackPictureUrls As NowPlaying_GetArtistPictureUrlsDelegate
        Public Library_GetDevicePersistentId As Library_GetDevicePersistentIdDelegate
        Public Library_SetDevicePersistentId As Library_SetDevicePersistentIdDelegate
        Public Library_FindDevicePersistentId As Library_FindDevicePersistentIdDelegate
        Public Setting_GetValue As Setting_GetValueDelegate
        Public Library_AddFileToLibrary As Library_AddFileToLibraryDelegate
        Public Playlist_DeletePlaylist As Playlist_DeletePlaylistDelegate
        Public Library_GetSyncDelta As Library_GetSyncDeltaDelegate
        ' api version 35
        Public Library_GetFileTags As Library_GetFileTagsDelegate
        Public NowPlaying_GetFileTags As NowPlaying_GetFileTagsDelegate
        Public NowPlayingList_GetFileTags As NowPlayingList_GetFileTagsDelegate
        ' api version 43
        Public MB_AddTreeNode As MB_AddTreeNodeDelegate
        Public MB_DownloadFile As MB_DownloadFileDelegate
        ' api version 47
        Public Setting_GetFileConvertCommandLine As Setting_GetFileConvertCommandLineDelegate
        Public Player_OpenStreamHandle As Player_OpenStreamHandleDelegate
        Public Player_UpdatePlayStatistics As Player_UpdatePlayStatisticsDelegate
        Public Library_GetArtworkEx As Library_GetArtworkExDelegate
        Public Library_SetArtworkEx As Library_SetArtworkExDelegate
        Public MB_GetVisualiserInformation As MB_GetVisualiserInformationDelegate
        Public MB_ShowVisualiser As MB_ShowVisualiserDelegate
        Public MB_GetPluginViewInformation As MB_GetPluginViewInformationDelegate
        Public MB_ShowPluginView As MB_ShowPluginViewDelegate
        Public Player_GetOutputDevices As Player_GetOutputDevicesDelegate
        Public Player_SetOutputDevice As Player_SetOutputDeviceDelegate
        ' api version 48
        Public MB_UninstallPlugin As MB_UninistallPluginDelegate
        ' api version 50
        Public Player_PlayPreviousAlbum As Player_ActionDelegate
        Public Player_PlayNextAlbum As Player_ActionDelegate
        ' api version 51
        Public Podcasts_QuerySubscriptions As Podcasts_QuerySubscriptionsDelegate
        Public Podcasts_GetSubscription As Podcasts_GetSubscriptionDelegate
        Public Podcasts_GetSubscriptionArtwork As Podcasts_GetSubscriptionArtworkDelegate
        Public Podcasts_GetSubscriptionEpisodes As Podcasts_GetSubscriptionEpisodesDelegate
        Public Podcasts_GetSubscriptionEpisode As Podcasts_GetSubscriptionEpisodeDelegate
        ' api version 52
        Public NowPlaying_GetSoundGraphEx As NowPlaying_GetSoundGraphExDelegate
        ' api version 53
        Public Sync_FileDeleteStart As Sync_FileStartDelegate
        Public Sync_FileDeleteEnd As Sync_FileEndDelegate
    End Structure  ' MusicBeeApiInterface

    Public Enum MusicBeeVersion
        v2_0 = 0
        v2_1 = 1
        v2_2 = 2
        v2_3 = 3
        v2_4 = 4
        v2_5 = 5
        v3_0 = 6
        v3_1 = 7
    End Enum  ' MusicBeeVersion

    Public Enum PluginType
        Unknown = 0
        General = 1
        LyricsRetrieval = 2
        ArtworkRetrieval = 3
        PanelView = 4
        DataStream = 5
        InstantMessenger = 6
        Storage = 7
        VideoPlayer = 8
        DSP = 9
        TagRetrieval = 10
        TagOrArtworkRetrieval = 11
        Upnp = 12
        WebBrowser = 13
    End Enum  ' PluginType

    <StructLayout(LayoutKind.Sequential)> _
    Public Class PluginInfo
        Public PluginInfoVersion As Short
        Public Type As PluginType
        Public Name As String
        Public Description As String
        Public Author As String
        Public TargetApplication As String
        Public VersionMajor As Short
        Public VersionMinor As Short
        Public Revision As Short
        Public MinInterfaceVersion As Short
        Public MinApiRevision As Short
        Public ReceiveNotifications As ReceiveNotificationFlags
        Public ConfigurationPanelHeight As Integer
    End Class

    <Flags()> _
    Public Enum ReceiveNotificationFlags
        StartupOnly = &H0
        PlayerEvents = &H1
        DataStreamEvents = &H2
        TagEvents = &H4
        DownloadEvents = &H8
    End Enum

    Public Enum NotificationType
        PluginStartup = 0    ' notification sent after successful initialisation for an enabled plugin
        TrackChanging = 16
        TrackChanged = 1
        PlayStateChanged = 2
        AutoDjStarted = 3
        AutoDjStopped = 4
        VolumeMuteChanged = 5
        VolumeLevelChanged = 6
        <Obsolete("Use PlayingTracksChanged")> NowPlayingListChanged = 7
        NowPlayingListEnded = 18
        NowPlayingArtworkReady = 8
        NowPlayingLyricsReady = 9
        TagsChanging = 10
        TagsChanged = 11
        RatingChanging = 15
        RatingChanged = 12
        PlayCountersChanged = 13
        ScreenSaverActivating = 14
        ShutdownStarted = 17
        EmbedInPanel = 19
        PlayerRepeatChanged = 20
        PlayerShuffleChanged = 21
        PlayerEqualiserOnOffChanged = 22
        PlayerScrobbleChanged = 23
        ReplayGainChanged = 24
        FileDeleting = 25
        FileDeleted = 26
        ApplicationWindowChanged = 27
        StopAfterCurrentChanged = 28
        LibrarySwitched = 29
        FileAddedToLibrary = 30
        FileAddedToInbox = 31
        SyncCompleted = 32
        DownloadCompleted = 33
        MusicBeeStarted = 34
        PlayingTracksChanged = 35
        PlayingTracksQueueChanged = 36
        PlaylistCreated = 37
        PlaylistUpdated = 38
        PlaylistDeleted = 39
    End Enum

    Public Enum CallbackType
        SettingsUpdated = 1
        StorageReady = 2
        StorageFailed = 3
        FilesRetrievedChanged = 4
        FilesRetrievedNoChange = 5
        FilesRetrievedFail = 6
        LyricsDownloaded = 7
        StorageEject = 8
        SuspendPlayCounters = 9
        ResumePlayCounters = 10
        EnablePlugin = 11
        DisablePlugin = 12
        RenderingDevicesChanged = 13
        FullscreenOn = 14
        FullscreenOff = 15
    End Enum

    Public Enum PluginCloseReason
        MusicBeeClosing = 1
        UserDisabled = 2
        StopNoUnload = 3
    End Enum

    Public Enum FilePropertyType
        Url = 2
        Kind = 4
        Format = 5
        Size = 7
        Channels = 8
        SampleRate = 9
        Bitrate = 10
        DateModified = 11
        DateAdded = 12
        LastPlayed = 13
        PlayCount = 14
        SkipCount = 15
        Duration = 16
        Status = 21
        NowPlayingListIndex = 78  ' only has meaning when called from NowPlayingList_* commands
        ReplayGainTrack = 94
        ReplayGainAlbum = 95
    End Enum

    Public Enum MetaDataType
        TrackTitle = 65
        Album = 30
        AlbumArtist = 31     ' displayed album artist
        AlbumArtistRaw = 34  ' stored album artist
        Artist = 32          ' displayed artist 
        MultiArtist = 33     ' individual artists, separated by ChrW(0)
        PrimaryArtist = 19   ' first artist from multi-artist tagged file, otherwise displayed artist
        Artists = 144
        ArtistsWithArtistRole = 145
        ArtistsWithPerformerRole = 146
        ArtistsWithGuestRole = 147
        ArtistsWithRemixerRole = 148
        Artwork = 40
        BeatsPerMin = 41
        Composer = 43        ' displayed composer
        MultiComposer = 89   ' individual composers, separated by ChrW(0)
        Comment = 44
        Conductor = 45
        Custom1 = 46
        Custom2 = 47
        Custom3 = 48
        Custom4 = 49
        Custom5 = 50
        Custom6 = 96
        Custom7 = 97
        Custom8 = 98
        Custom9 = 99
        Custom10 = 128
        Custom11 = 129
        Custom12 = 130
        Custom13 = 131
        Custom14 = 132
        Custom15 = 133
        Custom16 = 134
        DiscNo = 52
        DiscCount = 54
        Encoder = 55
        Genre = 59
        Genres = 143
        GenreCategory = 60
        Grouping = 61
        Keywords = 84
        HasLyrics = 63
        Lyricist = 62
        Lyrics = 114
        Mood = 64
        Occasion = 66
        Origin = 67
        Publisher = 73
        Quality = 74
        Rating = 75
        RatingLove = 76
        RatingAlbum = 104
        Tempo = 85
        TrackNo = 86
        TrackCount = 87
        Virtual1 = 109
        Virtual2 = 110
        Virtual3 = 111
        Virtual4 = 112
        Virtual5 = 113
        Virtual6 = 122
        Virtual7 = 123
        Virtual8 = 124
        Virtual9 = 125
        Virtual10 = 135
        Virtual11 = 136
        Virtual12 = 137
        Virtual13 = 138
        Virtual14 = 139
        Virtual15 = 140
        Virtual16 = 141
        Virtual17 = 149
        Virtual18 = 150
        Virtual19 = 151
        Virtual20 = 152
        Virtual21 = 153
        Virtual22 = 154
        Virtual23 = 155
        Virtual24 = 156
        Virtual25 = 157
        Year = 88
        SortTitle = 163
        SortAlbum = 164
        SortAlbumArtist = 165
        SortArtist = 166
        SortComposer = 167
        Work = 168
        MovementName = 169
        MovementNo = 170
        MovementCount = 171
        ShowMovement = 172
        Language = 173
        OriginalArtist = 174
        OriginalYear = 175
        OriginalTitle = 177
    End Enum

    Public Enum FileCodec
        Unknown = -1
        Mp3 = 1
        Aac = 2
        Flac = 3
        Ogg = 4
        WavPack = 5
        Wma = 6
        Tak = 7
        Mpc = 8
        Wave = 9
        Asx = 10
        Alac = 11
        Aiff = 12
        Pcm = 13
        Opus = 15
        Spx = 16
        Dsd = 17
        AacNoContainer = 18
    End Enum

    Public Enum EncodeQuality
        SmallSize = 1
        Portable = 2
        HighQuality = 3
        Archiving = 4
    End Enum

    <Flags()>
    Public Enum LibraryCategory
        Music = 0
        Audiobook = 1
        Video = 2
        Inbox = 4
    End Enum

    Public Enum DeviceIdType
        MusicBeeNativeId = 0
        GooglePlay = 1
        AppleDevice = 2
        GooglePlay2 = 3
        AppleDevice2 = 4
        WebDrivePluginOneDrive = 5
        WebDrivePluginGoogleDrive = 6
        WebDrivePluginDropBox = 7
    End Enum

    Public Enum DataType
        [String] = 0
        Number = 1
        DateTime = 2
        Rating = 3
    End Enum

    Public Enum SettingId
        CompactPlayerFlickrEnabled = 1
        FileTaggingPreserveModificationTime = 2
        LastDownloadFolder = 3
        CustomWebLinkName1 = 11
        CustomWebLinkName2 = 12
        CustomWebLinkName3 = 13
        CustomWebLinkName4 = 14
        CustomWebLinkName5 = 15
        CustomWebLinkName6 = 16
        CustomWebLinkName7 = 29
        CustomWebLinkName8 = 30
        CustomWebLinkName9 = 31
        CustomWebLinkName10 = 32
        CustomWebLink1 = 17
        CustomWebLink2 = 18
        CustomWebLink3 = 19
        CustomWebLink4 = 20
        CustomWebLink5 = 21
        CustomWebLink6 = 22
        CustomWebLink7 = 33
        CustomWebLink8 = 34
        CustomWebLink9 = 35
        CustomWebLink10 = 36
        CustomWebLinkNowPlaying1 = 23
        CustomWebLinkNowPlaying2 = 24
        CustomWebLinkNowPlaying3 = 25
        CustomWebLinkNowPlaying4 = 26
        CustomWebLinkNowPlaying5 = 27
        CustomWebLinkNowPlaying6 = 28
        CustomWebLinkNowPlaying7 = 37
        CustomWebLinkNowPlaying8 = 38
        CustomWebLinkNowPlaying9 = 39
        CustomWebLinkNowPlaying10 = 40
    End Enum

    Public Enum ComparisonType
        [Is] = 0
        [IsNot] = 1
        Contains = 4
        DoesNotContain = 5
        StartsWith = 6
        EndsWith = 7
        IsSimilar = 20
    End Enum

    Public Enum LyricsType
        NotSpecified = 0
        Synchronised = 1
        UnSynchronised = 2
    End Enum

    Public Enum PlayState
        Undefined = 0
        Loading = 1
        Playing = 3
        Paused = 6
        Stopped = 7
    End Enum

    Public Enum RepeatMode
        None = 0
        All = 1
        One = 2
    End Enum

    Public Enum PlayButtonType
        PreviousTrack = 0
        PlayPause = 1
        NextTrack = 2
        [Stop] = 3
    End Enum

    Public Enum PlaylistFormat
        Unknown = 0
        M3u = 1
        Xspf = 2
        Asx = 3
        Wpl = 4
        Pls = 5
        Auto = 7
        M3uAscii = 8
        AsxFile = 9
        Radio = 10
        M3uExtended = 11
        Mbp = 12
    End Enum

    Public Enum SkinElement
        SkinSubPanel = 0
        SkinInputControl = 7
        SkinInputPanel = 10
        SkinInputPanelLabel = 14
        SkinTrackAndArtistPanel = -1
    End Enum

    Public Enum ElementState
        ElementStateDefault = 0
        ElementStateModified = 6
    End Enum

    Public Enum ElementComponent
        ComponentBorder = 0
        ComponentBackground = 1
        ComponentForeground = 3
    End Enum

    Public Enum PluginPanelDock
        ApplicationWindow = 0
        TrackAndArtistPanel = 1
        TextBox = 3
        ComboBox = 4
        MainPanel = 5
    End Enum

    Public Enum ReplayGainMode
        Off = 0
        Track = 1
        Album = 2
        Smart = 3
    End Enum

    Public Enum PlayStatisticType
        NoChange = 0
        IncreasePlayCount = 1
        IncreaseSkipCount = 2
    End Enum

    Public Enum Command
        NavigateTo = 1
    End Enum

    Public Enum DownloadTarget
        Inbox = 0
        MusicLibrary = 1
        SpecificFolder = 3
    End Enum

    <Flags()> _
    Public Enum PictureLocations As Byte
        None = 0
        EmbedInFile = 1
        LinkToOrganisedCopy = 2
        LinkToSource = 4
        FolderThumb = 8
    End Enum

    Public Enum WindowState
        Off = -1
        Normal = 0
        Fullscreen = 1
        Desktop = 2
    End Enum

    Public Enum SubscriptionMetaDataType
        Id = 0
        Title = 1
        Grouping = 2
        Genre = 3
        Description = 4
        DounloadedCount = 5
    End Enum

    Public Enum EpisodeMetaDataType
        Id = 0
        Title = 1
        DateTime = 2
        Description = 3
        Duration = 4
        IsDownloaded = 5
        HasBeenPlayed = 6
    End Enum

    Public Delegate Sub MB_ReleaseStringDelegate(p1 As String)
    Public Delegate Sub MB_TraceDelegate(p1 As String)
    Public Delegate Function MB_WindowHandleDelegate() As IntPtr
    Public Delegate Sub MB_RefreshPanelsDelegate()
    Public Delegate Sub MB_SendNotificationDelegate(type As CallbackType)
    Public Delegate Function MB_AddMenuItemDelegate(menuPath As String, hotkeyDescription As String, handler As EventHandler) As System.Windows.Forms.ToolStripItem
    Public Delegate Function MB_AddTreeNodeDelegate(treePath As String, name As String, icon As Drawing.Bitmap, openHandler As EventHandler, closeHandler As EventHandler) As Boolean
    Public Delegate Sub MB_RegisterCommandDelegate(command As String, handler As EventHandler)
    Public Delegate Sub MB_CreateBackgroundTaskDelegate(taskCallback As Threading.ThreadStart, owner As Windows.Forms.Form)
    Public Delegate Sub MB_CreateParameterisedBackgroundTaskDelegate(taskCallback As Threading.ParameterizedThreadStart, parameters As Object, owner As Windows.Forms.Form)
    Public Delegate Sub MB_SetBackgroundTaskMessageDelegate(message As String)
    Public Delegate Function MB_GetPanelBoundsDelegate(dock As PluginPanelDock) As Drawing.Rectangle
    Public Delegate Function MB_SetPanelScrollableAreaDelegate(panel As Windows.Forms.Control, scrollArea As Drawing.Size, alwaysShowScrollBar As Boolean) As Boolean
    Public Delegate Function MB_AddPanelDelegate(panel As Windows.Forms.Control, dock As PluginPanelDock) As Windows.Forms.Control
    Public Delegate Sub MB_RemovePanelDelegate(panel As Windows.Forms.Control)
    Public Delegate Function MB_GetLocalisationDelegate(id As String, defaultText As String) As String
    Public Delegate Function MB_ShowNowPlayingAssistantDelegate() As Boolean
    Public Delegate Function MB_InvokeCommandDelegate(command As Command, parameter As Object) As Boolean
    Public Delegate Function MB_OpenFilterInTabDelegate(field1 As MetaDataType, comparison1 As ComparisonType, value1 As String, field2 As MetaDataType, comparison1 As ComparisonType, value2 As String) As Boolean
    Public Delegate Function MB_SetWindowSizeDelegate(width As Integer, height As Integer) As Boolean
    Public Delegate Function MB_DownloadFileDelegate(url As String, target As DownloadTarget, targetFolder As String, cancelDownload As Boolean) As Boolean
    Public Delegate Function MB_GetVisualiserInformationDelegate(ByRef visualiserNames() As String, ByRef defaultVisualiserName As String, ByRef defaultState As WindowState, ByRef currentState As WindowState) As Boolean
    Public Delegate Function MB_ShowVisualiserDelegate(visualiserName As String, state As WindowState) As Boolean
    Public Delegate Function MB_GetPluginViewInformationDelegate(pluginFilename As String, ByRef viewNames() As String, ByRef defaultViewName As String, ByRef defaultState As WindowState, ByRef currentState As WindowState) As Boolean
    Public Delegate Function MB_ShowPluginViewDelegate(pluginFilename As String, viewName As String, state As WindowState) As Boolean
    Public Delegate Function MB_UninistallPluginDelegate(pluginFilename As String, password As String) As Boolean
    Public Delegate Function Setting_GetFieldNameDelegate(field As MetaDataType) As String
    Public Delegate Function Setting_GetPersistentStoragePathDelegate() As String
    Public Delegate Function Setting_GetSkinDelegate() As String
    Public Delegate Function Setting_GetSkinElementColourDelegate(element As SkinElement, state As ElementState, component As ElementComponent) As Integer
    Public Delegate Function Setting_IsWindowBordersSkinnedDelegate() As Boolean
    Public Delegate Function Setting_GetDefaultFontDelegate() As Drawing.Font
    Public Delegate Function Setting_GetDataTypeDelegate(field As MetaDataType) As DataType
    Public Delegate Function Setting_GetLastFmUserIdDelegate() As String
    Public Delegate Function Setting_GetWebProxyDelegate() As String
    Public Delegate Function Setting_GetValueDelegate(id As SettingId, ByRef value As Object) As Boolean
    Public Delegate Function Setting_GetFileConvertCommandLineDelegate(codec As FileCodec, encodeQuality As EncodeQuality) As String
    Public Delegate Function Library_GetFilePropertyDelegate(sourceFileUrl As String, type As FilePropertyType) As String
    Public Delegate Function Library_GetFileTagDelegate(sourceFileUrl As String, field As MetaDataType) As String
    Public Delegate Function Library_GetFileTagsDelegate(sourceFileUrl As String, fields() As MetaDataType, ByRef results() As String) As Boolean
    Public Delegate Function Library_SetFileTagDelegate(sourceFileUrl As String, field As MetaDataType, value As String) As Boolean
    Public Delegate Function Library_GetDevicePersistentIdDelegate(sourceFileUrl As String, idType As DeviceIdType) As String
    Public Delegate Function Library_SetDevicePersistentIdDelegate(sourceFileUrl As String, idType As DeviceIdType, value As String) As Boolean
    Public Delegate Function Library_FindDevicePersistentIdDelegate(idType As Integer, ids() As String, ByRef values() As String) As Boolean
    Public Delegate Function Library_CommitTagsToFileDelegate(sourceFileUrl As String) As Boolean
    Public Delegate Function Library_AddFileToLibraryDelegate(sourceFileUrl As String, category As LibraryCategory) As String
    Public Delegate Function Library_GetSyncDeltaDelegate(cachedFiles() As String, updatedSince As DateTime, categories As LibraryCategory, ByRef newFiles() As String, ByRef updatedFiles() As String, ByRef deletedFiles() As String) As Boolean
    Public Delegate Function Library_GetLyricsDelegate(sourceFileUrl As String, type As LyricsType) As String
    Public Delegate Function Library_GetArtworkDelegate(sourceFileUrl As String, index As Integer) As String
    Public Delegate Function Library_GetArtworkExDelegate(sourceFileUrl As String, index As Integer, retrievePictureData As Boolean, ByRef pictureLocations As PictureLocations, ByRef pictureUrl As String, ByRef imageData() As Byte) As Boolean
    Public Delegate Function Library_SetArtworkExDelegate(sourceFileUrl As String, index As Integer, imageData() As Byte) As Boolean
    Public Delegate Function Library_GetArtistPictureDelegate(artistName As String, fadingPercent As Integer, fadingColor As Integer) As String
    Public Delegate Function Library_GetArtistPictureUrlsDelegate(artistName As String, localOnly As Boolean, ByRef urls() As String) As Boolean
    Public Delegate Function Library_GetArtistPictureThumbDelegate(artistName As String) As String
    Public Delegate Function Library_QueryFilesDelegate(query As String) As Boolean
    Public Delegate Function Library_QueryGetNextFileDelegate() As String
    Public Delegate Function Library_QueryGetAllFilesDelegate() As String
    Public Delegate Function Library_QueryFilesExDelegate(query As String, ByRef files() As String) As Boolean
    Public Delegate Function Library_QuerySimilarArtistsDelegate(artistName As String, minimumArtistSimilarityRating As Double) As String
    Public Delegate Function Library_QueryLookupTableDelegate(keyTags As String, valueTags As String, query As String) As Boolean
    Public Delegate Function Library_QueryGetLookupTableValueDelegate(key As String) As String
    Public Delegate Function Player_GetPositionDelegate() As Integer
    Public Delegate Function Player_SetPositionDelegate(position As Integer) As Boolean
    Public Delegate Function Player_GetPlayStateDelegate() As PlayState
    Public Delegate Function Player_GetButtonEnabledDelegate(button As PlayButtonType) As Boolean
    Public Delegate Function Player_ActionDelegate() As Boolean
    Public Delegate Function Player_QueueRandomTracksDelegate(count As Integer) As Integer
    Public Delegate Function Player_GetVolumeDelegate() As Single
    Public Delegate Function Player_SetVolumeDelegate(volume As Single) As Boolean
    Public Delegate Function Player_GetMuteDelegate() As Boolean
    Public Delegate Function Player_SetMuteDelegate(mute As Boolean) As Boolean
    Public Delegate Function Player_GetShuffleDelegate() As Boolean
    Public Delegate Function Player_SetShuffleDelegate(shuffle As Boolean) As Boolean
    Public Delegate Function Player_GetRepeatDelegate() As RepeatMode
    Public Delegate Function Player_SetRepeatDelegate(repeat As RepeatMode) As Boolean
    Public Delegate Function Player_GetEqualiserEnabledDelegate() As Boolean
    Public Delegate Function Player_SetEqualiserEnabledDelegate(enabled As Boolean) As Boolean
    Public Delegate Function Player_GetDspEnabledDelegate() As Boolean
    Public Delegate Function Player_SetDspEnabledDelegate(enabled As Boolean) As Boolean
    Public Delegate Function Player_GetScrobbleEnabledDelegate() As Boolean
    Public Delegate Function Player_SetScrobbleEnabledDelegate(enabled As Boolean) As Boolean
    Public Delegate Function Player_GetShowTimeRemainingDelegate() As Boolean
    Public Delegate Function Player_GetShowRatingTrackDelegate() As Boolean
    Public Delegate Function Player_GetShowRatingLoveDelegate() As Boolean
    Public Delegate Function Player_ShowEqualiserDelegate() As Boolean
    Public Delegate Function Player_GetAutoDjEnabledDelegate() As Boolean
    Public Delegate Function Player_GetStopAfterCurrentEnabledDelegate() As Boolean
    Public Delegate Function Player_GetCrossfadeDelegate() As Boolean
    Public Delegate Function Player_SetCrossfadeDelegate(crossfade As Boolean) As Boolean
    Public Delegate Function Player_GetReplayGainModeDelegate() As ReplayGainMode
    Public Delegate Function Player_SetReplayGainModeDelegate(mode As ReplayGainMode) As Boolean
    Public Delegate Function Player_OpenStreamHandleDelegate(url As String, useMusicBeeSettings As Boolean, enableDsp As Boolean, gainType As ReplayGainMode) As Integer
    Public Delegate Function Player_UpdatePlayStatisticsDelegate(url As String, countType As PlayStatisticType, disableScrobble As Boolean) As Boolean
    Public Delegate Function Player_GetOutputDevicesDelegate(ByRef deviceNames() As String, ByRef activeDeviceName As String) As Boolean
    Public Delegate Function Player_SetOutputDeviceDelegate(deviceName As String) As Boolean
    Public Delegate Function NowPlaying_GetFileUrlDelegate() As String
    Public Delegate Function NowPlaying_GetDurationDelegate() As Integer
    Public Delegate Function NowPlaying_GetFilePropertyDelegate(type As FilePropertyType) As String
    Public Delegate Function NowPlaying_GetFileTagDelegate(field As MetaDataType) As String
    Public Delegate Function NowPlaying_GetFileTagsDelegate(fields() As MetaDataType, ByRef results() As String) As Boolean
    Public Delegate Function NowPlaying_GetLyricsDelegate() As String
    Public Delegate Function NowPlaying_GetArtworkDelegate() As String
    Public Delegate Function NowPlaying_GetArtistPictureDelegate(fadingPercent As Integer) As String
    Public Delegate Function NowPlaying_GetArtistPictureUrlsDelegate(localOnly As Boolean, ByRef urls() As String) As Boolean
    Public Delegate Function NowPlaying_GetArtistPictureThumbDelegate() As String
    Public Delegate Function NowPlaying_IsSoundtrackDelegate() As Boolean
    Public Delegate Function NowPlaying_GetSpectrumDataDelegate(fftData() As Single) As Integer
    Public Delegate Function NowPlaying_GetSoundGraphDelegate(graphData() As Single) As Boolean
    Public Delegate Function NowPlaying_GetSoundGraphExDelegate(graphData() As Single, peak() As Single) As Boolean
    Public Delegate Function NowPlayingList_GetCurrentIndexDelegate() As Integer
    Public Delegate Function NowPlayingList_GetNextIndexDelegate(offset As Integer) As Integer
    Public Delegate Function NowPlayingList_GetFileUrlDelegate(index As Integer) As String
    Public Delegate Function NowPlayingList_GetFilePropertyDelegate(index As Integer, type As FilePropertyType) As String
    Public Delegate Function NowPlayingList_GetFileTagDelegate(index As Integer, field As MetaDataType) As String
    Public Delegate Function NowPlayingList_GetFileTagsDelegate(index As Integer, fields() As MetaDataType, ByRef results() As String) As Boolean
    Public Delegate Function NowPlayingList_ActionDelegate() As Boolean
    Public Delegate Function NowPlayingList_FileActionDelegate(sourceFileUrl As String) As Boolean
    Public Delegate Function NowPlayingList_FilesActionDelegate(sourceFileUrls() As String) As Boolean
    Public Delegate Function NowPlayingList_IsAnyPriorTracksDelegate() As Boolean
    Public Delegate Function NowPlayingList_IsAnyFollowingTracksDelegate() As Boolean
    Public Delegate Function NowPlayingList_RemoveAtDelegate(index As Integer) As Boolean
    Public Delegate Function NowPlayingList_MoveFilesDelegate(fromIndices() As Integer, toIndex As Integer) As Boolean
    Public Delegate Function Playlist_GetNameDelegate(playlistUrl As String) As String
    Public Delegate Function Playlist_GetTypeDelegate(playlistUrl As String) As PlaylistFormat
    Public Delegate Function Playlist_IsInListDelegate(playlistUrl As String, filename As String) As Boolean
    Public Delegate Function Playlist_QueryPlaylistsDelegate() As Boolean
    Public Delegate Function Playlist_QueryGetNextPlaylistDelegate() As String
    Public Delegate Function Playlist_QueryFilesDelegate(playlistUrl As String) As Boolean
    Public Delegate Function Playlist_QueryFilesExDelegate(playlistUrl As String, ByRef filenames() As String) As Boolean
    Public Delegate Function Playlist_CreatePlaylistDelegate(folderName As String, playlistName As String, filenames() As String) As String
    Public Delegate Function Playlist_DeletePlaylistDelegate(playlistUrl As String) As Boolean
    Public Delegate Function Playlist_SetFilesDelegate(playlistUrl As String, filenames() As String) As Boolean
    Public Delegate Function Playlist_AddFilesDelegate(playlistUrl As String, filenames() As String) As Boolean
    Public Delegate Function Playlist_RemoveAtDelegate(playlistUrl As String, index As Integer) As Boolean
    Public Delegate Function Playlist_MoveFilesDelegate(playlistUrl As String, fromIndices() As Integer, toIndex As Integer) As Boolean
    Public Delegate Function Playlist_PlayNowDelegate(playlistUrl As String) As Boolean
    Public Delegate Function Pending_GetFileUrlDelegate() As String
    Public Delegate Function Pending_GetFilePropertyDelegate(field As FilePropertyType) As String
    Public Delegate Function Pending_GetFileTagDelegate(field As MetaDataType) As String
    Public Delegate Function Podcasts_QuerySubscriptionsDelegate(query As String, ByRef ids() As String) As Boolean
    Public Delegate Function Podcasts_GetSubscriptionDelegate(id As String, ByRef subscription() As String) As Boolean
    Public Delegate Function Podcasts_GetSubscriptionArtworkDelegate(id As String, index As Integer, ByRef imageData() As Byte) As Boolean
    Public Delegate Function Podcasts_GetSubscriptionEpisodesDelegate(id As String, ByRef urls() As String) As Boolean
    Public Delegate Function Podcasts_GetSubscriptionEpisodeDelegate(id As String, index As Integer, ByRef episode() As String) As Boolean
    Public Delegate Function Sync_FileStartDelegate(filename As String) As String
    Public Delegate Sub Sync_FileEndDelegate(filename As String, success As Boolean, errorMessage As String)

    <System.Security.SuppressUnmanagedCodeSecurity()> _
    <DllImport("kernel32.dll")> _
    Private Shared Sub CopyMemory(ByRef mbApiInterface As MusicBeeApiInterface, src As IntPtr, length As Integer)
    End Sub
End Class
