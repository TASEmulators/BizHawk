using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace Jellyfish.Virtu
{
    public sealed partial class Cpu : MachineComponent
    {
        public Cpu(Machine machine) : 
            base(machine)
        {
            ExecuteOpCode65N02 = new Action[OpCodeCount]
            {
                Execute65X02Brk00, Execute65X02Ora01, Execute65N02Nop02, Execute65N02Nop03, 
                Execute65N02Nop04, Execute65X02Ora05, Execute65X02Asl06, Execute65N02Nop07, 
                Execute65X02Php08, Execute65X02Ora09, Execute65X02Asl0A, Execute65N02Nop0B, 
                Execute65N02Nop0C, Execute65X02Ora0D, Execute65X02Asl0E, Execute65N02Nop0F, 
                Execute65X02Bpl10, Execute65X02Ora11, Execute65N02Nop12, Execute65N02Nop13, 
                Execute65N02Nop14, Execute65X02Ora15, Execute65X02Asl16, Execute65N02Nop17, 
                Execute65X02Clc18, Execute65X02Ora19, Execute65N02Nop1A, Execute65N02Nop1B, 
                Execute65N02Nop1C, Execute65X02Ora1D, Execute65N02Asl1E, Execute65N02Nop1F, 
                Execute65X02Jsr20, Execute65X02And21, Execute65N02Nop22, Execute65N02Nop23, 
                Execute65X02Bit24, Execute65X02And25, Execute65X02Rol26, Execute65N02Nop27, 
                Execute65X02Plp28, Execute65X02And29, Execute65X02Rol2A, Execute65N02Nop2B, 
                Execute65X02Bit2C, Execute65X02And2D, Execute65X02Rol2E, Execute65N02Nop2F, 
                Execute65X02Bmi30, Execute65X02And31, Execute65N02Nop32, Execute65N02Nop33, 
                Execute65N02Nop34, Execute65X02And35, Execute65X02Rol36, Execute65N02Nop37, 
                Execute65X02Sec38, Execute65X02And39, Execute65N02Nop3A, Execute65N02Nop3B, 
                Execute65N02Nop3C, Execute65X02And3D, Execute65N02Rol3E, Execute65N02Nop3F, 
                Execute65X02Rti40, Execute65X02Eor41, Execute65N02Nop42, Execute65N02Nop43, 
                Execute65N02Nop44, Execute65X02Eor45, Execute65X02Lsr46, Execute65N02Nop47, 
                Execute65X02Pha48, Execute65X02Eor49, Execute65X02Lsr4A, Execute65N02Nop4B, 
                Execute65X02Jmp4C, Execute65X02Eor4D, Execute65X02Lsr4E, Execute65N02Nop4F, 
                Execute65X02Bvc50, Execute65X02Eor51, Execute65N02Nop52, Execute65N02Nop53, 
                Execute65N02Nop54, Execute65X02Eor55, Execute65X02Lsr56, Execute65N02Nop57, 
                Execute65X02Cli58, Execute65X02Eor59, Execute65N02Nop5A, Execute65N02Nop5B, 
                Execute65N02Nop5C, Execute65X02Eor5D, Execute65N02Lsr5E, Execute65N02Nop5F, 
                Execute65X02Rts60, Execute65N02Adc61, Execute65N02Nop62, Execute65N02Nop63, 
                Execute65N02Nop64, Execute65N02Adc65, Execute65X02Ror66, Execute65N02Nop67, 
                Execute65X02Pla68, Execute65N02Adc69, Execute65X02Ror6A, Execute65N02Nop6B, 
                Execute65N02Jmp6C, Execute65N02Adc6D, Execute65X02Ror6E, Execute65N02Nop6F, 
                Execute65X02Bvs70, Execute65N02Adc71, Execute65N02Nop72, Execute65N02Nop73, 
                Execute65N02Nop74, Execute65N02Adc75, Execute65X02Ror76, Execute65N02Nop77, 
                Execute65X02Sei78, Execute65N02Adc79, Execute65N02Nop7A, Execute65N02Nop7B, 
                Execute65N02Nop7C, Execute65N02Adc7D, Execute65N02Ror7E, Execute65N02Nop7F, 
                Execute65N02Nop80, Execute65X02Sta81, Execute65N02Nop82, Execute65N02Nop83, 
                Execute65X02Sty84, Execute65X02Sta85, Execute65X02Stx86, Execute65N02Nop87, 
                Execute65X02Dey88, Execute65N02Nop89, Execute65X02Txa8A, Execute65N02Nop8B, 
                Execute65X02Sty8C, Execute65X02Sta8D, Execute65X02Stx8E, Execute65N02Nop8F, 
                Execute65X02Bcc90, Execute65X02Sta91, Execute65N02Nop92, Execute65N02Nop93, 
                Execute65X02Sty94, Execute65X02Sta95, Execute65X02Stx96, Execute65N02Nop97, 
                Execute65X02Tya98, Execute65X02Sta99, Execute65X02Txs9A, Execute65N02Nop9B, 
                Execute65N02Nop9C, Execute65X02Sta9D, Execute65N02Nop9E, Execute65N02Nop9F, 
                Execute65X02LdyA0, Execute65X02LdaA1, Execute65X02LdxA2, Execute65N02NopA3, 
                Execute65X02LdyA4, Execute65X02LdaA5, Execute65X02LdxA6, Execute65N02NopA7, 
                Execute65X02TayA8, Execute65X02LdaA9, Execute65X02TaxAA, Execute65N02NopAB, 
                Execute65X02LdyAC, Execute65X02LdaAD, Execute65X02LdxAE, Execute65N02NopAF, 
                Execute65X02BcsB0, Execute65X02LdaB1, Execute65N02NopB2, Execute65N02NopB3, 
                Execute65X02LdyB4, Execute65X02LdaB5, Execute65X02LdxB6, Execute65N02NopB7, 
                Execute65X02ClvB8, Execute65X02LdaB9, Execute65X02TsxBA, Execute65N02NopBB, 
                Execute65X02LdyBC, Execute65X02LdaBD, Execute65X02LdxBE, Execute65N02NopBF, 
                Execute65X02CpyC0, Execute65X02CmpC1, Execute65N02NopC2, Execute65N02NopC3, 
                Execute65X02CpyC4, Execute65X02CmpC5, Execute65X02DecC6, Execute65N02NopC7, 
                Execute65X02InyC8, Execute65X02CmpC9, Execute65X02DexCA, Execute65N02NopCB, 
                Execute65X02CpyCC, Execute65X02CmpCD, Execute65X02DecCE, Execute65N02NopCF, 
                Execute65X02BneD0, Execute65X02CmpD1, Execute65N02NopD2, Execute65N02NopD3, 
                Execute65N02NopD4, Execute65X02CmpD5, Execute65X02DecD6, Execute65N02NopD7, 
                Execute65X02CldD8, Execute65X02CmpD9, Execute65N02NopDA, Execute65N02NopDB, 
                Execute65N02NopDC, Execute65X02CmpDD, Execute65N02DecDE, Execute65N02NopDF, 
                Execute65X02CpxE0, Execute65N02SbcE1, Execute65N02NopE2, Execute65N02NopE3, 
                Execute65X02CpxE4, Execute65N02SbcE5, Execute65X02IncE6, Execute65N02NopE7, 
                Execute65X02InxE8, Execute65N02SbcE9, Execute65X02NopEA, Execute65N02NopEB, 
                Execute65X02CpxEC, Execute65N02SbcED, Execute65X02IncEE, Execute65N02NopEF, 
                Execute65X02BeqF0, Execute65N02SbcF1, Execute65N02NopF2, Execute65N02NopF3, 
                Execute65N02NopF4, Execute65N02SbcF5, Execute65X02IncF6, Execute65N02NopF7, 
                Execute65X02SedF8, Execute65N02SbcF9, Execute65N02NopFA, Execute65N02NopFB, 
                Execute65N02NopFC, Execute65N02SbcFD, Execute65N02IncFE, Execute65N02NopFF
            };

            ExecuteOpCode65C02 = new Action[OpCodeCount]
            {
                Execute65X02Brk00, Execute65X02Ora01, Execute65C02Nop02, Execute65C02Nop03, 
                Execute65C02Tsb04, Execute65X02Ora05, Execute65X02Asl06, Execute65C02Nop07, 
                Execute65X02Php08, Execute65X02Ora09, Execute65X02Asl0A, Execute65C02Nop0B, 
                Execute65C02Tsb0C, Execute65X02Ora0D, Execute65X02Asl0E, Execute65C02Nop0F, 
                Execute65X02Bpl10, Execute65X02Ora11, Execute65C02Ora12, Execute65C02Nop13, 
                Execute65C02Trb14, Execute65X02Ora15, Execute65X02Asl16, Execute65C02Nop17, 
                Execute65X02Clc18, Execute65X02Ora19, Execute65C02Ina1A, Execute65C02Nop1B, 
                Execute65C02Trb1C, Execute65X02Ora1D, Execute65C02Asl1E, Execute65C02Nop1F, 
                Execute65X02Jsr20, Execute65X02And21, Execute65C02Nop22, Execute65C02Nop23, 
                Execute65X02Bit24, Execute65X02And25, Execute65X02Rol26, Execute65C02Nop27, 
                Execute65X02Plp28, Execute65X02And29, Execute65X02Rol2A, Execute65C02Nop2B, 
                Execute65X02Bit2C, Execute65X02And2D, Execute65X02Rol2E, Execute65C02Nop2F, 
                Execute65X02Bmi30, Execute65X02And31, Execute65C02And32, Execute65C02Nop33, 
                Execute65C02Bit34, Execute65X02And35, Execute65X02Rol36, Execute65C02Nop37, 
                Execute65X02Sec38, Execute65X02And39, Execute65C02Dea3A, Execute65C02Nop3B, 
                Execute65C02Bit3C, Execute65X02And3D, Execute65C02Rol3E, Execute65C02Nop3F, 
                Execute65X02Rti40, Execute65X02Eor41, Execute65C02Nop42, Execute65C02Nop43, 
                Execute65C02Nop44, Execute65X02Eor45, Execute65X02Lsr46, Execute65C02Nop47, 
                Execute65X02Pha48, Execute65X02Eor49, Execute65X02Lsr4A, Execute65C02Nop4B, 
                Execute65X02Jmp4C, Execute65X02Eor4D, Execute65X02Lsr4E, Execute65C02Nop4F, 
                Execute65X02Bvc50, Execute65X02Eor51, Execute65C02Eor52, Execute65C02Nop53, 
                Execute65C02Nop54, Execute65X02Eor55, Execute65X02Lsr56, Execute65C02Nop57, 
                Execute65X02Cli58, Execute65X02Eor59, Execute65C02Phy5A, Execute65C02Nop5B, 
                Execute65C02Nop5C, Execute65X02Eor5D, Execute65C02Lsr5E, Execute65C02Nop5F, 
                Execute65X02Rts60, Execute65C02Adc61, Execute65C02Nop62, Execute65C02Nop63, 
                Execute65C02Stz64, Execute65C02Adc65, Execute65X02Ror66, Execute65C02Nop67, 
                Execute65X02Pla68, Execute65C02Adc69, Execute65X02Ror6A, Execute65C02Nop6B, 
                Execute65C02Jmp6C, Execute65C02Adc6D, Execute65X02Ror6E, Execute65C02Nop6F, 
                Execute65X02Bvs70, Execute65C02Adc71, Execute65C02Adc72, Execute65C02Nop73, 
                Execute65C02Stz74, Execute65C02Adc75, Execute65X02Ror76, Execute65C02Nop77, 
                Execute65X02Sei78, Execute65C02Adc79, Execute65C02Ply7A, Execute65C02Nop7B, 
                Execute65C02Jmp7C, Execute65C02Adc7D, Execute65C02Ror7E, Execute65C02Nop7F, 
                Execute65C02Bra80, Execute65X02Sta81, Execute65C02Nop82, Execute65C02Nop83, 
                Execute65X02Sty84, Execute65X02Sta85, Execute65X02Stx86, Execute65C02Nop87, 
                Execute65X02Dey88, Execute65C02Bit89, Execute65X02Txa8A, Execute65C02Nop8B, 
                Execute65X02Sty8C, Execute65X02Sta8D, Execute65X02Stx8E, Execute65C02Nop8F, 
                Execute65X02Bcc90, Execute65X02Sta91, Execute65C02Sta92, Execute65C02Nop93, 
                Execute65X02Sty94, Execute65X02Sta95, Execute65X02Stx96, Execute65C02Nop97, 
                Execute65X02Tya98, Execute65X02Sta99, Execute65X02Txs9A, Execute65C02Nop9B, 
                Execute65C02Stz9C, Execute65X02Sta9D, Execute65C02Stz9E, Execute65C02Nop9F, 
                Execute65X02LdyA0, Execute65X02LdaA1, Execute65X02LdxA2, Execute65C02NopA3, 
                Execute65X02LdyA4, Execute65X02LdaA5, Execute65X02LdxA6, Execute65C02NopA7, 
                Execute65X02TayA8, Execute65X02LdaA9, Execute65X02TaxAA, Execute65C02NopAB, 
                Execute65X02LdyAC, Execute65X02LdaAD, Execute65X02LdxAE, Execute65C02NopAF, 
                Execute65X02BcsB0, Execute65X02LdaB1, Execute65C02LdaB2, Execute65C02NopB3, 
                Execute65X02LdyB4, Execute65X02LdaB5, Execute65X02LdxB6, Execute65C02NopB7, 
                Execute65X02ClvB8, Execute65X02LdaB9, Execute65X02TsxBA, Execute65C02NopBB, 
                Execute65X02LdyBC, Execute65X02LdaBD, Execute65X02LdxBE, Execute65C02NopBF, 
                Execute65X02CpyC0, Execute65X02CmpC1, Execute65C02NopC2, Execute65C02NopC3, 
                Execute65X02CpyC4, Execute65X02CmpC5, Execute65X02DecC6, Execute65C02NopC7, 
                Execute65X02InyC8, Execute65X02CmpC9, Execute65X02DexCA, Execute65C02NopCB, 
                Execute65X02CpyCC, Execute65X02CmpCD, Execute65X02DecCE, Execute65C02NopCF, 
                Execute65X02BneD0, Execute65X02CmpD1, Execute65C02CmpD2, Execute65C02NopD3, 
                Execute65C02NopD4, Execute65X02CmpD5, Execute65X02DecD6, Execute65C02NopD7, 
                Execute65X02CldD8, Execute65X02CmpD9, Execute65C02PhxDA, Execute65C02NopDB, 
                Execute65C02NopDC, Execute65X02CmpDD, Execute65C02DecDE, Execute65C02NopDF, 
                Execute65X02CpxE0, Execute65C02SbcE1, Execute65C02NopE2, Execute65C02NopE3, 
                Execute65X02CpxE4, Execute65C02SbcE5, Execute65X02IncE6, Execute65C02NopE7, 
                Execute65X02InxE8, Execute65C02SbcE9, Execute65X02NopEA, Execute65C02NopEB, 
                Execute65X02CpxEC, Execute65C02SbcED, Execute65X02IncEE, Execute65C02NopEF, 
                Execute65X02BeqF0, Execute65C02SbcF1, Execute65C02SbcF2, Execute65C02NopF3, 
                Execute65C02NopF4, Execute65C02SbcF5, Execute65X02IncF6, Execute65C02NopF7, 
                Execute65X02SedF8, Execute65C02SbcF9, Execute65C02PlxFA, Execute65C02NopFB, 
                Execute65C02NopFC, Execute65C02SbcFD, Execute65C02IncFE, Execute65C02NopFF
            };
        }

        public override void Initialize()
        {
            _memory = Machine.Memory;

            Is65C02 = true;
            IsThrottled = false;
            Multiplier = 1;

            RS = 0xFF;
        }

        public override void Reset()
        {
            RS = (RS - 3) & 0xFF; // [4-14]
            RPC = _memory.ReadRomRegionE0FF(0xFFFC) | (_memory.ReadRomRegionE0FF(0xFFFD) << 8);
            RP |= (PB | PI);
            if (Is65C02) // [C-10]
            {
                RP &= ~PD;
            }
        }

        public override void LoadState(BinaryReader reader, Version version)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            Is65C02 = reader.ReadBoolean();
            IsThrottled = reader.ReadBoolean();
            Multiplier = reader.ReadInt32();

            RA = reader.ReadInt32();
            RX = reader.ReadInt32();
            RY = reader.ReadInt32();
            RS = reader.ReadInt32();
            RP = reader.ReadInt32();
            RPC = reader.ReadInt32();
        }

        public override void SaveState(BinaryWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            writer.Write(Is65C02);
            writer.Write(IsThrottled);
            writer.Write(Multiplier);

            writer.Write(RA);
            writer.Write(RX);
            writer.Write(RY);
            writer.Write(RS);
            writer.Write(RP);
            writer.Write(RPC);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "A = 0x{0:X2} X = 0x{1:X2} Y = 0x{2:X2} P = 0x{3:X2} S = 0x01{4:X2} PC = 0x{5:X4} EA = 0x{6:X4} CC = {7}", 
                RA, RX, RY, RP, RS, RPC, EA, CC);
        }

        public int Execute()
        {
            CC = 0;
            OpCode = _memory.Read(RPC);
            RPC = (RPC + 1) & 0xFFFF;
            _executeOpCode[OpCode]();
            Cycles += CC;

            return CC;
        }

        #region Core Operand Actions
        private void GetAddressAbs() // abs
        {
            EA = _memory.Read(RPC) | (_memory.Read(RPC + 1) << 8);
            RPC = (RPC + 2) & 0xFFFF;
        }

        private void GetAddressAbsX() // abs, x
        {
            EA = (_memory.Read(RPC) + RX + (_memory.Read(RPC + 1) << 8)) & 0xFFFF;
            RPC = (RPC + 2) & 0xFFFF;
        }

        private void GetAddressAbsXCC() // abs, x
        {
            int ea = _memory.Read(RPC) + RX;
            EA = (ea + (_memory.Read(RPC + 1) << 8)) & 0xFFFF;
            RPC = (RPC + 2) & 0xFFFF;
            CC += (ea >> 8);
        }

        private void GetAddressAbsY() // abs, y
        {
            EA = (_memory.Read(RPC) + RY + (_memory.Read(RPC + 1) << 8)) & 0xFFFF;
            RPC = (RPC + 2) & 0xFFFF;
        }

        private void GetAddressAbsYCC() // abs, y
        {
            int ea = _memory.Read(RPC) + RY;
            EA = (ea + (_memory.Read(RPC + 1) << 8)) & 0xFFFF;
            RPC = (RPC + 2) & 0xFFFF;
            CC += (ea >> 8);
        }

        private void GetAddressZpg() // zpg
        {
            EA = _memory.Read(RPC);
            RPC = (RPC + 1) & 0xFFFF;
        }

        private void GetAddressZpgInd() // (zpg)
        {
            int zp = _memory.Read(RPC);
            EA = _memory.ReadZeroPage(zp) | (_memory.ReadZeroPage((zp + 1) & 0xFF) << 8);
            RPC = (RPC + 1) & 0xFFFF;
        }

        private void GetAddressZpgIndX() // (zpg, x)
        {
            int zp = (_memory.Read(RPC) + RX) & 0xFF;
            EA = _memory.ReadZeroPage(zp) | (_memory.ReadZeroPage((zp + 1) & 0xFF) << 8);
            RPC = (RPC + 1) & 0xFFFF;
        }

        private void GetAddressZpgIndY() // (zpg), y
        {
            int zp = _memory.Read(RPC);
            EA = (_memory.ReadZeroPage(zp) + RY + (_memory.ReadZeroPage((zp + 1) & 0xFF) << 8)) & 0xFFFF;
            RPC = (RPC + 1) & 0xFFFF;
        }

        private void GetAddressZpgIndYCC() // (zpg), y
        {
            int zp = _memory.Read(RPC);
            int ea = _memory.ReadZeroPage(zp) + RY;
            EA = (ea + (_memory.ReadZeroPage((zp + 1) & 0xFF) << 8)) & 0xFFFF;
            RPC = (RPC + 1) & 0xFFFF;
            CC += (ea >> 8);
        }

        private void GetAddressZpgX() // zpg, x
        {
            EA = (_memory.Read(RPC) + RX) & 0xFF;
            RPC = (RPC + 1) & 0xFFFF;
        }

        private void GetAddressZpgY() // zpg, y
        {
            EA = (_memory.Read(RPC) + RY) & 0xFF;
            RPC = (RPC + 1) & 0xFFFF;
        }

        private int Pull()
        {
            RS = (RS + 1) & 0xFF;

            return _memory.ReadZeroPage(0x0100 + RS);
        }

        private void Push(int data)
        {
            _memory.WriteZeroPage(0x0100 + RS, data);
            RS = (RS - 1) & 0xFF;
        }

        private int ReadAbs() // abs
        {
            return _memory.Read(EA);
        }

        private int ReadAbsX() // abs, x
        {
            return _memory.Read(EA);
        }

        private int ReadAbsY() // abs, y
        {
            return _memory.Read(EA);
        }

        private int ReadImm() // imm
        {
            int data = _memory.Read(RPC);
            RPC = (RPC + 1) & 0xFFFF;

            return data;
        }

        private int ReadZpg() // zpg
        {
            return _memory.ReadZeroPage(EA);
        }

        private int ReadZpgInd() // (zpg)
        {
            return _memory.Read(EA);
        }

        private int ReadZpgIndX() // (zpg, x)
        {
            return _memory.Read(EA);
        }

        private int ReadZpgIndY() // (zpg), y
        {
            return _memory.Read(EA);
        }

        private int ReadZpgX() // zpg, x
        {
            return _memory.ReadZeroPage(EA);
        }

        private int ReadZpgY() // zpg, y
        {
            return _memory.ReadZeroPage(EA);
        }

        private void WriteAbs(int data) // abs
        {
            _memory.Write(EA, data);
        }

        private void WriteAbsX(int data) // abs, x
        {
            _memory.Write(EA, data);
        }

        private void WriteAbsY(int data) // abs, y
        {
            _memory.Write(EA, data);
        }

        private void WriteZpg(int data) // zpg
        {
            _memory.WriteZeroPage(EA, data);
        }

        private void WriteZpgInd(int data) // (zpg)
        {
            _memory.Write(EA, data);
        }

        private void WriteZpgIndX(int data) // (zpg, x)
        {
            _memory.Write(EA, data);
        }

        private void WriteZpgIndY(int data) // (zpg), y
        {
            _memory.Write(EA, data);
        }

        private void WriteZpgX(int data) // zpg, x
        {
            _memory.WriteZeroPage(EA, data);
        }

        private void WriteZpgY(int data) // zpg, y
        {
            _memory.WriteZeroPage(EA, data);
        }
        #endregion

        #region Core OpCode Actions
        private void ExecuteAdc65N02(int data, int cc)
        {
            if ((RP & PD) == 0x0)
            {
                int ra = RA + data + (RP & PC);
                RP = RP & ~(PC | PN | PV | PZ) | ((ra >> 8) & PC) | DataPNZ[ra & 0xFF] | (((~(RA ^ data) & (RA ^ (ra & 0xFF))) >> 1) & PV);
                RA = ra & 0xFF;
                CC += cc;
            }
            else // decimal
            {
                int ral = (RA & 0x0F) + (data & 0x0F) + (RP & PC);
                int rah = (RA >> 4) + (data >> 4);
                if (ral >= 10)
                {
                    ral -= 10;
                    rah++;
                }
                int ra = (ral | (rah << 4)) & 0xFF;
                RP = RP & ~(PC | PN | PV | PZ) | DataPN[ra] | (((~(RA ^ data) & (RA ^ ra)) >> 1) & PV) | DataPZ[(RA + data + (RP & PC)) & 0xFF];
                if (rah >= 10)
                {
                    rah -= 10;
                    RP |= PC;
                }
                RA = (ral | (rah << 4)) & 0xFF;
                CC += cc;
            }
        }

        private void ExecuteAdc65C02(int data, int cc)
        {
            if ((RP & PD) == 0x0)
            {
                int ra = RA + data + (RP & PC);
                RP = RP & ~(PC | PN | PV | PZ) | ((ra >> 8) & PC) | DataPNZ[ra & 0xFF] | (((~(RA ^ data) & (RA ^ (ra & 0xFF))) >> 1) & PV);
                RA = ra & 0xFF;
                CC += cc;
            }
            else // decimal
            {
                int ral = (RA & 0x0F) + (data & 0x0F) + (RP & PC);
                int rah = (RA >> 4) + (data >> 4);
                if (ral >= 10)
                {
                    ral -= 10;
                    rah++;
                }
                RP &= ~PC;
                if (rah >= 10)
                {
                    rah -= 10;
                    RP |= PC;
                }
                int ra = (ral | (rah << 4)) & 0xFF;
                RP = RP & ~(PN | PV | PZ) | DataPNZ[ra] | (((~(RA ^ data) & (RA ^ ra)) >> 1) & PV);
                RA = ra;
                CC += cc + 1;
            }
        }

        private void ExecuteAnd(int data, int cc)
        {
            RA &= data;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private int ExecuteAsl(int data, int cc)
        {
            RP = RP & ~PC | ((data >> 7) & PC);
            data = (data << 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[data];
            CC += cc;

            return data;
        }

        private void ExecuteAslImp(int cc)
        {
            RP = RP & ~PC | ((RA >> 7) & PC);
            RA = (RA << 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private void ExecuteBcc(int cc)
        {
            if ((RP & PC) == 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteBcs(int cc)
        {
            if ((RP & PC) != 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteBeq(int cc)
        {
            if ((RP & PZ) != 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteBit(int data, int cc)
        {
            RP = RP & ~(PN | PV | PZ) | (data & (PN | PV)) | DataPZ[RA & data];
            CC += cc;
        }

        private void ExecuteBitImm(int data, int cc)
        {
            RP = RP & ~PZ | DataPZ[RA & data];
            CC += cc;
        }

        private void ExecuteBmi(int cc)
        {
            if ((RP & PN) != 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteBne(int cc)
        {
            if ((RP & PZ) == 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteBpl(int cc)
        {
            if ((RP & PN) == 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteBra(int cc)
        {
            int rpc = (RPC + 1) & 0xFFFF;
            RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
            CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
        }

        private void ExecuteBrk(int cc)
        {
            int rpc = (RPC + 1) & 0xFFFF; // [4-18]
            Push(rpc >> 8);
            Push(rpc & 0xFF);
            Push(RP | PB);
            RP |= PI;
            RPC = _memory.Read(0xFFFE) | (_memory.Read(0xFFFF) << 8);
            CC += cc;
        }

        private void ExecuteBvc(int cc)
        {
            if ((RP & PV) == 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteBvs(int cc)
        {
            if ((RP & PV) != 0x0)
            {
                int rpc = (RPC + 1) & 0xFFFF;
                RPC = (RPC + 1 + (sbyte)_memory.Read(RPC)) & 0xFFFF;
                CC += cc + 1 + (((RPC ^ rpc) >> 8) & 0x01);
            }
            else
            {
                RPC = (RPC + 1) & 0xFFFF;
                CC += cc;
            }
        }

        private void ExecuteClc(int cc)
        {
            RP &= ~PC;
            CC += cc;
        }

        private void ExecuteCld(int cc)
        {
            RP &= ~PD;
            CC += cc;
        }

        private void ExecuteCli(int cc)
        {
            RP &= ~PI;
            CC += cc;
        }

        private void ExecuteClv(int cc)
        {
            RP &= ~PV;
            CC += cc;
        }

        private void ExecuteCmp(int data, int cc)
        {
            int diff = RA - data;
            RP = RP & ~(PC | PN | PZ) | ((~diff >> 8) & PC) | DataPNZ[diff & 0xFF];
            CC += cc;
        }

        private void ExecuteCpx(int data, int cc)
        {
            int diff = RX - data;
            RP = RP & ~(PC | PN | PZ) | ((~diff >> 8) & PC) | DataPNZ[diff & 0xFF];
            CC += cc;
        }

        private void ExecuteCpy(int data, int cc)
        {
            int diff = RY - data;
            RP = RP & ~(PC | PN | PZ) | ((~diff >> 8) & PC) | DataPNZ[diff & 0xFF];
            CC += cc;
        }

        private void ExecuteDea(int cc)
        {
            RA = (RA - 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private int ExecuteDec(int data, int cc)
        {
            data = (data - 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[data];
            CC += cc;

            return data;
        }

        private void ExecuteDex(int cc)
        {
            RX = (RX - 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RX];
            CC += cc;
        }

        private void ExecuteDey(int cc)
        {
            RY = (RY - 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RY];
            CC += cc;
        }

        private void ExecuteEor(int data, int cc)
        {
            RA ^= data;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private void ExecuteIna(int cc)
        {
            RA = (RA + 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private int ExecuteInc(int data, int cc)
        {
            data = (data + 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[data];
            CC += cc;

            return data;
        }

        private void ExecuteInx(int cc)
        {
            RX = (RX + 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RX];
            CC += cc;
        }

        private void ExecuteIny(int cc)
        {
            RY = (RY + 1) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RY];
            CC += cc;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void ExecuteIrq(int cc)
        {
            Push(RPC >> 8);
            Push(RPC & 0xFF);
            Push(RP & ~PB);
            RP |= PI;
            if (Is65C02) // [C-10]
            {
                RP &= ~PD;
            }
            RPC = _memory.Read(0xFFFE) | (_memory.Read(0xFFFF) << 8);
            CC += cc;
        }

        private void ExecuteJmpAbs(int cc) // jmp abs
        {
            RPC = _memory.Read(RPC) | (_memory.Read(RPC + 1) << 8);
            CC += cc;
        }

        private void ExecuteJmpAbsInd65N02(int cc) // jmp (abs)
        {
            int ea = _memory.Read(RPC) | (_memory.Read(RPC + 1) << 8);
            RPC = _memory.Read(ea) | (_memory.Read((ea & 0xFF00) | ((ea + 1) & 0x00FF)) << 8);
            CC += cc;
        }

        private void ExecuteJmpAbsInd65C02(int cc) // jmp (abs)
        {
            int ea = _memory.Read(RPC) | (_memory.Read(RPC + 1) << 8);
            RPC = _memory.Read(ea) | (_memory.Read(ea + 1) << 8);
            CC += cc;
        }

        private void ExecuteJmpAbsIndX(int cc) // jmp (abs, x)
        {
            int ea = (_memory.Read(RPC) + RX + (_memory.Read(RPC + 1) << 8)) & 0xFFFF;
            RPC = _memory.Read(ea) | (_memory.Read(ea + 1) << 8);
            CC += cc;
        }

        private void ExecuteJsr(int cc) // jsr abs
        {
            int rpc = (RPC + 1) & 0xFFFF;
            RPC = _memory.Read(RPC) | (_memory.Read(RPC + 1) << 8);
            Push(rpc >> 8);
            Push(rpc & 0xFF);
            CC += cc;
        }

        private void ExecuteLda(int data, int cc)
        {
            RA = data;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private void ExecuteLdx(int data, int cc)
        {
            RX = data;
            RP = RP & ~(PN | PZ) | DataPNZ[RX];
            CC += cc;
        }

        private void ExecuteLdy(int data, int cc)
        {
            RY = data;
            RP = RP & ~(PN | PZ) | DataPNZ[RY];
            CC += cc;
        }

        private int ExecuteLsr(int data, int cc)
        {
            RP = RP & ~PC | (data & PC);
            data >>= 1;
            RP = RP & ~(PN | PZ) | DataPNZ[data];
            CC += cc;

            return data;
        }

        private void ExecuteLsrImp(int cc)
        {
            RP = RP & ~PC | (RA & PC);
            RA >>= 1;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private void ExecuteNmi(int cc)
        {
            Push(RPC >> 8);
            Push(RPC & 0xFF);
            Push(RP & ~PB);
            RP |= PI;
            if (Is65C02) // [C-10]
            {
                RP &= ~PD;
            }
            RPC = _memory.Read(0xFFFA) | (_memory.Read(0xFFFB) << 8);
            CC += cc;
        }

        private void ExecuteNop(int cc)
        {
            CC += cc;
        }

        private void ExecuteNop(int data, int cc)
        {
            RPC = (RPC + data) & 0xFFFF;
            CC += cc;
        }

        private void ExecuteOra(int data, int cc)
        {
            RA |= data;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private void ExecutePha(int cc)
        {
            Push(RA);
            CC += cc;
        }

        private void ExecutePhp(int cc)
        {
            Push(RP | PB); // [4-18]
            CC += cc;
        }

        private void ExecutePhx(int cc)
        {
            Push(RX);
            CC += cc;
        }

        private void ExecutePhy(int cc)
        {
            Push(RY);
            CC += cc;
        }

        private void ExecutePla(int cc)
        {
            RA = Pull();
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private void ExecutePlp(int cc)
        {
            RP = Pull();
            CC += cc;
        }

        private void ExecutePlx(int cc)
        {
            RX = Pull();
            RP = RP & ~(PN | PZ) | DataPNZ[RX];
            CC += cc;
        }

        private void ExecutePly(int cc)
        {
            RY = Pull();
            RP = RP & ~(PN | PZ) | DataPNZ[RY];
            CC += cc;
        }

        private int ExecuteRol(int data, int cc)
        {
            int c = RP & PC;
            RP = RP & ~PC | ((data >> 7) & PC);
            data = ((data << 1) | c) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[data];
            CC += cc;

            return data;
        }

        private void ExecuteRolImp(int cc)
        {
            int c = RP & PC;
            RP = RP & ~PC | ((RA >> 7) & PC);
            RA = ((RA << 1) | c) & 0xFF;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private int ExecuteRor(int data, int cc)
        {
            int c = RP & PC;
            RP = RP & ~PC | (data & PC);
            data = (c << 7) | (data >> 1);
            RP = RP & ~(PN | PZ) | DataPNZ[data];
            CC += cc;

            return data;
        }

        private void ExecuteRorImp(int cc)
        {
            int c = RP & PC;
            RP = RP & ~PC | (RA & PC);
            RA = (c << 7) | (RA >> 1);
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private void ExecuteRti(int cc)
        {
            RP = Pull();
            int rpc = Pull();
            RPC = rpc | (Pull() << 8);
            CC += cc;
        }

        private void ExecuteRts(int cc)
        {
            int rpc = Pull();
            RPC = (rpc + 1 + (Pull() << 8)) & 0xFFFF;
            CC += cc;
        }

        private void ExecuteSbc65N02(int data, int cc)
        {
            if ((RP & PD) == 0x0)
            {
                int ra = RA - data - (~RP & PC);
                RP = RP & ~(PC | PN | PV | PZ) | ((~ra >> 8) & PC) | DataPNZ[ra & 0xFF] | ((((RA ^ data) & (RA ^ (ra & 0xFF))) >> 1) & PV);
                RA = ra & 0xFF;
                CC += cc;
            }
            else // decimal
            {
                int ral = (RA & 0x0F) - (data & 0x0F) - (~RP & PC);
                int rah = (RA >> 4) - (data >> 4);
                if (ral < 0)
                {
                    ral += 10;
                    rah--;
                }
                int ra = (ral | (rah << 4)) & 0xFF;
                RP = RP & ~(PN | PV | PZ) | PC | DataPN[ra] | ((((RA ^ data) & (RA ^ ra)) >> 1) & PV) | DataPZ[(RA - data - (~RP & PC)) & 0xFF];
                if (rah < 0)
                {
                    rah += 10;
                    RP &= ~PC;
                }
                RA = (ral | (rah << 4)) & 0xFF;
                CC += cc;
            }
        }

        private void ExecuteSbc65C02(int data, int cc)
        {
            if ((RP & PD) == 0x0)
            {
                int ra = RA - data - (~RP & PC);
                RP = RP & ~(PC | PN | PV | PZ) | ((~ra >> 8) & PC) | DataPNZ[ra & 0xFF] | ((((RA ^ data) & (RA ^ (ra & 0xFF))) >> 1) & PV);
                RA = ra & 0xFF;
                CC += cc;
            }
            else // decimal
            {
                int ral = (RA & 0x0F) - (data & 0x0F) - (~RP & PC);
                int rah = (RA >> 4) - (data >> 4);
                if (ral < 0)
                {
                    ral += 10;
                    rah--;
                }
                RP |= PC;
                if (rah < 0)
                {
                    rah += 10;
                    RP &= ~PC;
                }
                int ra = (ral | (rah << 4)) & 0xFF;
                RP = RP & ~(PN | PV | PZ) | DataPNZ[ra] | ((((RA ^ data) & (RA ^ ra)) >> 1) & PV);
                RA = ra;
                CC += cc + 1;
            }
        }

        private void ExecuteSec(int cc)
        {
            RP |= PC;
            CC += cc;
        }

        private void ExecuteSed(int cc)
        {
            RP |= PD;
            CC += cc;
        }

        private void ExecuteSei(int cc)
        {
            RP |= PI;
            CC += cc;
        }

        private void ExecuteSta(int cc)
        {
            CC += cc;
        }

        private void ExecuteStx(int cc)
        {
            CC += cc;
        }

        private void ExecuteSty(int cc)
        {
            CC += cc;
        }

        private void ExecuteStz(int cc)
        {
            CC += cc;
        }

        private void ExecuteTax(int cc)
        {
            RX = RA;
            RP = RP & ~(PN | PZ) | DataPNZ[RX];
            CC += cc;
        }

        private void ExecuteTay(int cc)
        {
            RY = RA;
            RP = RP & ~(PN | PZ) | DataPNZ[RY];
            CC += cc;
        }

        private int ExecuteTrb(int data, int cc)
        {
            RP = RP & ~PZ | DataPZ[RA & data];
            data &= ~RA;
            CC += cc;

            return data;
        }

        private int ExecuteTsb(int data, int cc)
        {
            RP = RP & ~PZ | DataPZ[RA & data];
            data |= RA;
            CC += cc;

            return data;
        }

        private void ExecuteTsx(int cc)
        {
            RX = RS;
            RP = RP & ~(PN | PZ) | DataPNZ[RX];
            CC += cc;
        }

        private void ExecuteTxa(int cc)
        {
            RA = RX;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }

        private void ExecuteTxs(int cc)
        {
            RS = RX;
            CC += cc;
        }

        private void ExecuteTya(int cc)
        {
            RA = RY;
            RP = RP & ~(PN | PZ) | DataPNZ[RA];
            CC += cc;
        }
        #endregion

        #region 6502 OpCode Actions
        private void Execute65X02And21() // and (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteAnd(ReadZpgIndX(), 6);
        }

        private void Execute65X02And25() // and zpg
        {
            GetAddressZpg();
            ExecuteAnd(ReadZpg(), 3);
        }

        private void Execute65X02And29() // and imm
        {
            ExecuteAnd(ReadImm(), 2);
        }

        private void Execute65X02And2D() // and abs
        {
            GetAddressAbs();
            ExecuteAnd(ReadAbs(), 4);
        }

        private void Execute65X02And31() // and (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteAnd(ReadZpgIndY(), 5);
        }

        private void Execute65X02And35() // and zpg, x
        {
            GetAddressZpgX();
            ExecuteAnd(ReadZpgX(), 4);
        }

        private void Execute65X02And39() // and abs, y
        {
            GetAddressAbsYCC();
            ExecuteAnd(ReadAbsY(), 4);
        }

        private void Execute65X02And3D() // and abs, x
        {
            GetAddressAbsXCC();
            ExecuteAnd(ReadAbsX(), 4);
        }

        private void Execute65X02Asl06() // asl zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteAsl(ReadZpg(), 5));
        }

        private void Execute65X02Asl0A() // asl imp
        {
            ExecuteAslImp(2);
        }

        private void Execute65X02Asl0E() // asl abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteAsl(ReadAbs(), 6));
        }

        private void Execute65X02Asl16() // asl zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(ExecuteAsl(ReadZpgX(), 6));
        }

        private void Execute65X02Bcc90() // bcc rel
        {
            ExecuteBcc(2);
        }

        private void Execute65X02BcsB0() // bcs rel
        {
            ExecuteBcs(2);
        }

        private void Execute65X02BeqF0() // beq rel
        {
            ExecuteBeq(2);
        }

        private void Execute65X02Bit24() // bit zpg
        {
            GetAddressZpg();
            ExecuteBit(ReadZpg(), 3);
        }

        private void Execute65X02Bit2C() // bit abs
        {
            GetAddressAbs();
            ExecuteBit(ReadAbs(), 4);
        }

        private void Execute65X02Bmi30() // bmi rel
        {
            ExecuteBmi(2);
        }

        private void Execute65X02BneD0() // bne rel
        {
            ExecuteBne(2);
        }

        private void Execute65X02Bpl10() // bpl rel
        {
            ExecuteBpl(2);
        }

        private void Execute65X02Brk00() // brk imp
        {
            ExecuteBrk(7);
        }

        private void Execute65X02Bvc50() // bvc rel
        {
            ExecuteBvc(2);
        }

        private void Execute65X02Bvs70() // bvs rel
        {
            ExecuteBvs(2);
        }

        private void Execute65X02Clc18() // clc imp
        {
            ExecuteClc(2);
        }

        private void Execute65X02CldD8() // cld imp
        {
            ExecuteCld(2);
        }

        private void Execute65X02Cli58() // cli imp
        {
            ExecuteCli(2);
        }

        private void Execute65X02ClvB8() // clv imp
        {
            ExecuteClv(2);
        }

        private void Execute65X02CmpC1() // cmp (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteCmp(ReadZpgIndX(), 6);
        }

        private void Execute65X02CmpC5() // cmp zpg
        {
            GetAddressZpg();
            ExecuteCmp(ReadZpg(), 3);
        }

        private void Execute65X02CmpC9() // cmp imm
        {
            ExecuteCmp(ReadImm(), 2);
        }

        private void Execute65X02CmpCD() // cmp abs
        {
            GetAddressAbs();
            ExecuteCmp(ReadAbs(), 4);
        }

        private void Execute65X02CmpD1() // cmp (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteCmp(ReadZpgIndY(), 5);
        }

        private void Execute65X02CmpD5() // cmp zpg, x
        {
            GetAddressZpgX();
            ExecuteCmp(ReadZpgX(), 4);
        }

        private void Execute65X02CmpD9() // cmp abs, y
        {
            GetAddressAbsYCC();
            ExecuteCmp(ReadAbsY(), 4);
        }

        private void Execute65X02CmpDD() // cmp abs, x
        {
            GetAddressAbsXCC();
            ExecuteCmp(ReadAbsX(), 4);
        }

        private void Execute65X02CpxE0() // cpx imm
        {
            ExecuteCpx(ReadImm(), 2);
        }

        private void Execute65X02CpxE4() // cpx zpg
        {
            GetAddressZpg();
            ExecuteCpx(ReadZpg(), 3);
        }

        private void Execute65X02CpxEC() // cpx abs
        {
            GetAddressAbs();
            ExecuteCpx(ReadAbs(), 4);
        }

        private void Execute65X02CpyC0() // cpy imm
        {
            ExecuteCpy(ReadImm(), 2);
        }

        private void Execute65X02CpyC4() // cpy zpg
        {
            GetAddressZpg();
            ExecuteCpy(ReadZpg(), 3);
        }

        private void Execute65X02CpyCC() // cpy abs
        {
            GetAddressAbs();
            ExecuteCpy(ReadAbs(), 4);
        }

        private void Execute65X02DecC6() // dec zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteDec(ReadZpg(), 5));
        }

        private void Execute65X02DecCE() // dec abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteDec(ReadAbs(), 6));
        }

        private void Execute65X02DecD6() // dec zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(ExecuteDec(ReadZpgX(), 6));
        }

        private void Execute65X02DexCA() // dex imp
        {
            ExecuteDex(2);
        }

        private void Execute65X02Dey88() // dey imp
        {
            ExecuteDey(2);
        }

        private void Execute65X02Eor41() // eor (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteEor(ReadZpgIndX(), 6);
        }

        private void Execute65X02Eor45() // eor zpg
        {
            GetAddressZpg();
            ExecuteEor(ReadZpg(), 3);
        }

        private void Execute65X02Eor49() // eor imm
        {
            ExecuteEor(ReadImm(), 2);
        }

        private void Execute65X02Eor4D() // eor abs
        {
            GetAddressAbs();
            ExecuteEor(ReadAbs(), 4);
        }

        private void Execute65X02Eor51() // eor (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteEor(ReadZpgIndY(), 5);
        }

        private void Execute65X02Eor55() // eor zpg, x
        {
            GetAddressZpgX();
            ExecuteEor(ReadZpgX(), 4);
        }

        private void Execute65X02Eor59() // eor abs, y
        {
            GetAddressAbsYCC();
            ExecuteEor(ReadAbsY(), 4);
        }

        private void Execute65X02Eor5D() // eor abs, x
        {
            GetAddressAbsXCC();
            ExecuteEor(ReadAbsX(), 4);
        }

        private void Execute65X02IncE6() // inc zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteInc(ReadZpg(), 5));
        }

        private void Execute65X02IncEE() // inc abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteInc(ReadAbs(), 6));
        }

        private void Execute65X02IncF6() // inc zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(ExecuteInc(ReadZpgX(), 6));
        }

        private void Execute65X02InxE8() // inx imp
        {
            ExecuteInx(2);
        }

        private void Execute65X02InyC8() // iny imp
        {
            ExecuteIny(2);
        }

        private void Execute65X02Jmp4C() // jmp abs
        {
            ExecuteJmpAbs(3);
        }

        private void Execute65X02Jsr20() // jsr abs
        {
            ExecuteJsr(6);
        }

        private void Execute65X02LdaA1() // lda (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteLda(ReadZpgIndX(), 6);
        }

        private void Execute65X02LdaA5() // lda zpg
        {
            GetAddressZpg();
            ExecuteLda(ReadZpg(), 3);
        }

        private void Execute65X02LdaA9() // lda imm
        {
            ExecuteLda(ReadImm(), 2);
        }

        private void Execute65X02LdaAD() // lda abs
        {
            GetAddressAbs();
            ExecuteLda(ReadAbs(), 4);
        }

        private void Execute65X02LdaB1() // lda (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteLda(ReadZpgIndY(), 5);
        }

        private void Execute65X02LdaB5() // lda zpg, x
        {
            GetAddressZpgX();
            ExecuteLda(ReadZpgX(), 4);
        }

        private void Execute65X02LdaB9() // lda abs, y
        {
            GetAddressAbsYCC();
            ExecuteLda(ReadAbsY(), 4);
        }

        private void Execute65X02LdaBD() // lda abs, x
        {
            GetAddressAbsXCC();
            ExecuteLda(ReadAbsX(), 4);
        }

        private void Execute65X02LdxA2() // ldx imm
        {
            ExecuteLdx(ReadImm(), 2);
        }

        private void Execute65X02LdxA6() // ldx zpg
        {
            GetAddressZpg();
            ExecuteLdx(ReadZpg(), 3);
        }

        private void Execute65X02LdxAE() // ldx abs
        {
            GetAddressAbs();
            ExecuteLdx(ReadAbs(), 4);
        }

        private void Execute65X02LdxB6() // ldx zpg, y
        {
            GetAddressZpgY();
            ExecuteLdx(ReadZpgY(), 4);
        }

        private void Execute65X02LdxBE() // ldx abs, y
        {
            GetAddressAbsYCC();
            ExecuteLdx(ReadAbsY(), 4);
        }

        private void Execute65X02LdyA0() // ldy imm
        {
            ExecuteLdy(ReadImm(), 2);
        }

        private void Execute65X02LdyA4() // ldy zpg
        {
            GetAddressZpg();
            ExecuteLdy(ReadZpg(), 3);
        }

        private void Execute65X02LdyAC() // ldy abs
        {
            GetAddressAbs();
            ExecuteLdy(ReadAbs(), 4);
        }

        private void Execute65X02LdyB4() // ldy zpg, x
        {
            GetAddressZpgX();
            ExecuteLdy(ReadZpgX(), 4);
        }

        private void Execute65X02LdyBC() // ldy abs, x
        {
            GetAddressAbsXCC();
            ExecuteLdy(ReadAbsX(), 4);
        }

        private void Execute65X02Lsr46() // lsr zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteLsr(ReadZpg(), 5));
        }

        private void Execute65X02Lsr4A() // lsr imp
        {
            ExecuteLsrImp(2);
        }

        private void Execute65X02Lsr4E() // lsr abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteLsr(ReadAbs(), 6));
        }

        private void Execute65X02Lsr56() // lsr zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(ExecuteLsr(ReadZpgX(), 6));
        }

        private void Execute65X02NopEA() // nop imp
        {
            ExecuteNop(2);
        }

        private void Execute65X02Ora01() // ora (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteOra(ReadZpgIndX(), 6);
        }

        private void Execute65X02Ora05() // ora zpg
        {
            GetAddressZpg();
            ExecuteOra(ReadZpg(), 3);
        }

        private void Execute65X02Ora09() // ora imm
        {
            ExecuteOra(ReadImm(), 2);
        }

        private void Execute65X02Ora0D() // ora abs
        {
            GetAddressAbs();
            ExecuteOra(ReadAbs(), 4);
        }

        private void Execute65X02Ora11() // ora (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteOra(ReadZpgIndY(), 5);
        }

        private void Execute65X02Ora15() // ora zpg, x
        {
            GetAddressZpgX();
            ExecuteOra(ReadZpgX(), 4);
        }

        private void Execute65X02Ora19() // ora abs, y
        {
            GetAddressAbsYCC();
            ExecuteOra(ReadAbsY(), 4);
        }

        private void Execute65X02Ora1D() // ora abs, x
        {
            GetAddressAbsXCC();
            ExecuteOra(ReadAbsX(), 4);
        }

        private void Execute65X02Pha48() // pha imp
        {
            ExecutePha(3);
        }

        private void Execute65X02Php08() // php imp
        {
            ExecutePhp(3);
        }

        private void Execute65X02Pla68() // pla imp
        {
            ExecutePla(4);
        }

        private void Execute65X02Plp28() // plp imp
        {
            ExecutePlp(4);
        }

        private void Execute65X02Rol26() // rol zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteRol(ReadZpg(), 5));
        }

        private void Execute65X02Rol2A() // rol imp
        {
            ExecuteRolImp(2);
        }

        private void Execute65X02Rol2E() // rol abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteRol(ReadAbs(), 6));
        }

        private void Execute65X02Rol36() // rol zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(ExecuteRol(ReadZpgX(), 6));
        }

        private void Execute65X02Ror66() // ror zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteRor(ReadZpg(), 5));
        }

        private void Execute65X02Ror6A() // ror imp
        {
            ExecuteRorImp(2);
        }

        private void Execute65X02Ror6E() // ror abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteRor(ReadAbs(), 6));
        }

        private void Execute65X02Ror76() // ror zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(ExecuteRor(ReadZpgX(), 6));
        }

        private void Execute65X02Rti40() // rti imp
        {
            ExecuteRti(6);
        }

        private void Execute65X02Rts60() // rts imp
        {
            ExecuteRts(6);
        }

        private void Execute65X02Sec38() // sec imp
        {
            ExecuteSec(2);
        }

        private void Execute65X02SedF8() // sed imp
        {
            ExecuteSed(2);
        }

        private void Execute65X02Sei78() // sei imp
        {
            ExecuteSei(2);
        }

        private void Execute65X02Sta81() // sta (zpg, x)
        {
            GetAddressZpgIndX();
            WriteZpgIndX(RA);
            ExecuteSta(6);
        }

        private void Execute65X02Sta85() // sta zpg
        {
            GetAddressZpg();
            WriteZpg(RA);
            ExecuteSta(3);
        }

        private void Execute65X02Sta8D() // sta abs
        {
            GetAddressAbs();
            WriteAbs(RA);
            ExecuteSta(4);
        }

        private void Execute65X02Sta91() // sta (zpg), y
        {
            GetAddressZpgIndY();
            WriteZpgIndY(RA);
            ExecuteSta(6);
        }

        private void Execute65X02Sta95() // sta zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(RA);
            ExecuteSta(4);
        }

        private void Execute65X02Sta99() // sta abs, y
        {
            GetAddressAbsY();
            WriteAbsY(RA);
            ExecuteSta(5);
        }

        private void Execute65X02Sta9D() // sta abs, x
        {
            GetAddressAbsX();
            WriteAbsX(RA);
            ExecuteSta(5);
        }

        private void Execute65X02Stx86() // stx zpg
        {
            GetAddressZpg();
            WriteZpg(RX);
            ExecuteStx(3);
        }

        private void Execute65X02Stx8E() // stx abs
        {
            GetAddressAbs();
            WriteAbs(RX);
            ExecuteStx(4);
        }

        private void Execute65X02Stx96() // stx zpg, y
        {
            GetAddressZpgY();
            WriteZpgY(RX);
            ExecuteStx(4);
        }

        private void Execute65X02Sty84() // sty zpg
        {
            GetAddressZpg();
            WriteZpg(RY);
            ExecuteSty(3);
        }

        private void Execute65X02Sty8C() // sty abs
        {
            GetAddressAbs();
            WriteAbs(RY);
            ExecuteSty(4);
        }

        private void Execute65X02Sty94() // sty zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(RY);
            ExecuteSty(4);
        }

        private void Execute65X02TaxAA() // tax imp
        {
            ExecuteTax(2);
        }

        private void Execute65X02TayA8() // tay imp
        {
            ExecuteTay(2);
        }

        private void Execute65X02TsxBA() // tsx imp
        {
            ExecuteTsx(2);
        }

        private void Execute65X02Txa8A() // txa imp
        {
            ExecuteTxa(2);
        }

        private void Execute65X02Txs9A() // txs imp
        {
            ExecuteTxs(2);
        }

        private void Execute65X02Tya98() // tya imp
        {
            ExecuteTya(2);
        }
        #endregion

        #region 65N02 OpCode Actions
        private void Execute65N02Adc61() // adc (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteAdc65N02(ReadZpgIndX(), 6);
        }

        private void Execute65N02Adc65() // adc zpg
        {
            GetAddressZpg();
            ExecuteAdc65N02(ReadZpg(), 3);
        }

        private void Execute65N02Adc69() // adc imm
        {
            ExecuteAdc65N02(ReadImm(), 2);
        }

        private void Execute65N02Adc6D() // adc abs
        {
            GetAddressAbs();
            ExecuteAdc65N02(ReadAbs(), 4);
        }

        private void Execute65N02Adc71() // adc (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteAdc65N02(ReadZpgIndY(), 5);
        }

        private void Execute65N02Adc75() // adc zpg, x
        {
            GetAddressZpgX();
            ExecuteAdc65N02(ReadZpgX(), 4);
        }

        private void Execute65N02Adc79() // adc abs, y
        {
            GetAddressAbsYCC();
            ExecuteAdc65N02(ReadAbsY(), 4);
        }

        private void Execute65N02Adc7D() // adc abs, x
        {
            GetAddressAbsXCC();
            ExecuteAdc65N02(ReadAbsX(), 4);
        }

        private void Execute65N02Asl1E() // asl abs, x
        {
            GetAddressAbsX();
            WriteAbsX(ExecuteAsl(ReadAbsX(), 7));
        }

        private void Execute65N02DecDE() // dec abs, x
        {
            GetAddressAbsX();
            WriteAbsX(ExecuteDec(ReadAbsX(), 7));
        }

        private void Execute65N02IncFE() // inc abs, x
        {
            GetAddressAbsX();
            WriteAbsX(ExecuteInc(ReadAbsX(), 7));
        }

        private void Execute65N02Jmp6C() // jmp (abs)
        {
            ExecuteJmpAbsInd65N02(5);
        }

        private void Execute65N02Lsr5E() // lsr abs, x
        {
            GetAddressAbsX();
            WriteAbsX(ExecuteLsr(ReadAbsX(), 7));
        }

        private void Execute65N02Nop02() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop03() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02Nop04() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop07() // nop imp1
        {
            ExecuteNop(1, 5);
        }

        private void Execute65N02Nop0B() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop0C() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop0F() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02Nop12() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop13() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02Nop14() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop17() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02Nop1A() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop1B() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02Nop1C() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop1F() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02Nop22() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop23() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02Nop27() // nop imp1
        {
            ExecuteNop(1, 3);
        }

        private void Execute65N02Nop2B() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop2F() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop32() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop33() // nop imp1
        {
            ExecuteNop(1, 5);
        }

        private void Execute65N02Nop34() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop37() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65N02Nop3A() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop3B() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop3C() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop3F() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop42() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop43() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02Nop44() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop47() // nop imp1
        {
            ExecuteNop(1, 3);
        }

        private void Execute65N02Nop4B() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop4F() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop52() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop53() // nop imp1
        {
            ExecuteNop(1, 5);
        }

        private void Execute65N02Nop54() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop57() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65N02Nop5A() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop5B() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop5C() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop5F() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop62() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop63() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02Nop64() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop67() // nop imp1
        {
            ExecuteNop(1, 3);
        }

        private void Execute65N02Nop6B() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop6F() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop72() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop73() // nop imp1
        {
            ExecuteNop(1, 5);
        }

        private void Execute65N02Nop74() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop77() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65N02Nop7A() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop7B() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop7C() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop7F() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop80() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop82() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop83() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65N02Nop87() // nop imp1
        {
            ExecuteNop(1, 3);
        }

        private void Execute65N02Nop89() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop8B() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02Nop8F() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02Nop92() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02Nop93() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02Nop97() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65N02Nop9B() // nop imp2
        {
            ExecuteNop(2, 5);
        }

        private void Execute65N02Nop9C() // nop imp2
        {
            ExecuteNop(2, 5);
        }

        private void Execute65N02Nop9E() // nop imp2
        {
            ExecuteNop(2, 5);
        }

        private void Execute65N02Nop9F() // nop imp2
        {
            ExecuteNop(2, 5);
        }

        private void Execute65N02NopA3() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02NopA7() // nop imp1
        {
            ExecuteNop(1, 3);
        }

        private void Execute65N02NopAB() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02NopAF() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02NopB2() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02NopB3() // nop imp1
        {
            ExecuteNop(1, 5);
        }

        private void Execute65N02NopB7() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65N02NopBB() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02NopBF() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02NopC2() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02NopC3() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02NopC7() // nop imp1
        {
            ExecuteNop(1, 5);
        }

        private void Execute65N02NopCB() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02NopCF() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02NopD2() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02NopD3() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02NopD4() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02NopD7() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02NopDA() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02NopDB() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02NopDC() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02NopDF() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02NopE2() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02NopE3() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02NopE7() // nop imp1
        {
            ExecuteNop(1, 5);
        }

        private void Execute65N02NopEB() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02NopEF() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02NopF2() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02NopF3() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02NopF4() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65N02NopF7() // nop imp1
        {
            ExecuteNop(1, 6);
        }

        private void Execute65N02NopFA() // nop imp0
        {
            ExecuteNop(0, 2);
        }

        private void Execute65N02NopFB() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02NopFC() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65N02NopFF() // nop imp2
        {
            ExecuteNop(2, 6);
        }

        private void Execute65N02Rol3E() // rol abs, x
        {
            GetAddressAbsX();
            WriteAbsX(ExecuteRol(ReadAbsX(), 7));
        }

        private void Execute65N02Ror7E() // ror abs, x
        {
            GetAddressAbsX();
            WriteAbsX(ExecuteRor(ReadAbsX(), 7));
        }

        private void Execute65N02SbcE1() // sbc (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteSbc65N02(ReadZpgIndX(), 6);
        }

        private void Execute65N02SbcE5() // sbc zpg
        {
            GetAddressZpg();
            ExecuteSbc65N02(ReadZpg(), 3);
        }

        private void Execute65N02SbcE9() // sbc imm
        {
            ExecuteSbc65N02(ReadImm(), 2);
        }

        private void Execute65N02SbcED() // sbc abs
        {
            GetAddressAbs();
            ExecuteSbc65N02(ReadAbs(), 4);
        }

        private void Execute65N02SbcF1() // sbc (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteSbc65N02(ReadZpgIndY(), 5);
        }

        private void Execute65N02SbcF5() // sbc zpg, x
        {
            GetAddressZpgX();
            ExecuteSbc65N02(ReadZpgX(), 4);
        }

        private void Execute65N02SbcF9() // sbc abs, y
        {
            GetAddressAbsYCC();
            ExecuteSbc65N02(ReadAbsY(), 4);
        }

        private void Execute65N02SbcFD() // sbc abs, x
        {
            GetAddressAbsXCC();
            ExecuteSbc65N02(ReadAbsX(), 4);
        }
        #endregion

        #region 65C02 OpCode Actions
        private void Execute65C02Adc61() // adc (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteAdc65C02(ReadZpgIndX(), 6);
        }

        private void Execute65C02Adc65() // adc zpg
        {
            GetAddressZpg();
            ExecuteAdc65C02(ReadZpg(), 3);
        }

        private void Execute65C02Adc69() // adc imm
        {
            ExecuteAdc65C02(ReadImm(), 2);
        }

        private void Execute65C02Adc6D() // adc abs
        {
            GetAddressAbs();
            ExecuteAdc65C02(ReadAbs(), 4);
        }

        private void Execute65C02Adc71() // adc (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteAdc65C02(ReadZpgIndY(), 5);
        }

        private void Execute65C02Adc72() // adc (zpg)
        {
            GetAddressZpgInd();
            ExecuteAdc65C02(ReadZpgInd(), 5);
        }

        private void Execute65C02Adc75() // adc zpg, x
        {
            GetAddressZpgX();
            ExecuteAdc65C02(ReadZpgX(), 4);
        }

        private void Execute65C02Adc79() // adc abs, y
        {
            GetAddressAbsYCC();
            ExecuteAdc65C02(ReadAbsY(), 4);
        }

        private void Execute65C02Adc7D() // adc abs, x
        {
            GetAddressAbsXCC();
            ExecuteAdc65C02(ReadAbsX(), 4);
        }

        private void Execute65C02And32() // and (zpg)
        {
            GetAddressZpgInd();
            ExecuteAnd(ReadZpgInd(), 5);
        }

        private void Execute65C02Asl1E() // asl abs, x
        {
            GetAddressAbsXCC();
            WriteAbsX(ExecuteAsl(ReadAbsX(), 6));
        }

        private void Execute65C02Bit34() // bit zpg, x
        {
            GetAddressZpgX();
            ExecuteBit(ReadZpgX(), 4);
        }

        private void Execute65C02Bit3C() // bit abs, x
        {
            GetAddressAbsXCC();
            ExecuteBit(ReadAbsX(), 4);
        }

        private void Execute65C02Bit89() // bit imm
        {
            ExecuteBitImm(ReadImm(), 2);
        }

        private void Execute65C02Bra80() // bra rel
        {
            ExecuteBra(2);
        }

        private void Execute65C02CmpD2() // cmp (zpg)
        {
            GetAddressZpgInd();
            ExecuteCmp(ReadZpgInd(), 5);
        }

        private void Execute65C02Dea3A() // dea imp
        {
            ExecuteDea(2);
        }

        private void Execute65C02DecDE() // dec abs, x
        {
            GetAddressAbsXCC();
            WriteAbsX(ExecuteDec(ReadAbsX(), 6));
        }

        private void Execute65C02Eor52() // eor (zpg)
        {
            GetAddressZpgInd();
            ExecuteEor(ReadZpgInd(), 5);
        }

        private void Execute65C02Ina1A() // ina imp
        {
            ExecuteIna(2);
        }

        private void Execute65C02IncFE() // inc abs, x
        {
            GetAddressAbsXCC();
            WriteAbsX(ExecuteInc(ReadAbsX(), 6));
        }

        private void Execute65C02Jmp6C() // jmp (abs)
        {
            ExecuteJmpAbsInd65C02(6);
        }

        private void Execute65C02Jmp7C() // jmp (abs, x)
        {
            ExecuteJmpAbsIndX(6);
        }

        private void Execute65C02LdaB2() // lda (zpg)
        {
            GetAddressZpgInd();
            ExecuteLda(ReadZpgInd(), 5);
        }

        private void Execute65C02Lsr5E() // lsr abs, x
        {
            GetAddressAbsXCC();
            WriteAbsX(ExecuteLsr(ReadAbsX(), 6));
        }

        private void Execute65C02Nop02() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65C02Nop03() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop07() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop0B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop0F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop13() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop17() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop1B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop1F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop22() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65C02Nop23() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop27() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop2B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop2F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop33() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop37() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop3B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop3F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop42() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65C02Nop43() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop44() // nop imp1
        {
            ExecuteNop(1, 3);
        }

        private void Execute65C02Nop47() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop4B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop4F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop53() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop54() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65C02Nop57() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop5B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop5C() // nop imp2
        {
            ExecuteNop(2, 8);
        }

        private void Execute65C02Nop5F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop62() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65C02Nop63() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop67() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop6B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop6F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop73() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop77() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop7B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop7F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop82() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65C02Nop83() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop87() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop8B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop8F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop93() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop97() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop9B() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Nop9F() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopA3() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopA7() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopAB() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopAF() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopB3() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopB7() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopBB() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopBF() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopC2() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65C02NopC3() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopC7() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopCB() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopCF() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopD3() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopD4() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65C02NopD7() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopDB() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopDC() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65C02NopDF() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopE2() // nop imp1
        {
            ExecuteNop(1, 2);
        }

        private void Execute65C02NopE3() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopE7() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopEB() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopEF() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopF3() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopF4() // nop imp1
        {
            ExecuteNop(1, 4);
        }

        private void Execute65C02NopF7() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopFB() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02NopFC() // nop imp2
        {
            ExecuteNop(2, 4);
        }

        private void Execute65C02NopFF() // nop imp0
        {
            ExecuteNop(0, 1);
        }

        private void Execute65C02Ora12() // ora (zpg)
        {
            GetAddressZpgInd();
            ExecuteOra(ReadZpgInd(), 5);
        }

        private void Execute65C02PhxDA() // phx imp
        {
            ExecutePhx(3);
        }

        private void Execute65C02Phy5A() // phy imp
        {
            ExecutePhy(3);
        }

        private void Execute65C02PlxFA() // plx imp
        {
            ExecutePlx(4);
        }

        private void Execute65C02Ply7A() // ply imp
        {
            ExecutePly(4);
        }

        private void Execute65C02Rol3E() // rol abs, x
        {
            GetAddressAbsXCC();
            WriteAbsX(ExecuteRol(ReadAbsX(), 6));
        }

        private void Execute65C02Ror7E() // ror abs, x
        {
            GetAddressAbsXCC();
            WriteAbsX(ExecuteRor(ReadAbsX(), 6));
        }

        private void Execute65C02SbcE1() // sbc (zpg, x)
        {
            GetAddressZpgIndX();
            ExecuteSbc65C02(ReadZpgIndX(), 6);
        }

        private void Execute65C02SbcE5() // sbc zpg
        {
            GetAddressZpg();
            ExecuteSbc65C02(ReadZpg(), 3);
        }

        private void Execute65C02SbcE9() // sbc imm
        {
            ExecuteSbc65C02(ReadImm(), 2);
        }

        private void Execute65C02SbcED() // sbc abs
        {
            GetAddressAbs();
            ExecuteSbc65C02(ReadAbs(), 4);
        }

        private void Execute65C02SbcF1() // sbc (zpg), y
        {
            GetAddressZpgIndYCC();
            ExecuteSbc65C02(ReadZpgIndY(), 5);
        }

        private void Execute65C02SbcF2() // sbc (zpg)
        {
            GetAddressZpgInd();
            ExecuteSbc65C02(ReadZpgInd(), 5);
        }

        private void Execute65C02SbcF5() // sbc zpg, x
        {
            GetAddressZpgX();
            ExecuteSbc65C02(ReadZpgX(), 4);
        }

        private void Execute65C02SbcF9() // sbc abs, y
        {
            GetAddressAbsYCC();
            ExecuteSbc65C02(ReadAbsY(), 4);
        }

        private void Execute65C02SbcFD() // sbc abs, x
        {
            GetAddressAbsXCC();
            ExecuteSbc65C02(ReadAbsX(), 4);
        }

        private void Execute65C02Sta92() // sta (zpg)
        {
            GetAddressZpgInd();
            WriteZpgInd(RA);
            ExecuteSta(5);
        }

        private void Execute65C02Stz64() // stz zpg
        {
            GetAddressZpg();
            WriteZpg(0x00);
            ExecuteStz(3);
        }

        private void Execute65C02Stz74() // stz zpg, x
        {
            GetAddressZpgX();
            WriteZpgX(0x00);
            ExecuteStz(4);
        }

        private void Execute65C02Stz9C() // stz abs
        {
            GetAddressAbs();
            WriteAbs(0x00);
            ExecuteStz(4);
        }

        private void Execute65C02Stz9E() // stz abs, x
        {
            GetAddressAbsX();
            WriteAbsX(0x00);
            ExecuteStz(5);
        }

        private void Execute65C02Trb14() // trb zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteTrb(ReadZpg(), 5));
        }

        private void Execute65C02Trb1C() // trb abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteTrb(ReadAbs(), 6));
        }

        private void Execute65C02Tsb04() // tsb zpg
        {
            GetAddressZpg();
            WriteZpg(ExecuteTsb(ReadZpg(), 5));
        }

        private void Execute65C02Tsb0C() // tsb abs
        {
            GetAddressAbs();
            WriteAbs(ExecuteTsb(ReadAbs(), 6));
        }
        #endregion

        public bool Is65C02 { get { return _is65C02; } set { _is65C02 = value; _executeOpCode = _is65C02 ? ExecuteOpCode65C02 : ExecuteOpCode65N02; } }
        public bool IsThrottled { get; set; }
        public int Multiplier { get; set; }

        public int RA { get; private set; }
        public int RX { get; private set; }
        public int RY { get; private set; }
        public int RS { get; private set; }
        public int RP { get; private set; }
        public int RPC { get; private set; }
        public int EA { get; private set; }
        public int CC { get; private set; }
        public int OpCode { get; private set; }
        public long Cycles { get; private set; }

        private Memory _memory;

        private bool _is65C02;
        private Action[] _executeOpCode;
    }
}
