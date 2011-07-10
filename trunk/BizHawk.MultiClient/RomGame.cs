using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BizHawk.MultiClient
{
	public class RomGame : IGame
	{
		public byte[] RomData;
		public byte[] FileData;
		public string System;

		private string name;
		private string filesystemSafeName;
		private List<string> options;
		private const int BankSize = 4096;

		public RomGame(HawkFile file) : this(file, null) { }

		public RomGame(HawkFile file, string patch)
		{
			if (!file.Exists)
				throw new Exception("The file needs to exist, yo.");

			var stream = file.GetStream();

			FileData = Util.ReadAllBytes(stream);

			int header = (int)(stream.Length % BankSize);
			stream.Position = header;
			int length = (int)stream.Length - header;

			RomData = new byte[length];
			stream.Read(RomData, 0, length);

			if (file.Extension == "SMD")
				RomData = DeInterleaveSMD(RomData);

			var info = Database.GetGameInfo(RomData, file.Name);
			name = info.Name;
			System = info.System;
			options = new List<string>(info.GetOptions());
			CheckForPatchOptions();

			//build a safe filesystem name for use in auxilary files (savestates, saveram, etc)
			filesystemSafeName = file.CanonicalName.Replace("|", "+");
			filesystemSafeName = Path.Combine(Path.GetDirectoryName(filesystemSafeName), Path.GetFileNameWithoutExtension(filesystemSafeName));


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

		public void AddOptions(params string[] options)
		{
			this.options.AddRange(options);
		}

		private byte[] DeInterleaveSMD(byte[] source)
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
				foreach (var opt in options)
				{
					if (opt.StartsWith("PatchBytes"))
					{
						var split1 = opt.Split('=');
						foreach (var val in split1[1].Split(','))
						{
							var split3 = val.Split(':');
							int offset = int.Parse(split3[0], NumberStyles.HexNumber);
							byte value = byte.Parse(split3[1], NumberStyles.HexNumber);
							RomData[offset] = value;
						}
					}
				}
			}
			catch (Exception) { } // No need for errors in patching to propagate.
		}

		public byte[] GetRomData() { return RomData; }
		public byte[] GetFileData() { return FileData; }
		public IList<string> GetOptions() { return options; }
		public string Name { get { return name; } set { name = value; } }
		public string FilesystemSafeName { get { return filesystemSafeName; } }

		public string SaveRamPath
		{
			get
			{
				switch (System)
				{
					case "SMS": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathSMSSaveRAM, "SMS"), filesystemSafeName + ".SaveRAM");
					case "GG": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathGGSaveRAM, "GG"), filesystemSafeName + ".SaveRAM");
					case "SG": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathSGSaveRAM, "SG"), filesystemSafeName + ".SaveRAM");
					case "SGX": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathPCESaveRAM, "PCE"), filesystemSafeName + ".SaveRAM");
					case "PCE": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathPCESaveRAM, "PCE"), filesystemSafeName + ".SaveRAM");
					case "GB": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathGBSaveRAM, "GB"), filesystemSafeName + ".SaveRAM");
					case "GEN": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathGenesisSaveRAM, "GEN"), filesystemSafeName + ".SaveRAM");
					case "NES": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathNESSaveRAM, "NES"), filesystemSafeName + ".SaveRAM");
					case "TI83": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathTI83SaveRAM, "TI83"), filesystemSafeName + ".SaveRAM");
					default: return "";
				}
			}
		}

		public string SaveStatePrefix
		{
			get
			{
				string Bind = "";
				if (Global.Config.BindSavestatesToMovies && Global.MainForm.UserMovie.GetMovieMode() != MOVIEMODE.INACTIVE)
					Bind += " - " + Path.GetFileNameWithoutExtension(Global.MainForm.UserMovie.GetFilePath());
				switch (System)
				{
					case "SMS": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathSMSSavestates, "SMS"), filesystemSafeName + Bind);
					case "GG": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathGGSavestates, "GG"), filesystemSafeName + Bind);
					case "SG": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathSGSavestates, "SG"), filesystemSafeName + Bind);
					case "PCE": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE"), filesystemSafeName + Bind);
					case "SGX": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE"), filesystemSafeName + Bind);
					case "GB": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathGBSavestates, "GB"), filesystemSafeName + Bind);
					case "GEN": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathGenesisSavestates, "GEN"), filesystemSafeName + Bind);
					case "NES": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathNESSavestates, "NES"), filesystemSafeName + Bind);
					case "TI83": return Path.Combine(PathManager.MakeAbsolutePath(Global.Config.PathTI83Savestates, "TI83"), filesystemSafeName + Bind);
					default: return "";
				}

			}
		}

		public string MoviePrefix
		{
			//Obsolete because there is one singular Movie path
			get
			{
				switch (System)
				{
					case "SMS": return "SMS/Movie/" + filesystemSafeName;
					case "GG": return "Game Gear/Movie/" + filesystemSafeName;
					case "SG": return "SG-1000/Movie/" + filesystemSafeName;
					case "PCE": return "TurboGrafx/Movie/" + filesystemSafeName;
					case "SGX": return "TurboGrafx/Movie/" + filesystemSafeName;
					case "GB": return "Gameboy/Movie/" + filesystemSafeName;
					case "GEN": return "Genesis/Movie/" + filesystemSafeName;
					case "NES": return "NES/Movie/" + filesystemSafeName;
					case "TI83": return "TI83/Movie/" + filesystemSafeName;
					default: return "";
				}
			}
		}

		public string ScreenshotPrefix
		{
			get
			{
				switch (System)
				{
					case "SMS": return PathManager.MakeAbsolutePath(Global.Config.PathSMSScreenshots, "SMS") + "/" + filesystemSafeName;
					case "GG": return PathManager.MakeAbsolutePath(Global.Config.PathGGScreenshots, "GG") + "/" + filesystemSafeName;
					case "SG": return PathManager.MakeAbsolutePath(Global.Config.PathSGScreenshots, "SG") + "/" + filesystemSafeName;
					case "PCE": return PathManager.MakeAbsolutePath(Global.Config.PathPCEScreenshots, "PCE") + "/" + filesystemSafeName;
					case "SGX": return PathManager.MakeAbsolutePath(Global.Config.PathPCEScreenshots, "PCE") + "/" + filesystemSafeName;
					case "GB": return PathManager.MakeAbsolutePath(Global.Config.PathGBScreenshots, "GB") + "/" + filesystemSafeName;
					case "GEN": return PathManager.MakeAbsolutePath(Global.Config.PathGenesisScreenshots, "GEN") + "/" + filesystemSafeName;
					case "NES": return PathManager.MakeAbsolutePath(Global.Config.PathNESScreenshots, "NES") + "/" + filesystemSafeName;
					case "TI83": return PathManager.MakeAbsolutePath(Global.Config.PathTI83Screenshots, "TI83") + "/" + filesystemSafeName;
					default: return "";
				}
			}
		}
	}
}
