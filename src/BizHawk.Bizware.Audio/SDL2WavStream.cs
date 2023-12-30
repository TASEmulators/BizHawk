using System;
using System.IO;

using BizHawk.Common;

using static SDL2.SDL;

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
			U8 = 0x0008,
			S16LSB = 0x8010,
			S32LSB = 0x8020,
			F32LSB = 0x8120,
			S16MSB = 0x9010,
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

		public SDL2WavStream(string path)
		{
			// TODO: Perhaps this should just take a Stream?
			// need to update SDL2-CS since the version we're on doesn't expose SDL_LoadWAV_RW :(
			if (SDL_LoadWAV(path, out var spec, out var wav, out var len) == IntPtr.Zero)
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

		public unsafe int Read(Span<byte> buffer)
		{
			if (_wav == IntPtr.Zero)
			{
				throw new ObjectDisposedException(nameof(SDL2WavStream));
			}

			var count = (int)Math.Min(buffer.Length, _len - _pos);
			new ReadOnlySpan<byte>((void*)((nint)_wav + _pos), count).CopyTo(buffer);
			_pos += (uint)count;
			return count;
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
	}
}
