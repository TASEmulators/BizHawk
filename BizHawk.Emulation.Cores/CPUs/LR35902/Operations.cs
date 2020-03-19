using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components.LR35902
{
	public partial class LR35902
	{
		// local variables for operations, not stated
		int Reg16_d, Reg16_s, c;
		ushort ans, ans_l, ans_h, temp;
		byte a_d;
		bool imm;


		public void Read_Func(ushort dest, ushort src_l, ushort src_h)
		{
			ushort addr = (ushort)(Regs[src_l] | (Regs[src_h]) << 8);
			if (CDLCallback != null)
			{
				if (src_l == PCl) CDLCallback(addr, eCDLogMemFlags.FetchOperand);
				else CDLCallback(addr, eCDLogMemFlags.Data);
			}
			Regs[dest] = ReadMemory(addr);
		}

		// special read for POP AF that always clears the lower 4 bits of F 
		public void Read_Func_F(ushort dest, ushort src_l, ushort src_h)
		{
			Regs[dest] = (ushort)(ReadMemory((ushort)(Regs[src_l] | (Regs[src_h]) << 8)) & 0xF0);
		}

		public void Write_Func(ushort dest_l, ushort dest_h, ushort src)
		{
			ushort addr = (ushort)(Regs[dest_l] | (Regs[dest_h]) << 8);
			CDLCallback?.Invoke(addr, eCDLogMemFlags.Write | eCDLogMemFlags.Data);
			WriteMemory(addr, (byte)Regs[src]);
		}

		public void TR_Func(ushort dest, ushort src)
		{
			Regs[dest] = Regs[src];
		}

		public void ADD16_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			Reg16_d = Regs[dest_l] | (Regs[dest_h]  << 8);
			Reg16_s = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d += Reg16_s;

			FlagC = Reg16_d.Bit(16);

			ans_l = (ushort)(Reg16_d & 0xFF);
			ans_h = (ushort)((Reg16_d & 0xFF00) >> 8);

			// redo for half carry flag
			Reg16_d = Regs[dest_l] | ((Regs[dest_h] & 0x0F) << 8);
			Reg16_s = Regs[src_l] | ((Regs[src_h] & 0x0F) << 8);

			Reg16_d += Reg16_s;

			FlagH = Reg16_d.Bit(12);
			FlagN = false;

			Regs[dest_l] = ans_l;
			Regs[dest_h] = ans_h;
		}

		public void ADD8_Func(ushort dest, ushort src)
		{
			Reg16_d = Regs[dest];
			Reg16_d += Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += (Regs[src] & 0xF);

			FlagH = Reg16_d.Bit(4);

			FlagN = false;

			Regs[dest] = ans;
		}

		public void SUB8_Func(ushort dest, ushort src)
		{
			Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			FlagH = Reg16_d.Bit(4);
			FlagN = true;

			Regs[dest] = ans;
		}

		public void BIT_Func(ushort bit, ushort src)
		{
			FlagZ = !Regs[src].Bit(bit);
			FlagH = true;
			FlagN = false;
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

		public void SWAP_Func(ushort src)
		{
			temp = (ushort)((Regs[src] << 4) & 0xF0);
			Regs[src] = (ushort)(temp | (Regs[src] >> 4));

			FlagZ = Regs[src] == 0;
			FlagH = false;
			FlagN = false;
			FlagC = false;
		}

		public void SLA_Func(ushort src)
		{
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)((Regs[src] << 1) & 0xFF);

			FlagZ = Regs[src] == 0;
			FlagH = false;
			FlagN = false;
		}

		public void SRA_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			temp = (ushort)(Regs[src] & 0x80); // MSB doesn't change in this operation

			Regs[src] = (ushort)((Regs[src] >> 1) | temp);

			FlagZ = Regs[src] == 0;
			FlagH = false;
			FlagN = false;
		}

		public void SRL_Func(ushort src)
		{
			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(Regs[src] >> 1);

			FlagZ = Regs[src] == 0;
			FlagH = false;
			FlagN = false;
		}

		public void CPL_Func(ushort src)
		{
			Regs[src] = (ushort)((~Regs[src]) & 0xFF);

			FlagH = true;
			FlagN = true;
		}

		public void CCF_Func(ushort src)
		{
			FlagC = !FlagC;
			FlagH = false;
			FlagN = false;
		}

		public void SCF_Func(ushort src)
		{
			FlagC = true;
			FlagH = false;
			FlagN = false;
		}

		public void AND8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] & Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagH = true;
			FlagN = false;
		}

		public void OR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] | Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagH = false;
			FlagN = false;
		}

		public void XOR8_Func(ushort dest, ushort src)
		{
			Regs[dest] = (ushort)(Regs[dest] ^ Regs[src]);

			FlagZ = Regs[dest] == 0;
			FlagC = false;
			FlagH = false;
			FlagN = false;
		}

		public void CP8_Func(ushort dest, ushort src)
		{
			Reg16_d = Regs[dest];
			Reg16_d -= Regs[src];

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= (Regs[src] & 0xF);

			FlagH = Reg16_d.Bit(4);

			FlagN = true;
		}

		public void RRC_Func(ushort src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)((FlagC ? 0x80 : 0) | (Regs[src] >> 1));

			FlagZ = imm ? false : (Regs[src] == 0);
			FlagH = false;
			FlagN = false;
		}

		public void RR_Func(ushort src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			c = FlagC ? 0x80 : 0;

			FlagC = Regs[src].Bit(0);

			Regs[src] = (ushort)(c | (Regs[src] >> 1));

			FlagZ = imm ? false : (Regs[src] == 0);
			FlagH = false;
			FlagN = false;
		}

		public void RLC_Func(ushort src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			c = Regs[src].Bit(7) ? 1 : 0;
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);

			FlagZ = imm ? false : (Regs[src] == 0);
			FlagH = false;
			FlagN = false;
		}

		public void RL_Func(ushort src)
		{
			imm = src == Aim;
			if (imm) { src = A; }

			c = FlagC ? 1 : 0;
			FlagC = Regs[src].Bit(7);

			Regs[src] = (ushort)(((Regs[src] << 1) & 0xFF) | c);

			FlagZ = imm ? false : (Regs[src] == 0);
			FlagH = false;
			FlagN = false;
		}

		public void INC8_Func(ushort src)
		{
			Reg16_d = Regs[src];
			Reg16_d += 1;

			FlagZ = (Reg16_d & 0xFF) == 0;

			ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[src] & 0xF;
			Reg16_d += 1;

			FlagH = Reg16_d.Bit(4);
			FlagN = false;

			Regs[src] = ans;
		}

		public void DEC8_Func(ushort src)
		{
			Reg16_d = Regs[src];
			Reg16_d -= 1;

			FlagZ = (Reg16_d & 0xFF) == 0;

			ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[src] & 0xF;
			Reg16_d -= 1;

			FlagH = Reg16_d.Bit(4);
			FlagN = true;

			Regs[src] = ans;
		}

		public void INC16_Func(ushort src_l, ushort src_h)
		{
			Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d += 1;

			Regs[src_l] = (ushort)(Reg16_d & 0xFF);
			Regs[src_h] = (ushort)((Reg16_d & 0xFF00) >> 8);
		}

		public void DEC16_Func(ushort src_l, ushort src_h)
		{
			Reg16_d = Regs[src_l] | (Regs[src_h] << 8);

			Reg16_d -= 1;

			Regs[src_l] = (ushort)(Reg16_d & 0xFF);
			Regs[src_h] = (ushort)((Reg16_d & 0xFF00) >> 8);
		}

		public void ADC8_Func(ushort dest, ushort src)
		{
			Reg16_d = Regs[dest];
			c = FlagC ? 1 : 0;

			Reg16_d += (Regs[src] + c);

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d += ((Regs[src] & 0xF) + c);

			FlagH = Reg16_d.Bit(4);
			FlagN = false;

			Regs[dest] = ans;
		}

		public void SBC8_Func(ushort dest, ushort src)
		{
			Reg16_d = Regs[dest];
			c = FlagC ? 1 : 0;

			Reg16_d -= (Regs[src] + c);

			FlagC = Reg16_d.Bit(8);
			FlagZ = (Reg16_d & 0xFF) == 0;

			ans = (ushort)(Reg16_d & 0xFF);

			// redo for half carry flag
			Reg16_d = Regs[dest] & 0xF;
			Reg16_d -= ((Regs[src] & 0xF) + c);

			FlagH = Reg16_d.Bit(4);
			FlagN = true;

			Regs[dest] = ans;
		}

		// DA code courtesy of AWJ: http://forums.nesdev.com/viewtopic.php?f=20&t=15944
		public void DA_Func(ushort src)
		{
			a_d = (byte)Regs[src];

			if (!FlagN)
			{  // after an addition, adjust if (half-)carry occurred or if result is out of bounds
				if (FlagC || a_d > 0x99) { a_d += 0x60; FlagC = true; }
				if (FlagH || (a_d & 0x0f) > 0x09) { a_d += 0x6; }
			}
			else
			{  // after a subtraction, only adjust if (half-)carry occurred
				if (FlagC) { a_d -= 0x60; }
				if (FlagH) { a_d -= 0x6; }
			}

			a_d &= 0xFF;

			Regs[src] = a_d;

			FlagZ = a_d == 0; 
			FlagH = false;
		}

		// used for signed operations
		public void ADDS_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			Reg16_d = Regs[dest_l];
			Reg16_s = Regs[src_l];

			Reg16_d += Reg16_s;

			temp = 0;

			// since this is signed addition, calculate the high byte carry appropriately
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

			ans_l = (ushort)(Reg16_d & 0xFF);

			// JR operations do not effect flags
			if (dest_l != PCl)
			{
				FlagC = Reg16_d.Bit(8);

				// redo for half carry flag
				Reg16_d = Regs[dest_l] & 0xF;
				Reg16_d += Regs[src_l] & 0xF;

				FlagH = Reg16_d.Bit(4);
				FlagN = false;
				FlagZ = false; 			
			}

			Regs[dest_l] = ans_l;
			Regs[dest_h] += temp;
			Regs[dest_h] &= 0xFF;

		}
	}
}
