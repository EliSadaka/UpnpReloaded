Imports System.Runtime.InteropServices
Imports System.Net
Imports System.Net.NetworkInformation
Imports System.Net.Sockets
Imports System.Threading

Public Class Plugin
    Private Shared mbApiInterface As New MusicBeeApiInterface
    Private Shared ReadOnly about As New PluginInfo
    Private Const musicBeePluginVersion As String = "1.0"
    Private Shared startTimeTicks As Long
    Private Shared server As MediaServerDevice
    Private Shared controller As ControlPointManager
    Private Shared ReadOnly networkLock As New Object
    Private Shared ReadOnly logLock As New Object
    Private Shared ReadOnly errorCount As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
    Private Shared ReadOnly sendDataBarrier As New SemaphoreSlim(4)
    Private Shared ignoreNamePrefixes() As String = New String() {}
    Private Shared ignoreNameChars As String = Nothing
    Private Shared playCountTriggerPercent As Double
    Private Shared playCountTriggerSeconds As Integer
    Private Shared skipCountTriggerPercent As Double
    Private Shared skipCountTriggerSeconds As Integer
    Private Shared logCounter As Integer = 0
    Private Shared hostAddresses() As IPAddress
    Private Shared subnetMasks()() As Byte
    Private Shared ipOverrideAddressMatched As Boolean
    Private Shared defaultHost As String
    Private Shared localIpAddresses()() As Byte

    Public Function Initialise(ByVal apiInterfacePtr As IntPtr) As PluginInfo
        CopyMemory(mbApiInterface, apiInterfacePtr, Marshal.SizeOf(mbApiInterface))
        about.PluginInfoVersion = PluginInfoVersion
        about.Name = "MusicBee UPnP"
        about.Description = "UPnP/DLNA server and control point"
        about.Author = "Steven Mayall"
        about.TargetApplication = ""
        about.Type = PluginType.Upnp
        about.VersionMajor = 1
        about.VersionMinor = 0
        about.Revision = 1
        about.MinInterfaceVersion = MinInterfaceVersion
        about.MinApiRevision = MinApiRevision
        about.ReceiveNotifications = (ReceiveNotificationFlags.TagEvents Or ReceiveNotificationFlags.PlayerEvents)
        about.ConfigurationPanelHeight = 0
        Return about
    End Function

    Public Function Configure(ByVal panelHandle As IntPtr) As Boolean
        Using dialog As New SettingsDialog
            dialog.ShowDialog(Form.FromHandle(mbApiInterface.MB_GetWindowHandle()))
        End Using
        Return True
    End Function

    Public Sub SaveSettings()
    End Sub

    Public Sub Close(ByVal reason As PluginCloseReason)
        RemoveHandler NetworkChange.NetworkAddressChanged, AddressOf NetworkChange_NetworkAddressChanged
        Dim closeThread As New Thread(AddressOf ExecuteClose) With {
            .IsBackground = True
        }
        closeThread.Start()
    End Sub

    Private Sub ExecuteClose()
        If controller IsNot Nothing Then
            Try
                controller.Dispose()
            Catch
            End Try
        End If
        If server IsNot Nothing Then
            Try
                server.Dispose()
            Catch
            End Try
        End If
    End Sub

    Public Sub Uninstall()
    End Sub

    Public Sub ReceiveNotification(ByVal sourceFileUrl As String, ByVal type As NotificationType)
        Select Case type
            Case NotificationType.PluginStartup
                Dim value As Object
                mbApiInterface.Setting_GetValue(SettingId.IgnoreNamePrefixes, value)
                ignoreNamePrefixes = DirectCast(value, String())
                mbApiInterface.Setting_GetValue(SettingId.IgnoreNameChars, value)
                ignoreNameChars = DirectCast(value, String)
                mbApiInterface.Setting_GetValue(SettingId.PlayCountTriggerPercent, value)
                playCountTriggerPercent = DirectCast(value, Integer) / 100
                mbApiInterface.Setting_GetValue(SettingId.PlayCountTriggerSeconds, value)
                playCountTriggerSeconds = DirectCast(value, Integer)
                mbApiInterface.Setting_GetValue(SettingId.SkipCountTriggerPercent, value)
                skipCountTriggerPercent = DirectCast(value, Integer) / 100
                mbApiInterface.Setting_GetValue(SettingId.SkipCountTriggerSeconds, value)
                skipCountTriggerSeconds = DirectCast(value, Integer)
                Try
                    startTimeTicks = DateTime.UtcNow.Ticks
                    LogInformation("Initialise", DateTime.Now.ToString())
                    GetNetworkAddresses()
                    server = New MediaServerDevice(Settings.Udn)
                    server.Start()
                    controller = New ControlPointManager
                    controller.Start()
                    AddHandler NetworkChange.NetworkAddressChanged, AddressOf NetworkChange_NetworkAddressChanged
                Catch ex As Exception
                    LogError(ex, "Initialise", ex.StackTrace)
                End Try
            Case NotificationType.FileAddedToLibrary, NotificationType.FileAddedToInbox, NotificationType.FileDeleted, NotificationType.TagsChanged
                ItemManager.SetLibraryDirty()
            Case NotificationType.PlayStateChanged
                If activeRenderingDevice IsNot Nothing Then
                    Select Case mbApiInterface.Player_GetPlayState()
                        Case PlayState.Stopped
                            activeRenderingDevice.StopPlayback()
                        Case PlayState.Paused
                            activeRenderingDevice.PausePlayback()
                        Case PlayState.Playing
                            activeRenderingDevice.ResumePlayback()
                    End Select
                End If
            Case NotificationType.VolumeMuteChanged
                If activeRenderingDevice IsNot Nothing Then
                    activeRenderingDevice.SetMute(mbApiInterface.Player_GetMute())
                End If
            Case NotificationType.VolumeLevelChanged
                If activeRenderingDevice IsNot Nothing Then
                    activeRenderingDevice.SetVolume(mbApiInterface.Player_GetVolume())
                End If
        End Select
    End Sub

    Private Shared Sub NetworkChange_NetworkAddressChanged(sender As Object, e As EventArgs)
        Try
            LogInformation("NetworkChange_NetworkAddressChanged", "")
            SyncLock networkLock
                GetNetworkAddresses()
                If server IsNot Nothing Then
                    server.Restart(False)
                End If
                If controller IsNot Nothing Then
                    controller.Restart()
                End If
            End SyncLock
        Catch ex As Exception
            LogError(ex, "NetworkChange_NetworkAddressChanged")
        End Try
    End Sub

    Private Shared Sub GetNetworkAddresses()
        ' Dns.GetHostAddresses(Dns.GetHostName()).Where(Function(a) a.AddressFamily = AddressFamily.InterNetwork)
        Dim addressList As New List(Of IPAddress)
        Dim subnetMaskList As New List(Of Byte())
        ipOverrideAddressMatched = String.IsNullOrEmpty(Settings.IpAddress)
        defaultHost = Nothing
        For Each network As NetworkInterface In NetworkInterface.GetAllNetworkInterfaces()
            If network.OperationalStatus = OperationalStatus.Up Then 'AndAlso network.NetworkInterfaceType <> NetworkInterfaceType.Loopback Then
                'LogInformation("GetNetworkAdresseses", "id=" & network.Name & ",speed=" & network.Speed)
                For Each unicastAddress As UnicastIPAddressInformation In network.GetIPProperties.UnicastAddresses
                    If unicastAddress.Address.AddressFamily = AddressFamily.InterNetwork AndAlso unicastAddress.IPv4Mask IsNot Nothing Then  'IPv4
                        If Not addressList.Contains(unicastAddress.Address) Then
                            If unicastAddress.IsDnsEligible AndAlso defaultHost Is Nothing Then
                                defaultHost = unicastAddress.Address.ToString()
                            End If
                            LogInformation("GetNetworkAddresses", unicastAddress.Address.ToString() & ",dns=" & unicastAddress.IsDnsEligible & ",name=" & network.Name & ",speed=" & network.Speed)
                            addressList.Add(unicastAddress.Address)
                            If unicastAddress.Address.ToString() = Settings.IpAddress Then
                                ipOverrideAddressMatched = True
                            End If
                            subnetMaskList.Add(unicastAddress.IPv4Mask.GetAddressBytes())
                        End If
                        Exit For
                    End If
                Next unicastAddress
            End If
        Next network
        hostAddresses = addressList.ToArray()
        subnetMasks = subnetMaskList.ToArray()
        If defaultHost Is Nothing Then
            defaultHost = hostAddresses(0).ToString()
        End If
        'Try
        '    Dim settingsUrl As String = mbApiInterface.Setting_GetPersistentStoragePath() & "UPnPaddress.dat"
        '    If IO.File.Exists(settingsUrl) Then
        '        Using reader As New IO.StreamReader(settingsUrl)
        '            defaultHost = reader.ReadLine()
        '        End Using
        '    End If
        'Catch
        'End Try
        LogInformation("GetNetworkAddresses", PrimaryHostUrl)
        localIpAddresses = New Byte(hostAddresses.Length - 1)() {}
        For index As Integer = 0 To hostAddresses.Length - 1
            localIpAddresses(index) = hostAddresses(index).GetAddressBytes()
        Next index
    End Sub

    Private Shared ReadOnly Property PrimaryHostUrl() As String
        Get
            Return "http://" & If(String.IsNullOrEmpty(Settings.IpAddress), defaultHost, Settings.IpAddress) & ":" & Settings.ServerPort
        End Get
    End Property

    Public Function GetRenderingDevices() As String()
        Dim list As New List(Of String)
        SyncLock renderingDevices
            For Each device As MediaRendererDevice In renderingDevices
                list.Add(device.FriendlyName)
            Next device
        End SyncLock
        Return list.ToArray()
    End Function

    Public Function GetRenderingSettings() As Integer()
        Return New Integer() {CInt(Settings.ContinuousOutput), If(activeRenderingDevice Is Nothing, 44100, activeRenderingDevice.GetContinuousStreamingSampleRate()), 2, If(activeRenderingDevice Is Nothing, 16, activeRenderingDevice.GetContinuousStreamingBitDepth())}
    End Function

    Public Function SetActiveRenderingDevice(name As String) As Boolean
        SyncLock renderingDevices
            If name Is Nothing Then
                If activeRenderingDevice IsNot Nothing Then
                    activeRenderingDevice.Activate(False)
                    activeRenderingDevice = Nothing
                End If
                Return True
            Else
                If activeRenderingDevice IsNot Nothing Then
                    If String.Compare(activeRenderingDevice.FriendlyName, name, StringComparison.Ordinal) = 0 Then
                        Return True
                    End If
                    activeRenderingDevice.Activate(False)
                    activeRenderingDevice = Nothing
                End If
                For Each device As MediaRendererDevice In renderingDevices
                    If String.Compare(device.FriendlyName, name, StringComparison.Ordinal) = 0 Then
                        activeRenderingDevice = device
                        If activeRenderingDevice.Activate(True) Then
                            Return True
                        Else
                            activeRenderingDevice = Nothing
                            Return False
                        End If
                    End If
                Next device
            End If
        End SyncLock
        Return False
    End Function

    Public Function PlayToDevice(url As String, streamHandle As Integer) As Boolean
        If activeRenderingDevice Is Nothing Then
            LogInformation("PlayToDevice", url & " - no active device")
            Return False
        Else
            Return activeRenderingDevice.PlayToDevice(url, streamHandle)
        End If
    End Function

    Public Function QueueNext(url As String) As Boolean
        If activeRenderingDevice Is Nothing Then
            Return False
        Else
            Return activeRenderingDevice.QueueNext(url)
        End If
    End Function

    Public Function GetPlayPosition() As Integer
        If activeRenderingDevice Is Nothing Then
            Return 0
        Else
            Return activeRenderingDevice.PlayPositionMs
        End If
    End Function

    Public Sub SetPlayPosition(ms As Integer)
        If activeRenderingDevice IsNot Nothing Then
            activeRenderingDevice.Seek(ms)
        End If
    End Sub

    Private Shared Function LogError(ex As Exception, functionName As String, Optional extra As String = Nothing) As Exception
#If DEBUG Then
        Try
            Dim counter As Integer = Interlocked.Increment(logCounter)
            Dim gap As Long = (DateTime.UtcNow.Ticks - startTimeTicks) \ TimeSpan.TicksPerMillisecond
            SyncLock logLock
                Dim errorMessage As String = gap & "; " & counter & " " & functionName & " - " & ex.Message
                Debug.WriteLine(errorMessage)
                If Not String.IsNullOrEmpty(extra) Then
                    Debug.WriteLine(extra)
                End If
                If Settings.LogDebugInfo Then
                    Dim count As Integer
                    If Not errorCount.TryGetValue(functionName, count) Then
                        errorCount.Add(functionName, 1)
                    ElseIf count = 3 Then
                        Return ex
                    Else
                        errorCount(functionName) = count + 1
                    End If
                    Using writer As New IO.StreamWriter(mbApiInterface.Setting_GetPersistentStoragePath() & "UpnpErrorLog.dat", True)
                        writer.WriteLine(errorMessage)
                        If Not String.IsNullOrEmpty(extra) Then
                            writer.WriteLine(extra)
                        End If
                    End Using
                End If
            End SyncLock
        Catch
        End Try
        Return ex
#Else
        If Settings.LogDebugInfo Then
            Try
                Dim counter As Integer = Interlocked.Increment(logCounter)
                Dim gap As Long = (DateTime.UtcNow.Ticks - startTimeTicks) \ TimeSpan.TicksPerMillisecond
                SyncLock logLock
                    Dim count As Integer
                    If Not errorCount.TryGetValue(functionName, count) Then
                        errorCount.Add(functionName, 1)
                    ElseIf count = 3 Then
                        Return ex
                    Else
                        errorCount(functionName) = count + 1
                    End If
                    Using writer As New IO.StreamWriter(mbApiInterface.Setting_GetPersistentStoragePath() & "UpnpErrorLog.dat", True)
                        writer.WriteLine(gap & "; " & counter & " " & functionName & " - " & ex.Message)
                        If Not String.IsNullOrEmpty(extra) Then
                            writer.WriteLine(extra)
                        End If
                    End Using
                End SyncLock
            Catch
            End Try
        End If
        Return ex
#End If
    End Function

    Private Shared Sub LogInformation(functionName As String, information As String)
#If DEBUG Then
        Try
            Dim counter As Integer = Interlocked.Increment(logCounter)
            Dim gap As Long = (DateTime.UtcNow.Ticks - startTimeTicks) \ TimeSpan.TicksPerMillisecond
            SyncLock logLock
                Dim message As String = gap & "; " & counter & " " & functionName & " - " & information
                Debug.WriteLine(message)
                If Settings.LogDebugInfo Then
                    Using writer As New IO.StreamWriter(mbApiInterface.Setting_GetPersistentStoragePath() & "UpnpErrorLog.dat", True)
                        writer.WriteLine(message)
                    End Using
                End If
            End SyncLock
        Catch
        End Try
#Else
        If Settings.LogDebugInfo Then
            Try
                Dim counter As Integer = Interlocked.Increment(logCounter)
                Dim gap As Long = (DateTime.UtcNow.Ticks - startTimeTicks) \ TimeSpan.TicksPerMillisecond
                SyncLock logLock
                    Using writer As New IO.StreamWriter(mbApiInterface.Setting_GetPersistentStoragePath() & "UpnpErrorLog.dat", True)
                        writer.WriteLine(gap & "; " & counter & " " & functionName & " - " & information)
                    End Using
                End SyncLock
            Catch
            End Try
        End If
#End If
    End Sub

    Private Enum GainType
        None = 0
        Track = 1
        Album = 2
    End Enum  ' GainType

    Private Class StreamingProfile
        Public ProfileName As String
        Public UserAgents() As String = New String() {}
        Public PictureSize As UShort = 160
        Public MinimumSampleRate As Integer = 44100
        Public MaximumSampleRate As Integer = 48000
        Public StereoOnly As Boolean = True
        Public MaximumBitDepth As Integer = 16
        Public TranscodeCodec As FileCodec = FileCodec.Pcm
        Public TranscodeQuality As EncodeQuality = EncodeQuality.HighQuality
        Public TranscodeBitDepth As Integer = 16
        Public TranscodeSampleRate As Integer = -1
        Public WmcCompatability As Boolean = False
        Public Sub New()
        End Sub
        Public Sub New(name As String)
            ProfileName = name
        End Sub
        Public Overrides Function ToString() As String
            Return ProfileName
        End Function
    End Class  ' StreamingProfile

    Private Class Settings
        Public Shared EnableContentAccess As Boolean = True
        Public Shared EnablePlayToDevice As Boolean = True
        Public Shared ServerName As String = "MusicBee Media Library"
        Public Shared IpAddress As String = ""
        Public Shared ServerPort As Integer = 49382
        Public Shared Udn As Guid = Guid.Empty
        Public Shared DefaultProfileIndex As Integer = 0
        Public Shared StreamingProfiles As New List(Of StreamingProfile)
        Public Shared ProfileTemplates As New List(Of StreamingProfile)
        Public Shared ServerEnableSoundEffects As Boolean = False
        Public Shared ServerReplayGainMode As ReplayGainMode = ReplayGainMode.Off
        Public Shared ServerUpdatePlayStatistics As Boolean = True
        Public Shared BucketNodes As Boolean = True
        Public Shared BucketTrigger As Integer = 500
        Public Shared ContinuousOutput As Boolean = False
        Public Shared EnablePlayToSetNext As Boolean = False
        Public Shared AllowInternetConnections As Boolean = False
        Public Shared InternetHostAddress As String = ""
        Public Shared InternetUsername As String = ""
        Public Shared InternetPassword As String = ""
        Public Shared TryPortForwarding As Boolean = False
        Public Shared BandwidthConstrained As Boolean = False
        Public Shared LogDebugInfo As Boolean = False
        Private Shared ReadOnly userAgentProfiles As New Dictionary(Of String, StreamingProfile)(StringComparer.OrdinalIgnoreCase)

        Shared Sub New()
            Dim settingsUrl As String = mbApiInterface.Setting_GetPersistentStoragePath() & "UPnPSettings.dat"
            Dim profile As StreamingProfile
            If IO.File.Exists(settingsUrl) Then
                Try
                    Using stream As New IO.FileStream(settingsUrl, IO.FileMode.Open, IO.FileAccess.Read), _
                          reader As New IO.BinaryReader(stream)
                        Dim version As Integer = reader.ReadInt32()
                        EnablePlayToDevice = reader.ReadBoolean()
                        EnableContentAccess = reader.ReadBoolean()
                        ServerName = reader.ReadString()
                        ServerPort = reader.ReadInt32()
                        DefaultProfileIndex = reader.ReadInt32()
                        ServerEnableSoundEffects = reader.ReadBoolean()
                        ServerReplayGainMode = DirectCast(reader.ReadInt32(), ReplayGainMode)
                        ServerUpdatePlayStatistics = reader.ReadBoolean()
                        BucketNodes = reader.ReadBoolean()
                        BucketTrigger = reader.ReadInt32()
                        ContinuousOutput = reader.ReadBoolean()
                        EnablePlayToSetNext = reader.ReadBoolean()
                        AllowInternetConnections = reader.ReadBoolean()
                        InternetHostAddress = reader.ReadString()
                        InternetUsername = reader.ReadString()
                        InternetPassword = reader.ReadString()
                        TryPortForwarding = reader.ReadBoolean()
                        BandwidthConstrained = reader.ReadBoolean()
                        LogDebugInfo = reader.ReadBoolean()
                        Dim skipBytes As Integer = reader.ReadInt32()
                        If skipBytes > 0 Then
                            reader.ReadBytes(skipBytes)
                        End If
                        Dim profileCount As Integer = reader.ReadInt32()
                        For index As Integer = 0 To profileCount - 1
                            profile = New StreamingProfile With {
                                .ProfileName = reader.ReadString()
                            }
                            Dim userAgentCount As Integer = reader.ReadInt32()
                            profile.UserAgents = New String(userAgentCount - 1) {}
                            For agentIndex As Integer = 0 To userAgentCount - 1
                                profile.UserAgents(agentIndex) = reader.ReadString()
                            Next agentIndex
                            profile.MinimumSampleRate = reader.ReadInt32()
                            profile.MaximumSampleRate = reader.ReadInt32()
                            profile.StereoOnly = reader.ReadBoolean()
                            profile.MaximumBitDepth = reader.ReadInt32()
                            profile.TranscodeCodec = DirectCast(reader.ReadInt32(), FileCodec)
                            profile.TranscodeQuality = DirectCast(reader.ReadInt32(), EncodeQuality)
                            profile.TranscodeBitDepth = reader.ReadInt32()
                            profile.TranscodeSampleRate = reader.ReadInt32()
                            profile.WmcCompatability = reader.ReadBoolean()
                            skipBytes = reader.ReadInt32()
                            If version >= 4 Then
                                skipBytes -= 2
                                profile.PictureSize = reader.ReadUInt16()
                            End If
                            If skipBytes > 0 Then
                                reader.ReadBytes(skipBytes)
                            End If
                            StreamingProfiles.Add(profile)
                        Next index
                        If version >= 2 Then
                            Udn = New Guid(reader.ReadString())
                            If version >= 3 Then
                                IpAddress = reader.ReadString()
                            End If
                        End If
                    End Using
                Catch
                End Try
            End If
            If Udn = Guid.Empty Then
                Udn = Guid.NewGuid()
                SaveSettings()
            End If
            profile = New StreamingProfile("New Profile")
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("BubbleUPnP") With {
                .UserAgents = New String() {"BubbleUPnP"},
                .MinimumSampleRate = 11025,
                .MaximumSampleRate = 48000,
                .MaximumBitDepth = 16
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("foobar2000") With {
                .UserAgents = New String() {"foobar2000"},
                .MinimumSampleRate = 11025,
                .MaximumSampleRate = 2822400,
                .MaximumBitDepth = 24,
                .StereoOnly = False
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("JRiver Media Center") With {
                .UserAgents = New String() {"JRiver", "J. River"},
                .MinimumSampleRate = 11025,
                .MaximumSampleRate = 192000,
                .MaximumBitDepth = 24,
                .StereoOnly = False
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("Linn DS") With {
                .UserAgents = New String() {"Linn", "ChorusDS", "BubbleDS"},
                .MinimumSampleRate = 11025,
                .MaximumSampleRate = 192000,
                .MaximumBitDepth = 24
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("Playstation 3") With {
                .UserAgents = New String() {"PLAYSTATION 3"},
                .MinimumSampleRate = 44100,
                .MaximumSampleRate = 176400,
                .MaximumBitDepth = 16
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("PlugPlayer") With {
                .UserAgents = New String() {"PlugPlayer (iOS)"},
                .MinimumSampleRate = 11025,
                .MaximumSampleRate = 48000,
                .MaximumBitDepth = 16
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("UPnPlay") With {
                .UserAgents = New String() {"UPnPlay"},
                .MinimumSampleRate = 11025,
                .MaximumSampleRate = 48000,
                .MaximumBitDepth = 16
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("Windows Media Player") With {
                .UserAgents = New String() {"Windows-Media-Player", "WMFSDK", "Windows Media Player"},
                .MinimumSampleRate = 11025,
                .MaximumSampleRate = 192000,
                .MaximumBitDepth = 24,
                .StereoOnly = False,
                .WmcCompatability = True
            }
            ProfileTemplates.Add(profile)
            profile = New StreamingProfile("Xbox 360") With {
                .UserAgents = New String() {"Xenon", "Xbox"},
                .MinimumSampleRate = 44100,
                .MaximumSampleRate = 48000,
                .MaximumBitDepth = 16,
                .WmcCompatability = True
            }
            ProfileTemplates.Add(profile)
            If StreamingProfiles.Count = 0 Then
                StreamingProfiles.Add(New StreamingProfile("Generic Device"))
                For index As Integer = 1 To ProfileTemplates.Count - 1
                    StreamingProfiles.Add(ProfileTemplates(index))
                Next index
            End If
        End Sub

        Public Shared Sub SaveSettings()
            Dim settingsUrl As String = mbApiInterface.Setting_GetPersistentStoragePath() & "UPnPSettings.dat"
            Try
            Finally
                Using stream As New IO.FileStream(settingsUrl, IO.FileMode.Create, IO.FileAccess.Write, IO.FileShare.None), _
                      writer As New IO.BinaryWriter(stream)
                    writer.Write(4)
                    writer.Write(EnablePlayToDevice)
                    writer.Write(EnableContentAccess)
                    writer.Write(ServerName)
                    writer.Write(ServerPort)
                    writer.Write(DefaultProfileIndex)
                    writer.Write(ServerEnableSoundEffects)
                    writer.Write(ServerReplayGainMode)
                    writer.Write(ServerUpdatePlayStatistics)
                    writer.Write(BucketNodes)
                    writer.Write(BucketTrigger)
                    writer.Write(ContinuousOutput)
                    writer.Write(EnablePlayToSetNext)
                    writer.Write(AllowInternetConnections)
                    writer.Write(InternetHostAddress)
                    writer.Write(InternetUsername)
                    writer.Write(InternetPassword)
                    writer.Write(TryPortForwarding)
                    writer.Write(BandwidthConstrained)
                    writer.Write(LogDebugInfo)
                    writer.Write(0)
                    writer.Write(StreamingProfiles.Count)
                    For index As Integer = 0 To StreamingProfiles.Count - 1
                        Dim profile As StreamingProfile = StreamingProfiles(index)
                        writer.Write(profile.ProfileName)
                        writer.Write(profile.UserAgents.Length)
                        For agentIndex As Integer = 0 To profile.UserAgents.Length - 1
                            writer.Write(profile.UserAgents(agentIndex))
                        Next agentIndex
                        writer.Write(profile.MinimumSampleRate)
                        writer.Write(profile.MaximumSampleRate)
                        writer.Write(profile.StereoOnly)
                        writer.Write(profile.MaximumBitDepth)
                        writer.Write(profile.TranscodeCodec)
                        writer.Write(profile.TranscodeQuality)
                        writer.Write(profile.TranscodeBitDepth)
                        writer.Write(profile.TranscodeSampleRate)
                        writer.Write(profile.WmcCompatability)
                        writer.Write(2)
                        writer.Write(profile.PictureSize)
                    Next index
                    writer.Write(Udn.ToString())
                    writer.Write(IpAddress)
                End Using
            End Try
        End Sub

        Public Shared Function GetStreamingProfile(requestHeaders As Dictionary(Of String, String)) As StreamingProfile
            Dim userAgent As String
            If requestHeaders.TryGetValue("X-AV-Client-Info", userAgent) AndAlso userAgent.IndexOf("PLAYSTATION 3", StringComparison.OrdinalIgnoreCase) <> -1 Then
                userAgent = "PLAYSTATION 3"
            ElseIf Not requestHeaders.TryGetValue("User-Agent", userAgent) Then
                userAgent = ""
            End If
            SyncLock userAgentProfiles
                Dim profile As StreamingProfile = Nothing
                If Not userAgentProfiles.TryGetValue(userAgent, profile) Then
                    For index As Integer = 1 To StreamingProfiles.Count - 1
                        profile = StreamingProfiles(index)
                        For agentIndex As Integer = 0 To profile.UserAgents.Length - 1
                            If userAgent.IndexOf(profile.UserAgents(agentIndex), StringComparison.OrdinalIgnoreCase) <> -1 Then
                                userAgentProfiles.Add(userAgent, profile)
                                LogInformation("Profile", profile.ProfileName & ", useragent=" & userAgent)
                                Return profile
                            End If
                        Next agentIndex
                    Next index
                    profile = StreamingProfiles(0)
                    userAgentProfiles.Add(userAgent, profile)
                End If
                LogInformation("Profile", profile.ProfileName & ", useragent=" & userAgent)
                Return profile
            End SyncLock
        End Function
    End Class  ' Settings
End Class
