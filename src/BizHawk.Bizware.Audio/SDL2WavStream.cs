using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

using static SDL2.SDL;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Bizware.Audio
{
	internal sealed class SDL2WavStream : Stream, ISpanStream
	{
		private IntPtr _wav;
		private readonly uint _len;
		private uint _pos;

		// These are the only formats SDL2's wav loadder will output
		public enum AudioFormat : ushort
		{
			U8 = AUDIO_U8,
			S16LSB = AUDIO_S16LSB,
			S32LSB = AUDIO_S32LSB,
			F32LSB = AUDIO_F32LSB,
			S16MSB = AUDIO_S16MSB,
		}

		public int Frequency { get; }
		public AudioFormat Format { get; }
		public byte Channels { get; }

		public int BitsPerSample => Format switch
		{
			AudioFormat.U8 => 8,
			AudioFormat.S16LSB or AudioFormat.S16MSB => 16,
			AudioFormat.S32LSB or AudioFormat.F32LSB => 32,
			_ => throw new InvalidOperationException(),
		};
		
		[DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr SDL_LoadWAV_RW(
			IntPtr src,
			int freesrc,
			out SDL_AudioSpec spec,
			out IntPtr audio_buf,
			out uint audio_len);

		public SDL2WavStream(Stream wavFile)
		{
			using var rwOpWrapper = new SDLRwOpsStreamWrapper(wavFile);
			if (SDL_LoadWAV_RW(rwOpWrapper.Rw, 0, out var spec, out var wav, out var len) == IntPtr.Zero)
			{
				throw new($"Could not load WAV file! SDL error: {SDL_GetError()}");
			}

			Frequency = spec.freq;
			Format = (AudioFormat)spec.format;
			Channels = spec.channels;

			_wav = wav;
			_len = len;
		}

		protected override void Dispose(bool disposing)
		{
			SDL_FreeWAV(_wav);
			_wav = IntPtr.Zero;

			base.Dispose(disposing);
		}

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => _len;

		public override long Position
		{
			get => _pos;
			set
			{
				if (value < 0 || value > _len)
				{
					throw new ArgumentOutOfRangeException(paramName: nameof(value), value, message: "index out of range");
				}

				_pos = (uint)value;
			}
		}

		public override void Flush()
		{
		}

		public int Read(Span<byte> buffer)
		{
			if (_wav == IntPtr.Zero)
			{
				throw new ObjectDisposedException(nameof(SDL2WavStream));
			}

			uint count = Math.Min((uint) buffer.Length, _len - _pos);
			var countSigned = unchecked((int) count); // since `Span.Length` is at most `int.MaxValue`, so must `count` be
			// really, these fields should just be widened to whatever they're used as, which seems to be s64, and asserted to be in 0..<int.MaxValue
			Util.UnsafeSpanFromPointer(ptr: _wav.Plus(_pos), length: countSigned).CopyTo(buffer);
			_pos += count;
			return countSigned;
		}

		public override int Read(byte[] buffer, int offset, int count)
			=> Read(new(buffer, offset, count));

		public override long Seek(long offset, SeekOrigin origin)
		{
			var newpos = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => _pos + offset,
				SeekOrigin.End => _len + offset,
				_ => offset
			};

			Position = newpos;
			return newpos;
		}

		public override void SetLength(long value)
			=> throw new NotSupportedException();

		public void Write(ReadOnlySpan<byte> buffer)
			=> throw new NotSupportedException();

		public override void Write(byte[] buffer, int offset, int count)
			=> throw new NotSupportedException();

		private unsafe class SDLRwOpsStreamWrapper : IDisposable
		{
			public IntPtr Rw { get; private set; }
			private readonly Stream _s;

			private readonly SDLRWopsSizeCallback _sizeCallback;
			private readonly SDLRWopsSeekCallback _seekCallback;
			private readonly SDLRWopsReadCallback _readCallback;
			private readonly SDLRWopsWriteCallback _writeCallback;
			private readonly SDLRWopsCloseCallback _closeCallback;

			public SDLRwOpsStreamWrapper(Stream s)
			{
				Rw = SDL_AllocRW();
				if (Rw == IntPtr.Zero)
				{
					throw new($"Could not allocate SDL_RWops! SDL error: {SDL_GetError()}");
				}

				_s = s;
				_sizeCallback = SizeCallback;
				_seekCallback = SeekCallback;
				_readCallback = ReadCallback;
				_writeCallback = WriteCallback;
				_closeCallback = CloseCallback;

				var rw = (SDL_RWops*)Rw;
				rw->size = Marshal.GetFunctionPointerForDelegate(_sizeCallback);
				rw->seek = Marshal.GetFunctionPointerForDelegate(_seekCallback);
				rw->read = Marshal.GetFunctionPointerForDelegate(_readCallback);
				rw->write = Marshal.GetFunctionPointerForDelegate(_writeCallback);
				rw->close = Marshal.GetFunctionPointerForDelegate(_closeCallback);
				rw->type = SDL_RWOPS_UNKNOWN;
			}

			private long SizeCallback(IntPtr ctx)
				=> _s.Length;

			private long SeekCallback(IntPtr ctx, long offset, int whence)
				=> _s.Seek(offset, (SeekOrigin)whence);

			private nint ReadCallback(IntPtr ctx, IntPtr ptr, nint size, nint num)
			{
				const int TEMP_BUFFER_LENGTH = 65536;
				var tempBuffer = ArrayPool<byte>.Shared.Rent(TEMP_BUFFER_LENGTH);
				try
				{
					var numBytes = (nuint)size * (nuint)num;
					var remainingBytes = numBytes;
					while (remainingBytes != 0)
					{
						var numRead = _s.Read(tempBuffer, 0, (int)Math.Min(remainingBytes, TEMP_BUFFER_LENGTH));
						if (numRead == 0)
						{
							break;
						}

						Marshal.Copy(tempBuffer, 0, ptr, numRead);
						ptr += numRead;
						remainingBytes -= (uint)numRead;
					}

					return (nint)((numBytes - remainingBytes) / (nuint)size);
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(tempBuffer);
				}
			}

			private static nint WriteCallback(IntPtr ctx, IntPtr ptr, nint size, nint num)
				=> 0;

			private int CloseCallback(IntPtr ctx)
			{
				Dispose();
				return 0;
			}

			public void Dispose()
			{
				if (Rw != IntPtr.Zero)
				{
					SDL_FreeRW(Rw);
					Rw = IntPtr.Zero;
				}
			}
		}
	}
}
