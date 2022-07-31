Imports System.Text
Imports System.Xml
Imports System.Runtime.InteropServices
Imports System.Reflection
Imports System.Threading
Imports System.Net
Imports System.Net.Sockets

Partial Public Class Plugin
    Private NotInheritable Class MediaServerDevice
        Inherits UpnpDevice
        Private currentPlayStatsUrl As String = Nothing
        Private skipCountTriggerStartTime As Long
        Private skipCountTriggerEndTime As Long
        Private ReadOnly playStatisticsTimer As New Timer(AddressOf playStatisticsTimer_Tick, Nothing, Timeout.Infinite, Timeout.Infinite)
        Private ReadOnly usedStreamHandle As New HashSet(Of Integer)
        Private Shared requestCounter As Integer = 0

        Public Sub New(udn As Guid)
            MyBase.New(udn)
            Services.Add(New ConnectionManagerService(server))
            Services.Add(New ContentDirectoryService(server))
            Services.Add(New MediaReceiverRegistrarService(server))
            server.HttpServer.AddRoute("HEAD", "/Files/*", New HttpRouteDelegate(AddressOf GetFile))
            server.HttpServer.AddRoute("GET", "/Files/*", New HttpRouteDelegate(AddressOf GetFile))
            server.HttpServer.AddRoute("HEAD", "/Encode/*", New HttpRouteDelegate(AddressOf GetEncodedFile))
            server.HttpServer.AddRoute("GET", "/Encode/*", New HttpRouteDelegate(AddressOf GetEncodedFile))
            server.HttpServer.AddRoute("HEAD", "/Thumbnail/*", New HttpRouteDelegate(AddressOf GetThumbnailFile))
            server.HttpServer.AddRoute("GET", "/Thumbnail/*", New HttpRouteDelegate(AddressOf GetThumbnailFile))
            server.HttpServer.AddRoute("HEAD", "/Web/Images/htmllogo48.png", New HttpRouteDelegate(AddressOf GetWebPng))
            server.HttpServer.AddRoute("GET", "/Web/Images/htmllogo48.png", New HttpRouteDelegate(AddressOf GetWebPng))
            server.HttpServer.AddRoute("HEAD", "/Web/Images/htmllogo64.png", New HttpRouteDelegate(AddressOf GetWebPng))
            server.HttpServer.AddRoute("GET", "/Web/Images/htmllogo64.png", New HttpRouteDelegate(AddressOf GetWebPng))
        End Sub

        Public Sub Dispose()
            [Stop]()
            playStatisticsTimer.Dispose()
        End Sub

        Public ReadOnly Property HttpServer() As HttpServer
            Get
                Return server.HttpServer
            End Get
        End Property

        Public Overrides Sub Start()
            If Settings.TryPortForwarding Then
                UpnpControlPoint.StartPortForwarding()
            End If
            MyBase.Start()
        End Sub

        Protected Overrides Sub WriteSpecificDescription(writer As XmlTextWriter)
            writer.WriteElementString("dlna", "X_DLNADOC", "urn:schemas-dlna-org:device-1-0", "DMS-1.50")
            writer.WriteStartElement("iconList")
            For Each size As String In New String() {"64", "48"}
                writer.WriteStartElement("icon")
                writer.WriteElementString("mimetype", "image/jpeg")
                writer.WriteElementString("width", size)
                writer.WriteElementString("height", size)
                writer.WriteElementString("depth", "32")
                writer.WriteElementString("url", String.Format("/web/images/htmllogo{0}.png", size))
                writer.WriteEndElement()
            Next size
            writer.WriteEndElement()
        End Sub

        Private Sub GetFile(request As HttpRequest)
            Dim filename As String = request.Url.Substring(request.Url.LastIndexOf("/"c) + 1)
            If filename.Length < 19 Then
                LogInformation("GetFile", "Bad filename=" & request.Url)
                Throw New HttpException(404, "Bad parameter")
            End If
            Dim directory As ItemManager = ItemManager.GetItemManager(request.Headers)
            Dim id As String = filename.Substring(0, 16)
            Dim musicBeePlayToMode As Boolean = (filename.Chars(16) = "p"c)
            Dim mime As String = "audio/" & filename.Substring(If(Not musicBeePlayToMode, 17, 18))
            Dim url As String = Nothing
            Dim duration As TimeSpan
            If Not directory.TryGetFileInfo(id, url, duration) Then
                LogInformation("GetFile", "Bad id=" & request.Url)
                Throw New HttpException(404, "Bad parameter")
            End If
            Dim response As HttpResponse = request.Response
            If Not IO.File.Exists(url) Then
                LogInformation("GetFile", "Not found=" & request.Url)
                Throw New HttpException(404, "File not found")
            End If
            Dim counter As Integer = Interlocked.Increment(requestCounter)
            If Settings.LogDebugInfo Then
                Dim localAddress As String = "unknown address"
                Dim remoteAddress As String = "unknown address"
                If TypeOf request.Socket.Client.LocalEndPoint Is IPEndPoint Then
                    localAddress = DirectCast(request.Socket.Client.LocalEndPoint, IPEndPoint).Address.ToString()
                End If
                If TypeOf request.Socket.Client.RemoteEndPoint Is IPEndPoint Then
                    remoteAddress = DirectCast(request.Socket.Client.RemoteEndPoint, IPEndPoint).Address.ToString()
                End If
                LogInformation("GetFile[" & counter & "] " & localAddress, request.Method & " " & url & " to " & remoteAddress)
            End If
            Using stream As New IO.FileStream(url, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read, 65536, IO.FileOptions.SequentialScan)
                Dim fileLength As Long = stream.Length
                Dim range As String = Nothing
                If request.Headers.TryGetValue("range", range) Then
                    Dim values As String() = range.Split("="c).Last().Split("-"c).[Select](Function(a) a.Trim()).ToArray()
                    Dim byteRangeStart As Long = Long.Parse(values(0))
                    Dim byteRangeEnd As Long = byteRangeStart
                    If byteRangeStart < 0 Then
                        byteRangeStart += fileLength
                    End If
                    If values.Length < 2 OrElse Not Long.TryParse(values(1), byteRangeEnd) Then
                        byteRangeEnd = fileLength - 1
                    End If
                    If Settings.LogDebugInfo Then
                        LogInformation("GetFile", "range=" & String.Format("bytes {0}-{1}/{2}", byteRangeStart, byteRangeEnd, fileLength))
                    End If
                    response.AddHeader("Content-Range", String.Format("bytes {0}-{1}/{2}", byteRangeStart, byteRangeEnd, fileLength))
                    fileLength = byteRangeEnd - byteRangeStart + 1
                    response.StateCode = 206
                    stream.Position = byteRangeStart
                End If
                response.AddHeader(HttpHeader.ContentLength, fileLength.ToString())
                response.AddHeader(HttpHeader.ContentType, mime)
                response.AddHeader(HttpHeader.AcceptRanges, "bytes")
                response.AddHeader("transferMode.dlna.org", "Streaming")
                response.AddHeader("contentFeatures.dlna.org", directory.GetFileFeature(url, (duration.Ticks <= 0)))
                response.SendHeaders()
                If request.Method = "GET" Then
                    'Dim data(65535) As Byte
                    'Dim dataHandle As GCHandle
                    'dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned)
                    'Do
                    '    Dim count As Integer = stream.Read(data, 0, data.Length)
                    '    Debug.WriteLine(count)
                    '    If count <= 0 Then Exit Do
                    '    If send(request.Socket.Client.Handle, dataHandle.AddrOfPinnedObject, count, 0) = -1 Then
                    '        Debug.WriteLine("err=" & WSAGetLastError())
                    '        Exit Do
                    '    End If
                    '    Thread.Sleep(40)
                    'Loop
                    'Debug.WriteLine("done 1")
                    'shutdown(request.Socket.Client.Handle, 2)
                    'Debug.WriteLine("done 2")
                    'dataHandle.Free()
                    'Exit Sub
                    Dim startTime As Long
                    Dim errorCode As Integer
                    Dim playTime As Long
                    sendDataBarrier.Wait()
                    Try
                        If Not musicBeePlayToMode AndAlso range Is Nothing Then
                            StartPlayStatisticsTriggerTimer(url, duration)
                        End If
                        startTime = DateTime.UtcNow.Ticks
                        errorCode = Sockets_Stream_File(stream.SafeFileHandle.DangerousGetHandle, CUInt(fileLength), request.Socket.Client.Handle)
                        playTime = (DateTime.UtcNow.Ticks - startTime) \ TimeSpan.TicksPerMillisecond
                    Finally
                        sendDataBarrier.Release()
                    End Try
                    If Settings.LogDebugInfo Then
                        LogInformation("GetFile[" & counter & "]", "exit=" & errorCode & ", playtime=" & playTime)
                    End If
                End If
            End Using
        End Sub
        '<DllImport("ws2_32.dll", CharSet:=CharSet.Unicode)> _
        'Private Shared Function send(socketHandle As IntPtr, data As IntPtr, length As Integer, flags As Integer) As Integer
        'End Function
        '<DllImport("ws2_32.dll", CharSet:=CharSet.Unicode)> _
        'Private Shared Function shutdown(socketHandle As IntPtr, flags As Integer) As Integer
        'End Function
        '<DllImport("ws2_32.dll", CharSet:=CharSet.Unicode)> _
        'Private Shared Function WSAGetLastError() As Integer
        'End Function

        Private Sub GetEncodedFile(request As HttpRequest)
            Dim filename As String = request.Url.Substring(request.Url.LastIndexOf("/"c) + 1)
            If filename.Length < 19 Then
                LogInformation("GetEncodedFile", "Bad filename=" & request.Url)
                Throw New HttpException(404, "Bad parameter")
            End If
            Dim extIndex As Integer = filename.LastIndexOf("."c)
            Dim id As String = filename.Substring(0, 16)
            Dim targetMime As String = "audio/" & filename.Substring(extIndex + 1)
            Dim streamHandle As Integer = If(extIndex = 17, 0, CInt(filename.Substring(16, extIndex - 16)))
            Dim musicBeePlayToMode As Boolean = (streamHandle <> 0)
            If streamHandle <> 0 AndAlso String.Compare(request.Method, "GET", StringComparison.OrdinalIgnoreCase) = 0 Then
                SyncLock usedStreamHandle
                    If Not usedStreamHandle.Add(streamHandle) Then
                        ' stop closed stream handle being re-used for seek
                        streamHandle = 0
                    End If
                End SyncLock
            End If
            Dim encoder As AudioEncoder
            Select Case targetMime
                Case "audio/mpeg", "audio/mp3", "audio/x-mp3"
                    encoder = New AudioEncoder(FileCodec.Mp3)
                Case "audio/m4a", "audio/mp4", "audio/aac", "audio/x-aac"
                    encoder = New AudioEncoder(FileCodec.Aac)
                Case "audio/x-ogg", "audio/ogg"
                    encoder = New AudioEncoder(FileCodec.Ogg)
                Case "audio/x-ms-wma", "audio/wma", "audio/x-wma"
                    encoder = New AudioEncoder(FileCodec.Wma)
                Case "audio/wav", "audio/x-wav"
                    encoder = New AudioEncoder(FileCodec.Wave)
                Case Else
                    encoder = New AudioEncoder(FileCodec.Pcm)
            End Select
            Dim directory As ItemManager = ItemManager.GetItemManager(request.Headers)
            Dim isContinuousStream As Boolean = False
            Dim url As String
            Dim duration As TimeSpan
            If id = "continuousstream" Then
                isContinuousStream = True
                url = Nothing
                duration = TimeSpan.Zero
            ElseIf Not directory.TryGetFileInfo(id, url, duration) Then
                LogInformation("GetEncodedFile", "Bad id=" & request.Url)
                Throw New HttpException(404, "Bad parameter")
            ElseIf streamHandle = 0 Then
                streamHandle = mbApiInterface.Player_OpenStreamHandle(url, musicBeePlayToMode, Settings.ServerEnableSoundEffects, Settings.ServerReplayGainMode)
            End If
            If streamHandle = 0 Then
                LogInformation("GetEncodedFile", "Stream zero=" & request.Url)
                Throw New HttpException(404, "File not found")
            Else
                Dim response As HttpResponse = request.Response
                Dim streamingProfile As StreamingProfile = Settings.GetStreamingProfile(request.Headers)
                Dim fileDuration As Double = 0
                Dim fileDecodeStartPos As Long = 0
                Dim fileEncodeLength As Long = 0
                Dim isPartialContent As Boolean = False
                Dim isPcmData As Boolean = (encoder.Codec = FileCodec.Pcm OrElse encoder.Codec = FileCodec.Wave)
                Dim sampleRate As Integer
                Dim channelCount As Integer
                Dim streamCodec As FileCodec
                Dim bitDepth As Integer
                Bass.TryGetStreamInformation(streamHandle, sampleRate, channelCount, streamCodec)
                If isContinuousStream Then
                    fileDuration = 0
                ElseIf duration.Ticks <= 0 Then
                    fileDuration = Bass.GetDecodedDuration(streamHandle)
                    If fileDuration > 0 Then
                        duration = New TimeSpan(CLng(fileDuration * TimeSpan.TicksPerSecond))
                    End If
                Else
                    fileDuration = duration.Ticks / TimeSpan.TicksPerSecond
                End If
                If streamingProfile.TranscodeSampleRate <> -1 Then
                    sampleRate = streamingProfile.TranscodeSampleRate
                ElseIf sampleRate < streamingProfile.MinimumSampleRate Then
                    sampleRate = streamingProfile.MinimumSampleRate
                ElseIf sampleRate > streamingProfile.MaximumSampleRate Then
                    sampleRate = streamingProfile.MaximumSampleRate
                End If
                If streamingProfile.StereoOnly OrElse Not isPcmData Then
                    channelCount = 2
                End If
                bitDepth = If(Not isPcmData OrElse isContinuousStream, 16, streamingProfile.TranscodeBitDepth)
                Dim sourceStreamHande As Integer = streamHandle
                Dim streamStartPosition As Long = Bass.GetStreamPosition(sourceStreamHande)
                streamHandle = encoder.GetEncodeStreamHandle(sourceStreamHande, sampleRate, channelCount, bitDepth, (musicBeePlayToMode AndAlso Not isContinuousStream))
                Dim counter As Integer = Interlocked.Increment(requestCounter)
                Dim logId As String = "GetEncodedFile[" & counter & "]"
                If fileDuration <= 0 Then 'duration.Ticks <= 0 Then
                    response.AddHeader("transferMode.dlna.org", "Streaming")
                    response.AddHeader("contentFeatures.dlna.org", directory.GetContinuousStreamFeature(encoder.Codec))
                    response.AddHeader(HttpHeader.AcceptRanges, "none")
                    If isContinuousStream Then
                        fileEncodeLength = 4294967294
                        response.AddHeader(HttpHeader.ContentLength, "4294967294")
                    End If
                Else
                    If isPcmData Then
                        Dim decodedLength As Long = Bass.GetDecodedLength(streamHandle, fileDuration)
                        If bitDepth <> 24 Then
                            fileEncodeLength = decodedLength \ 2
                        Else
                            fileEncodeLength = (decodedLength * 3) \ 4
                        End If
                    End If
                    response.AddHeader("X-AvailableSeekRange", String.Format(System.Globalization.CultureInfo.InvariantCulture, "1 npt=0.0-{0:0.000}", duration.TotalSeconds))
                    Dim npt As String
                    If request.Headers.TryGetValue("timeSeekRange.dlna.org", npt) OrElse request.Headers.TryGetValue("npt", npt) Then
                        Dim timeRange As String() = npt.Split("="c).Last().Split("-"c).[Select](Function(a) a.Trim()).ToArray()
                        Dim timeRangeStart As Double = 0
                        If Not Double.TryParse(timeRange(0), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, timeRangeStart) Then
                            Dim startSpan As TimeSpan
                            If TimeSpan.TryParse(timeRange(0), startSpan) Then
                                timeRangeStart = startSpan.TotalSeconds
                            End If
                        End If
                        If timeRangeStart < 0 Then
                            timeRangeStart += duration.TotalSeconds
                        End If
                        Dim timeRangeEnd As Double
                        If timeRange.Length < 2 Then
                            timeRangeEnd = duration.TotalSeconds
                        ElseIf Not Double.TryParse(timeRange(1), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, timeRangeEnd) Then
                            Dim endSpan As TimeSpan
                            If Not TimeSpan.TryParse(timeRange(1), endSpan) Then
                                timeRangeEnd = endSpan.TotalSeconds
                            End If
                        Else
                            timeRangeEnd = duration.TotalSeconds
                        End If
                        response.AddHeader("Vary", "timeSeekRange.dlna.org")
                        response.AddHeader("timeSeekRange.dlna.org", String.Format(System.Globalization.CultureInfo.InvariantCulture, "npt={0:0.000}-{1:0.000}", timeRangeStart, timeRangeEnd)) ''/{2:0.000}", timeRangeStart, timeRangeEnd, duration.TotalSeconds))
                        If Settings.LogDebugInfo Then
                            LogInformation(logId, String.Format(System.Globalization.CultureInfo.InvariantCulture, "npt={0:0.000}-{1:0.000}", timeRangeStart, timeRangeEnd))
                        End If
                        response.AddHeader(HttpHeader.AcceptRanges, "none")
                        response.AddHeader("contentFeatures.dlna.org", directory.GetEncodeFeature(encoder.Codec, False))
                        isPartialContent = (timeRangeStart > 0)
                        fileDecodeStartPos = Bass.GetDecodedLength(streamHandle, timeRangeStart)
                        If isPcmData Then
                            ' shouldnt need to happen for pcm streams but just in case
                            fileEncodeLength = Bass.GetDecodedLength(streamHandle, fileDuration) - fileDecodeStartPos
                            If bitDepth <> 24 Then
                                fileEncodeLength \= 2
                            Else
                                fileEncodeLength = (fileEncodeLength * 3) \ 4
                            End If
                        End If
                    ElseIf Not isPcmData OrElse fileEncodeLength <= 0 Then
                        response.AddHeader(HttpHeader.AcceptRanges, "none")
                        response.AddHeader("contentFeatures.dlna.org", directory.GetEncodeFeature(encoder.Codec, (fileDuration <= 0)))
                    Else
                        response.AddHeader(HttpHeader.AcceptRanges, "bytes")
                        response.AddHeader("contentFeatures.dlna.org", directory.GetEncodeFeature(encoder.Codec, False))
                        Dim contentLength As Long = fileEncodeLength
                        If encoder.Codec = FileCodec.Wave Then
                            contentLength += 44
                        End If
                        Dim byteRange As String = Nothing
                        If request.Headers.TryGetValue("range", byteRange) Then
                            Dim values As String() = byteRange.Split("="c).Last().Split("-"c).[Select](Function(a) a.Trim()).ToArray()
                            Dim byteRangeStart As Long
                            Dim byteRangeEnd As Long
                            If Not Long.TryParse(values(0), byteRangeStart) Then
                                byteRangeStart = 0
                            ElseIf byteRangeStart < 0 Then
                                byteRangeStart += contentLength
                            End If
                            If values.Length < 2 OrElse Not Long.TryParse(values(1), byteRangeEnd) Then
                                byteRangeEnd = contentLength - 1
                            End If
                            response.StateCode = 206
                            response.AddHeader("Content-Range", String.Format("bytes {0}-{1}/{2}", byteRangeStart, byteRangeEnd, contentLength))
                            If Settings.LogDebugInfo Then
                                LogInformation(logId, "range=" & String.Format("bytes {0}-{1}/{2}", byteRangeStart, byteRangeEnd, contentLength))
                            End If
                            isPartialContent = (byteRangeStart > 0)
                            contentLength = byteRangeEnd - byteRangeStart + 1
                            fileEncodeLength = contentLength
                            If encoder.Codec = FileCodec.Wave AndAlso byteRangeStart > 0 Then
                                byteRangeStart -= 44
                            End If
                            If bitDepth <> 24 Then
                                fileDecodeStartPos = byteRangeStart * 2
                            Else
                                fileDecodeStartPos = (byteRangeStart * 4) \ 3
                            End If
                        End If
                        response.AddHeader(HttpHeader.ContentLength, contentLength.ToString())
                    End If
                    response.AddHeader("transferMode.dlna.org", "Streaming")
                End If
                If encoder.Codec = FileCodec.Pcm Then
                    targetMime = "audio/L" & If(bitDepth <> 24, "16", "24") & ";rate=" & sampleRate & ";channels=" & channelCount
                End If
                response.AddHeader(HttpHeader.ContentType, targetMime)
                If Settings.LogDebugInfo Then
                    Dim localAddress As String = "unknown address"
                    Dim remoteAddress As String = "unknown address"
                    If TypeOf request.Socket.Client.LocalEndPoint Is IPEndPoint Then
                        localAddress = DirectCast(request.Socket.Client.LocalEndPoint, IPEndPoint).Address.ToString()
                    End If
                    If TypeOf request.Socket.Client.RemoteEndPoint Is IPEndPoint Then
                        remoteAddress = DirectCast(request.Socket.Client.RemoteEndPoint, IPEndPoint).Address.ToString()
                    End If
                    LogInformation(logId & " " & localAddress, request.Method & " " & url & " to " & remoteAddress & "; mime=" & targetMime & ",rate=" & sampleRate & ",channels=" & channelCount)
                End If
                response.SendHeaders()
                If String.Compare(request.Method, "GET", StringComparison.OrdinalIgnoreCase) = 0 Then
                    If fileDecodeStartPos > 0 Then
                        Bass.SetEncodeStreamPosition(sourceStreamHande, streamStartPosition + fileDecodeStartPos)
                    ElseIf Not musicBeePlayToMode Then
                        StartPlayStatisticsTriggerTimer(url, duration)
                    End If
                    encoder.StartEncode(url, streamHandle, isPartialContent, fileEncodeLength, bitDepth, request.Socket.Client.Handle, logId)
                End If
            End If
        End Sub

        Private Sub StartPlayStatisticsTriggerTimer(url As String, duration As TimeSpan)
            If Not Settings.ServerUpdatePlayStatistics Then
                currentPlayStatsUrl = Nothing
                playStatisticsTimer.Change(Timeout.Infinite, Timeout.Infinite)
            Else
                If currentPlayStatsUrl IsNot Nothing AndAlso String.Compare(url, currentPlayStatsUrl, StringComparison.OrdinalIgnoreCase) <> 0 AndAlso DateTime.UtcNow.Ticks >= skipCountTriggerStartTime AndAlso DateTime.UtcNow.Ticks < skipCountTriggerEndTime Then
                    ' increment skip count
                    mbApiInterface.Player_UpdatePlayStatistics(currentPlayStatsUrl, PlayStatisticType.IncreaseSkipCount, False)
                End If
                Dim minPlayTimeMs As Integer = CInt(duration.Ticks / TimeSpan.TicksPerMillisecond * playCountTriggerPercent)
                If playCountTriggerSeconds > 0 AndAlso playCountTriggerSeconds * 1000 < minPlayTimeMs Then
                    minPlayTimeMs = playCountTriggerSeconds * 1000
                End If
                Dim maxSkipTimeTicks As Long = CLng(duration.Ticks * skipCountTriggerPercent)
                If skipCountTriggerSeconds * TimeSpan.TicksPerSecond > maxSkipTimeTicks Then
                    maxSkipTimeTicks = skipCountTriggerSeconds * TimeSpan.TicksPerSecond
                End If
                skipCountTriggerEndTime = DateTime.UtcNow.Ticks + maxSkipTimeTicks
                skipCountTriggerStartTime = DateTime.UtcNow.Ticks + 1500 * TimeSpan.TicksPerMillisecond
                currentPlayStatsUrl = url
                playStatisticsTimer.Change(minPlayTimeMs, Timeout.Infinite)
            End If
        End Sub

        Private Sub playStatisticsTimer_Tick(state As Object)
            If currentPlayStatsUrl IsNot Nothing Then
                ' increment playcount
                mbApiInterface.Player_UpdatePlayStatistics(currentPlayStatsUrl, PlayStatisticType.IncreasePlayCount, False)
                currentPlayStatsUrl = Nothing
            End If
        End Sub

        Private Sub GetThumbnailFile(request As HttpRequest)
            Dim filename As String = request.Url.Substring(request.Url.LastIndexOf("/"c) + 1)
            If filename.Length <> 24 Then
                Throw New HttpException(404, "Bad parameter")
            End If
            Dim id As String = filename.Substring(0, 16)
            Dim size As String = filename.Substring(17, 7)
            Dim directory As ItemManager = ItemManager.GetItemManager(request.Headers)
            Dim url As String
            If Not directory.TryGetThumbnailFile(id, url) Then
                Throw New HttpException(404, "Bad parameter")
            End If
            Dim encoder As ImageEncoder = ImageEncoder.TryCreate("jpeg", size)
            If encoder Is Nothing Then
                Throw New HttpException(404, "Bad parameter")
            End If
            Dim response As HttpResponse = request.Response
            response.AddHeader(HttpHeader.ContentType, encoder.GetMime())
            response.AddHeader("contentFeatures.dlna.org", "DLNA.ORG_PN=JPEG_TN;DLNA.ORG_OP=00;DLNA.ORG_CI=0;DLNA.ORG_FLAGS=00D00000000000000000000000000000")
            response.SendHeaders()
            If request.Method = "GET" Then
                encoder.StartEncode(response.Stream, url)
            End If
        End Sub

        Private Sub GetWebPng(request As HttpRequest)
            Dim response As HttpResponse = request.Response
            Dim name As String = request.Url.Split(New Char() {"/"c}, StringSplitOptions.RemoveEmptyEntries).Last()
            Dim resourceManager As New System.Resources.ResourceManager("MusicBeePlugin.Images", System.Reflection.Assembly.GetExecutingAssembly())
            Using resourceStream As IO.Stream = resourceManager.GetStream(name.Substring(0, name.Length - 4))
                response.AddHeader(HttpHeader.ContentLength, resourceStream.Length.ToString())
                response.AddHeader(HttpHeader.ContentType, "image/png")
                response.SendHeaders()
                If request.Method = "GET" Then
                    resourceStream.CopyTo(response.Stream)
                End If
            End Using
            resourceManager.ReleaseAllResources()
        End Sub

        <DllImport("MusicBeeBass.dll", CallingConvention:=CallingConvention.Cdecl)> _
        Private Shared Function Sockets_Stream_File(fileHandle As IntPtr, limit As UInteger, socketHandle As IntPtr) As Integer
        End Function
    End Class  ' MediaServerDevice
End Class