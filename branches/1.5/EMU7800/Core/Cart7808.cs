namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 non-bankswitched 8KB cartridge
    /// </summary>
    public sealed class Cart7808 : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // 0x0000:0x2000              0xE000:0x2000 (repeated downward to 0x4000)
        //

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return ROM[addr & 0x1fff]; }
            set { }
        }

        #endregion

        private Cart7808()
        {
        }

        public Cart7808(byte[] romBytes)
        {
            LoadRom(romBytes, 0x2000);
        }

        #region Serialization Members

        public Cart7808(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x2000), 0x2000);
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