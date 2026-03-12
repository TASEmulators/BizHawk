using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Common
{
	public static class BPSPatcher
	{
		/// <remarks>
		/// constructor assumes valid header/footer<br/>
		/// https://github.com/Alcaro/Flips/blob/master/bps_spec.md
		/// </remarks>
		/// <seealso cref="IsBPSFile"/>
		public readonly ref struct BPSPayload
		{
			internal static readonly byte[] HEADER = Encoding.ASCII.GetBytes("BPS1");

			public static int DecodeVarInt(ReadOnlySpan<byte> data, ref int i)
			{
				// no idea how this works or what it will do if a varint is too large, I just copied from the spec --yoshi
				// also casting to int (for use with Span) so the max is even smaller
				ulong num = 0, mul = 1;
				while (true)
				{
					var x = data[i++];
					num += (x & 0b0111_1111UL) * mul;
					if ((x & 0b1000_0000UL) is not 0UL) break;
					mul <<= 7;
					num += mul;
				}
				return (int) num;
			}

			private readonly ReadOnlySpan<byte> _data;

			private readonly bool _isValid;

			private readonly ReadOnlySpan<byte> _sourceChecksum;

			private readonly int _sourceSize;

			private readonly ReadOnlySpan<byte> _targetChecksum;

			public readonly int TargetSize;

			internal readonly ReadOnlySpan<byte> PatchChecksum;

			/// <remarks>assumes valid header/footer</remarks>
			/// <seealso cref="IsBPSFile"/>
			public BPSPayload(ReadOnlySpan<byte> dataWithHeader)
			{
				_isValid = true;
				var i = 4;
				_sourceSize = DecodeVarInt(dataWithHeader, ref i);
				TargetSize = DecodeVarInt(dataWithHeader, ref i);
				var metadataSize = DecodeVarInt(dataWithHeader, ref i);
				if (metadataSize is not 0)
				{
					Console.WriteLine($"ignoring {metadataSize} bytes of .bps metadata");
					i += metadataSize;
				}
				_data = dataWithHeader.Slice(start: i, length: dataWithHeader.Length - 12 - i);
				_sourceChecksum = dataWithHeader.Slice(start: dataWithHeader.Length - 12, length: 4);
				_targetChecksum = dataWithHeader.Slice(start: dataWithHeader.Length - 8, length: 4);
				PatchChecksum = dataWithHeader.Slice(start: dataWithHeader.Length - 4, length: 4);
			}

			/// <returns><see langword="true"/> iff checksums of base rom and result both matched</returns>
			public bool DoPatch(ReadOnlySpan<byte> baseRom, Span<byte> target)
			{
				if (!_isValid) throw new InvalidOperationException(ERR_MSG_UNINIT);
				if (target.Length != TargetSize) throw new ArgumentException(message: $"target buffer too {(target.Length < TargetSize ? "small" : "large")}", paramName: nameof(target));
				if (baseRom.Length != _sourceSize) throw new ArgumentException(message: $"target buffer too {(baseRom.Length < _sourceSize ? "small" : "large")}", paramName: nameof(baseRom));
				var checksumsMatch = CheckCRC(data: baseRom, reversedChecksum: _sourceChecksum);
				var outputOffset = 0;
				var sourceRelOffset = 0;
				var targetRelOffset = 0;
				var i = 0;
				while (i < _data.Length)
				{
					var actionAndLength = DecodeVarInt(_data, ref i);
					var length = (actionAndLength >> 2) + 1;
					switch (actionAndLength & 0b11)
					{
						case 0b00: // SourceRead
							while (length-- is not 0)
							{
								target[outputOffset] = baseRom[outputOffset];
								outputOffset++;
							}
							break;
						case 0b01: // TargetRead
							while (length-- is not 0) target[outputOffset++] = _data[i++];
							break;
						case 0b10: // SourceCopy
							var offset = DecodeVarInt(_data, ref i);
							if ((offset & 1) is 0) sourceRelOffset += offset >> 1;
							else sourceRelOffset -= offset >> 1;
							while (length-- is not 0) target[outputOffset++] = baseRom[sourceRelOffset++];
							break;
						case 0b11: // TargetCopy
							var offset1 = DecodeVarInt(_data, ref i);
							if ((offset1 & 1) is 0) targetRelOffset += offset1 >> 1;
							else targetRelOffset -= offset1 >> 1;
							while (length-- is not 0) target[outputOffset++] = target[targetRelOffset++];
							break;
					}
				}
				return checksumsMatch && CheckCRC(data: target, reversedChecksum: _targetChecksum);
			}
		}

		/// <remarks>
		/// constructor assumes valid header/footer<br/>
		/// https://zerosoft.zophar.net/ips.php
		/// </remarks>
		/// <seealso cref="IsIPSFile"/>
		public ref struct IPSPayload
		{
			internal static readonly byte[] FOOTER = Encoding.ASCII.GetBytes("EOF");

			internal static readonly byte[] HEADER = Encoding.ASCII.GetBytes("PATCH");

			internal static void CheckRomSize(ReadOnlySpan<byte> rom)
			{
				const int MAX_BASE_ROM_LENGTH = 0x1000000; // linked spec says 0xFFFFFF bits [sic] but that makes no sense
				if (MAX_BASE_ROM_LENGTH < rom.Length)
				{
#if true // it can patch the start of the file just fine, no need to throw here
					Console.WriteLine("warning: IPS uses 24-bit offsets, so it can only index the first 0x1000000 octets of this rom");
#else
					throw new ArgumentException(message: "IPS can't patch files this big", paramName: nameof(rom));
#endif
				}
			}

			private static IReadOnlyList<(int PatchOffset, int TargetOffset, int Size, bool IsRLE)> ParseRecords(ReadOnlySpan<byte> data)
			{
				List<(int PatchOffset, int TargetOffset, int Size, bool IsRLE)> records = new();
				try
				{
					var i = 0;
					while (i != data.Length)
					{
						var targetOffset = (data[i++] * 0x10000) | (data[i++] * 0x100) | data[i++];
						var size = (data[i++] * 0x100) | data[i++];
						if (size is 0)
						{
							var rleSize = (data[i++] * 0x100) | data[i++];
							Debug.Assert(rleSize is not 0, "may not run-length-encode nothing");
							records.Add((i, targetOffset, rleSize, true));
							i++;
						}
						else
						{
							records.Add((i, targetOffset, size, false));
							i += size;
						}
					}
				}
				catch (ArgumentOutOfRangeException e)
				{
					throw new Exception("unexpected EOF in IPS patch", e);
				}
				records.Sort((a, b) => (a.TargetOffset + a.Size).CompareTo(b.TargetOffset + b.Size));
				return records;
			}

			private readonly ReadOnlySpan<byte> _data;

			private readonly bool _isValid;

			private IReadOnlyList<(int PatchOffset, int TargetOffset, int Size, bool IsRLE)>? _records;

			internal IReadOnlyList<(int PatchOffset, int TargetOffset, int Size, bool IsRLE)> Records
				=> _isValid ? (_records ??= ParseRecords(_data)) : throw new InvalidOperationException(ERR_MSG_UNINIT);

			/// <remarks>assumes valid header/footer</remarks>
			/// <seealso cref="IsIPSFile"/>
			public IPSPayload(ReadOnlySpan<byte> dataWithHeader)
			{
				_data = dataWithHeader.Slice(start: 5, length: dataWithHeader.Length - 8);
				_isValid = true;
				_records = null;
			}

			internal void DoPatch(Span<byte> rom)
			{
				foreach (var (patchOffset, targetOffset, size, isRLE) in Records)
				{
					if (isRLE)
					{
						var value = _data[patchOffset];
						for (int j = targetOffset, endExclusive = j + size; j < endExclusive; j++) rom[j] = value;
					}
					else
					{
						for (var j = 0; j < size; j++) rom[targetOffset + j] = _data[patchOffset + j];
					}
				}
			}
		}

		private const string ERR_MSG_UNINIT = "uninitialised struct";

		private static bool CheckCRC(ReadOnlySpan<byte> data, ReadOnlySpan<byte> reversedChecksum)
			=> ((ReadOnlySpan<byte>) CRC32Checksum.Compute(data)).ReversedSequenceEqual(reversedChecksum);

		public static bool IsBPSFile(ReadOnlySpan<byte> dataWithHeader, out BPSPayload patchStruct)
		{
			patchStruct = default;
			const int MIN_VALID_BPS_SIZE = 20;
			if (dataWithHeader.Length < MIN_VALID_BPS_SIZE
				|| !dataWithHeader.Slice(start: 0, length: 4).SequenceEqual(BPSPayload.HEADER))
			{
				return false;
			}
			patchStruct = new(dataWithHeader);
			return CheckCRC(data: dataWithHeader.Slice(start: 0, length: dataWithHeader.Length - 4), reversedChecksum: patchStruct.PatchChecksum);
		}

		public static bool IsIPSFile(ReadOnlySpan<byte> dataWithHeader)
		{
			const int MIN_VALID_IPS_SIZE = 8;
			return MIN_VALID_IPS_SIZE <= dataWithHeader.Length
				&& dataWithHeader.Slice(start: 0, length: 5).SequenceEqual(IPSPayload.HEADER)
				&& dataWithHeader.Slice(start: dataWithHeader.Length - 3, length: 3).SequenceEqual(IPSPayload.FOOTER);
		}

		/// <remarks>always allocates a new array</remarks>
		public static byte[] Patch(ReadOnlySpan<byte> baseRom, BPSPayload patch, out bool checksumsMatch)
		{
			var target = new byte[patch.TargetSize];
			checksumsMatch = patch.DoPatch(baseRom: baseRom, target: target);
			return target;
		}

		/// <remarks>may patch in place, returning <paramref name="baseRom"/>, or allocate a new array</remarks>
		public static byte[] Patch(byte[] baseRom, IPSPayload patch)
		{
			var rom = baseRom;
			var last = patch.Records[patch.Records.Count - 1];
			var reqSize = last.TargetOffset + last.Size;
			if (baseRom.Length < reqSize)
			{
				rom = new byte[reqSize];
				Array.Copy(sourceArray: baseRom, destinationArray: rom, length: baseRom.Length);
			}
			IPSPayload.CheckRomSize(rom);
			patch.DoPatch(rom);
			return rom;
		}

		/// <remarks>is this even useful?</remarks>
		public static bool TryPatchInPlace(Span<byte> rom, IPSPayload patch)
		{
			IPSPayload.CheckRomSize(rom);
			var last = patch.Records[patch.Records.Count - 1];
			if (rom.Length < last.TargetOffset + last.Size) return false;
			patch.DoPatch(rom);
			return true;
		}
	}
}
