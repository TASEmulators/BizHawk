namespace EMU7800.Core
{
    /// <summary>
    /// M-Network 16KB bankswitched carts with 2KB RAM.
    /// </summary>
    public sealed class CartMN16K : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space	
        // Segment1: 0x0000:0x0800    Bank1:0x1000:0x0800  Select Seg: 1fe0-1fe6, 1fe7=RAM Seg1
        // Segment2: 0x0800:0x0800    Bank2:0x1800:0x0800  Always Seg8
        // Segment3: 0x1000:0x0800
        // Segment4: 0x1800:0x0800
        // Segment5: 0x2000:0x0800
        // Segment6: 0x2800:0x0800
        // Segment7: 0x3000:0x0800
        // Segment8: 0x3800:0x0800
        //
        // RAM                        RAM Segment1 when 1fe7 select is accessed
        // Segment1: 0x0000:0x0400    0x1000-0x13FF write port
        // Segment2: 0x0400:0x0400    0x1400-0x17FF read port
        //
        //                            RAM Segment2: 1ff8-1ffb selects 256-byte block
        //                            0x1800-0x18ff write port
        //                            0x1900-0x19ff read port
        //
        ushort BankBaseAddr, BankBaseRAMAddr;
        bool RAMBankOn;
        readonly byte[] RAM;

        int Bank
        {
            set
            {
                BankBaseAddr = (ushort)(value << 11);  // multiply by 2048
                RAMBankOn = (value == 0x07);
            }
        }

        int BankRAM
        {
            set { BankBaseRAMAddr = (ushort) (value << 8); } // multiply by 256
        }

        #region IDevice Members

        public override void Reset()
        {
            Bank = 0;
            BankRAM = 0;
        }
    
        public override byte this[ushort addr]
        {
            get
            {
                addr &= 0x0fff;
                UpdateBanks(addr);
                if (RAMBankOn && addr >= 0x0400 && addr < 0x0800)
                {
                    return RAM[addr & 0x03ff];
                }
                if (addr >= 0x0900 && addr < 0x0a00)
                {
                    return RAM[0x400 + BankBaseRAMAddr + (addr & 0xff)];
                }
                return addr < 0x0800 ? ROM[BankBaseAddr + (addr & 0x07ff)] : ROM[0x3800 + (addr & 0x07ff)];
            }
            set
            {
                addr &= 0x0fff;
                UpdateBanks(addr);
                if (RAMBankOn && addr < 0x0400)
                {
                    RAM[addr & 0x03ff] = value;
                } 
                else if (addr >= 0x0800 && addr < 0x0900)
                {
                    RAM[0x400 + BankBaseRAMAddr + (addr & 0xff)] = value;
                }
            }
        }

        #endregion

        private CartMN16K()
        {
        }

        public CartMN16K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x4000);
            RAM = new byte[0x800];
            Bank = 0;
            BankRAM = 0;
        }

        void UpdateBanks(ushort addr)
        {
            if (addr >= 0x0fe0 && addr < 0x0fe8)
            {
                Bank = addr & 0x07;
            } 
            else if (addr >= 0x0fe8 && addr < 0x0fec)
            {
                BankRAM = addr & 0x03;
            }
        }

        #region Serialization Members

        public CartMN16K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x4000), 0x4000);
            RAM = input.ReadExpectedBytes(0x800);
            BankBaseAddr = input.ReadUInt16();
            BankBaseRAMAddr = input.ReadUInt16();
            RAMBankOn = input.ReadBoolean();
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(RAM);
            output.Write(BankBaseAddr);
            output.Write(BankBaseRAMAddr);
            output.Write(RAMBankOn);
        }

        #endregion
    }
}