namespace EMU7800.Core
{
    /// <summary>
    /// Atari standard 8KB bankswitched carts
    /// </summary>
    public sealed class CartA8K : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank1: 0x0000:0x1000       0x1000:0x1000  Bank selected by accessing 0x1ff8,0x1ff9
        // Bank2: 0x1000:0x1000
        //
        ushort BankBaseAddr;

        int Bank
        {
            set { BankBaseAddr = (ushort)(value * 0x1000); }
        }

        #region IDevice Members

        public override void Reset()
        {
            Bank = 1;
        }

        public override byte this[ushort addr]
        {
            get
            {
                addr &= 0x0fff;
                UpdateBank(addr);
                return ROM[BankBaseAddr + addr];
            }
            set
            {
                addr &= 0x0fff;
                UpdateBank(addr);
            }
        }

        #endregion

        private CartA8K()
        {
        }

        public CartA8K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x2000);
            Bank = 1;
        }

        void UpdateBank(ushort addr)
        {
            switch(addr)
            {
                case 0x0ff8:
                    Bank = 0;
                    break;
                case 0x0ff9:
                    Bank = 1;
                    break;
            }
        }

        #region Serialization Members

        public CartA8K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x2000), 0x2000);
            BankBaseAddr = input.ReadUInt16();
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(BankBaseAddr);
        }

        #endregion
    }
}