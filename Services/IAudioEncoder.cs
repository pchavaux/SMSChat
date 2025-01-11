
    using global::SIPSorceryMedia.Abstractions;
    using System.Collections.Generic;

namespace SMSChat.Services
{

    public interface IAudioEncoder
    {
        List<AudioFormat> SupportedFormats { get; }

        byte[] EncodeAudio(short[] pcm, AudioFormat format);

        short[] DecodeAudio(byte[] encodedSample, AudioFormat format);
    }
}