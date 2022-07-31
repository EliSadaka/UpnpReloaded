<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class SettingsDialogCopy
    Inherits System.Windows.Forms.Form

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.bandwidthIsConstrained = New System.Windows.Forms.CheckBox()
        Me.enableReplayGain = New System.Windows.Forms.CheckBox()
        Me.continuousStream = New System.Windows.Forms.CheckBox()
        Me.enableController = New System.Windows.Forms.CheckBox()
        Me.continuousStreamLabel = New System.Windows.Forms.Label()
        Me.serverSettingsPrompt = New System.Windows.Forms.Label()
        Me.bucketTreeNodes = New System.Windows.Forms.CheckBox()
        Me.bucketItemCount = New System.Windows.Forms.TextBox()
        Me.bucketTreeNodesLabel = New System.Windows.Forms.Label()
        Me.maxBitDepth = New System.Windows.Forms.ComboBox()
        Me.logDebugInfo = New System.Windows.Forms.CheckBox()
        Me.enableSoundEffects = New System.Windows.Forms.CheckBox()
        Me.stereoOnly = New System.Windows.Forms.CheckBox()
        Me.streamingProfilesPrompt = New System.Windows.Forms.Label()
        Me.sampleRateFrom = New System.Windows.Forms.ComboBox()
        Me.userAgentPrompt = New System.Windows.Forms.Label()
        Me.userAgent = New System.Windows.Forms.TextBox()
        Me.profileName = New System.Windows.Forms.TextBox()
        Me.profileNamePrompt = New System.Windows.Forms.Label()
        Me.serverName = New System.Windows.Forms.TextBox()
        Me.serverNamePrompt = New System.Windows.Forms.Label()
        Me.portPrompt = New System.Windows.Forms.Label()
        Me.port = New System.Windows.Forms.TextBox()
        Me.closeButton = New System.Windows.Forms.Button()
        Me.saveButton = New System.Windows.Forms.Button()
        Me.deviceCapabilitiesPrompt = New System.Windows.Forms.Label()
        Me.sampleRateTo = New System.Windows.Forms.ComboBox()
        Me.sampleRateFromPrompt = New System.Windows.Forms.Label()
        Me.sampleRateToPrompt = New System.Windows.Forms.Label()
        Me.maxBitDepthPrompt = New System.Windows.Forms.Label()
        Me.transcodeFormat = New System.Windows.Forms.ComboBox()
        Me.transcodeFormatPrompt = New System.Windows.Forms.Label()
        Me.channelsPrompt = New System.Windows.Forms.Label()
        Me.transcodingHeader = New System.Windows.Forms.Label()
        Me.transcodeSampleRate = New System.Windows.Forms.ComboBox()
        Me.transcodeSampleRatePrompt = New System.Windows.Forms.Label()
        Me.controllerNonContinuousInfoLabel = New System.Windows.Forms.Label()
        Me.controllerContinuousInfoLabel = New System.Windows.Forms.Label()
        Me.activeStreamingProfiles = New System.Windows.Forms.ListBox()
        Me.addProfileButton = New MusicBeePlugin.SettingsDialogCopy.SplitButton()
        Me.removeProfileButton = New System.Windows.Forms.Button()
        Me.controllerSettingsInfo = New System.Windows.Forms.Label()
        Me.serverEnableBrowse = New System.Windows.Forms.CheckBox()
        Me.controllerOutputInfoLabel = New System.Windows.Forms.Label()
        Me.submitPlayStats = New System.Windows.Forms.CheckBox()
        Me.viewButton = New System.Windows.Forms.Button()
        Me.ipAddressPrompt = New System.Windows.Forms.Label()
        Me.ipAddress = New System.Windows.Forms.ComboBox()
        Me.pictureSizePrompt = New System.Windows.Forms.Label()
        Me.pictureSize = New System.Windows.Forms.TextBox()
        Me.pictureSizePrompt2 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'bandwidthIsConstrained
        '
        Me.bandwidthIsConstrained.AutoSize = True
        Me.bandwidthIsConstrained.Location = New System.Drawing.Point(36, 590)
        Me.bandwidthIsConstrained.Name = "bandwidthIsConstrained"
        Me.bandwidthIsConstrained.Size = New System.Drawing.Size(506, 17)
        Me.bandwidthIsConstrained.TabIndex = 51
        Me.bandwidthIsConstrained.Text = "network is bandwidth constrained - transcode output to the lossy format set in th" & _
    "e device profile above"
        Me.bandwidthIsConstrained.UseVisualStyleBackColor = True
        '
        'enableReplayGain
        '
        Me.enableReplayGain.AutoSize = True
        Me.enableReplayGain.Enabled = False
        Me.enableReplayGain.Location = New System.Drawing.Point(37, 218)
        Me.enableReplayGain.Name = "enableReplayGain"
        Me.enableReplayGain.Size = New System.Drawing.Size(473, 17)
        Me.enableReplayGain.TabIndex = 15
        Me.enableReplayGain.Text = "level the playback volume using the replay gain mode active in MusicBee   [forces" & _
    " transcoding]"
        Me.enableReplayGain.UseVisualStyleBackColor = True
        '
        'continuousStream
        '
        Me.continuousStream.AutoSize = True
        Me.continuousStream.Enabled = False
        Me.continuousStream.Location = New System.Drawing.Point(36, 27)
        Me.continuousStream.Name = "continuousStream"
        Me.continuousStream.Size = New System.Drawing.Size(168, 17)
        Me.continuousStream.TabIndex = 2
        Me.continuousStream.Text = "output as a continuous stream"
        Me.continuousStream.UseVisualStyleBackColor = True
        '
        'enableController
        '
        Me.enableController.AutoSize = True
        Me.enableController.Location = New System.Drawing.Point(16, 8)
        Me.enableController.Name = "enableController"
        Me.enableController.Size = New System.Drawing.Size(232, 17)
        Me.enableController.TabIndex = 1
        Me.enableController.Text = "enable MusicBee to play to a UPnP device:"
        '
        'continuousStreamLabel
        '
        Me.continuousStreamLabel.Enabled = False
        Me.continuousStreamLabel.Location = New System.Drawing.Point(51, 44)
        Me.continuousStreamLabel.Name = "continuousStreamLabel"
        Me.continuousStreamLabel.Size = New System.Drawing.Size(523, 34)
        Me.continuousStreamLabel.TabIndex = 4
        Me.continuousStreamLabel.Text = "[enables gapless playback and cross-fading but output will lag and the device wil" & _
    "l not be able to show current track information]"
        '
        'serverSettingsPrompt
        '
        Me.serverSettingsPrompt.AutoSize = True
        Me.serverSettingsPrompt.Location = New System.Drawing.Point(16, 247)
        Me.serverSettingsPrompt.Name = "serverSettingsPrompt"
        Me.serverSettingsPrompt.Size = New System.Drawing.Size(245, 13)
        Me.serverSettingsPrompt.TabIndex = 0
        Me.serverSettingsPrompt.Text = "server settings for file streaming and library access:"
        '
        'bucketTreeNodes
        '
        Me.bucketTreeNodes.AutoSize = True
        Me.bucketTreeNodes.Enabled = False
        Me.bucketTreeNodes.Location = New System.Drawing.Point(37, 158)
        Me.bucketTreeNodes.Name = "bucketTreeNodes"
        Me.bucketTreeNodes.Size = New System.Drawing.Size(268, 17)
        Me.bucketTreeNodes.TabIndex = 11
        Me.bucketTreeNodes.Text = "bucket tree nodes by the first letter when more than"
        Me.bucketTreeNodes.UseVisualStyleBackColor = True
        '
        'bucketItemCount
        '
        Me.bucketItemCount.Enabled = False
        Me.bucketItemCount.Location = New System.Drawing.Point(306, 156)
        Me.bucketItemCount.MaxLength = 4
        Me.bucketItemCount.Name = "bucketItemCount"
        Me.bucketItemCount.Size = New System.Drawing.Size(36, 20)
        Me.bucketItemCount.TabIndex = 12
        '
        'bucketTreeNodesLabel
        '
        Me.bucketTreeNodesLabel.AutoSize = True
        Me.bucketTreeNodesLabel.Enabled = False
        Me.bucketTreeNodesLabel.Location = New System.Drawing.Point(348, 159)
        Me.bucketTreeNodesLabel.Name = "bucketTreeNodesLabel"
        Me.bucketTreeNodesLabel.Size = New System.Drawing.Size(96, 13)
        Me.bucketTreeNodesLabel.TabIndex = 0
        Me.bucketTreeNodesLabel.Text = "items are displayed"
        '
        'maxBitDepth
        '
        Me.maxBitDepth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.maxBitDepth.FormattingEnabled = True
        Me.maxBitDepth.Location = New System.Drawing.Point(411, 492)
        Me.maxBitDepth.MaxDropDownItems = 2
        Me.maxBitDepth.Name = "maxBitDepth"
        Me.maxBitDepth.Size = New System.Drawing.Size(67, 21)
        Me.maxBitDepth.TabIndex = 36
        '
        'logDebugInfo
        '
        Me.logDebugInfo.AutoSize = True
        Me.logDebugInfo.Location = New System.Drawing.Point(19, 635)
        Me.logDebugInfo.Name = "logDebugInfo"
        Me.logDebugInfo.Size = New System.Drawing.Size(127, 17)
        Me.logDebugInfo.TabIndex = 60
        Me.logDebugInfo.Text = "log debug information"
        Me.logDebugInfo.UseVisualStyleBackColor = True
        '
        'enableSoundEffects
        '
        Me.enableSoundEffects.AutoSize = True
        Me.enableSoundEffects.Enabled = False
        Me.enableSoundEffects.Location = New System.Drawing.Point(37, 198)
        Me.enableSoundEffects.Name = "enableSoundEffects"
        Me.enableSoundEffects.Size = New System.Drawing.Size(382, 17)
        Me.enableSoundEffects.TabIndex = 14
        Me.enableSoundEffects.Text = "use the equaliser and DSP effects active in MusicBee   [forces transcoding]"
        Me.enableSoundEffects.UseVisualStyleBackColor = True
        '
        'stereoOnly
        '
        Me.stereoOnly.AutoSize = True
        Me.stereoOnly.Location = New System.Drawing.Point(412, 471)
        Me.stereoOnly.Name = "stereoOnly"
        Me.stereoOnly.Size = New System.Drawing.Size(77, 17)
        Me.stereoOnly.TabIndex = 35
        Me.stereoOnly.Text = "stereo only"
        Me.stereoOnly.UseVisualStyleBackColor = True
        '
        'streamingProfilesPrompt
        '
        Me.streamingProfilesPrompt.AutoSize = True
        Me.streamingProfilesPrompt.Location = New System.Drawing.Point(35, 319)
        Me.streamingProfilesPrompt.Name = "streamingProfilesPrompt"
        Me.streamingProfilesPrompt.Size = New System.Drawing.Size(110, 13)
        Me.streamingProfilesPrompt.TabIndex = 0
        Me.streamingProfilesPrompt.Text = "DLNA device profiles:"
        '
        'sampleRateFrom
        '
        Me.sampleRateFrom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.sampleRateFrom.FormattingEnabled = True
        Me.sampleRateFrom.Location = New System.Drawing.Point(411, 448)
        Me.sampleRateFrom.Name = "sampleRateFrom"
        Me.sampleRateFrom.Size = New System.Drawing.Size(67, 21)
        Me.sampleRateFrom.TabIndex = 33
        '
        'userAgentPrompt
        '
        Me.userAgentPrompt.AutoSize = True
        Me.userAgentPrompt.Location = New System.Drawing.Point(267, 362)
        Me.userAgentPrompt.Name = "userAgentPrompt"
        Me.userAgentPrompt.Size = New System.Drawing.Size(186, 13)
        Me.userAgentPrompt.TabIndex = 0
        Me.userAgentPrompt.Text = "applies when the user-agent contains:"
        '
        'userAgent
        '
        Me.userAgent.Enabled = False
        Me.userAgent.Location = New System.Drawing.Point(315, 379)
        Me.userAgent.Name = "userAgent"
        Me.userAgent.Size = New System.Drawing.Size(205, 20)
        Me.userAgent.TabIndex = 31
        '
        'profileName
        '
        Me.profileName.Enabled = False
        Me.profileName.Location = New System.Drawing.Point(315, 336)
        Me.profileName.Name = "profileName"
        Me.profileName.Size = New System.Drawing.Size(205, 20)
        Me.profileName.TabIndex = 30
        '
        'profileNamePrompt
        '
        Me.profileNamePrompt.AutoSize = True
        Me.profileNamePrompt.Location = New System.Drawing.Point(267, 339)
        Me.profileNamePrompt.Name = "profileNamePrompt"
        Me.profileNamePrompt.Size = New System.Drawing.Size(36, 13)
        Me.profileNamePrompt.TabIndex = 0
        Me.profileNamePrompt.Text = "name:"
        '
        'serverName
        '
        Me.serverName.Location = New System.Drawing.Point(117, 267)
        Me.serverName.Name = "serverName"
        Me.serverName.Size = New System.Drawing.Size(308, 20)
        Me.serverName.TabIndex = 20
        '
        'serverNamePrompt
        '
        Me.serverNamePrompt.AutoSize = True
        Me.serverNamePrompt.Location = New System.Drawing.Point(35, 270)
        Me.serverNamePrompt.Name = "serverNamePrompt"
        Me.serverNamePrompt.Size = New System.Drawing.Size(68, 13)
        Me.serverNamePrompt.TabIndex = 0
        Me.serverNamePrompt.Text = "server name:"
        '
        'portPrompt
        '
        Me.portPrompt.AutoSize = True
        Me.portPrompt.Location = New System.Drawing.Point(340, 293)
        Me.portPrompt.Name = "portPrompt"
        Me.portPrompt.Size = New System.Drawing.Size(28, 13)
        Me.portPrompt.TabIndex = 0
        Me.portPrompt.Text = "port:"
        '
        'port
        '
        Me.port.Location = New System.Drawing.Point(373, 290)
        Me.port.MaxLength = 6
        Me.port.Name = "port"
        Me.port.Size = New System.Drawing.Size(52, 20)
        Me.port.TabIndex = 22
        '
        'closeButton
        '
        Me.closeButton.Location = New System.Drawing.Point(494, 631)
        Me.closeButton.Name = "closeButton"
        Me.closeButton.Size = New System.Drawing.Size(80, 23)
        Me.closeButton.TabIndex = 62
        Me.closeButton.Text = "Cancel"
        Me.closeButton.UseVisualStyleBackColor = True
        '
        'saveButton
        '
        Me.saveButton.Location = New System.Drawing.Point(405, 631)
        Me.saveButton.Name = "saveButton"
        Me.saveButton.Size = New System.Drawing.Size(80, 23)
        Me.saveButton.TabIndex = 61
        Me.saveButton.Text = "Save"
        Me.saveButton.UseVisualStyleBackColor = True
        '
        'deviceCapabilitiesPrompt
        '
        Me.deviceCapabilitiesPrompt.AutoSize = True
        Me.deviceCapabilitiesPrompt.Location = New System.Drawing.Point(267, 406)
        Me.deviceCapabilitiesPrompt.Name = "deviceCapabilitiesPrompt"
        Me.deviceCapabilitiesPrompt.Size = New System.Drawing.Size(144, 13)
        Me.deviceCapabilitiesPrompt.TabIndex = 0
        Me.deviceCapabilitiesPrompt.Text = "device rendering capabilities:"
        '
        'sampleRateTo
        '
        Me.sampleRateTo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.sampleRateTo.FormattingEnabled = True
        Me.sampleRateTo.Location = New System.Drawing.Point(505, 447)
        Me.sampleRateTo.Name = "sampleRateTo"
        Me.sampleRateTo.Size = New System.Drawing.Size(67, 21)
        Me.sampleRateTo.TabIndex = 34
        '
        'sampleRateFromPrompt
        '
        Me.sampleRateFromPrompt.AutoSize = True
        Me.sampleRateFromPrompt.Location = New System.Drawing.Point(293, 451)
        Me.sampleRateFromPrompt.Name = "sampleRateFromPrompt"
        Me.sampleRateFromPrompt.Size = New System.Drawing.Size(97, 13)
        Me.sampleRateFromPrompt.TabIndex = 0
        Me.sampleRateFromPrompt.Text = "output sample rate:"
        '
        'sampleRateToPrompt
        '
        Me.sampleRateToPrompt.AutoSize = True
        Me.sampleRateToPrompt.Location = New System.Drawing.Point(482, 451)
        Me.sampleRateToPrompt.Name = "sampleRateToPrompt"
        Me.sampleRateToPrompt.Size = New System.Drawing.Size(19, 13)
        Me.sampleRateToPrompt.TabIndex = 0
        Me.sampleRateToPrompt.Text = "to:"
        '
        'maxBitDepthPrompt
        '
        Me.maxBitDepthPrompt.AutoSize = True
        Me.maxBitDepthPrompt.Location = New System.Drawing.Point(293, 495)
        Me.maxBitDepthPrompt.Name = "maxBitDepthPrompt"
        Me.maxBitDepthPrompt.Size = New System.Drawing.Size(97, 13)
        Me.maxBitDepthPrompt.TabIndex = 0
        Me.maxBitDepthPrompt.Text = "maximum bit depth:"
        '
        'transcodeFormat
        '
        Me.transcodeFormat.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.transcodeFormat.FormattingEnabled = True
        Me.transcodeFormat.Location = New System.Drawing.Point(411, 538)
        Me.transcodeFormat.MaxDropDownItems = 3
        Me.transcodeFormat.Name = "transcodeFormat"
        Me.transcodeFormat.Size = New System.Drawing.Size(105, 21)
        Me.transcodeFormat.TabIndex = 40
        '
        'transcodeFormatPrompt
        '
        Me.transcodeFormatPrompt.AutoSize = True
        Me.transcodeFormatPrompt.Location = New System.Drawing.Point(293, 541)
        Me.transcodeFormatPrompt.Name = "transcodeFormatPrompt"
        Me.transcodeFormatPrompt.Size = New System.Drawing.Size(72, 13)
        Me.transcodeFormatPrompt.TabIndex = 0
        Me.transcodeFormatPrompt.Text = "output format:"
        '
        'channelsPrompt
        '
        Me.channelsPrompt.AutoSize = True
        Me.channelsPrompt.Location = New System.Drawing.Point(294, 473)
        Me.channelsPrompt.Name = "channelsPrompt"
        Me.channelsPrompt.Size = New System.Drawing.Size(53, 13)
        Me.channelsPrompt.TabIndex = 0
        Me.channelsPrompt.Text = "channels:"
        '
        'transcodingHeader
        '
        Me.transcodingHeader.AutoSize = True
        Me.transcodingHeader.Location = New System.Drawing.Point(267, 520)
        Me.transcodingHeader.Name = "transcodingHeader"
        Me.transcodingHeader.Size = New System.Drawing.Size(187, 13)
        Me.transcodingHeader.TabIndex = 0
        Me.transcodingHeader.Text = "when transcoding the source file data:"
        '
        'transcodeSampleRate
        '
        Me.transcodeSampleRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.transcodeSampleRate.FormattingEnabled = True
        Me.transcodeSampleRate.Location = New System.Drawing.Point(411, 563)
        Me.transcodeSampleRate.Name = "transcodeSampleRate"
        Me.transcodeSampleRate.Size = New System.Drawing.Size(105, 21)
        Me.transcodeSampleRate.TabIndex = 41
        '
        'transcodeSampleRatePrompt
        '
        Me.transcodeSampleRatePrompt.AutoSize = True
        Me.transcodeSampleRatePrompt.Location = New System.Drawing.Point(293, 565)
        Me.transcodeSampleRatePrompt.Name = "transcodeSampleRatePrompt"
        Me.transcodeSampleRatePrompt.Size = New System.Drawing.Size(97, 13)
        Me.transcodeSampleRatePrompt.TabIndex = 0
        Me.transcodeSampleRatePrompt.Text = "output sample rate:"
        '
        'controllerNonContinuousInfoLabel
        '
        Me.controllerNonContinuousInfoLabel.AutoSize = True
        Me.controllerNonContinuousInfoLabel.Enabled = False
        Me.controllerNonContinuousInfoLabel.Location = New System.Drawing.Point(35, 112)
        Me.controllerNonContinuousInfoLabel.Name = "controllerNonContinuousInfoLabel"
        Me.controllerNonContinuousInfoLabel.Size = New System.Drawing.Size(471, 13)
        Me.controllerNonContinuousInfoLabel.TabIndex = 0
        Me.controllerNonContinuousInfoLabel.Text = "-  source file data will be transcoded when volume leveling or sound effects are " & _
    "active in MusicBee"
        '
        'controllerContinuousInfoLabel
        '
        Me.controllerContinuousInfoLabel.AutoSize = True
        Me.controllerContinuousInfoLabel.Enabled = False
        Me.controllerContinuousInfoLabel.Location = New System.Drawing.Point(35, 112)
        Me.controllerContinuousInfoLabel.Name = "controllerContinuousInfoLabel"
        Me.controllerContinuousInfoLabel.Size = New System.Drawing.Size(390, 13)
        Me.controllerContinuousInfoLabel.TabIndex = 0
        Me.controllerContinuousInfoLabel.Text = "-  source file data will be transcoded using the DLNA device profile settings bel" & _
    "ow"
        Me.controllerContinuousInfoLabel.Visible = False
        '
        'activeStreamingProfiles
        '
        Me.activeStreamingProfiles.FormattingEnabled = True
        Me.activeStreamingProfiles.Location = New System.Drawing.Point(36, 336)
        Me.activeStreamingProfiles.Name = "activeStreamingProfiles"
        Me.activeStreamingProfiles.Size = New System.Drawing.Size(210, 82)
        Me.activeStreamingProfiles.TabIndex = 23
        '
        'addProfileButton
        '
        Me.addProfileButton.Location = New System.Drawing.Point(36, 420)
        Me.addProfileButton.Name = "addProfileButton"
        Me.addProfileButton.Size = New System.Drawing.Size(75, 23)
        Me.addProfileButton.TabIndex = 23
        Me.addProfileButton.Text = "Add"
        Me.addProfileButton.UseVisualStyleBackColor = True
        '
        'removeProfileButton
        '
        Me.removeProfileButton.Location = New System.Drawing.Point(117, 420)
        Me.removeProfileButton.Name = "removeProfileButton"
        Me.removeProfileButton.Size = New System.Drawing.Size(75, 23)
        Me.removeProfileButton.TabIndex = 24
        Me.removeProfileButton.Text = "Remove"
        Me.removeProfileButton.UseVisualStyleBackColor = True
        '
        'controllerSettingsInfo
        '
        Me.controllerSettingsInfo.AutoSize = True
        Me.controllerSettingsInfo.Enabled = False
        Me.controllerSettingsInfo.Location = New System.Drawing.Point(35, 94)
        Me.controllerSettingsInfo.Name = "controllerSettingsInfo"
        Me.controllerSettingsInfo.Size = New System.Drawing.Size(376, 13)
        Me.controllerSettingsInfo.TabIndex = 0
        Me.controllerSettingsInfo.Text = "-  the replay gain, equaliser and DSP effects active in MusicBee will be applied"
        '
        'serverEnableBrowse
        '
        Me.serverEnableBrowse.AutoSize = True
        Me.serverEnableBrowse.Location = New System.Drawing.Point(16, 137)
        Me.serverEnableBrowse.Name = "serverEnableBrowse"
        Me.serverEnableBrowse.Size = New System.Drawing.Size(342, 17)
        Me.serverEnableBrowse.TabIndex = 10
        Me.serverEnableBrowse.Text = "enable UPnP devices to browse and play from the MusicBee library"
        '
        'controllerOutputInfoLabel
        '
        Me.controllerOutputInfoLabel.AutoSize = True
        Me.controllerOutputInfoLabel.Enabled = False
        Me.controllerOutputInfoLabel.Location = New System.Drawing.Point(35, 76)
        Me.controllerOutputInfoLabel.Name = "controllerOutputInfoLabel"
        Me.controllerOutputInfoLabel.Size = New System.Drawing.Size(474, 13)
        Me.controllerOutputInfoLabel.TabIndex = 64
        Me.controllerOutputInfoLabel.Text = "-  the UPnP device can be selected as an output device in the MusicBee Player pre" & _
    "ferences dialog"
        '
        'submitPlayStats
        '
        Me.submitPlayStats.AutoSize = True
        Me.submitPlayStats.Enabled = False
        Me.submitPlayStats.Location = New System.Drawing.Point(37, 178)
        Me.submitPlayStats.Name = "submitPlayStats"
        Me.submitPlayStats.Size = New System.Drawing.Size(185, 17)
        Me.submitPlayStats.TabIndex = 13
        Me.submitPlayStats.Text = "update play statistics in MusicBee"
        Me.submitPlayStats.UseVisualStyleBackColor = True
        '
        'viewButton
        '
        Me.viewButton.Location = New System.Drawing.Point(147, 631)
        Me.viewButton.Name = "viewButton"
        Me.viewButton.Size = New System.Drawing.Size(75, 23)
        Me.viewButton.TabIndex = 65
        Me.viewButton.Text = "View"
        Me.viewButton.UseVisualStyleBackColor = True
        Me.viewButton.Visible = False
        '
        'ipAddressPrompt
        '
        Me.ipAddressPrompt.AutoSize = True
        Me.ipAddressPrompt.Location = New System.Drawing.Point(35, 293)
        Me.ipAddressPrompt.Name = "ipAddressPrompt"
        Me.ipAddressPrompt.Size = New System.Drawing.Size(60, 13)
        Me.ipAddressPrompt.TabIndex = 0
        Me.ipAddressPrompt.Text = "IP address:"
        '
        'ipAddress
        '
        Me.ipAddress.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ipAddress.FormattingEnabled = True
        Me.ipAddress.Location = New System.Drawing.Point(117, 290)
        Me.ipAddress.Name = "ipAddress"
        Me.ipAddress.Size = New System.Drawing.Size(170, 21)
        Me.ipAddress.TabIndex = 21
        '
        'pictureSizePrompt
        '
        Me.pictureSizePrompt.AutoSize = True
        Me.pictureSizePrompt.Location = New System.Drawing.Point(293, 426)
        Me.pictureSizePrompt.Name = "pictureSizePrompt"
        Me.pictureSizePrompt.Size = New System.Drawing.Size(109, 13)
        Me.pictureSizePrompt.TabIndex = 0
        Me.pictureSizePrompt.Text = "maximum picture size:"
        '
        'pictureSize
        '
        Me.pictureSize.Location = New System.Drawing.Point(411, 424)
        Me.pictureSize.MaxLength = 4
        Me.pictureSize.Name = "pictureSize"
        Me.pictureSize.Size = New System.Drawing.Size(67, 20)
        Me.pictureSize.TabIndex = 32
        '
        'pictureSizePrompt2
        '
        Me.pictureSizePrompt2.AutoSize = True
        Me.pictureSizePrompt2.Location = New System.Drawing.Point(480, 426)
        Me.pictureSizePrompt2.Name = "pictureSizePrompt2"
        Me.pictureSizePrompt2.Size = New System.Drawing.Size(18, 13)
        Me.pictureSizePrompt2.TabIndex = 66
        Me.pictureSizePrompt2.Text = "px"
        '
        'SettingsDialogCopy
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(594, 662)
        Me.Controls.Add(Me.pictureSize)
        Me.Controls.Add(Me.pictureSizePrompt2)
        Me.Controls.Add(Me.pictureSizePrompt)
        Me.Controls.Add(Me.port)
        Me.Controls.Add(Me.ipAddress)
        Me.Controls.Add(Me.ipAddressPrompt)
        Me.Controls.Add(Me.viewButton)
        Me.Controls.Add(Me.submitPlayStats)
        Me.Controls.Add(Me.bucketItemCount)
        Me.Controls.Add(Me.controllerOutputInfoLabel)
        Me.Controls.Add(Me.serverEnableBrowse)
        Me.Controls.Add(Me.controllerSettingsInfo)
        Me.Controls.Add(Me.sampleRateFrom)
        Me.Controls.Add(Me.sampleRateTo)
        Me.Controls.Add(Me.portPrompt)
        Me.Controls.Add(Me.profileName)
        Me.Controls.Add(Me.userAgent)
        Me.Controls.Add(Me.removeProfileButton)
        Me.Controls.Add(Me.addProfileButton)
        Me.Controls.Add(Me.activeStreamingProfiles)
        Me.Controls.Add(Me.controllerContinuousInfoLabel)
        Me.Controls.Add(Me.controllerNonContinuousInfoLabel)
        Me.Controls.Add(Me.continuousStream)
        Me.Controls.Add(Me.serverName)
        Me.Controls.Add(Me.transcodeSampleRate)
        Me.Controls.Add(Me.transcodeFormat)
        Me.Controls.Add(Me.maxBitDepth)
        Me.Controls.Add(Me.stereoOnly)
        Me.Controls.Add(Me.transcodeSampleRatePrompt)
        Me.Controls.Add(Me.transcodingHeader)
        Me.Controls.Add(Me.channelsPrompt)
        Me.Controls.Add(Me.transcodeFormatPrompt)
        Me.Controls.Add(Me.maxBitDepthPrompt)
        Me.Controls.Add(Me.sampleRateToPrompt)
        Me.Controls.Add(Me.sampleRateFromPrompt)
        Me.Controls.Add(Me.deviceCapabilitiesPrompt)
        Me.Controls.Add(Me.saveButton)
        Me.Controls.Add(Me.closeButton)
        Me.Controls.Add(Me.serverNamePrompt)
        Me.Controls.Add(Me.profileNamePrompt)
        Me.Controls.Add(Me.userAgentPrompt)
        Me.Controls.Add(Me.streamingProfilesPrompt)
        Me.Controls.Add(Me.enableSoundEffects)
        Me.Controls.Add(Me.logDebugInfo)
        Me.Controls.Add(Me.bucketTreeNodesLabel)
        Me.Controls.Add(Me.bucketTreeNodes)
        Me.Controls.Add(Me.serverSettingsPrompt)
        Me.Controls.Add(Me.continuousStreamLabel)
        Me.Controls.Add(Me.enableController)
        Me.Controls.Add(Me.enableReplayGain)
        Me.Controls.Add(Me.bandwidthIsConstrained)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "SettingsDialogCopy"
        Me.ShowIcon = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "UPnP Plugin Settings"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Private bandwidthIsConstrained As System.Windows.Forms.CheckBox
    Private enableReplayGain As System.Windows.Forms.CheckBox
    Private continuousStream As System.Windows.Forms.CheckBox
    Private enableController As System.Windows.Forms.CheckBox
    Private continuousStreamLabel As System.Windows.Forms.Label
    Private serverSettingsPrompt As System.Windows.Forms.Label
    Private bucketTreeNodes As System.Windows.Forms.CheckBox
    Private bucketItemCount As System.Windows.Forms.TextBox
    Private bucketTreeNodesLabel As System.Windows.Forms.Label
    Private maxBitDepth As System.Windows.Forms.ComboBox
    Private logDebugInfo As System.Windows.Forms.CheckBox
    Private enableSoundEffects As System.Windows.Forms.CheckBox
    Private stereoOnly As System.Windows.Forms.CheckBox
    Private streamingProfilesPrompt As System.Windows.Forms.Label
    Private sampleRateFrom As System.Windows.Forms.ComboBox
    Private userAgentPrompt As System.Windows.Forms.Label
    Private userAgent As System.Windows.Forms.TextBox
    Private profileName As System.Windows.Forms.TextBox
    Private profileNamePrompt As System.Windows.Forms.Label
    Private serverName As System.Windows.Forms.TextBox
    Private serverNamePrompt As System.Windows.Forms.Label
    Private portPrompt As System.Windows.Forms.Label
    Private port As System.Windows.Forms.TextBox
    Private closeButton As System.Windows.Forms.Button
    Private saveButton As System.Windows.Forms.Button
    Private deviceCapabilitiesPrompt As System.Windows.Forms.Label
    Private sampleRateTo As System.Windows.Forms.ComboBox
    Private sampleRateFromPrompt As System.Windows.Forms.Label
    Private sampleRateToPrompt As System.Windows.Forms.Label
    Private maxBitDepthPrompt As System.Windows.Forms.Label
    Private transcodeFormat As System.Windows.Forms.ComboBox
    Private transcodeFormatPrompt As System.Windows.Forms.Label
    Private channelsPrompt As System.Windows.Forms.Label
    Private transcodingHeader As System.Windows.Forms.Label
    Private transcodeSampleRate As System.Windows.Forms.ComboBox
    Private transcodeSampleRatePrompt As System.Windows.Forms.Label
    Private controllerNonContinuousInfoLabel As System.Windows.Forms.Label
    Private controllerContinuousInfoLabel As System.Windows.Forms.Label
    Private activeStreamingProfiles As System.Windows.Forms.ListBox
    Private addProfileButton As SplitButton
    Private removeProfileButton As System.Windows.Forms.Button
    Private controllerSettingsInfo As System.Windows.Forms.Label
    Private serverEnableBrowse As System.Windows.Forms.CheckBox
    Private controllerOutputInfoLabel As System.Windows.Forms.Label
    Private submitPlayStats As System.Windows.Forms.CheckBox
    Private viewButton As System.Windows.Forms.Button
    Private ipAddressPrompt As System.Windows.Forms.Label
    Private ipAddress As System.Windows.Forms.ComboBox
    Private pictureSizePrompt As System.Windows.Forms.Label
    Private pictureSize As System.Windows.Forms.TextBox
    Private pictureSizePrompt2 As System.Windows.Forms.Label
End Class
