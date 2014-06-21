namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 Absolute bankswitched cartridge
    /// </summary>
    public sealed class Cart78AB : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank0: 0x00000:0x4000
        // Bank1: 0x04000:0x4000      0x4000:0x4000  Bank0-1 (0 on startup)
        // Bank2: 0x08000:0x4000      0x8000:0x4000  Bank2
        // Bank3: 0x0c000:0x4000      0xc000:0x4000  Bank3
        //
        readonly int[] Bank = new int[4];

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return ROM[ (Bank[addr >> 14] << 14) | (addr & 0x3fff) ]; }
            set
            {
                if ((addr >> 14) == 2)
                {
                    Bank[1] = (value - 1) & 1;
                }
            }
        }

        #endregion

        private Cart78AB()
        {
        }

        public Cart78AB(byte[] romBytes)
        {
            Bank[1] = 0;
            Bank[2] = 2;
            Bank[3] = 3;
            LoadRom(romBytes, 0x10000);
        }

        #region Serialization Members

        public Cart78AB(DeserializationContext input, MachineBase m) : base(input)
        {
            var version = input.CheckVersion(1, 2);
            LoadRom(input.ReadBytes());
            Bank = input.ReadIntegers(4);
            if (version == 1)
                input.ReadInt32();
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(2);
            output.Write(ROM);
            output.Write(Bank);
        }

        #endregion
    }
}