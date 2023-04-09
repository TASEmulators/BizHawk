using System;
using System.Runtime.InteropServices;
using SharpAudio.ALBinding;

namespace SharpAudio.AL
{
    internal sealed class ALBuffer : AudioBuffer
    {
        public uint Buffer { get; }

        public ALBuffer()
        {
            var buffers = new uint[1];
            AlNative.alGenBuffers(1, buffers);
            ALEngine.checkAlError();
            Buffer = buffers[0];
        }

        public override unsafe void BufferData(IntPtr ptr, int sizeInBytes, AudioFormat format)
        {
            int fmt = (format.Channels == 2) ? AlNative.AL_FORMAT_STEREO8 : AlNative.AL_FORMAT_MONO8;

            if (format.BitsPerSample == 16)
            {
                fmt++;
            }

            AlNative.alBufferData(Buffer, fmt, ptr, sizeInBytes, format.SampleRate);
            ALEngine.checkAlError();

            _format = format;
        }

        public override unsafe void BufferData<T>(T[] buffer, AudioFormat format)
        {
            int sizeInBytes = sizeof(T) * buffer.Length;

            var handle = GCHandle.Alloc(buffer);
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);

            BufferData(ptr, sizeInBytes, format);

            handle.Free();
        }

        public override unsafe void BufferData<T>(Span<T> buffer, AudioFormat format)
        {
            int sizeInBytes = sizeof(T) * buffer.Length;

            fixed (T* ptr = buffer)
            {
                BufferData((IntPtr) ptr, sizeInBytes, format);
            }
        }

        public override void Dispose()
        {
            AlNative.alDeleteBuffers(1, new uint[] { Buffer });
            ALEngine.checkAlError();
        }
    }
}
