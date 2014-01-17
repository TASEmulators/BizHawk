namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 SuperGame bankswitched cartridge
    /// </summary>
    public sealed class Cart78SG : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank0: 0x00000:0x4000
        // Bank1: 0x04000:0x4000      0x4000:0x4000  Bank6
        // Bank2: 0x08000:0x4000      0x8000:0x4000  Bank0-7 (0 on startup)
        // Bank3: 0x0c000:0x4000      0xc000:0x4000  Bank7
        // Bank4: 0x10000:0x4000
        // Bank5: 0x14000:0x4000
        // Bank6: 0x18000:0x4000
        // Bank7: 0x1c000:0x4000
        //
        readonly int[] Bank = new int[4];
        readonly byte[] RAM;

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get
            {
                var bankNo = addr >> 14;
                if (RAM != null && bankNo == 1)
                {
                    return RAM[addr & 0x3fff];
                }
                return ROM[ (Bank[bankNo] << 14) | (addr & 0x3fff) ];
            }
            set
            {
                var bankNo = addr >> 14;
                if (bankNo == 2)
                {
                    Bank[2] = value & 7;
                }
                else if (RAM != null && bankNo == 1)
                {
                    RAM[addr & 0x3fff] = value;
                }
            }
        }

        #endregion

        private Cart78SG()
        {
        }

        public Cart78SG(byte[] romBytes, bool needRAM)
        {
            if (needRAM)
            {
                // This works for titles that use 8KB instead of 16KB
                RAM = new byte[0x4000];
            }

            Bank[1] = 6;
            Bank[2] = 0;
            Bank[3] = 7;

            LoadRom(romBytes, 0x20000);
        }

        #region Serialization Members

        public Cart78SG(DeserializationContext input, MachineBase m) : base(input)
        {
            var version = input.CheckVersion(1, 2);
            LoadRom(input.ReadBytes());
            Bank = input.ReadIntegers(4);
            if (version == 1)
                input.ReadInt32();
            RAM = input.ReadOptionalBytes(0x4000);
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(2);
            output.Write(ROM);
            output.Write(Bank);
            output.WriteOptional(RAM);
        }

        #endregion
    }
}