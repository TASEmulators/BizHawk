/** GBDASM: GameBoy Disassembler ******************************/
/**                                                          **/
/**                         gbdasm.c                         **/
/**                                                          **/
/** This file contains the source of a portable disassembler **/
/** for the customized Z80 CPU used in GameBoy handheld      **/
/** videogame console.                                       **/
/**                                                          **/
/** Copyright (C) Marat Fayzullin 1995-2001                  **/
/**     You are not allowed to distribute this software      **/
/**     commercially. Please, notify me, if you make any     **/
/**     changes to this file.                                **/
/**************************************************************/

using System;
using System.Text;

namespace BizHawk.Emulation.CPUs.Z80GB
{
	public static class Disassembler
	{
		readonly static string[] mnemonics = new string[]
		{
		  "NOP","LD BC,#h","LD (BC),A","INC BC","INC B","DEC B","LD B,*h","RLCA",
		  "LD (#h),SP","ADD HL,BC","LD A,(BC)","DEC BC","INC C","DEC C","LD C,*h","RRCA",
		  "STOP","LD DE,#h","LD (DE),A","INC DE","INC D","DEC D","LD D,*h","RLA",
		  "JR @h","ADD HL,DE","LD A,(DE)","DEC DE","INC E","DEC E","LD E,*h","RRA",
		  "JR NZ,@h","LD HL,#h","LD (HL+),A","INC HL","INC H","DEC H","LD H,*h","DAA",
		  "JR Z,@h","ADD HL,HL","LD A,(HL+)","DEC HL","INC L","DEC L","LD L,*h","CPL",
		  "JR NC,@h","LD SP,#h","LD (HL-),A","INC SP","INC (HL)","DEC (HL)","LD (HL),*h","SCF",
		  "JR C,@h","ADD HL,SP","LD A,(HL-)","DEC SP","INC A","DEC A","LD A,*h","CCF",
		  "LD B,B","LD B,C","LD B,D","LD B,E","LD B,H","LD B,L","LD B,(HL)","LD B,A",
		  "LD C,B","LD C,C","LD C,D","LD C,E","LD C,H","LD C,L","LD C,(HL)","LD C,A",
		  "LD D,B","LD D,C","LD D,D","LD D,E","LD D,H","LD D,L","LD D,(HL)","LD D,A",
		  "LD E,B","LD E,C","LD E,D","LD E,E","LD E,H","LD E,L","LD E,(HL)","LD E,A",
		  "LD H,B","LD H,C","LD H,D","LD H,E","LD H,H","LD H,L","LD H,(HL)","LD H,A",
		  "LD L,B","LD L,C","LD L,D","LD L,E","LD L,H","LD L,L","LD L,(HL)","LD L,A",
		  "LD (HL),B","LD (HL),C","LD (HL),D","LD (HL),E","LD (HL),H","LD (HL),L","HALT","LD (HL),A",
		  "LD A,B","LD A,C","LD A,D","LD A,E","LD A,H","LD A,L","LD A,(HL)","LD A,A",
		  "ADD B","ADD C","ADD D","ADD E","ADD H","ADD L","ADD (HL)","ADD A",
		  "ADC B","ADC C","ADC D","ADC E","ADC H","ADC L","ADC (HL)","ADC A",
		  "SUB B","SUB C","SUB D","SUB E","SUB H","SUB L","SUB (HL)","SUB A",
		  "SBC B","SBC C","SBC D","SBC E","SBC H","SBC L","SBC (HL)","SBC A",
		  "AND B","AND C","AND D","AND E","AND H","AND L","AND (HL)","AND A",
		  "XOR B","XOR C","XOR D","XOR E","XOR H","XOR L","XOR (HL)","XOR A",
		  "OR B","OR C","OR D","OR E","OR H","OR L","OR (HL)","OR A",
		  "CP B","CP C","CP D","CP E","CP H","CP L","CP (HL)","CP A",
		  "RET NZ","POP BC","JP NZ,#h","JP #h","CALL NZ,#h","PUSH BC","ADD *h","RST 00h",
		  "RET Z","RET","JP Z,#h","PREFIX CBh","CALL Z,#h","CALL #h","ADC *h","RST 08h",
		  "RET NC","POP DE","JP NC,#h","DB D3h","CALL NC,#h","PUSH DE","SUB *h","RST 10h",
		  "RET C","RETI","JP C,#h","DB DBh","CALL C,#h","DB DDh","SBC *h","RST 18h",
		  "LD (FF*h),A","POP HL","LD (FF00h+C),A","DB E3h","DB E4h","PUSH HL","AND *h","RST 20h",
		  "ADD SP,@h","LD PC,HL","LD (#h),A","DB EBh","DB ECh","PREFIX EDh","XOR *h","RST 28h",
		  "LD A,(FF*h)","POP AF","LD A,(FF00h+C)","DI","DB F4h","PUSH AF","OR *h","RST 30h",
		  "LDHL SP,@h","LD SP,HL","LD A,(#h)","EI","DB FCh","DB FDh","CP *h","RST 38h"
		};

		readonly static string[] mnemonicsCB = new string[]
		{
		  "RLC B","RLC C","RLC D","RLC E","RLC H","RLC L","RLC (HL)","RLC A",
		  "RRC B","RRC C","RRC D","RRC E","RRC H","RRC L","RRC (HL)","RRC A",
		  "RL B","RL C","RL D","RL E","RL H","RL L","RL (HL)","RL A",
		  "RR B","RR C","RR D","RR E","RR H","RR L","RR (HL)","RR A",
		  "SLA B","SLA C","SLA D","SLA E","SLA H","SLA L","SLA (HL)","SLA A",
		  "SRA B","SRA C","SRA D","SRA E","SRA H","SRA L","SRA (HL)","SRA A",
		  "SWAP B","SWAP C","SWAP D","SWAP E","SWAP H","SWAP L","SWAP (HL)","SWAP A",
		  "SRL B","SRL C","SRL D","SRL E","SRL H","SRL L","SRL (HL)","SRL A",
		  "BIT 0,B","BIT 0,C","BIT 0,D","BIT 0,E","BIT 0,H","BIT 0,L","BIT 0,(HL)","BIT 0,A",
		  "BIT 1,B","BIT 1,C","BIT 1,D","BIT 1,E","BIT 1,H","BIT 1,L","BIT 1,(HL)","BIT 1,A",
		  "BIT 2,B","BIT 2,C","BIT 2,D","BIT 2,E","BIT 2,H","BIT 2,L","BIT 2,(HL)","BIT 2,A",
		  "BIT 3,B","BIT 3,C","BIT 3,D","BIT 3,E","BIT 3,H","BIT 3,L","BIT 3,(HL)","BIT 3,A",
		  "BIT 4,B","BIT 4,C","BIT 4,D","BIT 4,E","BIT 4,H","BIT 4,L","BIT 4,(HL)","BIT 4,A",
		  "BIT 5,B","BIT 5,C","BIT 5,D","BIT 5,E","BIT 5,H","BIT 5,L","BIT 5,(HL)","BIT 5,A",
		  "BIT 6,B","BIT 6,C","BIT 6,D","BIT 6,E","BIT 6,H","BIT 6,L","BIT 6,(HL)","BIT 6,A",
		  "BIT 7,B","BIT 7,C","BIT 7,D","BIT 7,E","BIT 7,H","BIT 7,L","BIT 7,(HL)","BIT 7,A",
		  "RES 0,B","RES 0,C","RES 0,D","RES 0,E","RES 0,H","RES 0,L","RES 0,(HL)","RES 0,A",
		  "RES 1,B","RES 1,C","RES 1,D","RES 1,E","RES 1,H","RES 1,L","RES 1,(HL)","RES 1,A",
		  "RES 2,B","RES 2,C","RES 2,D","RES 2,E","RES 2,H","RES 2,L","RES 2,(HL)","RES 2,A",
		  "RES 3,B","RES 3,C","RES 3,D","RES 3,E","RES 3,H","RES 3,L","RES 3,(HL)","RES 3,A",
		  "RES 4,B","RES 4,C","RES 4,D","RES 4,E","RES 4,H","RES 4,L","RES 4,(HL)","RES 4,A",
		  "RES 5,B","RES 5,C","RES 5,D","RES 5,E","RES 5,H","RES 5,L","RES 5,(HL)","RES 5,A",
		  "RES 6,B","RES 6,C","RES 6,D","RES 6,E","RES 6,H","RES 6,L","RES 6,(HL)","RES 6,A",
		  "RES 7,B","RES 7,C","RES 7,D","RES 7,E","RES 7,H","RES 7,L","RES 7,(HL)","RES 7,A",
		  "SET 0,B","SET 0,C","SET 0,D","SET 0,E","SET 0,H","SET 0,L","SET 0,(HL)","SET 0,A",
		  "SET 1,B","SET 1,C","SET 1,D","SET 1,E","SET 1,H","SET 1,L","SET 1,(HL)","SET 1,A",
		  "SET 2,B","SET 2,C","SET 2,D","SET 2,E","SET 2,H","SET 2,L","SET 2,(HL)","SET 2,A",
		  "SET 3,B","SET 3,C","SET 3,D","SET 3,E","SET 3,H","SET 3,L","SET 3,(HL)","SET 3,A",
		  "SET 4,B","SET 4,C","SET 4,D","SET 4,E","SET 4,H","SET 4,L","SET 4,(HL)","SET 4,A",
		  "SET 5,B","SET 5,C","SET 5,D","SET 5,E","SET 5,H","SET 5,L","SET 5,(HL)","SET 5,A",
		  "SET 6,B","SET 6,C","SET 6,D","SET 6,E","SET 6,H","SET 6,L","SET 6,(HL)","SET 6,A",
		  "SET 7,B","SET 7,C","SET 7,D","SET 7,E","SET 7,H","SET 7,L","SET 7,(HL)","SET 7,A"
		};

		class OpcodeReader
		{
			public OpcodeReader(ushort addr, Func<ushort, byte> reader)
			{
				this.startAddr = addr;
				this.addr = addr;
				this.reader = reader;
			}
			ushort addr, startAddr;

			Func<ushort, byte> reader;
			int[] bytes = new int[] {-1,-1,-1};
			int bytectr = 0;
			public int Size { get { return bytectr; } }
			public byte Read()
			{
				byte val = reader(addr++); ;
				bytes[bytectr] = val;
				bytectr++;
				return val;
			}

			public string Format(string mnemonic)
			{
				StringBuilder sb = new StringBuilder(32);
				sb.AppendFormat("{0:X4}:", startAddr);
				for (int i = 0; i < 3; i++)
				{
					if (bytectr > i)
						sb.AppendFormat("{0:X2} ", bytes[i]);
					else sb.AppendFormat("   ");
				}
				sb.AppendFormat(" {0}",mnemonic);
				return sb.ToString();
			}
		}

		public static string DAsm(ushort addr, Func<ushort,byte> reader, out ushort size)
		{
			string ret;
			OpcodeReader or = new OpcodeReader(addr, reader);

			byte A = or.Read();
			string str;
			switch (A)
			{
				case 0xCB:
					str = mnemonicsCB[or.Read()];
					break;
				default:
					str = mnemonics[A];
					break;
			}

			if (str.IndexOf('*') != -1)
			{
				ret = str.Replace("*", string.Format("{0:X2}", or.Read()));
			}
			else if (str.IndexOf('@') != -1)
			{
				byte B = or.Read();
				bool neg = ((B & 0x80) != 0);
				char sign = neg ? '-' : '+';
				int val = neg ? 256 - B : B;
				ret = str.Replace("@", string.Format("{0}{1:X2}", sign, val));
			}
			else if (str.IndexOf('#') != -1)
			{
				byte B = or.Read();
				byte C = or.Read();
				ret = str.Replace("#", string.Format("{0:X4}", B + C * 256));
			}
			else ret = str;

			size = (ushort)or.Size;
			return or.Format(ret);
		}

	}
}