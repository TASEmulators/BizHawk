using System;

namespace BizHawk.Emulation.Consoles.Sega
{
    partial class Genesis
    {
        bool SaveRamEnabled;
        bool SaveRamEveryOtherByte;
        int SaveRamStartOffset;
        int SaveRamEndOffset;
        int SaveRamLength;

        byte[] SaveRAM = new byte[0];

        void InitializeSaveRam(GameInfo game)
        {
            // TODO if eeprom in use, abort saveram check

            if (game["DisableSaveRam"] || RH_SRamPresent == false)
                return;

            SaveRamEnabled = true;
            SaveRamEveryOtherByte = RH_SRamCode != 0;
            SaveRamStartOffset = RH_SRamStart;
            SaveRamEndOffset = RH_SRamEnd;

            if (game["SaveRamStartOffset"])
                SaveRamStartOffset = game.GetHexValue("SaveRamStartOffset");
            if (game["SaveRamEndOffset"])  
                SaveRamEndOffset = game.GetHexValue("SaveRamEndOffset");

            SaveRamLength = (SaveRamEndOffset - SaveRamStartOffset) + 1;

            if (SaveRamEveryOtherByte)
            {
                SaveRamStartOffset &= 0xFFFFFE;
                SaveRamEndOffset &= 0xFFFFFE;
                SaveRamLength = ((SaveRamEndOffset - SaveRamStartOffset) / 2) + 1;
            }

            SaveRAM = new byte[SaveRamLength];

            Console.WriteLine("SaveRAM enabled. Start: ${0:X6} End: ${1:X6} Length: ${2:X} Mode: {3}", SaveRamStartOffset, SaveRamEndOffset, SaveRamLength, RH_SRamInterpretation());
        }

		public byte[] ReadSaveRam() { return (byte[])SaveRAM.Clone(); }
		public void StoreSaveRam(byte[] data) { Array.Copy(data, SaveRAM, data.Length); }
		public void ClearSaveRam() { SaveRAM = new byte[SaveRAM.Length]; }

		public bool SaveRamModified { get; set; }
    }
}
