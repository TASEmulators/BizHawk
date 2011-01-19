using System;

namespace BizHawk.MultiClient
{
    public class RomGame : IGame
    {
        public byte[] RomData;
        public string System;

        private string name;
        private string[] options = new string[0];
        private const int BankSize = 4096;

        public RomGame(string path) : this(path, null){}

        public RomGame(string path, string patch)
        {
            using (var file = new HawkFile(path))
            {
                if (!file.Exists)
                    throw new Exception("The file needs to exist, yo.");

                var stream = file.GetStream();
                 
                int header = (int) (stream.Length%BankSize);
                stream.Position = header;
                int length = (int) stream.Length - header;

                RomData = new byte[length];
                stream.Read(RomData, 0, length);

                if (file.Extension == "SMD")
                    RomData = DeInterleaveSMD(RomData);

                var info = Database.GetGameInfo(RomData, file.FullName);
                name = info.Name;
                System = info.System;
                options = info.GetOptions();
            }

            if (patch != null)
            {
                using (var stream = new HawkFile(patch).GetStream())
                {
                    IPS.Patch(RomData, stream);
                }    
            }
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

        public byte[] GetRomData() { return RomData; }
        public string[] GetOptions() { return options; }
        public string Name { get { return name; } }

        public string SaveRamPath
        {
            get
            {
                switch (System)
                {
                    case "SMS": return "SMS/SaveRAM/" + Name + ".SaveRAM";
                    case "GG":  return "Game Gear/SaveRAM/" + Name + ".SaveRAM";
                    case "SG":  return "SG-1000/SaveRAM/" + Name + ".SaveRAM";
                    case "SGX": return "TurboGrafx/SaveRAM/" + Name + ".SaveRAM";
                    case "PCE": return "TurboGrafx/SaveRAM/" + Name + ".SaveRAM";
                    case "GB":  return "Gameboy/SaveRAM/" + Name + ".SaveRAM";
                    case "GEN": return "Genesis/SaveRAM/" + Name + ".SaveRAM";
                    case "NES": return "NES/SaveRAM/" + Name + ".SaveRAM";
                    default:    return "";
                }
            }
        }

        public string SaveStatePrefix
        {
            get
            {
                switch (System)
                {
                    case "SMS": return "SMS/State/" + Name;
                    case "GG":  return "Game Gear/State/" + Name;
                    case "SG":  return "SG-1000/State/" + Name;
                    case "PCE": return "TurboGrafx/State/" + Name;
                    case "SGX": return "TurboGrafx/State/" + Name;
                    case "GB":  return "Gameboy/State/" + Name;
                    case "GEN": return "Genesis/State/" + Name;
                    case "NES": return "NES/State/" + Name;
                    default:    return "";
                }
                
            }
        }

        public string MoviePrefix
        {
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
                    case "SMS": return "SMS/Screenshot/" + Name;
                    case "GG":  return "Game Gear/Screenshot/" + Name;
                    case "SG":  return "SG-1000/Screenshot/" + Name;
                    case "PCE": return "TurboGrafx/Screenshot/" + Name;
                    case "SGX": return "TurboGrafx/Screenshot/" + Name;
                    case "GB":  return "Gameboy/Screenshot/" + Name;
                    case "GEN": return "Genesis/Screenshot/" + Name;
                    case "NES": return "NES/Screenshot/" + Name;
                    default:    return "";
                }
            }
        }
    }
}
