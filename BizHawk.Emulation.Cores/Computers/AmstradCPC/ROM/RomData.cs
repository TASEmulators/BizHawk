
namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// Information about Amstrad ROM
    /// </summary>
    public class RomData
    {
        /// <summary>
        /// ROM Contents
        /// </summary>
        public byte[] RomBytes
        {
            get { return _romBytes; }
            set { _romBytes = value; }
        }
        private byte[] _romBytes;

        public enum ROMChipType
        {
            Lower,
            Upper
        }

        /// <summary>
        /// Whether this is an Upper or Lower ROM
        /// </summary>
        public ROMChipType ROMType;

        /// <summary>
        /// The designated ROM position for this ROM
        /// </summary>
        public int ROMPosition;

        /// <summary>
        /// Initialise a RomData object
        /// </summary>
        /// <param name="machineType"></param>
        /// <param name="rom"></param>
        /// <param name="type"></param>
        /// <param name="romPosition"></param>
        /// <returns></returns>
        public static RomData InitROM(MachineType machineType, byte[] rom, ROMChipType type, int romPosition = 0)
        {
            RomData RD = new RomData();
            RD.RomBytes = new byte[rom.Length];
            RD.RomBytes = rom;
            RD.ROMType = type;

            if (type == ROMChipType.Upper)
            {
                RD.ROMPosition = romPosition;
            }

            for (int i = 0; i < rom.Length; i++)
                RD.RomBytes[i] = rom[i];

            switch (machineType)
            {
                case MachineType.CPC464:
                    break;
                case MachineType.CPC6128:
                    break;
            }

            return RD;
        }
    }
}
