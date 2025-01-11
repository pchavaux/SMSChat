 using System;
    using System.Collections.Generic;
    using System.Linq;
    using global::SIPSorcery.Media;
    using SIPSorceryMedia.Abstractions;

namespace SMSChat.Services
{
    public class AudioEncoder : IAudioEncoder
    {
        private const int G722_BIT_RATE = 64000;

        private G722Codec _g722Codec;

        private G722CodecState _g722CodecState;

        private G722Codec _g722Decoder;

        private G722CodecState _g722DecoderState;

        private G729Encoder _g729Encoder;

        private G729Decoder _g729Decoder;

        private List<AudioFormat> _linearFormats = new List<AudioFormat>
    {
        new AudioFormat(AudioCodecsEnum.L16, 117, 16000),
        new AudioFormat(AudioCodecsEnum.L16, 118)
    };

        private List<AudioFormat> _supportedFormats = new List<AudioFormat>
    {
        new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU),
        new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMA),
        new AudioFormat(SDPWellKnownMediaFormatsEnum.G722),
        new AudioFormat(SDPWellKnownMediaFormatsEnum.G729),
        
    };

        public List<AudioFormat> SupportedFormats => _supportedFormats;

        //
        // Summary:
        //     Creates a new audio encoder instance.
        //
        // Parameters:
        //   includeLinearFormats:
        //     If set to true the linear audio formats will be added to the list of supported
        //     formats. The reason they are only included if explicitly requested is they are
        //     not very popular for other VoIP systems and thereofre needlessly pollute the
        //     SDP.
        public AudioEncoder(bool includeLinearFormats = false)
        {
            if (includeLinearFormats)
            {
                _supportedFormats.AddRange(_linearFormats);
            }
        }

        public byte[] EncodeAudio(short[] pcm, AudioFormat format)
        {
            if (format.Codec == AudioCodecsEnum.G722)
            {
                if (_g722Codec == null)
                {
                    _g722Codec = new G722Codec();
                    _g722CodecState = new G722CodecState(64000, G722Flags.None);
                }

                byte[] array = new byte[pcm.Length / 2];
                _g722Codec.Encode(_g722CodecState, array, pcm, pcm.Length);
                return array;
            }

            if (format.Codec == AudioCodecsEnum.G729)
            {
                if (_g729Encoder == null)
                {
                    _g729Encoder = new G729Encoder();
                }

                byte[] array2 = new byte[pcm.Length * 2];
                Buffer.BlockCopy(pcm, 0, array2, 0, array2.Length);
                return _g729Encoder.Process(array2);
            }

            if (format.Codec == AudioCodecsEnum.PCMA)
            {
                return pcm.Select((short x) => ALawEncoder.LinearToALawSample(x)).ToArray();
            }

            if (format.Codec == AudioCodecsEnum.PCMU)
            {
                return pcm.Select((short x) => MuLawEncoder.LinearToMuLawSample(x)).ToArray();
            }

            if (format.Codec == AudioCodecsEnum.L16)
            {
                return pcm.SelectMany((short x) => new byte[2]
                {
                (byte)(x >> 8),
                (byte)x
                }).ToArray();
            }

            if (format.Codec == AudioCodecsEnum.PCM_S16LE)
            {
                return pcm.SelectMany((short x) => new byte[2]
                {
                (byte)x,
                (byte)(x >> 8)
                }).ToArray();
            }

            throw new ApplicationException($"Audio format {format.Codec} cannot be encoded.");
        }

        //
        // Summary:
        //     Event handler for receiving RTP packets from the remote party.
        //
        // Parameters:
        //   encodedSample:
        //     Data received from an RTP socket.
        //
        //   format:
        //     The audio format of the encoded packets.
        public short[] DecodeAudio(byte[] encodedSample, AudioFormat format)
        {
            if (format.Codec == AudioCodecsEnum.G722)
            {
                if (_g722Decoder == null)
                {
                    _g722Decoder = new G722Codec();
                    _g722DecoderState = new G722CodecState(64000, G722Flags.None);
                }

                short[] array = new short[encodedSample.Length * 2];
                int count = _g722Decoder.Decode(_g722DecoderState, array, encodedSample, encodedSample.Length);
                return array.Take(count).ToArray();
            }

            if (format.Codec == AudioCodecsEnum.G729)
            {
                if (_g729Decoder == null)
                {
                    _g729Decoder = new G729Decoder();
                }

                byte[] array2 = _g729Decoder.Process(encodedSample);
                short[] array3 = new short[array2.Length / 2];
                Buffer.BlockCopy(array2, 0, array3, 0, array2.Length);
                return array3;
            }

            if (format.Codec == AudioCodecsEnum.PCMA)
            {
                return encodedSample.Select((byte x) => ALawDecoder.ALawToLinearSample(x)).ToArray();
            }

            if (format.Codec == AudioCodecsEnum.PCMU)
            {
                return encodedSample.Select((byte x) => MuLawDecoder.MuLawToLinearSample(x)).ToArray();
            }

            if (format.Codec == AudioCodecsEnum.L16)
            {
                return encodedSample.Where((byte x, int i) => i % 2 == 0).Select((byte y, int i) => (short)((encodedSample[i * 2] << 8) | encodedSample[i * 2 + 1])).ToArray();
            }

            if (format.Codec == AudioCodecsEnum.PCM_S16LE)
            {
                return encodedSample.Where((byte x, int i) => i % 2 == 0).Select((byte y, int i) => (short)((encodedSample[i * 2 + 1] << 8) | encodedSample[i * 2])).ToArray();
            }

            throw new ApplicationException($"Audio format {format.Codec} cannot be decoded.");
        }

       // [Obsolete("No longer used. Use SIPSorcery.Media.PcmResampler.Resample instead.")]
        //public short[] Resample(short[] pcm, int inRate, int outRate)
        //{
        //    return PcmResampler.Resample(pcm, inRate, outRate);
        //}
    }
}