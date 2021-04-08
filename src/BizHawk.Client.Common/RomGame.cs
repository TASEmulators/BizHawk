using System;
using System.Globalization;

using BizHawk.Common;
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

		private const int BankSize = 1024;

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

			var stream = file.GetStream();
			int fileLength = (int)stream.Length;

			// read the entire contents of the file into memory.
			// unfortunate in the case of large files, but that's what we've got to work with for now.

			// if we're offset exactly 512 bytes from a 1024-byte boundary, 
			// assume we have a header of that size. Otherwise, assume it's just all rom.
			// Other 'recognized' header sizes may need to be added.
			int headerOffset = fileLength % BankSize;
			if (headerOffset.In(0, 128, 512) == false)
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
			stream.Read(FileData, 0, fileLength);

			// if there was no header offset, RomData is equivalent to FileData 
			// (except in cases where the original interleaved file data is necessary.. in that case we'll have problems.. 
			// but this whole architecture is not going to withstand every peculiarity and be fast as well.
			if (headerOffset == 0)
			{
				RomData = FileData;
			}
			else if (file.Extension == ".dsk" || file.Extension == ".tap" || file.Extension == ".tzx" ||
				file.Extension == ".pzx" || file.Extension == ".csw" || file.Extension == ".wav" || file.Extension == ".cdt")
			{
				// these are not roms. unfortunately if treated as such there are certain edge-cases
				// where a header offset is detected. This should mitigate this issue until a cleaner solution is found 
				// (-Asnivor)
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

			if (file.Extension == ".z64" || file.Extension == ".n64" || file.Extension == ".v64")
			{
				// Use a simple magic number to detect N64 rom format, then byteswap the ROM to ensure a consistent endianness/order
				RomData = RomData[0] switch
				{
					0x37 => EndiannessUtils.ByteSwap16(RomData), // V64 format (byte swapped)
					0x40 => EndiannessUtils.ByteSwap32(RomData), // N64 format (word swapped)
					_ => RomData // Z64 format (no swap), or something unexpected; in either case do nothing
				};
			}

			// note: this will be taking several hashes, of a potentially large amount of data.. yikes!
			GameInfo = Database.GetGameInfo(RomData, file.Name);

			if (GameInfo.NotInDatabase && headerOffset == 128 && file.Extension == ".a78")
			{
				// if the game is not in the DB, add the header back in so the core can use it
				// for now only .A78 games, but probably should be for other systems as well
				RomData = FileData;
			}
			
			CheckForPatchOptions();

			if (patch != null)
			{
				using var patchFile = new HawkFile(patch);
				patchFile.BindFirstOf(".ips");
				if (patchFile.IsBound)
				{
					RomData = IPS.Patch(RomData, patchFile.GetStream());
				}
			}
		}

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
