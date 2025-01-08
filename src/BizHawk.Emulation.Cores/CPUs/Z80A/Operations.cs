using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components.Z80A
{
	public partial class Z80A<TLink>
	{
		public void Read_Func(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = _link.ReadMemory((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];
		}

		public void Read_INC_Func(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = _link.ReadMemory((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];
			INC16_Func(src_l, src_h);
		}

		public void Read_INC_TR_PC_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			Regs[dest_h] = _link.ReadMemory((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest_h];
			INC16_Func(src_l, src_h);
			TR16_Func(PCl, PCh, dest_l, dest_h);
		}

		public void Write_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			Regs[DB] = Regs[src];
			_link.WriteMemory((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)Regs[src]);
		}

		public void Write_INC_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			Regs[DB] = Regs[src];
			_link.WriteMemory((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)Regs[src]);
			INC16_Func(dest_l, dest_h);
		}

		public void Write_DEC_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			Regs[DB] = Regs[src];
			_link.WriteMemory((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)Regs[src]);
			DEC16_Func(dest_l, dest_h);
		}

		public void Write_TR_PC_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			Regs[DB] = Regs[src];
			_link.WriteMemory((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)Regs[src]);
			TR16_Func(PCl, PCh, Z, W);
		}

		public void OUT_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			Regs[DB] = Regs[src];
			_link.WriteHardware((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)(Regs[src]));
		}

		public void OUT_INC_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			Regs[DB] = Regs[src];
			_link.WriteHardware((ushort)(Regs[dest_l] | (Regs[dest_h] << 8)), (byte)(Regs[src]));
			INC16_Func(dest_l, dest_h);
		}

		public void IN_Func(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = _link.ReadHardware((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];

			FlagZ = Regs[dest] == 0;
			FlagP = TableParity[Regs[dest]];
			FlagH = false;
			FlagN = false;
			FlagS = Regs[dest].Bit(7);
			Flag5 = Regs[dest].Bit(5);
			Flag3 = Regs[dest].Bit(3);
		}

		public void IN_INC_Func(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = _link.ReadHardware((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];

			FlagZ = Regs[dest] == 0;
			FlagP = TableParity[Regs[dest]];
			FlagH = false;
			FlagN = false;
			FlagS = Regs[dest].Bit(7);
			Flag5 = Regs[dest].Bit(5);
			Flag3 = Regs[dest].Bit(3);

			INC16_Func(src_l, src_h);
		}

		public void IN_A_N_INC_Func(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = _link.ReadHardware((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[DB] = Regs[dest];
			INC16_Func(src_l, src_h);
		}

		public void TR_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}

		public void TR16_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			Regs[dest_l] = Regs[src_l];
			Regs[dest_h] = Regs[src_h];
		}

		public void ADD16_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[dest_l] | (Regs[dest_h]  << 8);
			int Reg16_s = Regs[src_l] | (Regs[src_h] << 8);
			int temp = Reg16_d + Reg16_s;

			FlagC = temp.Bit(16);
			FlagH = ((Reg16_d & 0xFFF) + (Reg16_s & 0xFFF)) > 0xFFF;
			FlagN = false;
			Flag3 = (temp & 0x0800) != 0;
			Flag5 = (temp & 0x2000) != 0;

			Regs[dest_l] = (ushort)(temp & 0xFF);
			Regs[dest_h] = (ushort)((temp & 0xFF00) >> 8);
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
			FlagN = false;			
			Flag3 = (ans & 0x08) != 0;
			Flag5 = (ans & 0x20) != 0;
			FlagP = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

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
			FlagN = true;
			Flag3 = (ans & 0x08) != 0;
			Flag5 = (ans & 0x20) != 0;
			FlagP = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

			Regs[dest] = ans;
		}

		public void BIT_Func(ushort bit, ushort src)
		{
			FlagZ = !Regs[src].Bit(bit);
			FlagP = FlagZ; // special case
			FlagH = true;
			FlagN = false;
			FlagS = ((bit == 7) && Regs[src].Bit(bit));
			Flag5 = Regs[src].Bit(5);
			Flag3 = Regs[src].Bit(3);
		}

		// When doing I* + n bit tests, flags 3 and 5 come from I* + n
		// This cooresponds to the high byte of WZ
		// This is the same for the (HL) bit tests, except that WZ were not assigned to before the test occurs
		public void I_BIT_Func(ushort bit, ushort src)
		{
			FlagZ = !Regs[src].Bit(bit);
			FlagP = FlagZ; // special case
			FlagH = true;
			FlagN = false;
			FlagS = ((bit == 7) && Regs[src].Bit(bit));
			Flag5 = Regs[W].Bit(5);
			Flag3 = Regs[W].Bit(3);
		}

		public void SET_Func(ushort bit, ushort src)
		{
			Regs[src] |= (ushort)(1 << bit);
		}

		public void RES_Func(ushort bit, ushort src)
		{
			Regs[src] &= (ushort)(0xFF - (1 << bit));
		}

		public void ASGN_Func(ushort src, ushort val)
		{
			Regs[src] = val;
		}

		public void SLL_Func(ushort src)
		{
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | 0x1);

			FlagS = Regs[src].Bit(7);
			FlagZ = Regs[src] == 0;
			FlagP = TableParity[Regs[src]];
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void SLA_Func(ushort src)
		{
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)((Regs[src] << 1) & 0xFF);

			FlagS = Regs[src].Bit(7);
			FlagZ = Regs[src] == 0;
			FlagP = TableParity[Regs[src]];
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void SRA_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			ushort temp = (ushort)(Regs[src] & 0x80); // MSB doesn't change in this operation

			Regs[src] = (ushort)((Regs[src] >> 1) | temp);

			FlagS = Regs[src].Bit(7);
			FlagZ = Regs[src] == 0;
			FlagP = TableParity[Regs[src]];
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void SRL_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(Regs[src] >> 1);

			FlagS = Regs[src].Bit(7);
			FlagZ = Regs[src] == 0;
			FlagP = TableParity[Regs[src]];
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void CPL_Func(ushort src)
		{
			Regs[src] = (ushort)((~Regs[src]) & 0xFF);

			FlagH = true;
			FlagN = true;
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
		}

		public void CCF_Func(ushort src)
		{
			FlagH = FlagC;
			FlagC = !FlagC;		
			FlagN = false;
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
		}

		public void SCF_Func(ushort src)
		{
			FlagC = true;
			FlagH = false;
			FlagN = false;
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
		}

		public void AND8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] & Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagH = true;
			FlagN = false;
			Flag3 = (Regs[dest] & 0x08) != 0;
			Flag5 = (Regs[dest] & 0x20) != 0;
			FlagS = Regs[dest] > 127;
			FlagP = TableParity[Regs[dest]];
		}

		public void OR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] | Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagH = false;
			FlagN = false;
			Flag3 = (Regs[dest] & 0x08) != 0;
			Flag5 = (Regs[dest] & 0x20) != 0;
			FlagS = Regs[dest] > 127;
			FlagP = TableParity[Regs[dest]];
		}

		public void XOR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)((Regs[dest] ^ Regs[src]));

			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagH = false;
			FlagN = false;
			Flag3 = (Regs[dest] & 0x08) != 0;
			Flag5 = (Regs[dest] & 0x20) != 0;
			FlagS = Regs[dest] > 127;
			FlagP = TableParity[Regs[dest]];
		}

		public void CP8_Func(ushort dest, ushort src)
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
			FlagN = true;
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagP = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;
		}

		public void RRC_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)((FlagC ? 0x80 : 0) | (Regs[src] >> 1));

			if (!imm)
			{
				FlagS = Regs[src].Bit(7);
				FlagZ = Regs[src] == 0;
				FlagP = TableParity[Regs[src]];
			}

			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void RR_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			ushort c = (ushort)(FlagC ? 0x80 : 0);

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(c | (Regs[src] >> 1));

			if (!imm)
			{
				FlagS = Regs[src].Bit(7);
				FlagZ = Regs[src] == 0;
				FlagP = TableParity[Regs[src]];
			}

			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void RLC_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			ushort c = (ushort)(Regs[src].Bit(7) ? 1 : 0);
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);

			if (!imm)
			{
				FlagS = Regs[src].Bit(7);
				FlagZ = Regs[src] == 0;
				FlagP = TableParity[Regs[src]];
			}

			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void RL_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			ushort c = (ushort)(FlagC ? 1 : 0);
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);

			if (!imm)
			{
				FlagS = Regs[src].Bit(7);
				FlagZ = Regs[src] == 0;
				FlagP = TableParity[Regs[src]];
			}

			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
			FlagH = false;
			FlagN = false;
		}

		public void INC8_Func(ushort src)
		{
			int Reg16_d = Regs[src];
			Reg16_d += 1;

			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[src] & 0xF;
			Reg16_d += 1;

			FlagH = Reg16_d.Bit(4);
			FlagN = false;

			Regs[src] = ans;

			FlagS = Regs[src].Bit(7);
			FlagP = Regs[src] == 0x80;
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
		}

		public void DEC8_Func(ushort src)
		{
			int Reg16_d = Regs[src];
			Reg16_d -= 1;

			FlagZ = (Reg16_d & 0xFF) == 0;

			ushort ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[src] & 0xF;
			Reg16_d -= 1;

			FlagH = Reg16_d.Bit(4);
			FlagN = true;

			Regs[src] = ans;

			FlagS = Regs[src].Bit(7);
			FlagP = Regs[src] == 0x7F;
			Flag3 = (Regs[src] & 0x08) != 0;
			Flag5 = (Regs[src] & 0x20) != 0;
		}

		public void INC16_Func(ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d += 1;

			Regs[src_l] = (ushort)(Reg16_d & 0xFF);
			Regs[src_h] = (ushort)((Reg16_d & 0xFF00) >> 8);
		}

		public void DEC16_Func(ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d -= 1;

			Regs[src_l] = (ushort)(Reg16_d & 0xFF);
			Regs[src_h] = (ushort)((Reg16_d & 0xFF00) >> 8);
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
			FlagN = false;
			Flag3 = (ans & 0x08) != 0;
			Flag5 = (ans & 0x20) != 0;
			FlagP = (Regs[dest].Bit(7) == Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

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
			FlagN = true;
			Flag3 = (ans & 0x08) != 0;
			Flag5 = (ans & 0x20) != 0;
			FlagP = (Regs[dest].Bit(7) != Regs[src].Bit(7)) && (Regs[dest].Bit(7) != ans.Bit(7));
			FlagS = ans > 127;

			Regs[dest] = ans;
		}

		public void DA_Func(ushort src)
		{
			byte a = (byte)Regs[src];
			byte temp = a;

			if (FlagN)
			{
				if (FlagH || ((a & 0x0F) > 0x09)) { temp -= 0x06; }
				if (FlagC || a > 0x99) { temp -= 0x60; }
			}
			else
			{
				if (FlagH || ((a & 0x0F) > 0x09)) { temp += 0x06; }
				if (FlagC || a > 0x99) { temp += 0x60; }
			}

			temp &= 0xFF;

			FlagC = FlagC || a > 0x99;
			FlagZ = temp == 0;
			FlagH = ((a ^ temp) & 0x10) != 0;
			FlagP = TableParity[temp];
			FlagS = temp > 127;
			Flag3 = (temp & 0x08) != 0;
			Flag5 = (temp & 0x20) != 0;

			Regs[src] = temp;
		}

		// used for signed operations
		public void ADDS_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[dest_l];
			int Reg16_s = Regs[src_l];

			Reg16_d += Reg16_s;

			ushort temp = 0;

			// since this is signed addition, calculate the high byte carry appropriately
			// note that flags are unaffected by this operation
			if (Reg16_s.Bit(7))
			{
				if (((Reg16_d & 0xFF) >= Regs[dest_l]))
				{
					temp = 0xFF;
				}
				else
				{
					temp = 0;
				}
			}
			else
			{
				temp = (ushort)(Reg16_d.Bit(8) ? 1 : 0);
			}

			ushort ans_l = (ushort)(Reg16_d & 0xFF);

			Regs[dest_l] = ans_l;
			Regs[dest_h] += temp;
			Regs[dest_h] &= 0xFF;

		}

		public void EXCH_16_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			ushort temp = Regs[dest_l];
			Regs[dest_l] = Regs[src_l];
			Regs[src_l] = temp;

			temp = Regs[dest_h];
			Regs[dest_h] = Regs[src_h];
			Regs[src_h] = temp;
		}

		public void SBC_16_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[dest_l] | (Regs[dest_h] << 8);
			int Reg16_s = Regs[src_l] | (Regs[src_h] << 8);
			int c = FlagC ? 1 : 0;

			int ans = Reg16_d - Reg16_s - c;

			FlagN = true;
			FlagC = ans.Bit(16);
			FlagP = (Reg16_d.Bit(15) != Reg16_s.Bit(15)) && (Reg16_d.Bit(15) != ans.Bit(15));
			FlagS = (ushort)(ans & 0xFFFF) > 32767;
			FlagZ = (ans & 0xFFFF) == 0;
			Flag3 = (ans & 0x0800) != 0;
			Flag5 = (ans & 0x2000) != 0;

			// redo for half carry flag
			Reg16_d &= 0xFFF;
			Reg16_d -= ((Reg16_s & 0xFFF) + c);

			FlagH = Reg16_d.Bit(12);

			Regs[dest_l] = (ushort)(ans & 0xFF);
			Regs[dest_h] = (ushort)((ans >> 8) & 0xFF);
		}

		public void ADC_16_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[dest_l] | (Regs[dest_h] << 8);
			int Reg16_s = Regs[src_l] | (Regs[src_h] << 8);

			int ans = Reg16_d + Reg16_s + (FlagC ? 1 : 0);

			FlagH = ((Reg16_d & 0xFFF) + (Reg16_s & 0xFFF) + (FlagC ? 1 : 0)) > 0xFFF;
			FlagN = false;
			FlagC = ans.Bit(16);
			FlagP = (Reg16_d.Bit(15) == Reg16_s.Bit(15)) && (Reg16_d.Bit(15) != ans.Bit(15));
			FlagS = (ans & 0xFFFF) > 32767;
			FlagZ = (ans & 0xFFFF) == 0;
			Flag3 = (ans & 0x0800) != 0;
			Flag5 = (ans & 0x2000) != 0;

			Regs[dest_l] = (ushort)(ans & 0xFF);
			Regs[dest_h] = (ushort)((ans >> 8) & 0xFF);
		}

		public void NEG_8_Func(ushort src)
		{
			int Reg16_d = 0;
			Reg16_d -= Regs[src];

			FlagC = Regs[src] != 0x0;
			FlagZ = (Reg16_d & 0xFF) == 0;
			FlagP = Regs[src] == 0x80;
			FlagS = (Reg16_d & 0xFF) > 127;
			
			ushort ans = (ushort)(Reg16_d & 0xFF);
			// redo for half carry flag
			Reg16_d = 0;
			Reg16_d -= (Regs[src] & 0xF);
			FlagH = Reg16_d.Bit(4);
			Regs[src] = ans;			
			FlagN = true;
			Flag3 = (ans & 0x08) != 0;
			Flag5 = (ans & 0x20) != 0;
		}

		public void RRD_Func(ushort dest, ushort src)
		{
			ushort temp1 = Regs[src];
			ushort temp2 = Regs[dest];
			Regs[dest] = (ushort)(((temp1 & 0x0F) << 4) + ((temp2 & 0xF0) >> 4));
			Regs[src] = (ushort)((temp1 & 0xF0) + (temp2 & 0x0F));

			temp1 = Regs[src];
			FlagS = temp1 > 127;
			FlagZ = temp1 == 0;
			FlagH = false;
			FlagP = TableParity[temp1];
			FlagN = false;
			Flag3 = (temp1 & 0x08) != 0;
			Flag5 = (temp1 & 0x20) != 0;
		}

		public void RLD_Func(ushort dest, ushort src)
		{
			ushort temp1 = Regs[src];
			ushort temp2 = Regs[dest];
			Regs[dest] = (ushort)((temp1 & 0x0F) + ((temp2 & 0x0F) << 4));
			Regs[src] = (ushort)((temp1 & 0xF0) + ((temp2 & 0xF0) >> 4));

			temp1 = Regs[src];
			FlagS = temp1 > 127;
			FlagZ = temp1 == 0;
			FlagH = false;
			FlagP = TableParity[temp1];
			FlagN = false;
			Flag3 = (temp1 & 0x08) != 0;
			Flag5 = (temp1 & 0x20) != 0;
		}

		// sets flags for LD/R 
		public void SET_FL_LD_Func()
		{
			FlagP = (Regs[C] | (Regs[B] << 8)) != 0;
			FlagH = false;
			FlagN = false;
			Flag5 = ((Regs[ALU] + Regs[A]) & 0x02) != 0;
			Flag3 = ((Regs[ALU] + Regs[A]) & 0x08) != 0;
		}

		// set flags for CP/R
		public void SET_FL_CP_Func()
		{
			int Reg8_d = Regs[A];
			int Reg8_s = Regs[ALU];

			// get half carry flag
			byte temp = (byte)((Reg8_d & 0xF) - (Reg8_s & 0xF));
			FlagH = temp.Bit(4);

			temp = (byte)(Reg8_d - Reg8_s);
			FlagN = true;
			FlagZ = temp == 0;
			FlagS = temp > 127;
			FlagP = (Regs[C] | (Regs[B] << 8)) != 0;

			temp = (byte)(Reg8_d - Reg8_s - (FlagH ? 1 : 0));
			Flag5 = (temp & 0x02) != 0;
			Flag3 = (temp & 0x08) != 0;
		}

		// set flags for LD A, I/R
		public void SET_FL_IR_Func(ushort dest)
		{
			if (dest == A)
			{
				FlagN = false;
				FlagH = false;
				FlagZ = Regs[A] == 0;
				FlagS = Regs[A] > 127;
				FlagP = iff2;
				Flag5 = (Regs[A] & 0x20) != 0;
				Flag3 = (Regs[A] & 0x08) != 0;
			}
		}

		public void FTCH_DB_Func()
		{
			Regs[DB] = _link.FetchDB();
		}
	}
}
