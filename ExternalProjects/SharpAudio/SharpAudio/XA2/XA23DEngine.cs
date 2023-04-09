using System.Collections.Generic;
using System.Numerics;
using Vortice.Multimedia;
using Vortice.XAudio2;

namespace SharpAudio.XA2
{
    internal sealed class XA23DEngine : Audio3DEngine
    {
        X3DAudio _x3DAudio;
        Listener _x3DListener;
        Dictionary<AudioSource, Emitter> _x3DEmitters;
        private readonly XA2Engine _engine;

        public XA23DEngine(XA2Engine engine)
        {
            _engine = engine;
            _x3DListener = new Listener();
            _x3DListener.OrientTop = Vector3.UnitZ;
            _x3DListener.OrientFront = Vector3.UnitY;

            Speakers channels = (Speakers) _engine.MasterVoice.ChannelMask;
            _x3DAudio = new X3DAudio(channels);
            _x3DEmitters = new Dictionary<AudioSource, Emitter>();
        }

        public override void SetListenerPosition(Vector3 position)
        {
            _x3DListener.Position = position;
        }

        public override void SetSourcePosition(AudioSource source, Vector3 position)
        {
            XA2Source xa2Source = (XA2Source) source;

            if (!_x3DEmitters.TryGetValue(source, out var emitter))
            {
                emitter = new Emitter();
                emitter.CurveDistanceScaler = float.MinValue;
                emitter.OrientTop = Vector3.UnitZ;
                emitter.OrientFront = Vector3.UnitY;

                _x3DEmitters.Add(source, emitter);
            }

            emitter.ChannelCount = xa2Source.SourceVoice.VoiceDetails.InputChannels;
            emitter.Position = new Vector3(position.X, position.Y, position.Z);

            var outChannels = _engine.MasterVoice.VoiceDetails.InputChannels;

            DspSettings dspSettings = new DspSettings(1, outChannels);

            _x3DAudio.Calculate(_x3DListener, emitter, CalculateFlags.Matrix, dspSettings);
            xa2Source.SourceVoice.SetOutputMatrix(_engine.MasterVoice, 1, outChannels, dspSettings.MatrixCoefficients);
        }

        public override void SetListenerOrientation(Vector3 top, Vector3 front)
        {
            _x3DListener.OrientTop = top;
            _x3DListener.OrientFront = front;
        }
    }
}
