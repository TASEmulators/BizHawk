using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <remarks>Implementation of CRC-32 (i.e. POSIX cksum), intended for comparing discs against the Redump.org database</remarks>
	public sealed class CRC32
	{
		/// <remarks>coefficients of the polynomial, in the format Wikipedia calls "reversed"</remarks>
		public const uint POLYNOMIAL_CONST = 0xEDB88320U;

		/// <summary>
		/// Delegate to unmanaged code that actually does the calculation.
		/// This may be hardware accelerated, if the CPU supports such.
		/// </summary>
		private static readonly LibBizHash.CalcCRC _calcCRC;

		private static readonly uint[] COMBINER_INIT_STATE;

		static CRC32()
		{
			// for Add (CRC32 computation):
			_calcCRC = Marshal.GetDelegateForFunctionPointer<LibBizHash.CalcCRC>(LibBizHash.BizCalcCrcFunc());

			// for Incorporate:
			var combinerState = (COMBINER_INIT_STATE = new uint[64]).AsSpan();
			var even = combinerState.Slice(start: 0, length: 32); // even-power-of-two zeros operator
			var odd = combinerState.Slice(start: 32, length: 32); // odd-power-of-two zeros operator
			// put operator for one zero bit in odd
			odd[0] = POLYNOMIAL_CONST;
			var oddTail = odd.Slice(1);
			for (var n = 0; n < oddTail.Length; n++) oddTail[n] = 1U << n;
			// put operator for two zero bits in even
			gf2_matrix_square(even, odd);
			// put operator for four zero bits in odd
			gf2_matrix_square(odd, even);
		}

		public static uint Calculate(ReadOnlySpan<byte> data)
		{
			CRC32 crc32 = new();
			crc32.Add(data);
			return crc32.Result;
		}

		private static void gf2_matrix_square(Span<uint> square, ReadOnlySpan<uint> mat)
		{
			if (mat.Length != square.Length) throw new ArgumentException(message: "must be same length as " + nameof(square), paramName: nameof(mat));
			for (var n = 0; n < square.Length; n++) square[n] = gf2_matrix_times(mat, mat[n]);
		}

		private static uint gf2_matrix_times(ReadOnlySpan<uint> mat, uint vec)
		{
			var matIdx = 0;
			uint sum = 0U;
			while (vec != 0U)
			{
				if ((vec & 1U) != 0U) sum ^= mat[matIdx];
				vec >>= 1;
				matIdx++;
			}
			return sum;
		}

		private uint _current = 0xFFFFFFFFU;

		/// <summary>The raw non-negated output</summary>
		public uint Current
		{
			get => _current;
			set => _current = value;
		}

		/// <summary>The negated output (the typical result of the CRC calculation)</summary>
		public uint Result => ~_current;

		public unsafe void Add(ReadOnlySpan<byte> data)
		{
			fixed (byte* d = &data.GetPinnableReference())
			{
				_current = _calcCRC(_current, (IntPtr) d, data.Length);
			}
		}

		/// <summary>
		/// Incorporates a pre-calculated CRC with the given length by combining crcs<br/>
		/// It's a bit flaky, so be careful, but it works
		/// </summary>
		/// <remarks>algorithm from zlib's crc32_combine. read http://www.leapsecond.com/tools/crcomb.c for more</remarks>
		public void Incorporate(uint crc, int len)
		{
			if (len == 0) return; // degenerate case

			Span<uint> combinerState = stackalloc uint[64];
			COMBINER_INIT_STATE.CopyTo(combinerState);
			var even = combinerState.Slice(start: 0, length: 32);
			var odd = combinerState.Slice(start: 32, length: 32);

			// apply len zeros to crc1 (first square will put the operator for one zero byte, eight zero bits, in even)
			do
			{
				// apply zeros operator for this bit of len
				gf2_matrix_square(even, odd);
				if ((len & 1U) != 0U) _current = gf2_matrix_times(even, _current);
				len >>= 1;

				// if no more bits set, then done
				if (len == 0U) break;

				// another iteration of the loop with odd and even swapped
				gf2_matrix_square(odd, even);
				if ((len & 1U) != 0U) _current = gf2_matrix_times(odd, _current);
				len >>= 1;

				// if no more bits set, then done
			} while (len != 0U);

			// finally, combine and return
			_current ^= crc;
		}
	}
}
