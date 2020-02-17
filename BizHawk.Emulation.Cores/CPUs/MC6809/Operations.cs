using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components.MC6809
{
	public partial class MC6809
	{
		public void Read_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null)
			{
				if (src == PC) CDLCallback(Regs[src], eCDLogMemFlags.FetchOperand);
				else CDLCallback(Regs[src], eCDLogMemFlags.Data);
			}
			Regs[dest] = ReadMemory(Regs[src]);
		}

		public void Read_Inc_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null)
			{
				if (src == PC) CDLCallback(Regs[src], eCDLogMemFlags.FetchOperand);
				else CDLCallback(Regs[src], eCDLogMemFlags.Data);
			}
			//Console.WriteLine(dest + " " + src + " " + opcode_see);

			Regs[dest] = ReadMemory(Regs[src]);

			Regs[src]++;
		}

		public void Write_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)Regs[src]);
		}

		public void Write_Dec_Lo_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)Regs[src]);
			Regs[dest] -= 1;
		}

		public void Write_Dec_HI_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)(Regs[src] >> 8));
			Regs[dest] -= 1;
		}

		public void Write_Hi_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)(Regs[src] >> 8));
		}

		public void Write_Hi_Inc_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)(Regs[src] >> 8));
			Regs[dest]++;
		}

		public void Write_Lo_Inc_Func(ushort dest, ushort src)
		{
			if (CDLCallback != null) CDLCallback(Regs[dest], eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(Regs[dest], (byte)Regs[src]);
			Regs[dest]++;
		}

		public void NEG_8_Func(ushort src)
		{
			int Reg16_d = 0;
			Reg16_d -= Regs[src];

			FlagC = Regs[src] != 0x0;
			FlagZ = (Reg16_d & 0xFF) == 0;
			FlagV = Regs[src] == 0x80;
			FlagN = (Reg16_d & 0xFF) > 127;

			ushort ans = (ushort)(Reg16_d & 0xFF);
			// redo for half carry flag
			Reg16_d = 0;
			Reg16_d -= (Regs[src] & 0xF);
			FlagH = Reg16_d.Bit(4);
			Regs[src] = ans;
		}

		public void TR_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}

		public void LD_8_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];

			FlagZ = (Regs[dest] & 0xFF) == 0;
			FlagV = false;
			FlagN = (Regs[dest] & 0xFF) > 127;
		}

		public void ST_8_Func(ushort dest)
		{
			FlagZ = (Regs[dest] & 0xFF) == 0;
			FlagV = false;
			FlagN = (Regs[dest] & 0xFF) > 127;
		}

		public void LD_16_Func(ushort dest, ushort src_h, ushort src_l)
		{
			Regs[dest] = (ushort)(Regs[src_h] << 8 | Regs[src_l]);

			FlagZ = Regs[dest] == 0;
			FlagV = false;
			FlagN = Regs[dest] > 0x7FFF;
		}

		public void ST_16_Func(ushort dest)
		{
			if (dest == Dr)
			{
				Regs[dest] = D;
			}

			FlagZ = Regs[dest] == 0;
			FlagV = false;
			FlagN = Regs[dest] > 0x7FFF;		
		}

		// for LEAX/Y, zero flag can be effected, but not for U/S
		public void LEA_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];

			if ((dest == X) || (dest == Y))
			{
				FlagZ = Regs[dest] == 0;
			}
		}

		public void TST_Func(ushort src)
		{
			FlagZ = Regs[src] == 0;
			FlagV = false;
			FlagN = (Regs[src] & 0xFF) > 127;
		}

		public void CLR_Func(ushort src)
		{
			Regs[src] = 0;

			FlagZ = true;
			FlagV = false;
			FlagC = false;
			FlagN = false;
		}

		// source is considered a 16 bit signed value, used for long relative branch
		// no flags used
		public void ADD16BR_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] + (short)Regs[src]);
		}

		public void ADD8BR_Func(ushort dest, ushort src)
		{
			if (Regs[src] > 127) { Regs[src] |= 0xFF00; }
			Regs[dest] = (ushort)(Regs[dest] + (short)Regs[src]);
		}

		public void Mul_Func()
		{
			Regs[ALU] = (ushort)(Regs[A] * Regs[B]);
			D = Regs[ALU];
			FlagC = Regs[A] > 127;
			FlagZ = D == 0;
		}

		public void ADD8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d += Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += (Regs[src] & 0xF);

			FlagH = Reg16_d.Bit(4);
			FlagV = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagN = ans > 127;

			Regs[dest] = ans;
		}

		public void SUB8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			FlagH = Reg16_d.Bit(4);
			FlagN = ans > 127;
			FlagV = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));

			Regs[dest] = ans;
		}

		// same as SUB8 but result not stored
		public void CMP8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			FlagH = Reg16_d.Bit(4);
			FlagN = ans > 127;
			FlagV = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
		}

		public void BIT_Func(ushort dest, ushort src)
		{
			ushort ans = (ushort)(Regs[dest] & Regs[src]);

			FlagZ = ans == 0;
			FlagV = false;
			FlagN = ans > 127;
		}

		public void ASL_Func(ushort src)
		{
			FlagC = Regs[src].Bit(7);
			FlagV = Regs[src].Bit(7) ^ Regs[src].Bit(6);

			Regs[src] = (ushort)((Regs[src] << 1) & 0xFF);

			FlagZ = Regs[src] == 0;
			FlagH = false;
			FlagN = (Regs[src] & 0xFF) > 127;

		}

		public void ASR_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			ushort temp = (ushort)(Regs[src] & 0x80); // MSB doesn't change in this operation

			Regs[src] = (ushort)((Regs[src] >> 1) | temp);

			FlagZ = Regs[src] == 0;
			FlagH = false;
			FlagN = (Regs[src] & 0xFF) > 127;
		}

		public void LSR_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(Regs[src] >> 1);

			FlagZ = Regs[src] == 0;
			FlagN = false;
		}

		public void COM_Func(ushort src)
		{
			Regs[src] = (ushort)((~Regs[src]) & 0xFF);

			FlagC = true;
			FlagZ = Regs[src] == 0;
			FlagV = false;
			FlagN = (Regs[src] & 0xFF) > 127;
		}

		public void SEX_Func(ushort src)
		{
			if (Regs[B] > 127)
			{
				Regs[A] = 0xFF;
			}
			else
			{
				Regs[A] = 0;
			}

			FlagZ = D == 0;
			FlagN = Regs[A] > 127;
		}

		public void AND8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] & Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagV = false;
			FlagN = Regs[dest] > 127;
		}

		public void OR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] | Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagV = false;
			FlagN = Regs[dest] > 127;
		}

		public void XOR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagV = false;
			FlagN = Regs[dest] > 127;
		}

		public void ROR_Func(ushort src)
		{
			ushort c = (ushort)(FlagC ? 0x80 : 0);

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(c | (Regs[src] >> 1));

			FlagZ = Regs[src] == 0;
			FlagN = (Regs[src] & 0xFF) > 127;
		}

		public void ROL_Func(ushort src)
		{
			ushort c = (ushort)(FlagC ? 1 : 0);
			FlagC = Regs[src].Bit(7);
			FlagV = Regs[src].Bit(7) ^ Regs[src].Bit(6);


			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);

			FlagZ = Regs[src] == 0;
			FlagN = (Regs[src] & 0xFF) > 127;
		}

		public void INC8_Func(ushort src)
		{
			FlagV = Regs[src] == 0x7F;

			Regs[src] = (ushort)((Regs[src] + 1) & 0xFF);

			FlagZ = Regs[src] == 0;
			FlagN = (Regs[src] & 0xFF) > 127;
		}

		public void DEC8_Func(ushort src)
		{
			FlagV = Regs[src] == 0x80;

			Regs[src] = (ushort)((Regs[src] - 1) & 0xFF);

			FlagZ = Regs[src] == 0;
			FlagN = (Regs[src] & 0xFF) > 127;
		}

		public void INC16_Func(ushort src)
		{
			Regs[src] += 1;
		}

		public void DEC16_Func(ushort src)
		{
			Regs[src] -= 1;
		}

		public void ADC8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			int c = FlagC ? 1 : 0;

			Reg16_d += (Regs[src] + c);

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += ((Regs[src] & 0xF) + c);

			FlagH = Reg16_d.Bit(4);
			FlagV = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagN = false;

			Regs[dest] = ans;
		}

		public void SBC8_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			int c = FlagC ? 1 : 0;

			Reg16_d -= (Regs[src] + c);

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= ((Regs[src] & 0xF) + c);

			FlagH = Reg16_d.Bit(4);
			FlagN = ans > 127;
			FlagV = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));

			Regs[dest] = ans;
		}

		public void DA_Func(ushort src)
		{
			int a = Regs[src];

			byte CF = 0;
			if (FlagC || ((a & 0xF) > 9))
			{
				CF = 6;
			}
			if (FlagC || (((a >> 4) & 0xF) > 9) || ((((a >> 4) & 0xF) > 8) && ((a & 0xF) > 9)))
			{
				CF |= (byte)(6 << 4);
			}

			a += CF;

			if ((a > 0xFF) || FlagC)
			{
				FlagC = true;
			}
			else
			{
				FlagC = false;
			}
			Regs[src] = (byte)a;
			FlagN = a > 127;
			FlagZ = a == 0;
			// FlagV is listed as undefined in the documentation
		}

		// D register implied
		public void SUB16_Func(ushort src)
		{
			int Reg16_d = D;
			int Reg16_s = Regs[src];

			Reg16_d -= Reg16_s;

			FlagC = Reg16_d.Bit(16);
			FlagZ = (Reg16_d & 0xFFFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFFFF);

			FlagN = ans > 0x7FFF;
			FlagV = (D.Bit(15) != Regs[src].Bit(15)) && (D.Bit(15) != ans.Bit(15));

			D = ans;
		}

		// D register implied
		public void ADD16_Func(ushort src)
		{
			int Reg16_d = D;
			int Reg16_s = Regs[src];

			Reg16_d += Reg16_s;

			FlagC = Reg16_d.Bit(16);
			FlagZ = (Reg16_d & 0xFFFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFFFF);

			FlagN = ans > 0x7FFF;
			FlagV = (D.Bit(15) == Regs[src].Bit(15)) && (D.Bit(15) != ans.Bit(15));

			D = ans;
		}

		// D register implied
		public void CMP16D_Func(ushort src)
		{
			int Reg16_d = D;
			int Reg16_s = Regs[src];

			Reg16_d -= Reg16_s;

			FlagC = Reg16_d.Bit(16);
			FlagZ = (Reg16_d & 0xFFFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFFFF);

			FlagN = ans > 0x7FFF;
			FlagV = (D.Bit(15) != Regs[src].Bit(15)) && (D.Bit(15) != ans.Bit(15));
		}

		public void CMP16_Func(ushort dest, ushort src)
		{
			int Reg16_d = Regs[dest];
			int Reg16_s = Regs[src];

			Reg16_d -= Reg16_s;

			FlagC = Reg16_d.Bit(16);
			FlagZ = (Reg16_d & 0xFFFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFFFF);

			FlagN = ans > 0x7FFF;
			FlagV = (Regs[dest].Bit(15) != Regs[src].Bit(15)) && (Regs[dest].Bit(15) != ans.Bit(15));
		}

		public void EXG_Func(ushort sel)
		{
			ushort src = 0;
			ushort dest = 0;
			ushort temp = 0;
			if ((Regs[sel] & 0x8) == 0)
			{
				switch (Regs[sel] & 0xF)
				{
					case 0: src = Dr; break;
					case 1: src = X; break;
					case 2: src = Y; break;
					case 3: src = US; break;
					case 4: src = SP; break;
					case 5: src = PC; break;
					case 6: src = 0xFF; break;
					case 7: src = 0xFF; break;
				}

				switch ((Regs[sel] >> 4) & 0xF)
				{
					case 0: dest = Dr; break;
					case 1: dest = X; break;
					case 2: dest = Y; break;
					case 3: dest = US; break;
					case 4: dest = SP; break;
					case 5: dest = PC; break;
					case 6: dest = 0xFF; break;
					case 7: dest = 0xFF; break;
					default: dest = 0xFF; break;
				}
			}
			else
			{
				switch (Regs[sel] & 0xF)
				{
					case 8: src = A; break;
					case 9: src = B; break;
					case 10: src = CC; break;
					case 11: src = DP; break;
					case 12: src = 0xFF; break;
					case 13: src = 0xFF; break;
					case 14: src = 0xFF; break;
					case 15: src = 0xFF; break;
				}

				switch ((Regs[sel] >> 4) & 0xF)
				{
					case 8: dest = A; break;
					case 9: dest = B; break;
					case 10: dest = CC; break;
					case 11: dest = DP; break;
					case 12: dest = 0xFF; break;
					case 13: dest = 0xFF; break;
					case 14: dest = 0xFF; break;
					case 15: dest = 0xFF; break;
					default: dest = 0xFF; break;
				}
			}

			if ((src != 0xFF) && (dest != 0xFF))
			{
				if (src == Dr)
				{
					temp = D;
					D = Regs[dest];
					Regs[dest] = temp;
				}
				else if (dest == Dr)
				{
					temp = D;
					D = Regs[src];
					Regs[src] = temp;
				}
				else
				{
					temp = Regs[src];
					Regs[src] = Regs[dest];
					Regs[dest] = temp;
				}
			}
		}

		public void TFR_Func(ushort sel)
		{
			ushort src = 0;
			ushort dest = 0;

			if ((Regs[sel] & 0x8) == 0)
			{
				switch (Regs[sel] & 0xF)
				{
					case 0: dest = Dr; break;
					case 1: dest = X; break;
					case 2: dest = Y; break;
					case 3: dest = US; break;
					case 4: dest = SP; break;
					case 5: dest = PC; break;
					case 6: dest = 0xFF; break;
					case 7: dest = 0xFF; break;
				}

				switch ((Regs[sel] >> 4) & 0xF)
				{
					case 0: src = Dr; break;
					case 1: src = X; break;
					case 2: src = Y; break;
					case 3: src = US; break;
					case 4: src = SP; break;
					case 5: src = PC; break;
					case 6: src = 0xFF; break;
					case 7: src = 0xFF; break;
					default: src = 0xFF; break;
				}
			}
			else
			{
				switch (Regs[sel] & 0xF)
				{
					case 8: dest = A; break;
					case 9: dest = B; break;
					case 10: dest = CC; break;
					case 11: dest = DP; break;
					case 12: dest = 0xFF; break;
					case 13: dest = 0xFF; break;
					case 14: dest = 0xFF; break;
					case 15: dest = 0xFF; break;
				}

				switch ((Regs[sel] >> 4) & 0xF)
				{
					case 8: src = A; break;
					case 9: src = B; break;
					case 10: src = CC; break;
					case 11: src = DP; break;
					case 12: src = 0xFF; break;
					case 13: src = 0xFF; break;
					case 14: src = 0xFF; break;
					case 15: src = 0xFF; break;
					default: src = 0xFF; break;
				}
			}

			if ((src != 0xFF) && (dest != 0xFF))
			{
				if (src == Dr)
				{
					Regs[dest] = D;
				}
				else if (dest == Dr)
				{
					D = Regs[src];
				}
				else
				{
					Regs[dest] = Regs[src];
				}
			}
		}
	}
}
