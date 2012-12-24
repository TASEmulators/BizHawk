namespace EMU7800.Core
{
    /// <summary>
    ///  Atari standard 2KB carts (no bankswitching)
    /// </summary>
    public sealed class CartA2K : Cart
    {
        //
        //  Cart Format                Mapping to ROM Address Space
        //  0x0000:0x0800              0x1000:0x0800
        //                             0x1800:0x0800  (1st 2k bank repeated)
        //

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return ROM[addr & 0x07ff]; }
            set { }
        }

        #endregion

        private CartA2K()
        {
        }

        public CartA2K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x0800);
        }

        public CartA2K(byte[] romBytes, int multicartBankSelector)
        {
            LoadRom(romBytes, 0x800, multicartBankSelector & 0x1f);
        }

        #region Serialization Members

        public CartA2K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x0800), 0x0800);
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