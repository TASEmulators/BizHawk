namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 non-bankswitched 16KB cartridge
    /// </summary>
    public sealed class Cart7816 : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // 0x0000:0x4000              0xC000:0x4000 (repeated downward to 0x4000)
        //

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return ROM[addr & 0x3fff]; }
            set { }
        }

        #endregion

        private Cart7816()
        {
        }

        public Cart7816(byte[] romBytes)
        {
            LoadRom(romBytes, 0x4000);
        }

        #region Serialization Members

        public Cart7816(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x4000), 0x4000);
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