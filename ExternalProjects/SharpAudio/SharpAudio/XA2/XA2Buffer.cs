using System;
using System.Runtime.InteropServices;
using Vortice;

namespace SharpAudio.XA2
{
    internal sealed class XA2Buffer : AudioBuffer
    {
        private DataStream _dataStream;

        public Vortice.XAudio2.AudioBuffer Buffer { get; }

        public int SizeInBytes { get; private set; }
        public int TotalSamples => SizeInBytes / Format.BytesPerSample;

        public XA2Buffer()
        {
            Buffer = new Vortice.XAudio2.AudioBuffer();
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

        public override void BufferData(IntPtr buffer, int sizeInBytes, AudioFormat format)
        {
            _dataStream?.Dispose();
            _dataStream = new DataStream(sizeInBytes, true, true);

            _dataStream.WriteRange(buffer, sizeInBytes);
            _dataStream.Position = 0;

            _format = format;
            SizeInBytes = sizeInBytes;
            Buffer.AudioDataPointer = _dataStream.PositionPointer;
            Buffer.AudioBytes = SizeInBytes;
        }

        public override void Dispose()
        {
            _dataStream?.Dispose();
        }
    }
}
