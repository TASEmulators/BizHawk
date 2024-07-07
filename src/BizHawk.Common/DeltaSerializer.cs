using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>
	/// Serializes deltas between data, mainly for ROM like structures which are actually writable, and therefore the differences need to be saved
	/// Uses a simple delta format in order to keep size down
	/// DELTA FORMAT DETAILS FOLLOWS
	/// The format comprises of an indeterminate amount of blocks. These blocks start with a 4 byte header. This header is read as a native endian 32-bit two's complement signed integer.
	/// If the header is positive, then the header indicates the amount of bytes which are identical between the original and current spans.
	/// Positive headers are blocks by themselves, so the next header will proceed immediately after a positive header.
	/// If the header is negative, then the header indicates the negation of the amount of bytes which differ between the original and current spans.
	/// A negative header will have the negated header amount of bytes proceed it, which will be the bitwise XOR between the original and differing bytes.
	/// A header of -0x80000000 is considered ill-formed.
	/// This format does not stipulate requirements for whether blocks of non-differing bytes necessarily will use a positive header.
	/// Thus, an implementation is free to use negative headers only, although without combination of positive headers, this will obviously not have great results wrt final size.
	/// More practically, an implementation may want to avoid using positive headers when the block is rather small (e.g. smaller than the header itself, and thus not shrinking the result).
	/// Subsequently, it may not mind putting some identical bytes within the negative header's block.
	/// XORing the same values result in 0, so doing this will not leave trace of the original data.
	/// </summary>
	public static class DeltaSerializer
	{
		public static ReadOnlySpan<byte> GetDelta<T>(ReadOnlySpan<T> original, ReadOnlySpan<T> current)
			where T : unmanaged
		{
			var orignalAsBytes = MemoryMarshal.AsBytes(original);
			var currentAsBytes = MemoryMarshal.AsBytes(current);

			if (orignalAsBytes.Length != currentAsBytes.Length)
			{
				throw new InvalidOperationException($"{nameof(orignalAsBytes.Length)} must equal {nameof(currentAsBytes.Length)}");
			}

			var index = 0;
			var end = currentAsBytes.Length;
			var ret = new byte[end + 4].AsSpan(); // worst case scenario size (i.e. everything is different)
			var retSize = 0;

			while (index < end)
			{
				var blockStart = index;
				while (index < end && orignalAsBytes[index] == currentAsBytes[index])
				{
					index++;
				}

				var same = index - blockStart;

				if (same < 4) // something changed, or we hit end of spans, count how many different bytes come after
				{
					var different = 0;
					while (index < end && same < 8) // in case we hit end of span before, this does nothing and different is 0
					{
						if (orignalAsBytes[index] != currentAsBytes[index])
						{
							different++;
							same = 0; // note: same is set to 0 on first iteration
						}
						else
						{
							// we don't end on hitting a same byte, only after a sufficent number of same bytes are encountered
							// this would help against possibly having a few stray same bytes splattered around changes
							same++;
						}

						index++;
					}

					if (different > 0) // only not 0 if index == end immediately
					{
						different = index - blockStart - same;
	
						if (same < 4) // we have different bytes, but we hit the end of the spans before the 8 limit, and we have less than what a same block will save
						{
							different += same;
							same = 0;
						}

						different = -different; // negative is a signal that these are different bytes
						MemoryMarshal.Write(ret.Slice(retSize, 4), ref different);
						retSize += 4;
						for (var i = blockStart; i < index - same; i++)
						{
							ret[retSize++] = (byte)(orignalAsBytes[i] ^ currentAsBytes[i]);
						}

						blockStart = index - same;
					}

					if (same > 0) // same is 4-8, 8 indicates we hit the 8 same bytes threshold, 4-7 indicate hit end of span
					{
						if (same == 8)
						{
							while (index < end && orignalAsBytes[index] == currentAsBytes[index])
							{
								index++;
							}
						}

						same = index - blockStart;
						MemoryMarshal.Write(ret.Slice(retSize, 4), ref same);
						retSize += 4;
					}
				}
				else // count amount of same bytes in this block
				{
					MemoryMarshal.Write(ret.Slice(retSize, 4), ref same);
					retSize += 4;
				}
			}

			return ret.Slice(0, retSize);
		}

		public static void ApplyDelta<T>(ReadOnlySpan<T> original, Span<T> current, ReadOnlySpan<byte> delta)
			where T : unmanaged
		{
			var orignalAsBytes = MemoryMarshal.AsBytes(original);
			var currentAsBytes = MemoryMarshal.AsBytes(current);

			if (orignalAsBytes.Length != currentAsBytes.Length)
			{
				throw new InvalidOperationException($"{nameof(orignalAsBytes.Length)} must equal {nameof(currentAsBytes.Length)}");
			}

			var dataPos = 0;
			var dataEnd = currentAsBytes.Length;
			var deltaPos = 0;
			var deltaEnd = delta.Length;

			while (deltaPos < deltaEnd)
			{
				if (deltaEnd - deltaPos < 4)
				{
					throw new InvalidOperationException("Hit end of delta unexpectingly!");
				}

				var header = MemoryMarshal.Read<int>(delta.Slice(deltaPos, 4));
				deltaPos += 4;
				if (header < 0) // difference block
				{
					header = -header;

					if (header == int.MinValue || dataEnd - dataPos < header || deltaEnd - deltaPos < header)
					{
						throw new InvalidOperationException("Corrupt delta header!");
					}

					for (var i = 0; i < header; i++)
					{
						currentAsBytes[dataPos + i] = (byte)(orignalAsBytes[dataPos + i] ^ delta[deltaPos + i]);
					}

					deltaPos += header;
				}
				else // sameness block
				{
					if (dataEnd - dataPos < header)
					{
						throw new InvalidOperationException("Corrupt delta header!");
					}

					orignalAsBytes.Slice(dataPos, header).CopyTo(currentAsBytes.Slice(dataPos, header));
				}

				dataPos += header;
			}

			if (dataPos != dataEnd)
			{
				throw new InvalidOperationException("Did not reach end of data after applying delta!");
			}
		}
	}
}
