namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 Activision bankswitched cartridge
    /// </summary>
    public sealed class Cart78AC : Cart
    {
        //
        // Cart Format                 Mapping to ROM Address Space
        // Bank0 : 0x00000:0x2000
        // Bank1 : 0x02000:0x2000
        // Bank2 : 0x04000:0x2000      0x4000:0x2000  Bank13
        // Bank3 : 0x06000:0x2000      0x6000:0x2000  Bank12
        // Bank4 : 0x08000:0x2000      0x8000:0x2000  Bank15
        // Bank5 : 0x0a000:0x2000      0xa000:0x2000  Bank(2*n)   n in [0-7], n=0 on startup
        // Bank6 : 0x0c000:0x2000      0xc000:0x2000  Bank(2*n+1)
        // Bank7 : 0x0e000:0x2000      0xe000:0x2000  Bank14
        // Bank8 : 0x10000:0x2000
        // Bank9 : 0x12000:0x2000
        // Bank10: 0x14000:0x2000
        // Bank11: 0x16000:0x2000
        // Bank12: 0x18000:0x2000
        // Bank13: 0x1a000:0x2000
        // Bank14: 0x1c000:0x2000
        // Bank15: 0x1e000:0x2000
        //
        // Banks are actually 16KB, but handled as 8KB for implementation ease.
        //
        readonly int[] Bank = new int[8];

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get
            {
                return ROM[ (Bank[addr >> 13] << 13) | (addr & 0x1fff) ];
            }
            set
            {
                if ((addr & 0xfff0) == 0xff80)
                {
                    Bank[5] = (addr & 7) << 1;
                    Bank[6] = Bank[5] + 1;
                }
            }
        }

        #endregion

        private Cart78AC()
        {
        }

        public Cart78AC(byte[] romBytes)
        {
            Bank[2] = 13;
            Bank[3] = 12;
            Bank[4] = 15;
            Bank[5] = 0;
            Bank[6] = 1;
            Bank[7] = 14;
            LoadRom(romBytes, 0x20000);
        }

        #region Serialization Members

        public Cart78AC(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadBytes());
            Bank = input.ReadIntegers(8);
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(Bank);
        }

        #endregion
    }
}