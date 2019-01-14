using System;
using System.Text;

namespace SharpCompress.Compressors.PPMd.H
{
    internal class StateRef
    {
        private int _symbol;

        private int _freq;

        private int _successor; // pointer ppmcontext

        internal int Symbol { get => _symbol; set => _symbol = value & 0xff; }

        internal int Freq { get => _freq; set => _freq = value & 0xff; }

        internal State Values
        {
            set
            {
                Freq = value.Freq;
                SetSuccessor(value.GetSuccessor());
                Symbol = value.Symbol;
            }
        }

        public virtual void IncrementFreq(int dFreq)
        {
            _freq = (_freq + dFreq) & 0xff;
        }

        public virtual void DecrementFreq(int dFreq)
        {
            _freq = (_freq - dFreq) & 0xff;
        }

        public virtual int GetSuccessor()
        {
            return _successor;
        }

        public virtual void SetSuccessor(PpmContext successor)
        {
            SetSuccessor(successor.Address);
        }

        public virtual void SetSuccessor(int successor)
        {
            _successor = successor;
        }

        public override String ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("State[");
            buffer.Append("\n  symbol=");
            buffer.Append(Symbol);
            buffer.Append("\n  freq=");
            buffer.Append(Freq);
            buffer.Append("\n  successor=");
            buffer.Append(GetSuccessor());
            buffer.Append("\n]");
            return buffer.ToString();
        }
    }
}