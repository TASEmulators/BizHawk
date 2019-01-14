using SharpCompress.Converters;

namespace SharpCompress.Compressors.PPMd.H
{
    internal class RarMemBlock : Pointer
    {
        public const int SIZE = 12;

        private int _stamp, _nu;

        private int _next, _prev; // Pointer RarMemBlock

        public RarMemBlock(byte[] memory)
            : base(memory)
        {
        }

        internal int Stamp
        {
            get
            {
                if (Memory != null)
                {
                    _stamp = DataConverter.LittleEndian.GetInt16(Memory, Address) & 0xffff;
                }
                return _stamp;
            }

            set
            {
                _stamp = value;
                if (Memory != null)
                {
                    DataConverter.LittleEndian.PutBytes(Memory, Address, (short)value);
                }
            }
        }

        internal void InsertAt(RarMemBlock p)
        {
            RarMemBlock temp = new RarMemBlock(Memory);
            SetPrev(p.Address);
            temp.Address = GetPrev();
            SetNext(temp.GetNext()); // prev.getNext();
            temp.SetNext(this); // prev.setNext(this);
            temp.Address = GetNext();
            temp.SetPrev(this); // next.setPrev(this);
        }

        internal void Remove()
        {
            RarMemBlock temp = new RarMemBlock(Memory);
            temp.Address = GetPrev();
            temp.SetNext(GetNext()); // prev.setNext(next);
            temp.Address = GetNext();
            temp.SetPrev(GetPrev()); // next.setPrev(prev);

            //		next = -1;
            //		prev = -1;
        }

        internal int GetNext()
        {
            if (Memory != null)
            {
                _next = DataConverter.LittleEndian.GetInt32(Memory, Address + 4);
            }
            return _next;
        }

        internal void SetNext(RarMemBlock next)
        {
            SetNext(next.Address);
        }

        internal void SetNext(int next)
        {
            _next = next;
            if (Memory != null)
            {
                DataConverter.LittleEndian.PutBytes(Memory, Address + 4, next);
            }
        }

        internal int GetNu()
        {
            if (Memory != null)
            {
                _nu = DataConverter.LittleEndian.GetInt16(Memory, Address + 2) & 0xffff;
            }
            return _nu;
        }

        internal void SetNu(int nu)
        {
            _nu = nu & 0xffff;
            if (Memory != null)
            {
                DataConverter.LittleEndian.PutBytes(Memory, Address + 2, (short)nu);
            }
        }

        internal int GetPrev()
        {
            if (Memory != null)
            {
                _prev = DataConverter.LittleEndian.GetInt32(Memory, Address + 8);
            }
            return _prev;
        }

        internal void SetPrev(RarMemBlock prev)
        {
            SetPrev(prev.Address);
        }

        internal void SetPrev(int prev)
        {
            _prev = prev;
            if (Memory != null)
            {
                DataConverter.LittleEndian.PutBytes(Memory, Address + 8, prev);
            }
        }
    }
}