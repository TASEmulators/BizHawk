namespace SharpCompress.Compressors.Rar.VM
{
    internal class BitInput
    {
        /// <summary> the max size of the input</summary>
        internal const int MAX_SIZE = 0x8000;

        public int inAddr;
        public int inBit;

// TODO: rename var
        public int InAddr { get { return inAddr; } set { inAddr = value; }  }
        public int InBit { get { return inBit; } set { inBit = value; }  }
        public bool ExternalBuffer;


        /// <summary>  </summary>
        internal BitInput()
        {
            InBuf = new byte[MAX_SIZE];
        }

        internal byte[] InBuf { get; }

        internal void InitBitInput()
        {
            inAddr = 0;
            inBit = 0;
        }

        internal void faddbits(uint bits) {
            // TODO uint
            AddBits((int)bits);
        }

        /// <summary>
        /// also named faddbits
        /// </summary>
        /// <param name="bits"></param>
        internal void AddBits(int bits)
        {
            bits += inBit;
            inAddr += (bits >> 3);
            inBit = bits & 7;
        }

        internal uint fgetbits() {
            // TODO uint
            return (uint)GetBits();
        }

        internal uint getbits() {
            // TODO uint
            return (uint)GetBits();
        }

        /// <summary> 
        /// (also named fgetbits)
        /// </summary>
        /// <returns>
        /// the bits (unsigned short) 
        /// </returns>
        internal int GetBits()
        {
            //      int BitField=0;
            //      BitField|=(int)(inBuf[inAddr] << 16)&0xFF0000;
            //      BitField|=(int)(inBuf[inAddr+1] << 8)&0xff00;
            //      BitField|=(int)(inBuf[inAddr+2])&0xFF;
            //      BitField >>>= (8-inBit);
            //      return (BitField & 0xffff);
            return ((Utility.URShift((((InBuf[inAddr] & 0xff) << 16)
                                      + ((InBuf[inAddr + 1] & 0xff) << 8)
                                      + ((InBuf[inAddr + 2] & 0xff))), (8 - inBit))) & 0xffff);
        }

        /// <summary> Indicates an Overfow</summary>
        /// <param name="IncPtr">how many bytes to inc
        /// </param>
        /// <returns> true if an Oververflow would occur
        /// </returns>
        internal bool Overflow(int IncPtr)
        {
            return (inAddr + IncPtr >= MAX_SIZE);
        }
    }
}