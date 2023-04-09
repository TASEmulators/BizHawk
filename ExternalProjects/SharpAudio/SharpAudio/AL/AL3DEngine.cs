using System.Numerics;
using SharpAudio.ALBinding;

namespace SharpAudio.AL
{
    internal sealed class AL3DEngine : Audio3DEngine
    {
        public override void SetListenerOrientation(Vector3 top, Vector3 front)
        {
            float[] listenerOri = new float[] { front.X, front.Y, front.Z, top.X, top.Y, top.Z };
            AlNative.alListenerfv(AlNative.AL_ORIENTATION, listenerOri);
            ALEngine.checkAlError();
        }

        public override void SetListenerPosition(Vector3 position)
        {
            AlNative.alListener3f(AlNative.AL_POSITION, position.X, position.Y, position.Z);
            ALEngine.checkAlError();
        }

        public override void SetSourcePosition(AudioSource source, Vector3 position)
        {
            ALSource alSource = (ALSource) source;
            AlNative.alSource3f(alSource._source, AlNative.AL_POSITION, position.X, position.Y, position.Z);
            ALEngine.checkAlError();
        }
    }
}
