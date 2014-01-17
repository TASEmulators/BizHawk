namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 SuperGame S4 bankswitched cartridge
    /// </summary>
    public sealed class Cart78S4 : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank0: 0x00000:0x4000
        // Bank1: 0x04000:0x4000      0x4000:0x4000  Bank2
        // Bank2: 0x08000:0x4000      0x8000:0x4000  Bank0 (0 on startup)
        // Bank3: 0x0c000:0x4000      0xc000:0x4000  Bank3
        //
        // Banks 0-3 are the same as banks 4-7
        //
        readonly byte[] RAM;
        readonly int[] Bank = new int[4];

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get
            {
                if (RAM != null && addr >= 0x6000 && addr <= 0x7fff)
                {
                    return RAM[addr & 0x1fff];
                }
                return ROM[(Bank[addr >> 14] << 14) | (addr & 0x3fff)];
            }
            set
            {
                if (RAM != null && addr >= 0x6000 && addr <= 0x7fff)
                {
                    RAM[addr & 0x1fff] = value;
                }
                else if ((addr >> 14) == 2)
                {
                    Bank[2] = value & 3;
                }
            }
        }

        #endregion

        private Cart78S4()
        {
        }

        public Cart78S4(byte[] romBytes, bool needRAM)
        {
            if (needRAM)
            {
                RAM = new byte[0x2000];
            }

            LoadRom(romBytes, 0xffff);

            Bank[1] = 2;
            Bank[2] = 0;
            Bank[3] = 3;
        }

        #region Serialization Members

        public Cart78S4(DeserializationContext input, MachineBase m) : base(input)
        {
            var version = input.CheckVersion(1, 2);
            LoadRom(input.ReadBytes());
            Bank = input.ReadIntegers(4);
            if (version == 1)
                input.ReadInt32();
            RAM = input.ReadOptionalBytes();
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