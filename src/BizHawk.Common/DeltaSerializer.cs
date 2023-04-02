using System;
using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <summary>
	/// Serializes deltas between data, mainly for ROM like structures which are actually writable, and therefore the differences need to be saved
	/// Uses simple RLE encoding to keep the size down
	/// </summary>
	public static class DeltaSerializer
	{
		public static byte[] GetDelta<T>(ReadOnlySpan<T> original, ReadOnlySpan<T> data)
			where T : unmanaged
		{
			var orignalAsBytes = MemoryMarshal.AsBytes(original);
			var dataAsBytes = MemoryMarshal.AsBytes(data);

			if (orignalAsBytes.Length != dataAsBytes.Length)
			{
				throw new InvalidOperationException($"{nameof(orignalAsBytes.Length)} must equal {nameof(dataAsBytes.Length)}");
			}

			var index = 0;
			var end = dataAsBytes.Length;
			var ret = new byte[end + 4].AsSpan(); // worst case scenario size (i.e. everything is different)
			var retSize = 0;

			while (index < end)
			{
				var blockStart = index;
				while (index < end && orignalAsBytes[index] == dataAsBytes[index])
				{
					index++;
				}

				var same = index - blockStart;

				if (same < 4) // something changed, or we hit end of spans, count how many different bytes come after
				{
					var different = 0;
					while (index < end && same < 8) // in case we hit end of span before, this does nothing and different is 0
					{
						if (orignalAsBytes[index] != dataAsBytes[index])
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
							ret[retSize++] = (byte)(orignalAsBytes[i] ^ dataAsBytes[i]);
						}
					}

					if (same > 0) // same is 4-8, 8 indicates we hit the 8 same bytes threshold, 4-7 indicate hit end of span
					{
						if (same == 8)
						{
							while (index < end && orignalAsBytes[index] == dataAsBytes[index])
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

			return ret.Slice(0, retSize).ToArray();
		}

		public static void ApplyDelta<T>(ReadOnlySpan<T> original, Span<T> data, ReadOnlySpan<byte> delta)
			where T : unmanaged
		{
			var orignalAsBytes = MemoryMarshal.AsBytes(original);
			var dataAsBytes = MemoryMarshal.AsBytes(data);

			if (orignalAsBytes.Length != dataAsBytes.Length)
			{
				throw new InvalidOperationException($"{nameof(orignalAsBytes.Length)} must equal {nameof(dataAsBytes.Length)}");
			}

			var dataPos = 0;
			var dataEnd = dataAsBytes.Length;
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

					if (dataEnd - dataPos < header || deltaEnd - deltaPos < header)
					{
						throw new InvalidOperationException("Corrupt delta header!");
					}

					for (var i = 0; i < header; i++)
					{
						dataAsBytes[dataPos + i] = (byte)(orignalAsBytes[dataPos + i] ^ delta[deltaPos + i]);
					}

					deltaPos += header;
				}
				else // sameness block
				{
					if (dataEnd - dataPos < header)
					{
						throw new InvalidOperationException("Corrupt delta header!");
					}

					orignalAsBytes.Slice(dataPos, header).CopyTo(dataAsBytes.Slice(dataPos, header));
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
