namespace EMU7800.Core
{
    /// <summary>
    /// Atari standard 32KB bankswitched carts with 128 bytes of RAM
    /// </summary>
    public sealed class CartA32KR : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Bank1: 0x0000:0x1000       0x1000:0x1000  Bank selected by accessing 0x0ff4-0x0ffc
        // Bank2: 0x1000:0x1000
        // Bank3: 0x2000:0x1000
        // Bank4: 0x3000:0x1000
        // Bank5: 0x4000:0x1000
        // Bank6: 0x5000:0x1000
        // Bank7: 0x6000:0x1000
        // Bank8: 0x7000:0x1000
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
            Bank = 7;
        }

        public override byte this[ushort addr]
        {
            get
            {
                addr &= 0x0fff;
                if (addr >= 0x0080 && addr < 0x0100)
                {
                    return RAM[addr & 0x007f];
                }
                UpdateBank(addr);
                return ROM[BankBaseAddr + addr];
            }
            set
            {
                addr &= 0x0fff;
                if (addr < 0x0080)
                {
                    RAM[addr & 0x007f] = value;
                    return;
                }
                UpdateBank(addr);
            }
        }

        #endregion

        private CartA32KR()
        {
        }

        public CartA32KR(byte[] romBytes)
        {
            LoadRom(romBytes, 0x8000);
            RAM = new byte[0x80];
            Bank = 7;
        }

        void UpdateBank(ushort addr)
        {
            if (addr < 0x0ffc && addr >= 0x0ff4 )
            {
                Bank = addr - 0x0ff4;
            }
        }

        #region Serialization Members

        public CartA32KR(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x8000), 0x8000);
            RAM = input.ReadExpectedBytes(0x80);
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