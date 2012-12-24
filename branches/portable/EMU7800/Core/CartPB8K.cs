namespace EMU7800.Core
{
    /// <summary>
    /// Parker Brothers 8KB bankswitched carts.
    /// </summary>
    public sealed class CartPB8K : Cart
    {
        //
        // Cart Format                Mapping to ROM Address Space	
        // Segment1: 0x0000:0x0400    Bank1:0x1000:0x0400  Select Segment: 1fe0-1fe7
        // Segment2: 0x0400:0x0400    Bank2:0x1400:0x0400  Select Segment: 1fe8-1ff0
        // Segment3: 0x0800:0x0400    Bank3:0x1800:0x0400  Select Segment: 1ff0-1ff8
        // Segment4: 0x0c00:0x0400    Bank4:0x1c00:0x0400  Always Segment8
        // Segment5: 0x1000:0x0400
        // Segment6: 0x1400:0x0400
        // Segment7: 0x1800:0x0400
        // Segment8: 0x1c00:0x0400
        //
        readonly ushort[] SegmentBase;

        #region IDevice Members

        public override void Reset()
        {
            SegmentBase[0] = ComputeSegmentBase(4);
            SegmentBase[1] = ComputeSegmentBase(5);
            SegmentBase[2] = ComputeSegmentBase(6);
        }

        public override byte this[ushort addr]
        {
            get
            {
                addr &= 0x0fff;
                UpdateSegmentBases(addr);
                return ROM[SegmentBase[addr >> 10] + (addr & 0x03ff)];
            }
            set
            {
                addr &= 0x0fff;
                UpdateSegmentBases(addr);
            }
        }

        #endregion

        private CartPB8K()
        {
        }

        public CartPB8K(byte[] romBytes)
        {
            LoadRom(romBytes, 0x2000);
            SegmentBase = new ushort[4];
            SegmentBase[0] = ComputeSegmentBase(4);
            SegmentBase[1] = ComputeSegmentBase(5);
            SegmentBase[2] = ComputeSegmentBase(6);
            SegmentBase[3] = ComputeSegmentBase(7);
        }

        static ushort ComputeSegmentBase(int slice)
        {
            return (ushort)(slice << 10);  // multiply by 1024
        }

        void UpdateSegmentBases(ushort addr)
        {
            if (addr < 0xfe0 || addr >= 0x0ff8) { }
            else if (addr >= 0x0fe0 && addr < 0x0fe8)
            {
                SegmentBase[0] = ComputeSegmentBase(addr & 0x07);
            }
            else if (addr >= 0x0fe8 && addr < 0x0ff0)
            {
                SegmentBase[1] = ComputeSegmentBase(addr & 0x07);
            }
            else if (addr >= 0x0ff0 && addr < 0x0ff8)
            {
                SegmentBase[2] = ComputeSegmentBase(addr & 0x07);
            }
        }

        #region Serialization Members

        public CartPB8K(DeserializationContext input, MachineBase m) : base(input)
        {
            input.CheckVersion(1);
            LoadRom(input.ReadExpectedBytes(0x2000), 0x2000);
            SegmentBase = input.ReadUnsignedShorts();
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);

            output.WriteVersion(1);
            output.Write(ROM);
            output.Write(SegmentBase);
        }

        #endregion
    }
}