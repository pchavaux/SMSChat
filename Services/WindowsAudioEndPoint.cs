

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using global::SIPSorceryMedia.Abstractions;
    using Microsoft.Extensions.Logging;
    using NAudio.Wave;
    using SIPSorcery;
    using SIPSorceryMedia.Abstractions;


namespace SMSChat.Services
{
    public class WindowsAudioEndPoint : IAudioSource, IAudioSink
    {
        private const int DEVICE_BITS_PER_SAMPLE = 16;

        private const int DEVICE_CHANNELS = 1;

        private const int INPUT_BUFFERS = 2;

        private const int AUDIO_SAMPLE_PERIOD_MILLISECONDS = 20;

        private const int AUDIO_INPUTDEVICE_INDEX = -1;

        private const int AUDIO_OUTPUTDEVICE_INDEX = -1;

        public static readonly AudioSamplingRatesEnum DefaultAudioSourceSamplingRate = AudioSamplingRatesEnum.Rate8KHz;

        public static readonly AudioSamplingRatesEnum DefaultAudioPlaybackRate = AudioSamplingRatesEnum.Rate8KHz;

        private ILogger logger = LogFactory.CreateLogger<WindowsAudioEndPoint>();

        private WaveFormat _waveSinkFormat;

        private WaveFormat _waveSourceFormat;

        private WaveOutEvent _waveOutEvent;

        private BufferedWaveProvider _waveProvider;

        private WaveInEvent _waveInEvent;

        private IAudioEncoder _audioEncoder;

        private MediaFormatManager<AudioFormat> _audioFormatManager;

        private bool _disableSink;

        private int _audioOutDeviceIndex;

        private int _audioInDeviceIndex;

        private bool _disableSource;

        protected bool _isAudioSourceStarted;

        protected bool _isAudioSinkStarted;

        protected bool _isAudioSourcePaused;

        protected bool _isAudioSinkPaused;

        protected bool _isAudioSourceClosed;

        protected bool _isAudioSinkClosed;

        public event EncodedSampleDelegate OnAudioSourceEncodedSample;

        [Obsolete("The audio source only generates encoded samples.")]
        public event RawAudioSampleDelegate OnAudioSourceRawSample
        {
            add
            {
            }
            remove
            {
            }
        }

        public event SourceErrorDelegate OnAudioSourceError;

        public event SourceErrorDelegate OnAudioSinkError;

        public WindowsAudioEndPoint(IAudioEncoder audioEncoder, int audioOutDeviceIndex = -1, int audioInDeviceIndex = -1, bool disableSource = false, bool disableSink = false)
        {
            logger = LogFactory.CreateLogger<WindowsAudioEndPoint>();
            _audioFormatManager = new MediaFormatManager<AudioFormat>(audioEncoder.SupportedFormats);
            _audioEncoder = audioEncoder;
            _audioOutDeviceIndex = audioOutDeviceIndex;
            _audioInDeviceIndex = audioInDeviceIndex;
            _disableSource = disableSource;
            _disableSink = disableSink;
            if (!_disableSink)
            {
                InitPlaybackDevice(_audioOutDeviceIndex, DefaultAudioPlaybackRate.GetHashCode());
            }

            if (!_disableSource)
            {
                InitCaptureDevice(_audioInDeviceIndex, (int)DefaultAudioSourceSamplingRate);
            }
        }

        public void RestrictFormats(Func<AudioFormat, bool> filter)
        {
            _audioFormatManager.RestrictFormats(filter);
        }

        public List<AudioFormat> GetAudioSourceFormats()
        {
            return _audioFormatManager.GetSourceFormats();
        }

        public List<AudioFormat> GetAudioSinkFormats()
        {
            return _audioFormatManager.GetSourceFormats();
        }

        public bool HasEncodedAudioSubscribers()
        {
            return this.OnAudioSourceEncodedSample != null;
        }

        public bool IsAudioSourcePaused()
        {
            return _isAudioSourcePaused;
        }

        public bool IsAudioSinkPaused()
        {
            return _isAudioSinkPaused;
        }

        public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample)
        {
            throw new NotImplementedException();
        }

        public void SetAudioSourceFormat(AudioFormat audioFormat)
        {
            _audioFormatManager.SetSelectedFormat(audioFormat);
            if (!_disableSource && _waveSourceFormat.SampleRate != _audioFormatManager.SelectedFormat.ClockRate)
            {
                logger.LogDebug($"Windows audio end point adjusting capture rate from {_waveSourceFormat.SampleRate} to {_audioFormatManager.SelectedFormat.ClockRate}.");
                InitCaptureDevice(_audioInDeviceIndex, _audioFormatManager.SelectedFormat.ClockRate);
            }
        }

        public void SetAudioSinkFormat(AudioFormat audioFormat)
        {
            _audioFormatManager.SetSelectedFormat(audioFormat);
            if (!_disableSink && _waveSinkFormat.SampleRate != _audioFormatManager.SelectedFormat.ClockRate)
            {
                logger.LogDebug($"Windows audio end point adjusting playback rate from {_waveSinkFormat.SampleRate} to {_audioFormatManager.SelectedFormat.ClockRate}.");
                InitPlaybackDevice(_audioOutDeviceIndex, _audioFormatManager.SelectedFormat.ClockRate);
            }
        }

        public MediaEndPoints ToMediaEndPoints()
        {
            return new MediaEndPoints
            {
                AudioSource = (_disableSource ? null : this),
                AudioSink = (_disableSink ? null : this)
            };
        }

        public Task StartAudio()
        {
            if (!_isAudioSourceStarted)
            {
                _isAudioSourceStarted = true;
                _waveInEvent?.StartRecording();
            }

            return Task.CompletedTask;
        }

        public Task CloseAudio()
        {
            if (!_isAudioSourceClosed)
            {
                _isAudioSourceClosed = true;
                if (_waveInEvent != null)
                {
                    _waveInEvent.DataAvailable -= LocalAudioSampleAvailable;
                    _waveInEvent.StopRecording();
                }
            }

            return Task.CompletedTask;
        }

        public Task PauseAudio()
        {
            _isAudioSourcePaused = true;
            _waveInEvent?.StopRecording();
            return Task.CompletedTask;
        }

        public Task ResumeAudio()
        {
            _isAudioSourcePaused = false;
            _waveInEvent?.StartRecording();
            return Task.CompletedTask;
        }

        private void InitPlaybackDevice(int audioOutDeviceIndex, int audioSinkSampleRate)
        {
            try
            {
                _waveOutEvent?.Stop();
                _waveSinkFormat = new WaveFormat(audioSinkSampleRate, 16, 1);
                _waveOutEvent = new WaveOutEvent();
                _waveOutEvent.DeviceNumber = audioOutDeviceIndex;
                _waveProvider = new BufferedWaveProvider(_waveSinkFormat);
                _waveProvider.DiscardOnBufferOverflow = true;
                _waveOutEvent.Init(_waveProvider);
            }
            catch (Exception ex)
            {
                logger.LogWarning(0, ex, "WindowsAudioEndPoint failed to initialise playback device.");
                this.OnAudioSinkError?.Invoke("WindowsAudioEndPoint failed to initialise playback device. " + ex.Message);
            }
        }

        private void InitCaptureDevice(int audioInDeviceIndex, int audioSourceSampleRate)
        {
            if (WaveInEvent.DeviceCount > 0)
            {
                if (WaveInEvent.DeviceCount > audioInDeviceIndex)
                {
                    if (_waveInEvent != null)
                    {
                        _waveInEvent.DataAvailable -= LocalAudioSampleAvailable;
                        _waveInEvent.StopRecording();
                    }

                    _waveSourceFormat = new WaveFormat(audioSourceSampleRate, 16, 1);
                    _waveInEvent = new WaveInEvent();
                    _waveInEvent.BufferMilliseconds = 20;
                    _waveInEvent.NumberOfBuffers = 2;
                    _waveInEvent.DeviceNumber = audioInDeviceIndex;
                    _waveInEvent.WaveFormat = _waveSourceFormat;
                    _waveInEvent.DataAvailable += LocalAudioSampleAvailable;
                }
                else
                {
                    logger.LogWarning($"The requested audio input device index {audioInDeviceIndex} exceeds the maximum index of {WaveInEvent.DeviceCount - 1}.");
                    this.OnAudioSourceError?.Invoke($"The requested audio input device index {audioInDeviceIndex} exceeds the maximum index of {WaveInEvent.DeviceCount - 1}.");
                }
            }
            else
            {
                logger.LogWarning("No audio capture devices are available.");
                this.OnAudioSourceError?.Invoke("No audio capture devices are available.");
            }
        }

        private void LocalAudioSampleAvailable(object sender, WaveInEventArgs args)
        {
            byte[] buffer = args.Buffer.Take(args.BytesRecorded).ToArray();
            short[] pcm = buffer.Where((byte x, int i) => i % 2 == 0).Select((byte y, int i) => BitConverter.ToInt16(buffer, i * 2)).ToArray();
            byte[] array = _audioEncoder.EncodeAudio(pcm, _audioFormatManager.SelectedFormat);
            this.OnAudioSourceEncodedSample?.Invoke((uint)array.Length, array);
        }

        public void GotAudioSample(byte[] pcmSample)
        {
            if (_waveProvider != null)
            {
                _waveProvider.AddSamples(pcmSample, 0, pcmSample.Length);
            }
        }

        public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
        {
            if (_waveProvider != null && _audioEncoder != null)
            {
                byte[] array = _audioEncoder.DecodeAudio(payload, _audioFormatManager.SelectedFormat).SelectMany((short x) => BitConverter.GetBytes(x)).ToArray();
                _waveProvider?.AddSamples(array, 0, array.Length);
            }
        }

        public Task PauseAudioSink()
        {
            _isAudioSinkPaused = true;
            _waveOutEvent?.Pause();
            return Task.CompletedTask;
        }

        public Task ResumeAudioSink()
        {
            _isAudioSinkPaused = false;
            _waveOutEvent?.Play();
            return Task.CompletedTask;
        }

        public Task StartAudioSink()
        {
            if (!_isAudioSinkStarted)
            {
                _isAudioSinkStarted = true;
                _waveOutEvent?.Play();
            }

            return Task.CompletedTask;
        }

        public Task CloseAudioSink()
        {
            if (!_isAudioSinkClosed)
            {
                _isAudioSinkClosed = true;
                _waveOutEvent?.Stop();
            }

            return Task.CompletedTask;
        }
    }
}