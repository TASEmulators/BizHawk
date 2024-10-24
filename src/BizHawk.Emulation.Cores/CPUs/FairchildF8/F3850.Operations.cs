using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components.FairchildF8
{
	/// <summary>
	/// ALU Operations
	/// </summary>
	public sealed partial class F3850<TLink>
	{
		public void Read_Func(byte dest, byte src_l, byte src_h)
		{
			Regs[dest] = _link.ReadMemory((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
		}

		public void Write_Func(byte dest_l, byte dest_h, byte src)
		{
			_link.WriteMemory((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), Regs[src]);
		}

		public void IN_Func(byte dest, byte src)
		{
			Regs[dest] = _link.ReadHardware(Regs[src]);
		}

		/// <summary>
		/// Helper method moving from IO pins to accumulator
		/// (complement and flags set)
		/// </summary>
		public void LR_A_IO_Func(byte dest, byte src)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			// data is complemented between I/O pins and accumulator (because PINs are active-low)
			// however for ease we will make them active-high here
			Regs[dest] = Regs[src];

			FlagS = !Regs[dest].Bit(7);
			FlagZ = (Regs[dest] & 0xFF) == 0;
		}

		/// <summary>
		/// Helper method moving from accumulator to IO pins 
		/// (complement)
		/// </summary>
		public void OUT_Func(byte dest, byte src)
		{
			// data is complemented between accumulator and I/O pins (because PINs are active-low)
			// however for ease here we will make them active-high
			_link.WriteHardware(Regs[dest], Regs[src]);
		}

		public void ClearFlags_Func()
		{
			FlagC = false;
			FlagO = false;
			FlagS = false;
			FlagZ = false;
		}

		/// <summary>
		/// Helper function for transferring data between registers
		/// </summary>
		public void LR_Func(byte dest, byte src)
		{
			if (dest == DB)
			{
				// byte storage
				Regs[dest] = (byte)(Regs[src] & 0xFF);
			}
			else if (dest == W)
			{
				// mask for status register
				Regs[dest] = (byte)(Regs[src] & 0x1F);
			}
			else if (dest == ISAR)
			{
				// mask for ISAR register
				Regs[dest] = (byte)(Regs[src] & 0x3F);
			}
			else
			{
				Regs[dest] = Regs[src];
			}
		}

		/// <summary>
		/// Right shift 'src' 'shift' positions (zero fill)
		/// </summary>
		public void SR_Func(byte src, byte shift)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[src] = (byte)((Regs[src] >> shift) & 0xFF);

			FlagS = !Regs[src].Bit(7);
			FlagZ = (Regs[src] & 0xFF) == 0;
		}

		/// <summary>
		/// Left shift 'src' 'shift' positions (zero fill)
		/// </summary>
		public void SL_Func(byte src, byte shift)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[src] = (byte)((Regs[src] << shift) & 0xFF);

			FlagS = !Regs[src].Bit(7);
			FlagZ = (Regs[src] & 0xFF) == 0;
		}

		/// <summary>
		/// Binary addition
		/// Statuses modified: OVF, ZERO, CARRY, SIGN 
		/// Statuses unaffected: ICB
		/// </summary>
		public void ADD_Func(byte dest, byte src, byte src2 = ZERO)
		{
			ushort res = (ushort)(Regs[dest] + Regs[src] + Regs[src2]);
			FlagS = !res.Bit(7);
			FlagC = res.Bit(8);
			FlagZ = (res & 0xFF) == 0;
			FlagO = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != res.Bit(7));
			Regs[dest] = (byte)(res & 0xFF);
		}

		public void SUB_Func(byte dest, byte src)
		{
			Regs[ALU0] = (byte)((Regs[src] ^ 0xff) + 1);
			ADD_Func(dest, ALU0);
		}

		/// <summary>
		/// Decimal Add
		/// http://www.bitsavers.org/components/fairchild/f8/67095664_F8_Guide_To_Programming_1976.pdf - page 40
		/// </summary>
		public void ADDD_Func(byte dest, byte src)
		{
			// The accumulator and the memory location addressed by the DCO registers are assumed to contain two BCD digits.
			// The content of the address memory byte is added to the contents of the accumulator to give a BCD result in the accumulator
			// providing these steps are followed:
			// 
			// Decimal addition is, in reality, three binary events. Consider 8-bit decimal addition.
			// Assume two BCD digit augend XY is added to two BCD digit addend l).N, to give a BCD result PQ:
			//  XY
			// +ZW
			//  --
			// =PQ
			// 
			// Two carries are important: any intermediate carry (IC) out of the low order answer digit (Q), and any overall carry (C) out of the high order digit (P).
			// The three binary steps required to perform BCD addition are as follows:

			// STEP 1: Binary add H'66' to the augend. (this should happen before this function is called)
			// STEP 2: Binary add the addend to the sum from Step 1. Record the status of the carry (C) and intermediate carry (IC). 

			var augend = Regs[dest];
			var addend = Regs[src];
			var working = (byte)(augend + addend);

			bool highCarry;
			bool lowCarry;

			highCarry = ((augend + addend) & 0xFF0) > 0xF0;
			lowCarry = (augend & 0x0F) + (addend & 0x0F) > 0x0F;

			var res = augend + addend;
			FlagC = res.Bit(8);
			FlagO = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != res.Bit(7));
			FlagS = !working.Bit(7);
			FlagZ = (working & 0xFF) == 0;


			// STEP 3: Add a factor to the sum from Step 2, based on the status of C and IC. The factor to be added is given by the following table: 
			// C  IC	Sum to be added
			// ------------------------
			// 0  0		0xAA
			// 0  1		0xA0
			// 1  0		0x0A
			// 1  1		0x00
			// 
			// In Step 3, any carry from the low order digit to the high order digit is suppressed. 

			if (!highCarry && !lowCarry)
			{
				working = (byte)(((working + 0xa0) & 0xf0) + ((working + 0x0a) & 0x0f));
			}
			else if (!highCarry && lowCarry)
			{
				working = (byte)(((working + 0xa0) & 0xf0) + (working & 0x0f));
			}
			else if (highCarry && !lowCarry)
			{
				working = (byte)((working & 0xf0) + ((working + 0x0a) & 0x0f));
			}
			else
			{
				// add nothing (0x00)
			}

			Regs[dest] = (byte)(working & 0xFF);
		}
		
		/// <summary>
		/// Binary add the two's compliment of the accumulator to the value on the databus
		/// Set flags accordingly but accumlator is not touched
		/// </summary>
		public void CI_Func()
		{
			//var twosComp = (byte)((Regs[A] ^ 0xFF) + 1);
			var twosComp = (byte)(~Regs[A]);
			Regs[ALU0] = twosComp;
			Regs[ALU1] = Regs[DB];
			ADD_Func(ALU0, ALU1, ONE);
		}		

		/// <summary>
		/// Logical AND regs[dest] with regs[src] and store the result in regs[dest]
		/// </summary>
		public void AND_Func(byte dest, byte src)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[dest] = (byte)(Regs[dest] & Regs[src]);

			FlagS = !Regs[dest].Bit(7);
			FlagZ = (Regs[dest] & 0xFF) == 0;
		}

		/// <summary>
		/// Logical OR regs[dest] with regs[src] and store the result in regs[dest]
		/// </summary>
		public void OR_Func(byte dest, byte src)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[dest] = (byte)(Regs[dest] | Regs[src]);

			FlagS = !Regs[dest].Bit(7);
			FlagZ = (Regs[dest] & 0xFF) == 0;
		}

		/// <summary>
		/// The destination (regs[dest]) is XORed with (regs[src]).
		/// </summary>
		public void XOR_Func(byte dest, byte src)
		{
			// overflow and carry unconditionally reset
			FlagO = false;
			FlagC = false;

			Regs[dest] = (byte)(Regs[dest] ^ Regs[src]);

			FlagS = !Regs[dest].Bit(7);
			FlagZ = (Regs[dest] & 0xFF) == 0;			
		}
	}
}
