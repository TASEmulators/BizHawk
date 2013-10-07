using System;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class RomGame
	{
		public byte[] RomData;
		public byte[] FileData;
		public GameInfo GameInfo;
		public string Extension;

		private const int BankSize = 1024;

		public RomGame() { }

		public RomGame(HawkFile file) : this(file, null) { }

		public RomGame(HawkFile file, string patch)
		{
			if (!file.Exists)
				throw new Exception("The file needs to exist, yo.");

			Extension = file.Extension;

			var stream = file.GetStream();
			int fileLength = (int)stream.Length;

			//read the entire contents of the file into memory.
			//unfortunate in the case of large files, but thats what we've got to work with for now.

			// if we're offset exactly 512 bytes from a 1024-byte boundary, 
			// assume we have a header of that size. Otherwise, assume it's just all rom.
			// Other 'recognized' header sizes may need to be added.
			int headerOffset = fileLength % BankSize;
			if (headerOffset.In(0, 512) == false)
			{
				Console.WriteLine("ROM was not a multiple of 1024 bytes, and not a recognized header size: {0}. Assume it's purely ROM data.", headerOffset);
				headerOffset = 0;
			}
			else if (headerOffset > 0)
				Console.WriteLine("Assuming header of {0} bytes.", headerOffset);

			//read the entire file into FileData.
			FileData = new byte[fileLength];
			stream.Read(FileData, 0, fileLength);

			//if there was no header offset, RomData is equivalent to FileData 
			//(except in cases where the original interleaved file data is necessary.. in that case we'll have problems.. 
			//but this whole architecture is not going to withstand every peculiarity and be fast as well.
			if (headerOffset == 0)
			{
				RomData = FileData;
			}
			else
			{
				//if there was a header offset, read the whole file into FileData and then copy it into RomData (this is unfortunate, in case RomData isnt needed)
				int romLength = fileLength - headerOffset;
				RomData = new byte[romLength];
				Buffer.BlockCopy(FileData, headerOffset, RomData, 0, romLength);
			}

			if (file.Extension == ".SMD")
				RomData = DeInterleaveSMD(RomData);

			if (file.Extension == ".Z64" || file.Extension == ".N64" || file.Extension == ".V64")
				RomData = MutateSwapN64(RomData);

			//note: this will be taking several hashes, of a potentially large amount of data.. yikes!
			GameInfo = Database.GetGameInfo(RomData, file.Name);
			
			CheckForPatchOptions();

			if (patch != null)
			{
				using (var patchFile = new HawkFile(patch))
				{
					patchFile.BindFirstOf("IPS");
					if (patchFile.IsBound)
						IPS.Patch(RomData, patchFile.GetStream());
				}
			}
		}

		private static byte[] DeInterleaveSMD(byte[] source)
		{
			// SMD files are interleaved in pages of 16k, with the first 8k containing all 
			// odd bytes and the second 8k containing all even bytes.

			int size = source.Length;
			if (size > 0x400000) size = 0x400000;
			int pages = size / 0x4000;
			byte[] output = new byte[size];

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

		private unsafe static byte[] MutateSwapN64(byte[] source)
		{
			// N64 roms are in one of the following formats:
			//  .Z64 = No swapping
			//  .N64 = Word Swapped
			//  .V64 = Bytse Swapped

			// File extension does not always match the format

			int size = source.Length;

			// V64 format
			fixed (byte* pSource = &source[0])
			{
				if (pSource[0] == 0x37)
				{
					for (int i = 0; i < size; i += 2)
					{
						byte temp = pSource[i];
						pSource[i] = pSource[i + 1];
						pSource[i + 1] = temp;
					}
				}
				// N64 format
				else if (pSource[0] == 0x40)
				{
					for (int i = 0; i < size; i += 4)
					{
						//output[i] = source[i + 3];
						//output[i + 3] = source[i];
						//output[i + 1] = source[i + 2];
						//output[i + 2] = source[i + 1];

						byte temp = pSource[i];
						pSource[i] = source[i + 3];
						pSource[i + 3] = temp;

						temp = pSource[i + 1];
						pSource[i + 1] = pSource[i + 2];
						pSource[i + 2] = temp;
					}
				}
				// Z64 format (or some other unknown format)
				else
				{
				}
			}

			return source;
		}

		private void CheckForPatchOptions()
		{
			try
			{
				if (GameInfo["PatchBytes"])
				{
				    string args = GameInfo.OptionValue("PatchBytes");
					foreach (var val in args.Split(','))
					{
						var split = val.Split(':');
						int offset = int.Parse(split[0], NumberStyles.HexNumber);
						byte value = byte.Parse(split[1], NumberStyles.HexNumber);
						RomData[offset] = value;
					}
				}
			}
			catch (Exception) { } // No need for errors in patching to propagate.
		}
	}
}
