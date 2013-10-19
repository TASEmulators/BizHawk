namespace EMU7800.Core
{
    /// <summary>
    /// Tigervision 8KB bankswitched carts
    /// </summary>
    public sealed class CartTV8K : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space
        // Segment1: 0x0000:0x0800    0x1000:0x0800  Selected segment via $003F
        // Segment2: 0x0800:0x0800    0x1800:0x0800  Always last segment
        // Segment3: 0x1000:0x0800
        // Segment4: 0x1800:0x0800
        //
        ushort BankBaseAddr;
        readonly ushort LastBankBaseAddr;

        byte Bank
        {
            set
            {
                BankBaseAddr = (ushort)(0x0800 * value);
                BankBaseAddr %= (ushort)ROM.Length;
            }
        }

        protected internal override bool RequestSnooping
        {
            get { return true; }
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
                return addr < 0x0800 ? ROM[BankBaseAddr + (addr & 0x07ff)] : ROM[LastBankBaseAddr + (addr & 0x07ff)];
            }
            set
            {
                if (addr <= 0x003f)
                {
                    Bank = value;
                }
            }
        }

        #endregion

        private CartTV8K()
        {
        }

        public CartTV8K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x1000);
            Bank = 0;
            LastBankBaseAddr = (ushort)(ROM.Length - 0x0800);
        }

        #region Serialization Members

        public CartTV8K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x1000), 0x1000);
            BankBaseAddr = input.ReadUInt16();
            LastBankBaseAddr = input.ReadUInt16();
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(BankBaseAddr);
            output.Write(LastBankBaseAddr);
        }

        #endregion
    }
}