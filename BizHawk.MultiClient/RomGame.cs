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
        private List<string> options;
        private const int BankSize = 4096;

        public RomGame(HawkFile file) : this(file, null){}

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

			if (patch != null)
			{
				using (var patchFile = new HawkFile(patch))
				{
					patchFile.BindFirstOf("IPS");
					if(patchFile.IsBound)
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
            } catch (Exception) { } // No need for errors in patching to propagate.
        }

        public byte[] GetRomData() { return RomData; }
		public byte[] GetFileData() { return FileData; }
        public IList<string> GetOptions() { return options; }
		public string Name { get { return name; } set { name = value; } }

        public string SaveRamPath
        {
            get
            {
                switch (System)
                {
                    case "SMS": return PathManager.MakeAbsolutePath(Global.Config.PathSMSSaveRAM, "SMS") + Name + ".SaveRAM";
                    case "GG": return PathManager.MakeAbsolutePath(Global.Config.PathGGSaveRAM, "GG") + Name + ".SaveRAM";
                    case "SG": return PathManager.MakeAbsolutePath(Global.Config.PathSGSaveRAM, "SG") + Name + ".SaveRAM";
                    case "SGX": return PathManager.MakeAbsolutePath(Global.Config.PathPCESaveRAM, "PCE") + Name + ".SaveRAM";
                    case "PCE": return PathManager.MakeAbsolutePath(Global.Config.PathPCESaveRAM, "PCE") + Name + ".SaveRAM";
                    case "GB": return PathManager.MakeAbsolutePath(Global.Config.PathGBSaveRAM, "GB") + Name + ".SaveRAM";
                    case "GEN": return PathManager.MakeAbsolutePath(Global.Config.PathGenesisSaveRAM, "GEN") + Name + ".SaveRAM";
                    case "NES": return PathManager.MakeAbsolutePath(Global.Config.PathNESSaveRAM, "NES") + Name + ".SaveRAM";
                    case "TI83": return PathManager.MakeAbsolutePath(Global.Config.PathTI83SaveRAM, "TI83") + Name + ".SaveRAM";
                    default:    return "";
                }
            }
        }

        public string SaveStatePrefix
        {
            get
            {
                string Bind = "";
                if (Global.Config.BindSavestatesToMovies && Global.MainForm.UserMovie.GetMovieMode() != MOVIEMODE.FINISHED) //TODO: what about movie finished?
                    Bind += " - " + Path.GetFileNameWithoutExtension(Global.MainForm.UserMovie.GetFilePath());
                switch (System)
                {
                    case "SMS": return PathManager.MakeAbsolutePath(Global.Config.PathSMSSavestates, "SMS") + "/" + Name + Bind;
                    case "GG": return PathManager.MakeAbsolutePath(Global.Config.PathGGSavestates, "GG") + "/" + Name + Bind;
                    case "SG": return PathManager.MakeAbsolutePath(Global.Config.PathSGSavestates, "SG") + "/" + Name + Bind;
                    case "PCE": return PathManager.MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE") + "/" + Name + Bind;
                    case "SGX": return PathManager.MakeAbsolutePath(Global.Config.PathPCESavestates, "PCE") + "/" + Name + Bind;
                    case "GB":  return PathManager.MakeAbsolutePath(Global.Config.PathGBSavestates, "GB") + "/" + Name + Bind;
                    case "GEN": return PathManager.MakeAbsolutePath(Global.Config.PathGenesisSavestates, "GEN") + "/" + Name + Bind;
                    case "NES": return PathManager.MakeAbsolutePath(Global.Config.PathNESSavestates, "NES") + "/" + Name + Bind;
                    case "TI83": return PathManager.MakeAbsolutePath(Global.Config.PathTI83Savestates, "TI83") + "/" + Name + Bind;
                    default:    return "";
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
                    case "SMS": return "SMS/Movie/" + Name;
                    case "GG":  return "Game Gear/Movie/" + Name;
                    case "SG":  return "SG-1000/Movie/" + Name;
                    case "PCE": return "TurboGrafx/Movie/" + Name;
                    case "SGX": return "TurboGrafx/Movie/" + Name;
                    case "GB":  return "Gameboy/Movie/" + Name;
                    case "GEN": return "Genesis/Movie/" + Name;
                    case "NES": return "NES/Movie/" + Name;
                    case "TI83": return "TI83/Movie/" + Name;
                    default:    return "";
                }
            }
        }

        public string ScreenshotPrefix
        {
            get
            {
                switch (System)
                {
                    case "SMS": return PathManager.MakeAbsolutePath(Global.Config.PathSMSScreenshots, "SMS") + "/" + Name;
                    case "GG": return PathManager.MakeAbsolutePath(Global.Config.PathGGScreenshots, "GG") + "/" + Name;
                    case "SG": return PathManager.MakeAbsolutePath(Global.Config.PathSGScreenshots, "SG") + "/" + Name;
                    case "PCE": return PathManager.MakeAbsolutePath(Global.Config.PathPCEScreenshots, "PCE") + "/" + Name;
                    case "SGX": return PathManager.MakeAbsolutePath(Global.Config.PathPCEScreenshots, "PCE") + "/" + Name;
                    case "GB": return PathManager.MakeAbsolutePath(Global.Config.PathGBScreenshots, "GB") + "/" + Name;
                    case "GEN": return PathManager.MakeAbsolutePath(Global.Config.PathGenesisScreenshots, "GEN") + "/" + Name;
                    case "NES": return PathManager.MakeAbsolutePath(Global.Config.PathNESScreenshots, "NES") + "/" + Name;
                    case "TI83": return PathManager.MakeAbsolutePath(Global.Config.PathTI83Screenshots, "TI83") + "/" + Name;
                    default:    return "";
                }
            }
        }
    }
}
