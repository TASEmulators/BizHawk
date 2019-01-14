using System;
using System.Text;
using SharpCompress.Converters;

namespace SharpCompress.Compressors.PPMd.H
{
    internal class FreqData : Pointer
    {
        internal const int SIZE = 6;

        //    struct FreqData
        //    {
        //        ushort SummFreq;
        //        STATE _PACK_ATTR * Stats;
        //    };

        internal FreqData(byte[] memory)
            : base(memory)
        {
        }

        internal int SummFreq { get => DataConverter.LittleEndian.GetInt16(Memory, Address) & 0xffff; set => DataConverter.LittleEndian.PutBytes(Memory, Address, (short)value); }

        internal FreqData Initialize(byte[] mem)
        {
            return base.Initialize<FreqData>(mem);
        }

        internal void IncrementSummFreq(int dSummFreq)
        {
            short summFreq = DataConverter.LittleEndian.GetInt16(Memory, Address);
            summFreq += (short)dSummFreq;
            DataConverter.LittleEndian.PutBytes(Memory, Address, summFreq);
        }

        internal int GetStats()
        {
            return DataConverter.LittleEndian.GetInt32(Memory, Address + 2);
        }

        internal virtual void SetStats(State state)
        {
            SetStats(state.Address);
        }

        internal void SetStats(int state)
        {
            DataConverter.LittleEndian.PutBytes(Memory, Address + 2, state);
        }

        public override String ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("FreqData[");
            buffer.Append("\n  Address=");
            buffer.Append(Address);
            buffer.Append("\n  size=");
            buffer.Append(SIZE);
            buffer.Append("\n  summFreq=");
            buffer.Append(SummFreq);
            buffer.Append("\n  stats=");
            buffer.Append(GetStats());
            buffer.Append("\n]");
            return buffer.ToString();
        }
    }
}