using System.IO;

namespace BizHawk.Common
{
	public class MemoryBlock : IDisposable
	{
		/// <summary>allocate <paramref name="size"/> bytes</summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is not aligned or is <c>0</c></exception>
		public MemoryBlock(ulong size)
		{
			if (!MemoryBlockUtils.Aligned(size))
			{
				throw new ArgumentOutOfRangeException(nameof(size), size, "size must be aligned");
			}

			if (size == 0)
			{
				throw new ArgumentOutOfRangeException(nameof(size), size, "cannot create 0-length block");
			}

			Size = MemoryBlockUtils.AlignUp(size);
			_pal = OSTailoredCode.IsUnixHost
				? new MemoryBlockLinuxPal(Size)
				: new MemoryBlockWindowsPal(Size);
			Start = _pal.Start;
			EndExclusive = Start + Size;
		}

		private IMemoryBlockPal? _pal;

		/// <summary>
		/// end address of the memory block (not part of the block; class invariant: equal to <see cref="Start"/> + <see cref="Size"/>)
		/// </summary>
		public readonly ulong EndExclusive;

		/// <summary>total size of the memory block</summary>
		public readonly ulong Size;

		/// <summary>starting address of the memory block</summary>
		public readonly ulong Start;

		/// <summary>
		/// Get a stream that can be used to read or write from part of the block. Does not check for or change <see cref="Protect"/>!
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="start"/> or end (= <paramref name="start"/> + <paramref name="length"/> - <c>1</c>)
		/// are outside [<see cref="Start"/>, <see cref="EndExclusive"/>), the range of the block
		/// </exception>
		/// <exception cref="ObjectDisposedException">disposed</exception>
		public Stream GetStream(ulong start, ulong length, bool writer)
		{
			if (_pal == null)
			{
				throw new ObjectDisposedException(nameof(MemoryBlock));
			}

			if (start < Start)
			{
				throw new ArgumentOutOfRangeException(nameof(start), start, "invalid address");
			}

			if (EndExclusive < start + length)
			{
				throw new ArgumentOutOfRangeException(nameof(length), length, "requested length implies invalid end address");
			}

			return new MemoryViewStream(!writer, writer, (long)start, (long)length);
		}

		/// <summary>Memory protection constant</summary>
		public enum Protection : byte
		{
			None,
			R,
			RW,
			RX,
		}

		/// <summary>set r/w/x protection on a portion of memory. rounded to encompassing pages</summary>
		/// <exception cref="InvalidOperationException">failed to protect memory</exception>
		/// <exception cref="ObjectDisposedException">disposed</exception>
		public void Protect(ulong start, ulong length, Protection prot)
		{
			if (_pal == null)
			{
				throw new ObjectDisposedException(nameof(MemoryBlock));
			}

			if (length == 0)
			{
				return;
			}

			// Note: asking for prot.none on memory that was not previously committed, commits it

			var computedStart = MemoryBlockUtils.AlignDown(start);
			var computedEnd = MemoryBlockUtils.AlignUp(start + length);
			var computedLength = computedEnd - computedStart;

			_pal.Protect(computedStart, computedLength, prot);
		}

		public void Dispose()
		{
			if (_pal != null)
			{
				_pal.Dispose();
				_pal = null;
			}
		}
	}
}
