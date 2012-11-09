namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 SuperGame S9 bankswitched cartridge
    /// </summary>
    public sealed class Cart78S9 : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank0: 0x00000:0x4000
        // Bank1: 0x04000:0x4000      0x4000:0x4000  Bank0
        // Bank2: 0x08000:0x4000      0x8000:0x4000  Bank0-8 (1 on startup)
        // Bank3: 0x0c000:0x4000      0xc000:0x4000  Bank8
        // Bank4: 0x10000:0x4000
        // Bank5: 0x14000:0x4000
        // Bank6: 0x18000:0x4000
        // Bank7: 0x1c000:0x4000
        // Bank8: 0x20000:0x4000
        //
        readonly int[] Bank = new int[4];

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return ROM[ (Bank[addr >> 14] << 14) | (addr & 0x3fff) ]; }
            set
            {
                if ((addr >> 14) == 2 && value < 8)
                {
                    Bank[2] = (value + 1);
                }
            }
        }

        #endregion

        private Cart78S9()
        {
        }

        public Cart78S9(byte[] romBytes)
        {
            Bank[1] = 0;
            Bank[2] = 1;
            Bank[3] = 8;
            LoadRom(romBytes, 0x24000);
        }

        #region Serialization Members

        public Cart78S9(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadBytes());
            Bank = input.ReadIntegers(4);
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