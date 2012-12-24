namespace EMU7800.Core
{
    /// <summary>
    /// Atari standard 16KB bankswitched carts
    /// </summary>
    public sealed class CartA16K : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank1: 0x0000:0x1000       0x1000:0x1000  Bank selected by accessing 0x1ff9-0x1ff9
        // Bank2: 0x1000:0x1000
        // Bank3: 0x2000:0x1000
        // Bank4: 0x3000:0x1000
        //
        ushort BankBaseAddr;

        int Bank
        {
            set { BankBaseAddr = (ushort)(value * 0x1000); }
        }

        #region IDevice Members

        public override void Reset()
        {
            Bank = 0;
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

        private CartA16K()
        {
        }

        public CartA16K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x4000);
            Bank = 0;
        }

        void UpdateBank(ushort addr)
        {
            if (addr < 0x0ff6 || addr > 0x0ff9)
            {}
            else
            {
                Bank = addr - 0x0ff6;
            }
        }

        #region Serialization Members

        public CartA16K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x4000), 0x4000);
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