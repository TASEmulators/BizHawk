namespace EMU7800.Core
{
    /// <summary>
    /// Atari 7800 non-bankswitched 32KB cartridge
    /// </summary>
    public sealed class Cart7832 : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // 0x0000:0x8000              0x8000:0x8000 (repeated downward until 0x4000)
        //

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return ROM[addr & 0x7fff]; }
            set { }
        }

        #endregion

        private Cart7832()
        {
        }

        public Cart7832(byte[] romBytes)
        {
            LoadRom(romBytes, 0x8000);
        }

        #region Serialization Members

        public Cart7832(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x8000), 0x8000);
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