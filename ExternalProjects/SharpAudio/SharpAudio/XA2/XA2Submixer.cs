using Vortice.XAudio2;

namespace SharpAudio.XA2
{
    internal sealed class XA2Submixer : Submixer
    {

        private readonly XA2Engine _engine;

        internal IXAudio2SubmixVoice SubMixerVoice { get; }

        public XA2Submixer(XA2Engine engine)
        {
            _engine = engine;
            SubMixerVoice = _engine.Device.CreateSubmixVoice();
        }

        public override float Volume
        {
            get { return _volume; }
            set { _volume = value; SubMixerVoice?.SetVolume(value); }
        }

        public override void Dispose()
        {
            SubMixerVoice.DestroyVoice();
            SubMixerVoice.Dispose();
        }
    }
}
