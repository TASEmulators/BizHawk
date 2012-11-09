namespace EMU7800.Core
{
    /// <summary>
    /// Activison's Robot Tank and Decathlon 8KB bankswitching cart.
    /// </summary>
    public sealed class CartDC8K : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank1: 0x0000:0x1000       0x1000:0x1000  Bank selected by A13=0/1?
        // Bank2: 0x1000:0x1000
        //
        // This does what the Stella code does, which is to follow A13 to determine
        // the bank.  Since A0-A12 are the only significant bits on the program
        // counter, I am unsure how the cart/hardware could utilize this.
        //

        #region IDevice Members

        public override byte this[ushort addr]
        {
            get { return (addr & 0x2000) == 0 ? ROM[addr & 0x0fff + 0x1000] : ROM[addr & 0x0fff]; }
            set { }
        }

        #endregion

        private CartDC8K()
        {
        }

        public CartDC8K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x2000);
        }

        #region Serialization Members

        public CartDC8K(DeserializationContext input, MachineBase m) : base(input)
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