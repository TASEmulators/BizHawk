using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace BizHawk.Common
{
	public static class BPSPatcher
	{
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
							Debug.Assert(rleSize is not 0);
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

		public static bool IsIPSFile(ReadOnlySpan<byte> dataWithHeader)
		{
			const int MIN_VALID_IPS_SIZE = 8;
			return MIN_VALID_IPS_SIZE <= dataWithHeader.Length
				&& dataWithHeader.Slice(start: 0, length: 5).SequenceEqual(IPSPayload.HEADER)
				&& dataWithHeader.Slice(start: dataWithHeader.Length - 3, length: 3).SequenceEqual(IPSPayload.FOOTER);
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
