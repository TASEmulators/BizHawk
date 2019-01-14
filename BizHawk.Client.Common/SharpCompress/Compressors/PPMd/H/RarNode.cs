using System.Text;
using SharpCompress.Converters;

namespace SharpCompress.Compressors.PPMd.H
{
    internal class RarNode : Pointer
    {
        private int _next; //rarnode pointer

        public const int SIZE = 4;

        public RarNode(byte[] memory)
            : base(memory)
        {
        }

        internal int GetNext()
        {
            if (Memory != null)
            {
                _next = DataConverter.LittleEndian.GetInt32(Memory, Address);
            }
            return _next;
        }

        internal void SetNext(RarNode next)
        {
            SetNext(next.Address);
        }

        internal void SetNext(int next)
        {
            _next = next;
            if (Memory != null)
            {
                DataConverter.LittleEndian.PutBytes(Memory, Address, next);
            }
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("State[");
            buffer.Append("\n  Address=");
            buffer.Append(Address);
            buffer.Append("\n  size=");
            buffer.Append(SIZE);
            buffer.Append("\n  next=");
            buffer.Append(GetNext());
            buffer.Append("\n]");
            return buffer.ToString();
        }
    }
}