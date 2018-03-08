using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZXSpectrum
    {
        /*
         *  CPU Helper Methods
         */

        public ushort RegPC
        {
            get { return (ushort)((_cpu.Regs[0] << 8 | _cpu.Regs[1])); }
            set
            {
                _cpu.Regs[1] = (ushort)(value & 0xFF);
                _cpu.Regs[0] = (ushort)((value >> 8) & 0xFF);
            }
        }

        public ushort RegIX
        {
            get { return (ushort)((_cpu.Regs[15] << 8 | _cpu.Regs[16] )); }
            set
            {
                _cpu.Regs[16] = (ushort)(value & 0xFF);
                _cpu.Regs[15] = (ushort)((value >> 8) & 0xFF);
            }
        }

        public ushort RegDE
        {
            get { return (ushort)((_cpu.Regs[8] << 8 | _cpu.Regs[9] )); }
            set
            {
                _cpu.Regs[9] = (ushort)(value & 0xFF);
                _cpu.Regs[8] = (ushort)((value >> 8) & 0xFF);
            }
        }

        public ushort RegAF
        {
            get { return (ushort)((_cpu.Regs[4] << 8 | _cpu.Regs[5])); }
            set
            {
                _cpu.Regs[5] = (ushort)(value & 0xFF);
                _cpu.Regs[4] = (ushort)((value >> 8) & 0xFF);
            }
        }


        /// <summary>
        /// Gets the IX word value
        /// </summary>
        /// <returns></returns>
        public ushort Get16BitIX()
        {
            return Convert.ToUInt16(_cpu.Regs[_cpu.Ixh] | _cpu.Regs[_cpu.Ixl] << 8);
        }

        /// <summary>
        /// Set the IX word value
        /// </summary>
        /// <param name="Ixh"></param>
        /// <param name="Ixl"></param>
        public void Set16BitIX(ushort IX)
        {
            _cpu.Regs[_cpu.Ixh] = (ushort)(IX & 0xFF);
            _cpu.Regs[_cpu.Ixl] = (ushort)((IX >> 8) & 0xff);
        }

        /// <summary>
        /// Gets the AF word value
        /// </summary>
        /// <returns></returns>
        public ushort Get16BitAF()
        {
            return Convert.ToUInt16(_cpu.Regs[_cpu.A] | _cpu.Regs[_cpu.F] << 8);
        }

        /// <summary>
        /// Set the AF word value
        /// </summary>
        /// <param name="Ixh"></param>
        /// <param name="Ixl"></param>
        public void Set16BitAF(ushort AF)
        {
            _cpu.Regs[_cpu.A] = (ushort)(AF & 0xFF);
            _cpu.Regs[_cpu.F] = (ushort)((AF >> 8) & 0xff);
        }

        /// <summary>
        /// Gets the AF shadow word value
        /// </summary>
        /// <returns></returns>
        public ushort Get16BitAF_()
        {
            return Convert.ToUInt16(_cpu.Regs[_cpu.A_s] | _cpu.Regs[_cpu.F_s] << 8);
        }

        /// <summary>
        /// Set the AF shadow word value
        /// </summary>
        /// <param name="Ixh"></param>
        /// <param name="Ixl"></param>
        public void Set16BitAF_(ushort AF_)
        {
            _cpu.Regs[_cpu.A_s] = (ushort)(AF_ & 0xFF);
            _cpu.Regs[_cpu.F_s] = (ushort)((AF_ >> 8) & 0xff);
        }

        /// <summary>
        /// Gets the DE word value
        /// </summary>
        /// <returns></returns>
        public ushort Get16BitDE()
        {
            return Convert.ToUInt16(_cpu.Regs[_cpu.E] | _cpu.Regs[_cpu.D] << 8);
        }

        /// <summary>
        /// Set the DE word value
        /// </summary>
        /// <param name="Ixh"></param>
        /// <param name="Ixl"></param>
        public void Set16BitDE(ushort DE)
        {
            _cpu.Regs[_cpu.D] = (ushort)(DE & 0xFF);
            _cpu.Regs[_cpu.E] = (ushort)((DE >> 8) & 0xff);
        }


        /// <summary>
        /// Z80 Status Indicator Flag Reset masks
        /// </summary>
        /// <seealso cref="FlagsSetMask"/>
        [Flags]
        public enum FlagsResetMask : byte
        {
            /// <summary>Sign Flag</summary>
            S = 0x7F,

            /// <summary>Zero Flag</summary>
            Z = 0xBF,

            /// <summary>This flag is not used.</summary>
            R5 = 0xDF,

            /// <summary>Half Carry Flag</summary>
            H = 0xEF,

            /// <summary>This flag is not used.</summary>
            R3 = 0xF7,

            /// <summary>Parity/Overflow Flag</summary>
            PV = 0xFB,

            /// <summary>Add/Subtract Flag</summary>
            N = 0xFD,

            /// <summary>Carry Flag</summary>
            C = 0xFE,
        }

        /// <summary>
        /// Z80 Status Indicator Flag Set masks
        /// </summary>
        /// <seealso cref="FlagsResetMask"/>
        [Flags]
        public enum FlagsSetMask : byte
        {
            /// <summary>Sign Flag</summary>
            /// <remarks>
            /// The Sign Flag (S) stores the state of the most-significant bit of
            /// the Accumulator (bit 7). When the Z80 CPU performs arithmetic 
            /// operations on signed numbers, the binary twos complement notation 
            /// is used to represent and process numeric information.
            /// </remarks>
            S = 0x80,

            /// <summary>
            /// Zero Flag
            /// </summary>
            /// <remarks>
            /// The Zero Flag is set (1) or cleared (0) if the result generated by 
            /// the execution of certain instructions is 0. For 8-bit arithmetic and 
            /// logical operations, the Z flag is set to a 1 if the resulting byte in 
            /// the Accumulator is 0. If the byte is not 0, the Z flag is reset to 0.
            /// </remarks>
            Z = 0x40,

            /// <summary>This flag is not used.</summary>
            R5 = 0x20,

            /// <summary>Half Carry Flag</summary>
            /// <remarks>
            /// The Half Carry Flag (H) is set (1) or cleared (0) depending on the 
            /// Carry and Borrow status between bits 3 and 4 of an 8-bit arithmetic 
            /// operation. This flag is used by the Decimal Adjust Accumulator (DAA) 
            /// instruction to correct the result of a packed BCD add or subtract operation.
            /// </remarks>
            H = 0x10,

            /// <summary>This flag is not used.</summary>
            R3 = 0x08,

            /// <summary>Parity/Overflow Flag</summary>
            /// <remarks>
            /// The Parity/Overflow (P/V) Flag is set to a specific state depending on 
            /// the operation being performed. For arithmetic operations, this flag 
            /// indicates an overflow condition when the result in the Accumulator is 
            /// greater than the maximum possible number (+127) or is less than the 
            /// minimum possible number (–128). This overflow condition is determined by 
            /// examining the sign bits of the operands.
            /// </remarks>
            PV = 0x04,

            /// <summary>Add/Subtract Flag</summary>
            /// <remarks>
            /// The Add/Subtract Flag (N) is used by the Decimal Adjust Accumulator 
            /// instruction (DAA) to distinguish between the ADD and SUB instructions.
            /// For ADD instructions, N is cleared to 0. For SUB instructions, N is set to 1.
            /// </remarks>
            N = 0x02,

            /// <summary>Carry Flag</summary>
            /// <remarks>
            /// The Carry Flag (C) is set or cleared depending on the operation being performed.
            /// </remarks>
            C = 0x01,

            /// <summary>
            /// Combination of S, Z, and PV
            /// </summary>
            SZPV = S | Z | PV,

            /// <summary>
            /// Combination of N, and H
            /// </summary>
            NH = N | H,

            /// <summary>
            /// Combination of R3, and R5
            /// </summary>
            R3R5 = R3 | R5
        }

        /// <summary>
        /// Helper method that returns a single INT32 from a BitArray
        /// </summary>
        /// <param name="bitarray"></param>
        /// <returns></returns>
        public static int GetIntFromBitArray(BitArray bitArray)
        {
            if (bitArray.Length > 32)
                throw new ArgumentException("Argument length shall be at most 32 bits.");

            int[] array = new int[1];
            bitArray.CopyTo(array, 0);
            return array[0];
        }
    }
}
