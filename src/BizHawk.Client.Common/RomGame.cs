using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class RomGame
	{
		public byte[] RomData { get; }
		public byte[] FileData { get; }
		public GameInfo GameInfo { get; }
		public string Extension { get; }

		// false positives of the header check (512 bytes in intv games)
		public const string Flappy_Bird_INTV = "SHA1:C4ABF77C2CFC0E7B590E2260C56360F9738C45D6";
		public const string Minehunter_INTV = "SHA1:F91D4507BAF41626D839308659E68DE048C767C8";

		private const int BankSize = 1024;

		// 3DS roms typically exceed 2GiB, so we don't want to load them into memory
		// TODO: Don't rely only on extension if this is actually a 3DS ROM (validate in some way)
		// TODO: ELF is another 3DS extension, but it's too generic / might be used for other systems...
		public static bool Is3DSRom(string ext)
			=> ext is ".3DS" or ".3DSX" or ".AXF" or ".CCI" or ".CXI" or ".APP" or ".CIA";

		public RomGame(HawkFile file)
			: this(file, null)
		{
		}

		/// <exception cref="Exception"><paramref name="file"/> does not exist</exception>
		public RomGame(HawkFile file, string patch)
		{
			if (!file.Exists)
			{
				throw new Exception("The file needs to exist, yo.");
			}

			Extension = file.Extension.ToUpperInvariant();

			if (Is3DSRom(Extension))
			{
				if (file.IsArchive)
				{
					throw new InvalidOperationException("3DS ROMs cannot be in archives.");
				}

				Console.WriteLine("3DS ROM detected, skipping full file hashing...");

				FileData = RomData = [ ];
				GameInfo = new()
				{
					Name = Path.GetFileNameWithoutExtension(file.Name).Replace('_', ' '),
					System = VSystemID.Raw.N3DS,
					Hash = "N/A",
					Status = RomStatus.NotInDatabase,
					NotInDatabase = true
				};

				if (!string.IsNullOrWhiteSpace(GameInfo.Name) && GameInfo.Name == GameInfo.Name.ToUpperInvariant())
				{
					GameInfo.Name = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(GameInfo.Name.ToLowerInvariant());
				}

				return;
			}

			var stream = file.GetStream();
			int fileLength = (int)stream.Length;

			// read the entire contents of the file into memory.
			// unfortunate in the case of large files, but that's what we've got to work with for now.

			// if we're offset exactly 512 bytes from a 1024-byte boundary,
			// assume we have a header of that size. Otherwise, assume it's just all rom.
			// Other 'recognized' header sizes may need to be added.
			int headerOffset = fileLength % BankSize;
			if (!headerOffset.In(0, 128, 512))
			{
				Console.WriteLine("ROM was not a multiple of 1024 bytes, and not a recognized header size: {0}. Assume it's purely ROM data.", headerOffset);
				headerOffset = 0;
			}
			else if (headerOffset > 0)
			{
				Console.WriteLine("Assuming header of {0} bytes.", headerOffset);
			}

			// read the entire file into FileData.
			FileData = new byte[fileLength];
			stream.Position = 0;
			var bytesRead = stream.Read(FileData, offset: 0, count: fileLength);
			Debug.Assert(bytesRead == fileLength, "failed to read whole rom stream");

			string SHA1_check = SHA1Checksum.ComputePrefixedHex(FileData);

			// if there was no header offset, RomData is equivalent to FileData
			// (except in cases where the original interleaved file data is necessary.. in that case we'll have problems..
			// but this whole architecture is not going to withstand every peculiarity and be fast as well.
			if (headerOffset == 0)
			{
				RomData = FileData;
			}
			else if (file.Extension is ".cdt" or ".csw" or ".dsk" or ".pzx" or ".tap" or ".tzx" or ".wav")
			{
				// these are not roms. unfortunately if treated as such there are certain edge-cases
				// where a header offset is detected. This should mitigate this issue until a cleaner solution is found
				// (-Asnivor)
				RomData = FileData;
			}
			else if (SHA1_check == Flappy_Bird_INTV || SHA1_check == Minehunter_INTV)
			{
				// several INTV games have sizes that are multiples of 512 bytes
				Console.WriteLine("False positive detected in Header Check, using entire file.");
				RomData = FileData;
			}
			else
			{
				// if there was a header offset, read the whole file into FileData and then copy it into RomData (this is unfortunate, in case RomData isn't needed)
				int romLength = fileLength - headerOffset;
				RomData = new byte[romLength];
				Buffer.BlockCopy(FileData, headerOffset, RomData, 0, romLength);
			}

			if (file.Extension == ".smd")
			{
				RomData = DeInterleaveSMD(RomData);
			}

			if (file.Extension is ".n64" or ".v64" or ".z64") _ = N64RomByteswapper.ToZ64Native(RomData); //TODO don't use file extension for N64 rom detection (yes that means detecting all formats before converting to Z64)

			// note: this will be taking several hashes, of a potentially large amount of data.. yikes!
			GameInfo = Database.GetGameInfo(RomData, file.Name);

			if (GameInfo.NotInDatabase && headerOffset == 128 && file.Extension == ".a78")
			{
				// if the game is not in the DB, add the header back in so the core can use it
				// for now only .A78 games, but probably should be for other systems as well
				RomData = FileData;
			}

			CheckForPatchOptions();

			if (patch is null) return;
			using var patchFile = new HawkFile(patch);
			patchFile.BindFirstOf(".ips");
			if (!patchFile.IsBound) patchFile.BindFirstOf(".bps");
			if (!patchFile.IsBound) return;
			var patchBytes = patchFile.GetStream().ReadAllBytes();
			if (BPSPatcher.IsIPSFile(patchBytes))
			{
				RomData = BPSPatcher.Patch(RomData, new BPSPatcher.IPSPayload(patchBytes));
			}
			else if (BPSPatcher.IsBPSFile(patchBytes, out var patchStruct))
			{
				var ignoreBaseChecksum = true; //TODO check base checksum and ask user before continuing
				RomData = BPSPatcher.Patch(StripSNESDumpHeader(RomData), patchStruct, out var checksumsMatch);
				if (!checksumsMatch && !ignoreBaseChecksum) throw new Exception("BPS patch didn't produce the expected output");
			}
			else
			{
				throw new Exception("doesn't appear to be a BPS or IPS patch");
			}
		}

		/// <remarks>https://snes.nesdev.org/wiki/ROM_file_formats#Detecting_Headered_ROM</remarks>
		private static ReadOnlySpan<byte> StripSNESDumpHeader(ReadOnlySpan<byte> rom)
			=> rom.Length % 512 is 0 ? rom : rom.Slice(start: 512);

		private static byte[] DeInterleaveSMD(byte[] source)
		{
			// SMD files are interleaved in pages of 16k, with the first 8k containing all
			// odd bytes and the second 8k containing all even bytes.
			int size = source.Length;
			if (size > 0x400000)
			{
				size = 0x400000;
			}

			int pages = size / 0x4000;
			var output = new byte[size];

			for (int page = 0; page < pages; page++)
			{
				for (int i = 0; i < 0x2000; i++)
				{
					output[(page * 0x4000) + (i * 2) + 0] = source[(page * 0x4000) + 0x2000 + i];
					output[(page * 0x4000) + (i * 2) + 1] = source[(page * 0x4000) + 0x0000 + i];
				}
			}

			return output;
		}

		private void CheckForPatchOptions()
		{
			try
			{
				if (GameInfo["PatchBytes"])
				{
					var args = GameInfo.OptionValue("PatchBytes");
					foreach (var val in args.Split(','))
					{
						var split = val.Split(':');
						int offset = int.Parse(split[0], NumberStyles.HexNumber);
						byte value = byte.Parse(split[1], NumberStyles.HexNumber);
						RomData[offset] = value;
					}
				}
			}
			catch (Exception)
			{
				// No need for errors in patching to propagate.
			}
		}
	}
}
