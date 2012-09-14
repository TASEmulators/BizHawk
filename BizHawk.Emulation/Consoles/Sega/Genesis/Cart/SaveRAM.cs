using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    partial class Genesis
    {
        bool SaveRamEnabled = false;
        int SaveRamStartOffset;
        int SaveRamEndOffset;
        int SaveRamLength;

        byte[] SaveRAM = new byte[0];

        void InitializeSaveRam(GameInfo game)
        {
            if (game["SaveRamOffset"])
            {
                SaveRamEnabled = true;
                SaveRamStartOffset = game.GetHexValue("SaveRamOffset");
                SaveRamLength = game.GetHexValue("SaveRamLength");
                SaveRamEndOffset = SaveRamStartOffset + SaveRamLength;
                SaveRAM = new byte[SaveRamLength];
            }
        }

		public byte[] ReadSaveRam() { return (byte[])SaveRAM.Clone(); }
		public void StoreSaveRam(byte[] data) { Array.Copy(data, SaveRAM, data.Length); }
		public void ClearSaveRam() { SaveRAM = new byte[SaveRAM.Length]; }

		public bool SaveRamModified { get; set; }
    }
}
