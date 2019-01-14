using System;
using System.Text;
using SharpCompress.Converters;

namespace SharpCompress.Compressors.PPMd.H
{
    internal class State : Pointer
    {
        internal const int SIZE = 6;

        internal State(byte[] memory)
            : base(memory)
        {
        }

        internal int Symbol { get => Memory[Address] & 0xff; set => Memory[Address] = (byte)value; }

        internal int Freq { get => Memory[Address + 1] & 0xff; set => Memory[Address + 1] = (byte)value; }

        internal State Initialize(byte[] mem)
        {
            return base.Initialize<State>(mem);
        }

        internal void IncrementFreq(int dFreq)
        {
            Memory[Address + 1] = (byte)(Memory[Address + 1] + dFreq);
        }

        internal int GetSuccessor()
        {
            return DataConverter.LittleEndian.GetInt32(Memory, Address + 2);
        }

        internal void SetSuccessor(PpmContext successor)
        {
            SetSuccessor(successor.Address);
        }

        internal void SetSuccessor(int successor)
        {
            DataConverter.LittleEndian.PutBytes(Memory, Address + 2, successor);
        }

        internal void SetValues(StateRef state)
        {
            Symbol = state.Symbol;
            Freq = state.Freq;
            SetSuccessor(state.GetSuccessor());
        }

        internal void SetValues(State ptr)
        {
            Array.Copy(ptr.Memory, ptr.Address, Memory, Address, SIZE);
        }

        internal State DecrementAddress()
        {
            Address = Address - SIZE;
            return this;
        }

        internal State IncrementAddress()
        {
            Address = Address + SIZE;
            return this;
        }

        internal static void PpmdSwap(State ptr1, State ptr2)
        {
            byte[] mem1 = ptr1.Memory, mem2 = ptr2.Memory;
            for (int i = 0, pos1 = ptr1.Address, pos2 = ptr2.Address; i < SIZE; i++, pos1++, pos2++)
            {
                byte temp = mem1[pos1];
                mem1[pos1] = mem2[pos2];
                mem2[pos2] = temp;
            }
        }

        public override String ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("State[");
            buffer.Append("\n  Address=");
            buffer.Append(Address);
            buffer.Append("\n  size=");
            buffer.Append(SIZE);
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