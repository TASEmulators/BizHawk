using System;
using System.Diagnostics;
using System.Text;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.CPUs.ARM
{
	//reference: ARM manual DDI0406B

	public enum AT
	{
		PEEK, POKE, READ, WRITE, FETCH
	}

	//this may need to be rearchitectured to pass the entire instruction, as specified in the docs...
	public interface ICoprocessorSet
	{
		//read
		uint MRC(uint cp, uint opc1, uint t, uint crn, uint crm, uint opc2);
		string Describe(uint cp, uint opc1, uint t, uint crn, uint crm, uint opc2);
	}

	public interface ARM_SYS
	{
		uint svc(uint num);
		string svc_name(uint num);
		ICoprocessorSet coprocessors { get; }
	}

	public interface ARM_BUS
	{
		byte Read08(AT at, uint addr);
		ushort Read16(AT at, uint addr);
		uint Read32(AT at, uint addr);
		void Write08(AT at, uint addr, byte val);
		void Write16(AT at, uint addr, ushort val);
		void Write32(AT at, uint addr, uint val);
		void Write64(AT at, uint addr, ulong val);
	}

	public unsafe partial class ARM
	{
		public ARM_SYS sys;
		public ARM_BUS bus;

		public enum Mode
		{
			USR = 0x10,
			FIQ = 0x11,
			IRQ = 0x12,
			SVC = 0x13,
			ABT = 0x17,
			UND = 0x1B,
			SYS = 0x1F
		}

		public struct Status_Reg
		{
			//public uint val;
			//public uint mode { get { return val & 0x1F; } set { val = (uint)((val & ~0x1F) | (value & 0x1F)); } }
			//public uint T { get { return val & 0x20; } set { val = (uint)((val & ~0x20) | (value & 0x20)); } }
			//public uint F { get { return val & 0x40; } set { val = (uint)((val & ~0x40) | (value & 0x40)); } }
			//public uint I { get { return val & 0x80; } set { val = (uint)((val & ~0x80) | (value & 0x80)); } }
			//public uint N { get { return val & 0x80000000; } set { val = (uint)((val & ~0x80000000) | (value & 0x80000000)); } }

			public Mode mode;
			public Bit T, F, I, Q, V, C, Z, N;
			//todo - consider combining the condition flags together for various speedups
		}

		public uint next_instruct_adr;
		public uint instruct_adr;
		public uint instruction;
		uint thumb_32bit_extra;
		bool thumb_32bit;

		public class Registers
		{
			ARM cpu;
			public Registers(ARM cpu) { this.cpu = cpu; }
			public uint this[int n] { get { return this[(uint)n]; } set { this[(uint)n] = value; } }
			public uint this[uint n]
			{
				//this is a bit different than the docs, in the interest of performance
				get
				{
					if (n == 15)
					{
						uint offset = (cpu._CurrentInstrSet() == EInstrSet.ARM ? 8U : 4U);
						return cpu._R[15] + offset;
					}
					else
					{
						//TODO - junk about security and monitor mode and FIQ and whatnot
						return cpu._R[n];
					}
				}
				set
				{
					Debug.Assert(n >= 0 && n <= 14);
					//TODO - junk about security and monitor mode and FIQ and whatnot
					if (n == 13 && (value & 3) != 0 && cpu._CurrentInstrSet() != EInstrSet.ARM) cpu._FlagUnpredictable();
					cpu._R[n] = value;
				}
			}
		}

		//todo: make R[] indexer which is defined as in manual with an assert when R[15] is written to
		public Registers r;
		uint[] _R = new uint[16];
		public Status_Reg APSR;
		public Status_Reg SPSR;

		public uint SP { get { return r[13]; } set { r[13] = value; } }
		public uint LR { get { return r[14]; } set { r[14] = value; } }
		public uint PC { get { return r[15]; } set { r[15] = value; } }
		
		uint R13_usr, R14_usr;
		uint R13_svc, R14_svc;
		uint R13_abt, R14_abt;
		uint R13_und, R14_und;
		uint R13_irq, R14_irq;
		uint R8_fiq, R9_fiq, R10_fiq, R11_fiq, R12_fiq, R13_fiq, R14_fiq;
		Status_Reg SPSR_svc, SPSR_abt, SPSR_und, SPSR_irq, SPSR_fiq;


		uint FPSCR;

		public ARM(ARM_SYS sys, ARM_BUS bus)
		{
			this.sys = sys;
			this.bus = bus;
			r = new Registers(this);
		}

		//disassembly state
		public bool disassemble;
		public bool nstyle;
		public string disassembly;

		enum Encoding
		{
			T1, T2, T3, T4,
			A1, A2, A3, A4
		}
		bool EncodingT(Encoding e)
		{
			return e == Encoding.T1 || e == Encoding.T2 || e == Encoding.T3 || e == Encoding.T4;
		}
		bool EncodingA(Encoding e)
		{
			return e == Encoding.A1 || e == Encoding.A2 || e == Encoding.A3 || e == Encoding.A4;
		}

		public enum TraceType
		{
			Full, Short
		}
		ulong tracetr = 0;
		public string Trace(TraceType tt)
		{
			disassemble = true;
			Execute();
			StringBuilder sb = new StringBuilder(256);
			sb.AppendFormat("{0}:{1}:{2:X8} ", tracetr, _CurrentInstrSet() == EInstrSet.ARM ? 'A' : 'T', instruct_adr);
			tracetr++;
			if(thumb_32bit)
				sb.AppendFormat("{0:X4}{1:X4} ", instruction,thumb_32bit_extra);
			else
				sb.AppendFormat("{0:X8} ",instruction);
			sb.Append(disassembly.PadRight(30,' '));
			if(tt == TraceType.Full)
				for(int i=0;i<16;i++)
					sb.AppendFormat(" {0:X8}", r[i]);
			disassemble = false;
			return sb.ToString();
		}

		public uint Fetch()
		{
			thumb_32bit = false;
			instruct_adr = next_instruct_adr;
			if (APSR.T)
			{
				//THUMB:
				Debug.Assert((instruct_adr & 1) == 0);
				_R[15] = instruct_adr;
				next_instruct_adr += 2;
				instruction = bus.Read16(AT.FETCH, instruct_adr);
			}
			else
			{
				Debug.Assert((instruct_adr & 3) == 0);
				_R[15] = instruct_adr;
				next_instruct_adr += 4;
				instruction = bus.Read32(AT.FETCH, instruct_adr);
			}

			return 1;
		}

		void ThumbFetchExtra()
		{
			thumb_32bit = true;
			thumb_32bit_extra = bus.Read16(AT.FETCH, next_instruct_adr);
			next_instruct_adr = instruct_adr + 2;
		}

	}

}