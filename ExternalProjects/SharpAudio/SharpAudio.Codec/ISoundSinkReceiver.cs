using System;

namespace SharpAudio.Codec
{
    public interface ISoundSinkReceiver : IDisposable
    {
        void Receive(byte[] tempBuf);
    }
}
