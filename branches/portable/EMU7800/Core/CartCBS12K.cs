namespace EMU7800.Core
{
    /// <summary>
    /// CBS RAM Plus 12KB bankswitched carts with 128 bytes of RAM.
    /// </summary>
    public sealed class CartCBS12K : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank1: 0x0000:0x1000       Bank1:0x1000:0x1000  Select Segment: 0ff8-0ffa
        // Bank2: 0x1000:0x1000
        // Bank3: 0x2000:0x1000
        //                            Shadows ROM
        //                            0x1000:0x80 RAM write port
        //                            0x1080:0x80 RAM read port
        //
        ushort BankBaseAddr;
        readonly byte[] RAM;

        int Bank
        {
            set { BankBaseAddr = (ushort)(value * 0x1000); }
        }

        #region IDevice Members

        public override void Reset()
        {
            Bank = 2;
        }

        public override byte this[ushort addr]
        {
            get
            {
                addr &= 0x0fff;
                if (addr < 0x0200 && addr >= 0x0100)
                {
                    return RAM[addr & 0xff];
                }
                UpdateBank(addr);
                return ROM[BankBaseAddr + addr];
            }
            set
            {
                addr &= 0x0fff;
                if (addr < 0x0100)
                {
                    RAM[addr & 0xff] = value;
                    return;
                }
                UpdateBank(addr);
            }
        }

        #endregion

        private CartCBS12K()
        {
        }

        public CartCBS12K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x3000);
            Bank = 2;
            RAM = new byte[0x100];
        }

        void UpdateBank(ushort addr)
        {
            if (addr < 0x0ff8 || addr > 0x0ffa) { }
            else
            {
                Bank = addr - 0x0ff8;
            }
        }

        #region Serialization Members

        public CartCBS12K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x3000), 0x3000);
            RAM = input.ReadExpectedBytes(0x100);
            BankBaseAddr = input.ReadUInt16();
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(RAM);
            output.Write(BankBaseAddr);
        }

        #endregion
    }
}