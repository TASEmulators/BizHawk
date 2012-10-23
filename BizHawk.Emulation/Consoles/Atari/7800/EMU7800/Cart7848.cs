namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 non-bankswitched 48KB cartridge
    /// </summary>
    public sealed class Cart7848 : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // 0x0000:0xc000              0x4000:0xc000
        //

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return ROM[addr - 0x4000]; }
            set { }
        }

        #endregion

        private Cart7848()
        {
        }

        public Cart7848(byte[] romBytes)
        {
            LoadRom(romBytes, 0xc000);
        }

        #region Serialization Members

        public Cart7848(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0xc000), 0xc000);
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
        }

        #endregion
    }
}