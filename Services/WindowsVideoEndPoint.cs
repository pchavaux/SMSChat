 
using System.Net;
using System.Runtime.InteropServices;
 
using global::SIPSorceryMedia.Abstractions;
using global::SIPSorceryMedia.Windows;
using Microsoft.Extensions.Logging;
using SIPSorcery;
using SIPSorceryMedia.Abstractions;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using WinRT;
namespace SMSChat.Services
{


    
    public class WindowsVideoEndPoint : IVideoSource, IVideoSink, IDisposable
    {
        private const int VIDEO_SAMPLING_RATE = 90000;

        private const int DEFAULT_FRAMES_PER_SECOND = 30;

        private readonly string MF_NV12_PIXEL_FORMAT = MediaEncodingSubtypes.Nv12;

        public const string MF_I420_PIXEL_FORMAT = "{30323449-0000-0010-8000-00AA00389B71}";

        private readonly VideoPixelFormatsEnum EncoderInputFormat = VideoPixelFormatsEnum.NV12;

        private static ILogger logger = LogFactory.CreateLogger<WindowsVideoEndPoint>();

        private MediaFormatManager<VideoFormat> _videoFormatManager;

        private IVideoEncoder _videoEncoder;

        private bool _forceKeyFrame;

        private bool _isInitialised;

        private bool _isStarted;

        private bool _isPaused;

        private bool _isClosed;

        private MediaCapture _mediaCapture;

        private MediaFrameReader _mediaFrameReader;

        private SoftwareBitmap _backBuffer;

        private string _videoDeviceID;

        private uint _width;

        private uint _height;

        private uint _fpsNumerator;

        private uint _fpsDenominator = 1u;

        private bool _videoCaptureDeviceFailed;

        private DateTime _lastFrameAt = DateTime.MinValue;

        public event RawVideoSampleDelegate OnVideoSourceRawSample;

        public event RawVideoSampleFasterDelegate OnVideoSourceRawSampleFaster;

        public event EncodedSampleDelegate OnVideoSourceEncodedSample;

        public event VideoSinkSampleDecodedDelegate OnVideoSinkDecodedSample;

        public event VideoSinkSampleDecodedFasterDelegate OnVideoSinkDecodedSampleFaster;

        public event SourceErrorDelegate OnVideoSourceError;

        public WindowsVideoEndPoint(IVideoEncoder videoEncoder, string videoDeviceID = null, uint width = 0u, uint height = 0u, uint fps = 0u)
        {
            _videoEncoder = videoEncoder;
            _videoDeviceID = videoDeviceID;
            _width = width;
            _height = height;
            _fpsNumerator = fps;
            _mediaCapture = new MediaCapture();
            _mediaCapture.Failed += VideoCaptureDevice_Failed;
            _videoFormatManager = new MediaFormatManager<VideoFormat>(videoEncoder.SupportedFormats);
        }

        public void RestrictFormats(Func<VideoFormat, bool> filter)
        {
            _videoFormatManager.RestrictFormats(filter);
        }

        public List<VideoFormat> GetVideoSourceFormats()
        {
            return _videoFormatManager.GetSourceFormats();
        }

        public void SetVideoSourceFormat(VideoFormat videoFormat)
        {
            _videoFormatManager.SetSelectedFormat(videoFormat);
        }

        public List<VideoFormat> GetVideoSinkFormats()
        {
            return _videoFormatManager.GetSourceFormats();
        }

        public void SetVideoSinkFormat(VideoFormat videoFormat)
        {
            _videoFormatManager.SetSelectedFormat(videoFormat);
        }

        public void ExternalVideoSourceRawSample(uint durationMilliseconds, int width, int height, byte[] sample, VideoPixelFormatsEnum pixelFormat)
        {
            throw new ApplicationException("The Windows Video End Point does not support external samples. Use the video end point from SIPSorceryMedia.Encoders.");
        }

        public void ExternalVideoSourceRawSampleFaster(uint durationMilliseconds, RawImage rawImage)
        {
            throw new ApplicationException("The Windows Video End Point does not support external samples. Use the video end point from SIPSorceryMedia.Encoders.");
        }

        public void ForceKeyFrame()
        {
            _forceKeyFrame = true;
        }

        public void GotVideoRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
        {
            throw new ApplicationException("The Windows Video End Point requires full video frames rather than individual RTP packets.");
        }

        public bool HasEncodedVideoSubscribers()
        {
            return this.OnVideoSourceEncodedSample != null;
        }

        public bool IsVideoSourcePaused()
        {
            return _isPaused;
        }

        private async void VideoCaptureDevice_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            if (!_videoCaptureDeviceFailed)
            {
                _videoCaptureDeviceFailed = true;
                this.OnVideoSourceError?.Invoke(errorEventArgs.Message);
                await CloseVideoCaptureDevice().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public Task<bool> InitialiseVideoSourceDevice()
        {
            if (!_isInitialised)
            {
                _isInitialised = true;
                return InitialiseDevice(_width, _height, _fpsNumerator);
            }

            return Task.FromResult(result: true);
        }

        public MediaEndPoints ToMediaEndPoints()
        {
            return new MediaEndPoints
            {
                VideoSource = this,
                VideoSink = this
            };
        }

        private async Task<bool> InitialiseDevice(uint width, uint height, uint fps)
        {
            MediaCaptureInitializationSettings mediaCaptureSettings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Video,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MediaCategory = MediaCategory.Communications
            };
            if (_videoDeviceID != null)
            {
                DeviceInformation deviceInformation = (await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask().ConfigureAwait(continueOnCapturedContext: false)).FirstOrDefault((DeviceInformation x) => x.Id == _videoDeviceID || x.Name == _videoDeviceID);
                if (deviceInformation == null)
                {
                    logger.LogWarning("Could not find video capture device for specified ID " + _videoDeviceID + ", using default device.");
                }
                else
                {
                    logger.LogInformation("Video capture device " + deviceInformation.Name + " selected.");
                    mediaCaptureSettings.VideoDeviceId = deviceInformation.Id;
                }
            }

            await _mediaCapture.InitializeAsync(mediaCaptureSettings).AsTask().ConfigureAwait(continueOnCapturedContext: false);
            MediaFrameSourceInfo mediaFrameSourceInfo = null;
            foreach (KeyValuePair<string, MediaFrameSource> frameSource in _mediaCapture.FrameSources)
            {
                if (frameSource.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && frameSource.Value.Info.SourceKind == MediaFrameSourceKind.Color)
                {
                    mediaFrameSourceInfo = frameSource.Value.Info;
                    break;
                }
            }

            MediaFrameSource colorFrameSource = _mediaCapture.FrameSources[mediaFrameSourceInfo.Id];
            MediaFrameFormat preferredFormat = colorFrameSource.SupportedFormats.Where((MediaFrameFormat format) => format.VideoFormat.Width >= _width && format.VideoFormat.Width >= _height && format.FrameRate.Numerator / format.FrameRate.Denominator >= fps && format.Subtype == MF_NV12_PIXEL_FORMAT).FirstOrDefault();
            if (preferredFormat == null)
            {
                preferredFormat = colorFrameSource.SupportedFormats.Where((MediaFrameFormat format) => format.VideoFormat.Width >= _width && format.VideoFormat.Width >= _height && format.FrameRate.Numerator / format.FrameRate.Denominator >= fps).FirstOrDefault();
            }

            if (preferredFormat == null)
            {
                logger.LogWarning($"The video capture device did not support the requested format (or better) {_width}x{_height} {fps}fps. Using default mode.");
                preferredFormat = colorFrameSource.SupportedFormats.First();
            }

            if (preferredFormat == null)
            {
                throw new ApplicationException("The video capture device does not support a compatible video format for the requested parameters.");
            }

            await colorFrameSource.SetFormatAsync(preferredFormat).AsTask().ConfigureAwait(continueOnCapturedContext: false);
            _mediaFrameReader = await _mediaCapture.CreateFrameReaderAsync(colorFrameSource).AsTask().ConfigureAwait(continueOnCapturedContext: false);
            _mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            _width = preferredFormat.VideoFormat.Width;
            _height = preferredFormat.VideoFormat.Height;
            _fpsNumerator = preferredFormat.FrameRate.Numerator;
            _fpsDenominator = preferredFormat.FrameRate.Denominator;
            PrintFrameSourceInfo(colorFrameSource);
            _mediaFrameReader.FrameArrived += FrameArrivedHandler;
            return true;
        }

        public void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] frame, VideoFormat format)
        {
            if (_isClosed)
            {
                return;
            }

            IEnumerable<VideoSample> enumerable = _videoEncoder.DecodeVideo(frame, EncoderInputFormat, _videoFormatManager.SelectedFormat.Codec);
            if (enumerable == null)
            {
                logger.LogWarning("VPX decode of video sample failed.");
                return;
            }

            foreach (VideoSample item in enumerable)
            {
                this.OnVideoSinkDecodedSample(item.Sample, item.Width, item.Height, (int)(item.Width * 3), VideoPixelFormatsEnum.Bgr);
            }
        }

        public Task PauseVideo()
        {
            _isPaused = true;
            if (_mediaFrameReader != null)
            {
                return _mediaFrameReader.StopAsync().AsTask();
            }

            return Task.CompletedTask;
        }

        public Task ResumeVideo()
        {
            _isPaused = false;
            if (_mediaFrameReader != null)
            {
                return _mediaFrameReader.StartAsync().AsTask();
            }

            return Task.CompletedTask;
        }

        public async Task StartVideo()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                if (!_isInitialised)
                {
                    await InitialiseVideoSourceDevice().ConfigureAwait(continueOnCapturedContext: false);
                }

                await _mediaFrameReader.StartAsync().AsTask().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        public async Task CloseVideo()
        {
            if (_isClosed)
            {
                return;
            }

            _isClosed = true;
            await CloseVideoCaptureDevice().ConfigureAwait(continueOnCapturedContext: false);
            if (_videoEncoder != null)
            {
                lock (_videoEncoder)
                {
                    Dispose();
                    return;
                }
            }

            Dispose();
        }

        public static async Task<List<VideoCaptureDeviceInfo>> GetVideoCatpureDevices()
        {
            DeviceInformationCollection deviceInformationCollection = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (deviceInformationCollection != null)
            {
                return deviceInformationCollection.Select(delegate (DeviceInformation x)
                {
                    VideoCaptureDeviceInfo result = default(VideoCaptureDeviceInfo);
                    result.ID = x.Id;
                    result.Name = x.Name;
                    return result;
                }).ToList();
            }

            return null;
        }

        public static async Task ListDevicesAndFormats()
        {
            foreach (DeviceInformation vidCapDevice in await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture))
            {
                MediaCaptureInitializationSettings mediaCaptureInitializationSettings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                    VideoDeviceId = vidCapDevice.Id
                };
                MediaCapture mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(mediaCaptureInitializationSettings);
                foreach (List<MediaFrameFormat> item in from x in mediaCapture.FrameSources.Values
                                                        select x.SupportedFormats into y
                                                        select y.ToList())
                {
                    foreach (MediaFrameFormat item2 in item)
                    {
                        VideoMediaFrameFormat videoFormat = item2.VideoFormat;
                        float value = videoFormat.MediaFrameFormat.FrameRate.Numerator / videoFormat.MediaFrameFormat.FrameRate.Denominator;
                        string value2 = ((videoFormat.MediaFrameFormat.Subtype == "{30323449-0000-0010-8000-00AA00389B71}") ? "I420" : videoFormat.MediaFrameFormat.Subtype);
                        logger.LogDebug($"Video Capture device {vidCapDevice.Name} format {videoFormat.Width}x{videoFormat.Height} {value:0.##}fps {value2}");
                    }
                }
            }
        }

        public static async Task<List<VideoMediaFrameFormat>> GetDeviceFrameFormats(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
            {
                throw new ArgumentNullException("deviceName", "A webcam name must be specified to get the video formats for.");
            }

            List<VideoMediaFrameFormat> formats = new List<VideoMediaFrameFormat>();
            foreach (DeviceInformation item in await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture))
            {
                if (!(item.Name.ToLower() == deviceName.ToLower()))
                {
                    continue;
                }

                MediaCaptureInitializationSettings mediaCaptureInitializationSettings = new MediaCaptureInitializationSettings
                {
                    StreamingCaptureMode = StreamingCaptureMode.Video,
                    SharingMode = MediaCaptureSharingMode.SharedReadOnly,
                    VideoDeviceId = item.Id
                };
                MediaCapture mediaCapture = new MediaCapture();
                await mediaCapture.InitializeAsync(mediaCaptureInitializationSettings);
                foreach (List<MediaFrameFormat> item2 in from x in mediaCapture.FrameSources.Values
                                                         select x.SupportedFormats into y
                                                         select y.ToList())
                {
                    foreach (MediaFrameFormat item3 in item2)
                    {
                        formats.Add(item3.VideoFormat);
                    }
                }
            }

            return formats;
        }

        private async Task CloseVideoCaptureDevice()
        {
            if (_mediaFrameReader != null)
            {
                _mediaFrameReader.FrameArrived -= FrameArrivedHandler;
                await _mediaFrameReader.StopAsync().AsTask().ConfigureAwait(continueOnCapturedContext: false);
            }

            if (_mediaCapture != null && _mediaCapture.CameraStreamState == CameraStreamState.Streaming)
            {
                await _mediaCapture.StopRecordAsync().AsTask().ConfigureAwait(continueOnCapturedContext: false);
            }
        }

        private unsafe async void FrameArrivedHandler(MediaFrameReader sender, MediaFrameArrivedEventArgs e)
        {
            if (_isClosed || _videoFormatManager.SelectedFormat.IsEmpty() || (this.OnVideoSourceEncodedSample == null && this.OnVideoSourceRawSample == null))
            {
                return;
            }

            using MediaFrameReference mediaFrameReference = sender.TryAcquireLatestFrame();
            VideoMediaFrame videoMediaFrame = mediaFrameReference?.VideoMediaFrame;
            SoftwareBitmap softwareBitmap = videoMediaFrame?.SoftwareBitmap;
            if (softwareBitmap == null && videoMediaFrame != null)
            {
               // softwareBitmap = SoftwareBitmap.CreateCopyFromSurfaceAsync(videoMediaFrame.GetVideoFrame().Direct3DSurface);
            }

            if (softwareBitmap != null)
            {
                int pixelWidth = softwareBitmap.PixelWidth;
                int pixelHeight = softwareBitmap.PixelHeight;
                if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Nv12)
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Nv12, BitmapAlphaMode.Ignore);
                }

                softwareBitmap = Interlocked.Exchange(ref _backBuffer, softwareBitmap);
                using (BitmapBuffer bitmapBuffer = _backBuffer.LockBuffer(BitmapBufferAccessMode.Read))
                {
                    using IMemoryBufferReference value = bitmapBuffer.CreateReference();
                    //value.As<IMemoryBufferByteAccess>().GetBuffer(out var buffer, out var capacity);
                    //byte[] array = new byte[capacity];
                    //Marshal.Copy((nint)buffer, array, 0, (int)capacity);
                    //if (this.OnVideoSourceEncodedSample != null)
                    //{
                    //    lock (_videoEncoder)
                    //    {
                    //        byte[] array2 = _videoEncoder.EncodeVideo(pixelWidth, pixelHeight, array, EncoderInputFormat, _videoFormatManager.SelectedFormat.Codec);
                    //        if (array2 != null)
                    //        {
                    //            uint num = ((_fpsDenominator != 0 && _fpsNumerator != 0) ? (_fpsNumerator / _fpsDenominator) : 30u);
                    //            uint durationRtpUnits = 90000 / num;
                    //            this.OnVideoSourceEncodedSample(durationRtpUnits, array2);
                    //        }

                    //        if (_forceKeyFrame)
                    //        {
                    //            _forceKeyFrame = false;
                    //        }
                    //    }
                    //}

                    if (this.OnVideoSourceRawSample != null)
                    {
                        uint durationMilliseconds = 0u;
                        if (_lastFrameAt != DateTime.MinValue)
                        {
                            durationMilliseconds = Convert.ToUInt32(DateTime.Now.Subtract(_lastFrameAt).TotalMilliseconds);
                        }

                    //    byte[] sample = PixelConverter.NV12toBGR(array, pixelWidth, pixelHeight, pixelWidth * 3);
                      //  this.OnVideoSourceRawSample(durationMilliseconds, pixelWidth, pixelHeight, sample, VideoPixelFormatsEnum.Bgr);
                    }
                }

                _backBuffer?.Dispose();
                softwareBitmap?.Dispose();
            }

            _lastFrameAt = DateTime.Now;
        }

        private unsafe void SetBitmapData(byte[] buffer, SoftwareBitmap sbmp, VideoPixelFormatsEnum pixelFormat)
        {
          //  using BitmapBuffer bitmapBuffer = sbmp.LockBuffer(BitmapBufferAccessMode.Write);
          //  using IMemoryBufferReference memoryBufferReference = bitmapBuffer.CreateReference();
          ////  ((IMemoryBufferByteAccess)memoryBufferReference).GetBuffer(out var buffer2, out var _);
          //  int num = 0;
          //  BitmapPlaneDescription planeDescription = bitmapBuffer.GetPlaneDescription(0);
          //  for (int i = 0; i < planeDescription.Height; i++)
          //  {
          //      for (int j = 0; j < planeDescription.Width; j++)
          //      {
          //          switch (pixelFormat)
          //          {
          //              case VideoPixelFormatsEnum.Rgb:
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 1] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 2] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 3] = byte.MaxValue;
          //                  break;
          //              case VideoPixelFormatsEnum.Bgr:
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 2] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 1] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 3] = byte.MaxValue;
          //                  break;
          //              case VideoPixelFormatsEnum.Bgra:
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 2] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 1] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j] = buffer[num++];
          //                  buffer2[planeDescription.StartIndex + planeDescription.Stride * i + 4 * j + 3] = buffer[num++];
          //                  break;
          //          }
          //      }
          //  }
        }

        private void PrintFrameSourceInfo(MediaFrameSource frameSource)
        {
            uint width = frameSource.CurrentFormat.VideoFormat.Width;
            uint height = frameSource.CurrentFormat.VideoFormat.Height;
            uint numerator = frameSource.CurrentFormat.FrameRate.Numerator;
            uint denominator = frameSource.CurrentFormat.FrameRate.Denominator;
            double value = numerator / denominator;
            string subtype = frameSource.CurrentFormat.Subtype;
            string name = frameSource.Info.DeviceInformation.Name;
            logger.LogInformation($"Video capture device {name} successfully initialised: {width}x{height} {value:0.##}fps pixel format {subtype}.");
        }

        public void Dispose()
        {
            if (_videoEncoder != null)
            {
                lock (_videoEncoder)
                {
                    _videoEncoder.Dispose();
                }
            }
        }

        public Task PauseVideoSink()
        {
            return Task.CompletedTask;
        }

        public Task ResumeVideoSink()
        {
            return Task.CompletedTask;
        }

        public Task StartVideoSink()
        {
            return Task.CompletedTask;
        }

        public Task CloseVideoSink()
        {
            return Task.CompletedTask;
        }
    }
}
