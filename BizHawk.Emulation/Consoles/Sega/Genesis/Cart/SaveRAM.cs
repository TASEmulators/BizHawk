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

        public byte[] ReadSaveRam // TODO if you're going to rename this to ReadSaveRam, refactor it to be a method, not a property.
		{
            get { return SaveRAM; }
		}

		public bool SaveRamModified { get; set; }
    }
}
