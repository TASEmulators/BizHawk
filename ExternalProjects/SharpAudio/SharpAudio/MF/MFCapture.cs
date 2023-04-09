using System;
using Vortice.MediaFoundation;

namespace SharpAudio.MF
{
    internal sealed class MFCapture : AudioCapture
    {
        private IMFMediaSource _audioSource;
        public override AudioBackend BackendType => AudioBackend.MediaFoundation;

        public MFCapture(AudioCaptureOptions options)
        {
            IMFAttributes attribs = null;
            var result = MediaFactory.MFCreateDeviceSource(attribs, out _audioSource);
        }

        protected override void PlatformDispose()
        {
            _audioSource.Shutdown();
            _audioSource.Release();

        }
    }
}
