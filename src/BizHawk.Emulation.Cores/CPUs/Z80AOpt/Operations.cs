using BizHawk.Common.NumberExtensions;

using BizHawk.Emulation.Cores.Components.Z80A;

namespace BizHawk.Emulation.Cores.Components.Z80AOpt
{
	public partial class Z80AOpt<TLink>
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
			// Flags accumulated into F once (see ADD8_Func). IN preserves C.
			byte val = _link.ReadHardware((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[dest] = val;
			Regs[DB] = val;

			byte f = (byte)(Regs[F] & 0x01);                                    // preserve C
			f |= (byte)(val & 0xA8);                                            // S, F5, F3
			if (val == 0) f |= 0x40;                                            // Z
			if (TableParity[val]) f |= 0x04;                                    // P
			// H = N = 0
			Regs[F] = f;
		}

		public void IN_INC_Func(ushort dest, ushort src_l, ushort src_h)
		{
			byte val = _link.ReadHardware((ushort)(Regs[src_l] | (Regs[src_h]) << 8));
			Regs[dest] = val;
			Regs[DB] = val;

			byte f = (byte)(Regs[F] & 0x01);                                    // preserve C
			f |= (byte)(val & 0xA8);                                            // S, F5, F3
			if (val == 0) f |= 0x40;                                            // Z
			if (TableParity[val]) f |= 0x04;                                    // P
			// H = N = 0
			Regs[F] = f;

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

			// ADD16 preserves S, Z, P; accumulate the rest into F once.
			byte f = (byte)(Regs[F] & 0xC4);                                    // preserve S, Z, P
			if (temp.Bit(16)) f |= 0x01;                                        // C
			if (((Reg16_d & 0xFFF) + (Reg16_s & 0xFFF)) > 0xFFF) f |= 0x10;     // H
			if ((temp & 0x0800) != 0) f |= 0x08;                               // F3
			if ((temp & 0x2000) != 0) f |= 0x20;                               // F5
			// N = 0
			Regs[dest_l] = (ushort)(temp & 0xFF);
			Regs[dest_h] = (ushort)((temp & 0xFF00) >> 8);
			Regs[F] = f;
		}

		public void ADD8_Func(ushort dest, ushort src)
		{
			// Flags are accumulated into a single local and written to F once, instead of the
			// 8 read-modify-writes of Regs[F] the flag properties would do. Bit masks:
			// C=0x01 N=0x02 P=0x04 F3=0x08 H=0x10 F5=0x20 Z=0x40 S=0x80. Values are identical.
			int d = Regs[dest];
			int s = Regs[src];
			int sum = d + s;
			ushort ans = (ushort)(sum & 0xFF);

			byte f = (byte)(ans & 0x28);                                        // F3, F5 from result
			if ((sum & 0x100) != 0) f |= 0x01;                                  // C
			if ((((d & 0xF) + (s & 0xF)) & 0x10) != 0) f |= 0x10;               // H
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (((d ^ s) & 0x80) == 0 && ((d ^ ans) & 0x80) != 0) f |= 0x04;    // P/V (overflow)
			// N = 0

			Regs[dest] = ans;
			Regs[F] = f;
		}

		public void SUB8_Func(ushort dest, ushort src)
		{
			int d = Regs[dest];
			int s = Regs[src];
			int diff = d - s;
			ushort ans = (ushort)(diff & 0xFF);

			byte f = (byte)((ans & 0x28) | 0x02);                               // F3, F5, N
			if ((diff & 0x100) != 0) f |= 0x01;                                 // C (borrow)
			if ((((d & 0xF) - (s & 0xF)) & 0x10) != 0) f |= 0x10;               // H
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (((d ^ s) & 0x80) != 0 && ((d ^ ans) & 0x80) != 0) f |= 0x04;    // P/V (overflow)

			Regs[dest] = ans;
			Regs[F] = f;
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
			int v = Regs[src];
			byte f = (byte)((v >> 7) & 0x01);                                   // C = old bit 7
			int r = ((v << 1) & 0xFF) | 0x01;
			f |= (byte)(r & 0xA8);                                              // S, F5, F3
			if (r == 0) f |= 0x40;                                              // Z
			if (TableParity[r]) f |= 0x04;                                      // P
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
		}

		public void SLA_Func(ushort src)
		{
			int v = Regs[src];
			byte f = (byte)((v >> 7) & 0x01);                                   // C = old bit 7
			int r = (v << 1) & 0xFF;
			f |= (byte)(r & 0xA8);                                              // S, F5, F3
			if (r == 0) f |= 0x40;                                              // Z
			if (TableParity[r]) f |= 0x04;                                      // P
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
		}

		public void SRA_Func(ushort src)
		{
			int v = Regs[src];
			byte f = (byte)(v & 0x01);                                          // C = old bit 0
			int r = (v >> 1) | (v & 0x80);                                      // MSB unchanged
			f |= (byte)(r & 0xA8);                                              // S, F5, F3
			if (r == 0) f |= 0x40;                                              // Z
			if (TableParity[r]) f |= 0x04;                                      // P
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
		}

		public void SRL_Func(ushort src)
		{
			int v = Regs[src];
			byte f = (byte)(v & 0x01);                                          // C = old bit 0
			int r = v >> 1;
			f |= (byte)(r & 0xA8);                                              // S, F5, F3
			if (r == 0) f |= 0x40;                                              // Z
			if (TableParity[r]) f |= 0x04;                                      // P
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
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
			ushort ans = (ushort)(Regs[dest] & Regs[src]);

			byte f = (byte)((ans & 0x28) | 0x10);                               // F3, F5, H (C=N=0)
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (TableParity[ans]) f |= 0x04;                                    // P

			Regs[dest] = ans;
			Regs[F] = f;
		}

		public void OR8_Func(ushort dest, ushort src)
		{
			ushort ans = (ushort)(Regs[dest] | Regs[src]);

			byte f = (byte)(ans & 0x28);                                        // F3, F5 (C=H=N=0)
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (TableParity[ans]) f |= 0x04;                                    // P

			Regs[dest] = ans;
			Regs[F] = f;
		}

		public void XOR8_Func(ushort dest, ushort src)
		{
			ushort ans = (ushort)(Regs[dest] ^ Regs[src]);

			byte f = (byte)(ans & 0x28);                                        // F3, F5 (C=H=N=0)
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (TableParity[ans]) f |= 0x04;                                    // P

			Regs[dest] = ans;
			Regs[F] = f;
		}

		public void CP8_Func(ushort dest, ushort src)
		{
			// CP is SUB with the result discarded; F3/F5 come from the SOURCE operand, not the result.
			int d = Regs[dest];
			int s = Regs[src];
			int diff = d - s;
			ushort ans = (ushort)(diff & 0xFF);

			byte f = (byte)((s & 0x28) | 0x02);                                 // F3, F5 from src; N
			if ((diff & 0x100) != 0) f |= 0x01;                                 // C
			if ((((d & 0xF) - (s & 0xF)) & 0x10) != 0) f |= 0x10;               // H
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (((d ^ s) & 0x80) != 0 && ((d ^ ans) & 0x80) != 0) f |= 0x04;    // P/V

			Regs[F] = f;
		}

		public void RRC_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			int v = Regs[src];
			byte f = (byte)(v & 0x01);                                          // C = old bit 0
			int r = ((v & 0x01) << 7) | (v >> 1);
			if (imm)
				f |= (byte)((Regs[F] & 0xC4) | (r & 0x28));                     // preserve S,Z,P; F3,F5 from result
			else
			{
				f |= (byte)(r & 0xA8);                                          // S, F5, F3
				if (r == 0) f |= 0x40;                                          // Z
				if (TableParity[r]) f |= 0x04;                                  // P
			}
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
		}

		public void RR_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			int fin = Regs[F];
			int v = Regs[src];
			byte f = (byte)(v & 0x01);                                          // new C = old bit 0
			int r = ((fin & 0x01) << 7) | (v >> 1);                             // shift in old carry
			if (imm)
				f |= (byte)((fin & 0xC4) | (r & 0x28));
			else
			{
				f |= (byte)(r & 0xA8);
				if (r == 0) f |= 0x40;
				if (TableParity[r]) f |= 0x04;
			}
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
		}

		public void RLC_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			int v = Regs[src];
			byte f = (byte)((v >> 7) & 0x01);                                   // C = old bit 7
			int r = ((v << 1) & 0xFF) | ((v >> 7) & 0x01);
			if (imm)
				f |= (byte)((Regs[F] & 0xC4) | (r & 0x28));
			else
			{
				f |= (byte)(r & 0xA8);
				if (r == 0) f |= 0x40;
				if (TableParity[r]) f |= 0x04;
			}
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
		}

		public void RL_Func(ushort src)
		{
			bool imm = src == Aim;
			if (imm) { src = A; }

			int fin = Regs[F];
			int v = Regs[src];
			byte f = (byte)((v >> 7) & 0x01);                                   // new C = old bit 7
			int r = ((v << 1) & 0xFF) | (fin & 0x01);                           // shift in old carry
			if (imm)
				f |= (byte)((fin & 0xC4) | (r & 0x28));
			else
			{
				f |= (byte)(r & 0xA8);
				if (r == 0) f |= 0x40;
				if (TableParity[r]) f |= 0x04;
			}
			// H = N = 0
			Regs[src] = (ushort)r;
			Regs[F] = f;
		}

		public void INC8_Func(ushort src)
		{
			// INC8 does NOT affect the carry flag — preserve it.
			int v = Regs[src];
			ushort ans = (ushort)((v + 1) & 0xFF);

			byte f = (byte)(Regs[F] & 0x01);                                    // preserve C
			f |= (byte)(ans & 0x28);                                            // F3, F5
			if ((((v & 0xF) + 1) & 0x10) != 0) f |= 0x10;                       // H
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (ans == 0x80) f |= 0x04;                                         // P/V (overflow into 0x80)
			// N = 0

			Regs[src] = ans;
			Regs[F] = f;
		}

		public void DEC8_Func(ushort src)
		{
			// DEC8 does NOT affect the carry flag — preserve it.
			int v = Regs[src];
			ushort ans = (ushort)((v - 1) & 0xFF);

			byte f = (byte)((Regs[F] & 0x01) | 0x02);                           // preserve C; N
			f |= (byte)(ans & 0x28);                                            // F3, F5
			if ((((v & 0xF) - 1) & 0x10) != 0) f |= 0x10;                       // H
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (ans == 0x7F) f |= 0x04;                                         // P/V

			Regs[src] = ans;
			Regs[F] = f;
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
			int d = Regs[dest];
			int s = Regs[src];
			int c = Regs[F] & 0x01;                                             // carry-in (read before F is overwritten)
			int sum = d + s + c;
			ushort ans = (ushort)(sum & 0xFF);

			byte f = (byte)(ans & 0x28);
			if ((sum & 0x100) != 0) f |= 0x01;                                  // C
			if ((((d & 0xF) + (s & 0xF) + c) & 0x10) != 0) f |= 0x10;           // H
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (((d ^ s) & 0x80) == 0 && ((d ^ ans) & 0x80) != 0) f |= 0x04;    // P/V
			// N = 0

			Regs[dest] = ans;
			Regs[F] = f;
		}

		public void SBC8_Func(ushort dest, ushort src)
		{
			int d = Regs[dest];
			int s = Regs[src];
			int c = Regs[F] & 0x01;                                             // carry-in
			int diff = d - s - c;
			ushort ans = (ushort)(diff & 0xFF);

			byte f = (byte)((ans & 0x28) | 0x02);                               // F3, F5, N
			if ((diff & 0x100) != 0) f |= 0x01;                                 // C
			if ((((d & 0xF) - (s & 0xF) - c) & 0x10) != 0) f |= 0x10;           // H
			if (ans == 0) f |= 0x40;                                            // Z
			if ((ans & 0x80) != 0) f |= 0x80;                                   // S
			if (((d ^ s) & 0x80) != 0 && ((d ^ ans) & 0x80) != 0) f |= 0x04;    // P/V

			Regs[dest] = ans;
			Regs[F] = f;
		}

		public void DA_Func(ushort src)
		{
			int fin = Regs[F];
			bool fN = (fin & 0x02) != 0;
			bool fH = (fin & 0x10) != 0;
			bool fC = (fin & 0x01) != 0;

			byte a = (byte)Regs[src];
			int temp = a;                                                       // int + final &0xFF matches byte wrap

			if (fN)
			{
				if (fH || ((a & 0x0F) > 0x09)) { temp -= 0x06; }
				if (fC || a > 0x99) { temp -= 0x60; }
			}
			else
			{
				if (fH || ((a & 0x0F) > 0x09)) { temp += 0x06; }
				if (fC || a > 0x99) { temp += 0x60; }
			}

			temp &= 0xFF;

			// DAA preserves N; accumulate the rest into F once.
			byte f = (byte)(fin & 0x02);                                        // preserve N
			if (fC || a > 0x99) f |= 0x01;                                      // C
			if (temp == 0) f |= 0x40;                                           // Z
			if (((a ^ temp) & 0x10) != 0) f |= 0x10;                            // H
			if (TableParity[temp]) f |= 0x04;                                   // P
			f |= (byte)(temp & 0xA8);                                           // S, F5, F3
			Regs[src] = (ushort)temp;
			Regs[F] = f;
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
			int c = Regs[F] & 0x01;

			int ans = Reg16_d - Reg16_s - c;

			// half carry: same computation as before, on a copy of the low 12 bits
			int hd = (Reg16_d & 0xFFF) - ((Reg16_s & 0xFFF) + c);

			byte f = 0x02;                                                      // N
			if (ans.Bit(16)) f |= 0x01;                                         // C
			if ((Reg16_d.Bit(15) != Reg16_s.Bit(15)) && (Reg16_d.Bit(15) != ans.Bit(15))) f |= 0x04;  // P/V
			if ((ushort)(ans & 0xFFFF) > 32767) f |= 0x80;                      // S
			if ((ans & 0xFFFF) == 0) f |= 0x40;                                 // Z
			if ((ans & 0x0800) != 0) f |= 0x08;                                // F3
			if ((ans & 0x2000) != 0) f |= 0x20;                                // F5
			if (hd.Bit(12)) f |= 0x10;                                          // H
			Regs[dest_l] = (ushort)(ans & 0xFF);
			Regs[dest_h] = (ushort)((ans >> 8) & 0xFF);
			Regs[F] = f;
		}

		public void ADC_16_Func(ushort dest_l, ushort dest_h, ushort src_l, ushort src_h)
		{
			int Reg16_d = Regs[dest_l] | (Regs[dest_h] << 8);
			int Reg16_s = Regs[src_l] | (Regs[src_h] << 8);

			int c = Regs[F] & 0x01;
			int ans = Reg16_d + Reg16_s + c;

			byte f = 0;                                                         // N = 0
			if (((Reg16_d & 0xFFF) + (Reg16_s & 0xFFF) + c) > 0xFFF) f |= 0x10; // H
			if (ans.Bit(16)) f |= 0x01;                                         // C
			if ((Reg16_d.Bit(15) == Reg16_s.Bit(15)) && (Reg16_d.Bit(15) != ans.Bit(15))) f |= 0x04;  // P/V
			if ((ans & 0xFFFF) > 32767) f |= 0x80;                              // S
			if ((ans & 0xFFFF) == 0) f |= 0x40;                                 // Z
			if ((ans & 0x0800) != 0) f |= 0x08;                                // F3
			if ((ans & 0x2000) != 0) f |= 0x20;                                // F5
			Regs[dest_l] = (ushort)(ans & 0xFF);
			Regs[dest_h] = (ushort)((ans >> 8) & 0xFF);
			Regs[F] = f;
		}

		public void NEG_8_Func(ushort src)
		{
			int s = Regs[src];
			ushort ans = (ushort)((-s) & 0xFF);

			byte f = 0x02;                                                      // N
			if (s != 0x00) f |= 0x01;                                           // C
			if (ans == 0) f |= 0x40;                                            // Z
			if (s == 0x80) f |= 0x04;                                           // P/V
			f |= (byte)(ans & 0xA8);                                            // S, F5, F3
			if (((-(s & 0xF)) & 0x10) != 0) f |= 0x10;                          // H
			Regs[src] = ans;
			Regs[F] = f;
		}

		public void RRD_Func(ushort dest, ushort src)
		{
			ushort temp1 = Regs[src];
			ushort temp2 = Regs[dest];
			Regs[dest] = (ushort)(((temp1 & 0x0F) << 4) + ((temp2 & 0xF0) >> 4));
			Regs[src] = (ushort)((temp1 & 0xF0) + (temp2 & 0x0F));

			int r = Regs[src];
			byte f = (byte)(Regs[F] & 0x01);                                    // preserve C
			f |= (byte)(r & 0xA8);                                              // S, F5, F3
			if (r == 0) f |= 0x40;                                              // Z
			if (TableParity[r]) f |= 0x04;                                      // P
			// H = N = 0
			Regs[F] = f;
		}

		public void RLD_Func(ushort dest, ushort src)
		{
			ushort temp1 = Regs[src];
			ushort temp2 = Regs[dest];
			Regs[dest] = (ushort)((temp1 & 0x0F) + ((temp2 & 0x0F) << 4));
			Regs[src] = (ushort)((temp1 & 0xF0) + ((temp2 & 0xF0) >> 4));

			int r = Regs[src];
			byte f = (byte)(Regs[F] & 0x01);                                    // preserve C
			f |= (byte)(r & 0xA8);                                              // S, F5, F3
			if (r == 0) f |= 0x40;                                              // Z
			if (TableParity[r]) f |= 0x04;                                      // P
			// H = N = 0
			Regs[F] = f;
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
