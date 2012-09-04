using System;
using System.Globalization;

namespace BizHawk.MultiClient
{
	public class RomGame
	{
		public byte[] RomData;
		public byte[] FileData;
		public GameInfo GameInfo;

		private const int BankSize = 1024;

		public RomGame() { }

		public RomGame(HawkFile file) : this(file, null) { }

		public RomGame(HawkFile file, string patch)
		{
			if (!file.Exists)
				throw new Exception("The file needs to exist, yo.");

			var stream = file.GetStream();
			FileData = Util.ReadAllBytes(stream);

			// if we're offset exactly 512 bytes from a 1024-byte boundary, 
			// assume we have a header of that size. Otherwise, assume it's just all rom.
			// Other 'recognized' header sizes may need to be added.
			int header = (int)(stream.Length % BankSize);
			if (header.In(0, 512) == false)
			{
				Console.WriteLine("ROM was not a multiple of 1024 bytes, and not a recognized header size: {0}. Assume it's purely ROM data.", header);
				header = 0;
			}
			else if (header > 0)
				Console.WriteLine("Assuming header of {0} bytes.", header);

			stream.Position = header;
			int length = (int)stream.Length - header;

			RomData = new byte[length];
			stream.Read(RomData, 0, length);

			if (file.Extension == ".SMD")
				RomData = DeInterleaveSMD(RomData);

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
