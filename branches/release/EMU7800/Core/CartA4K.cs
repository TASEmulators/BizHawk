namespace EMU7800.Core
{
    /// <summary>
    /// Atari standard 4KB carts (no bankswitching)
    /// </summary>
    public sealed class CartA4K : Cart
    {
        #region IDevice Members

        public override void Reset() { }

        public override byte this[ushort addr]
        {
            get { return ROM[addr & 0x0fff]; }
            set { }
        }

        #endregion

        private CartA4K()
        {
        }

        public CartA4K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x1000);
        }

        #region Serialization Members

        public CartA4K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x1000), 0x1000);
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