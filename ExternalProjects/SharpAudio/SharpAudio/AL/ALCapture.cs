using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpAudio.ALBinding;

namespace SharpAudio.AL
{
    internal sealed class ALCapture : AudioCapture
    {
        private static IntPtr _device;      
        private static int usingResource = 0;

        public override AudioBackend BackendType => AudioBackend.OpenAL;
        private static Mutex mutex = new Mutex();

        public ALCapture(AudioCaptureOptions options)
        {
            mutex.WaitOne();
            usingResource++;
            if (usingResource == 1)
            {
                // opens the default device.
                _device = AlNative.alcCaptureOpenDevice(null, (uint)options.SampleRate, AlNative.AL_FORMAT_MONO16, 128);
                checkAlcError();
            }
            mutex.ReleaseMutex();
        }

        internal static void checkAlError()
        {
            int error = AlNative.alGetError();
            if (error != AlNative.AL_NO_ERROR)
            {
                string formatErrMsg = string.Format("OpenAL Error: {0} - {1}", Marshal.PtrToStringAuto(AlNative.alGetString(error)), AlNative.alcGetCurrentContext().ToString());
                throw new SharpAudioException(formatErrMsg);
            }
        }

        private void checkAlcError()
        {
            int error = AlNative.alcGetError(_device);
            if (error != AlNative.ALC_NO_ERROR)
            {
                string formatErrMsg = string.Format("OpenALc Error: {0} - {1}", Marshal.PtrToStringAuto(AlNative.alcGetString(_device, error)), AlNative.alcGetCurrentContext().ToString());
                throw new SharpAudioException(formatErrMsg);
            }
        }

        protected override void PlatformDispose()
        {
            mutex.WaitOne();
            if (usingResource == 1)
            {
                AlNative.alcMakeContextCurrent(IntPtr.Zero);
                checkAlcError();

                if (_device != IntPtr.Zero)
                {
                    AlNative.alcCloseDevice(_device);
                    checkAlcError();
                    _device = IntPtr.Zero;

                }
                usingResource = 0;
            }
            mutex.ReleaseMutex();
        }
    }
}
