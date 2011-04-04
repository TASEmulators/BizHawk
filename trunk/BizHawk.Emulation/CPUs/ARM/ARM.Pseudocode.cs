using System;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.ARM
{

	/// <summary>
	/// this file contains functions transcribed as closely as possible from arm's docs. theyre all _Prefixed
	/// </summary>
	partial class ARM
	{
		uint _ArchVersion() { return 6; }

		enum EInstrSet
		{
			ARM, THUMB, THUMBEE
		}
		EInstrSet _CurrentInstrSet() { return APSR.T == 1 ? EInstrSet.THUMB : EInstrSet.ARM; }
		void _SelectInstrSet(EInstrSet newset)
		{
			//TODO - copy from manual
			APSR.T = (newset == EInstrSet.THUMB) ? 1U : 0U;
		}

		uint _Align(uint value, int level)
		{
			return (uint)(value & ~(level - 1));
		}

		bool _InITBlock() { return false; }
		bool _LastInITBlock()
		{
			//how the hell are we going to implement this?
			return false;
		}

		int _LowestSetBit(uint val, int size)
		{
			for (int i = 0; i < size; i++)
				if (_.BITN(i, val) == 1) return i;
			return size;
		}

		void _FlagUnpredictable()
		{
			unpredictable = true;
		}

		uint _UNPREDICTABLE()
		{
			_FlagUnpredictable();
			Console.WriteLine("UNPREDICTABLE!");
			return 1;
		}

		bool _CheckVFPEnabled(bool arg)
		{
			return true;
		}

		void _SerializeVFP() { }
		void _VFPExcBarrier() { }

		uint _PERMANENTLY_UNDEFINED()
		{
			Console.WriteLine("PERMANENTLY UNDEFINED! this space will not be allocated in future. (why not?)");
			return 0;
		}

		ulong _UNKNOWN(int bits, ulong data)
		{
			Console.WriteLine("UNKNOWN {0} BITS! (using {0:x16} anyway)", bits, data);
			return data;
		}

		uint _PCStoreValue()
		{
			// This function returns the PC value. On architecture versions before ARMv7, it
			// is permitted to instead return PC+4, provided it does so consistently. It is
			// used only to describe ARM instructions, so it returns the address of the current
			// instruction plus 8 (normally) or 12 (when the alternative is permitted).
			return PC;
		}

		uint MemA_Read32(uint addr)
		{
			addr = _Align(addr, 4);
			return bus.Read32(AT.READ, addr);
		}

		ulong MemA_Read64(uint addr)
		{
			ulong ret = MemA_Read32(addr);
			ret |= (((ulong)MemA_Read32(addr + 4)) << 32);
			return ret;
		}

		void _SetExclusiveMonitors(uint address, int size)
		{
			//TODO!!! boring!!

		}

		bool _ExclusiveMonitorsPass(uint address, int size)
		{
			//TODO!!! boring!!
			return true;
		}

		void MemA_WriteSingle(uint addr, float val)
		{
			MemA_Write32(addr,float_downcast(val));
		}

		float MemA_ReadSingle(uint addr)
		{
			return float_upcast(MemA_Read32(addr));
		}

		double MemA_ReadDouble(uint addr)
		{
			return double_upcast(MemA_Read64(addr));
		}

		void MemA_WriteDouble(uint addr, double val)
		{
			ulong uval = double_downcast(val);
			//TODO endian
			MemA_Write32(addr,(uint)uval);
			MemA_Write32(addr+4,(uint)(uval>>32));
		}

		void MemA_Write32(uint addr, uint val)
		{
			addr = _Align(addr, 4);
			bus.Write32(AT.WRITE, addr, val);
		}

		void MemU_Write08(uint addr, uint val)
		{
			bus.Write08(AT.WRITE, addr, (byte)val);
		}

		byte MemU_Read08(uint addr)
		{
			return bus.Read08(AT.READ, addr);
		}

		ushort MemU_Read16(uint addr)
		{
			return bus.Read08(AT.READ, addr);
		}

		void MemU_Write16(uint addr, uint val)
		{
			bus.Write16(AT.WRITE, addr, (ushort)val);
		}

		void MemU_Write32(uint addr, uint val)
		{
			bus.Write32(AT.WRITE, addr, val);
		}

		uint MemU_Read32(uint addr)
		{
			return bus.Read32(AT.READ, addr);
		}

		uint _MemU(uint addr, uint size)
		{
			//TODO - differentiate from MemA
			switch (size)
			{
				case 4: return bus.Read32(AT.READ, addr);
				case 2: return bus.Read16(AT.READ, addr);
				case 1: return bus.Read08(AT.READ, addr);
				default: throw new ArgumentException();
			}
		}

		uint _MemA(uint addr, uint size)
		{
			//TODO - differentiate from MemU
			switch (size)
			{
				case 4: return bus.Read32(AT.READ, addr);
				case 2: return bus.Read16(AT.READ, addr);
				case 1: return bus.Read08(AT.READ, addr);
				default: throw new ArgumentException();
			}
		}


		void _ALUWritePC(uint address)
		{
			if (_ArchVersion() >= 7 && _CurrentInstrSet() == EInstrSet.ARM)
				_BXWritePC(address);
			else
				_BranchWritePC(address);
		}

		void _BranchWritePC(uint address)
		{
			if (_CurrentInstrSet() == EInstrSet.ARM)
			{
				if (_ArchVersion() < 6 && ((address & 0x3) != 0)) { _UNPREDICTABLE(); }
				_BranchTo((uint)(address & ~3));
			}
			else
				_BranchTo((uint)(address & ~1));
		}

		bool _BigEndian() { return false; }

		void _BranchTo(uint address)
		{
			_R[15] = address;
			next_instruct_adr = address;
		}

		uint _LoadWritePC(uint address)
		{
			if (_ArchVersion() >= 5)
				_BXWritePC(address);
			else
				_BranchWritePC(address);
			return 1;
		}

		void _BXWritePC(uint address)
		{
			if (_CurrentInstrSet() == EInstrSet.THUMBEE)
			{
				throw new InvalidOperationException();
			}
			else
			{
				if (_.BIT0(address) == 1)
				{
					_SelectInstrSet(EInstrSet.THUMB);
					_BranchTo((uint)(address & ~1));
				}
				else if (_.BIT1(address) == 0)
				{
					_SelectInstrSet(EInstrSet.ARM);
					_BranchTo(address);
				}
				else
				{
					_UNPREDICTABLE();
				}
			}
		}

		enum SRType
		{
			LSL = 0, LSR = 1, ASR = 2, ROR = 3, RRX = 4
		}

		uint _ZeroExtend_32(uint val)
		{
			//TODO - any tricky behaviour from doc?
			return val;
		}

		int _SignExtend_32(int bits_present, uint val)
		{
			int temp = (int)val;
			int shift = 32 - bits_present;
			temp <<= (shift);
			return temp >> shift;
		}

		SRType shift_t;
		int shift_n;
		/// <summary>
		/// decodes shifter arguments to shift_t and shift_n
		/// </summary>
		void _DecodeImmShift(uint arg, uint imm5)
		{
			switch (arg)
			{
				case _.b00:
					shift_t = SRType.LSL;
					shift_n = (int)imm5;
					break;
				case _.b01:
					shift_t = SRType.LSR;
					if (imm5 == 0) shift_n = 32;
					else shift_n = (int)imm5;
					break;
				case _.b10:
					shift_t = SRType.LSR;
					if (imm5 == 0) shift_n = 32;
					else shift_n = (int)imm5;
					break;
				case _.b11:
					if (imm5 == 0)
					{
						shift_t = SRType.RRX;
						shift_n = 1;
					}
					else
					{
						shift_t = SRType.ROR;
						shift_n = (int)imm5;
					}
					break;
			}
		}

		bool _UnalignedSupport() { return false; /*TODO*/ }



		bool _NullCheckIfThumbEE(uint num) { return false; }

		bool _IsZeroBit(uint value) { return value == 0; }

		uint _Shift(uint value, SRType type, int amount, Bit carry_in)
		{
			uint result; Bit carry_out;
			_Shift_C(value, type, amount, carry_in, out result, out carry_out);
			return result;
		}

		void _Shift_C(uint value, SRType type, int amount, Bit carry_in, out uint result, out Bit carry_out)
		{
			if (type == SRType.RRX && amount == 1) throw new InvalidOperationException("bogus shift");
			if (amount == 0)
			{
				result = value;
				carry_out = carry_in;
			}
			else
			{
				switch (type)
				{
					case SRType.LSL: _LSL_C(value, amount, out result, out carry_out); break;
					case SRType.LSR: _LSR_C(value, amount, out result, out carry_out); break;
					case SRType.ASR: _ASR_C(value, amount, out result, out carry_out); break;
					case SRType.ROR: _ROR_C(value, amount, out result, out carry_out); break;
					case SRType.RRX: _RRX_C(value, carry_in, out result, out carry_out); break;
					default: throw new ArgumentException();
				}
			}
		}

		void _Assert(bool condition)
		{
#if DEBUG
			System.Diagnostics.Debug.Assert(condition);
#endif
		}

		void _LSL_C(uint x, int shift, out uint result, out Bit carry_out)
		{
			//A2-5
			_Assert(shift > 0);
			ulong extended_x = (ulong)x << shift;
			result = (uint)extended_x;
			carry_out = _.BIT32(extended_x);
		}

		uint _LSL(uint x, int shift)
		{
			//A2-5
			_Assert(shift >= 0);
			if (shift == 0) return x;
			else
			{
				uint result;
				Bit carry_out;
				_LSL_C(x, shift, out result, out carry_out);
				return result;
			}
		}

		void _LSR_C(uint x, int shift, out uint result, out Bit carry_out)
		{
			//A2-6 but coded my own way
			_Assert(shift > 0);
			result = x >> shift;
			carry_out = ((x >> (shift - 1)) & 1);
		}

		uint _LSR(uint x, int shift)
		{
			//A2-6
			_Assert(shift >= 0);
			if (shift == 0) return x;
			else
			{
				uint result;
				Bit carry_out;
				_LSR_C(x, shift, out result, out carry_out);
				return result;
			}
		}

		void _ASR_C(uint x, int shift, out uint result, out Bit carry_out)
		{
			//A2-6 but coded my own way
			_Assert(shift > 0);
			int temp = (int)x;
			temp >>= shift;
			result = (uint)temp;
			carry_out = ((x >> (shift - 1)) & 1);
		}

		uint _ASR(uint x, int shift)
		{
			//A2-6
			_Assert(shift >= 0);
			if (shift == 0) return x;
			else
			{
				uint result;
				Bit carry_out;
				_ASR_C(x, shift, out result, out carry_out);
				return result;
			}
		}

		void _ROR_C(uint x, int shift, out uint result, out Bit carry_out)
		{
			//A2-6 
			_Assert(shift != 0);
			int m = shift & 31;
			result = _LSR(x, m) | _LSL(x, 32 - m);
			carry_out = _.BIT31(result);
		}

		uint _ROR(uint x, int shift)
		{
			//A5-7 //TODO - there is an error in this pseudocode. report it to ARM! (if(n==0) should be if(shift==0)
			if (shift == 0) return x;
			else
			{
				uint result;
				Bit carry_out;
				_ROR_C(x, shift, out result, out carry_out);
				return result;
			}
		}

		void _RRX_C(uint x, Bit carry_in, out uint result, out Bit carry_out)
		{
			//A2-7
			result = ((carry_in) ? (uint)0x80000000 : (uint)0) | (x >> 1);
			carry_out = _.BIT0(x);
		}

		uint _RRX(uint x, Bit carry_in)
		{
			uint result;
			Bit carry_out;
			_RRX_C(x, carry_in, out result, out carry_out);
			return result;
		}

		float _FPAdd(float op1, float op2, bool fpscr_controlled)
		{
			//TODO
			return op1 + op2;
		}

		double _FPAdd(double op1, double op2, bool fpscr_controlled)
		{
			//TODO
			return op1 + op2;
		}

		bool _CheckAdvSIMDOrVFPEnabled(bool x, bool y)
		{
			//TODO
			return true;
		}

		static uint[] __VFPExpandImm32_table;
		static uint __VFPExpandImm32_calc(uint imm8)
		{
			uint a = _.BIT7(imm8);
			uint B = _.BIT6(imm8)^1;
			uint b = _.BIT6(imm8); 
			b |= (b << 1); //replicated to 2 bits
			b |= (b << 2); //replicated to 4 bits
			b |= (b << 1); //replicated to 5 bits
			uint cdefgh = imm8 & 0x3F;

			return (a<<31)|(B<<30)|(b<<29)|(cdefgh<<24);
		}

		static ulong[] __VFPExpandImm64_table;
		static ulong __VFPExpandImm64_calc(uint imm8)
		{
			uint a = _.BIT7(imm8);
			uint B = _.BIT6(imm8) ^ 1;
			uint b = _.BIT6(imm8);
			b |= (b << 1); //replicated to 2 bits
			b |= (b << 2); //replicated to 4 bits
			b |= (b << 4); //replicated to 8 bits
			uint cdefgh = imm8 & 0x3F;

			uint uintret = (a << 31) | (B << 30) | (b << 29) | (cdefgh << 20);
			ulong ulongret = uintret << 32;
			return ulongret;
		}

		uint _VFPExpandImm32(uint imm8)
		{
			if(__VFPExpandImm32_table == null)
			{
				__VFPExpandImm32_table = new uint[256];
				for(uint i=0;i<256;i++)
					__VFPExpandImm32_table[i] = __VFPExpandImm32_calc(i);
			}

			return __VFPExpandImm32_table[imm8];
		}

		ulong _VFPExpandImm64(uint imm8)
		{
			if (__VFPExpandImm64_table == null)
			{
				__VFPExpandImm64_table = new ulong[256];
				for (uint i = 0; i < 256; i++)
					__VFPExpandImm64_table[i] = __VFPExpandImm64_calc(i);
			}

			return __VFPExpandImm64_table[imm8];
		}

		uint _ARMExpandImm(uint imm12)
		{
			//A5-10
			uint imm32;
			Bit trash;
			_ARMExpandImm_C(imm12, 0, out imm32, out trash); //DEVIATION: docs say to pass in APSR.C even though it doesnt matter.
			return imm32;
		}

		void _ARMExpandImm_C(uint imm12, Bit carry_in, out uint imm32, out Bit carry_out)
		{
			//A5-10
			uint unrotated_value = imm12 & 0xFF;
			_Shift_C(unrotated_value, SRType.ROR, 2 * (int)((imm12 >> 8) & 0xF), carry_in, out imm32, out carry_out);
		}

		uint alu_result_32;
		Bit alu_carry_out;
		Bit alu_overflow;
		void _AddWithCarry32(uint x, uint y, Bit carry_in)
		{
			ulong unsigned_sum = (ulong)x + (ulong)y + (uint)carry_in;
			long signed_sum = (long)(int)x + (long)(int)y + (uint)carry_in;
			alu_result_32 = (uint)(unsigned_sum & 0xFFFFFFFF);
			alu_carry_out = (alu_result_32 == unsigned_sum) ? 0 : 1;
			alu_overflow = ((long)(int)alu_result_32 == signed_sum) ? 0 : 1;
		}
	}

}