using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// SZX Methods
    /// Based on the work done by ArjunNair in ZERO spectrum emulator: https://github.com/ArjunNair/Zero-Emulator/blob/master/Ziggy/Peripherals/SZXFile.cs
    /// </summary>
    public partial class SZX
    {
        private SpectrumBase _machine;

        private Z80A _cpu => _machine.CPU;

        private SZX(SpectrumBase machine)
        {
            _machine = machine;
        }

        /// <summary>
        /// Exports state information to a byte array in ZX-State format
        /// </summary>
        /// <param name="machine"></param>
        /// <returns></returns>
        public static byte[] ExportSZX(SpectrumBase machine)
        {
            var s = new SZX(machine);

            byte[] result = null;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter r = new BinaryWriter(ms))
                {
                    // temp buffer
                    byte[] buff;
                    // working block
                    ZXSTBLOCK block = new ZXSTBLOCK();

                    // header
                    ZXSTHEADER header = new ZXSTHEADER();
                    header.dwMagic = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("ZXST"), 0);
                    header.chMajorVersion = 1;
                    header.chMinorVersion = 4;
                    header.chFlags = 0;
                    switch (s._machine.Spectrum.MachineType)
                    {
                        case MachineType.ZXSpectrum16: header.chMachineId = (int)MachineIdentifier.ZXSTMID_16K; break;
                        case MachineType.ZXSpectrum48: header.chMachineId = (int)MachineIdentifier.ZXSTMID_48K; break;
                        case MachineType.ZXSpectrum128: header.chMachineId = (int)MachineIdentifier.ZXSTMID_128K; break;
                        case MachineType.ZXSpectrum128Plus2: header.chMachineId = (int)MachineIdentifier.ZXSTMID_PLUS2; break;
                        case MachineType.ZXSpectrum128Plus2a: header.chMachineId = (int)MachineIdentifier.ZXSTMID_PLUS2A; break;
                        case MachineType.ZXSpectrum128Plus3: header.chMachineId = (int)MachineIdentifier.ZXSTMID_PLUS3; break;
                    }
                    buff = MediaConverter.SerializeRaw(header);
                    r.Write(buff);

                    // ZXSTCREATOR
                    var bStruct = s.GetZXSTCREATOR();
                    block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("CRTR"), 0);
                    block.dwSize = (uint)Marshal.SizeOf(bStruct);
                    buff = MediaConverter.SerializeRaw(block);
                    r.Write(buff);
                    buff = MediaConverter.SerializeRaw(bStruct);
                    r.Write(buff);

                    // ZXSTZ80REGS
                    var cStruct = s.GetZXSTZ80REGS();
                    block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("Z80R"), 0);
                    block.dwSize = (uint)Marshal.SizeOf(cStruct);
                    buff = MediaConverter.SerializeRaw(block);
                    r.Write(buff);
                    buff = MediaConverter.SerializeRaw(cStruct);
                    r.Write(buff);

                    // ZXSTSPECREGS
                    var dStruct = s.GetZXSTSPECREGS();
                    block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("SPCR"), 0);
                    block.dwSize = (uint)Marshal.SizeOf(dStruct);
                    buff = MediaConverter.SerializeRaw(block);
                    r.Write(buff);
                    buff = MediaConverter.SerializeRaw(dStruct);
                    r.Write(buff);

                    // ZXSTKEYBOARD
                    var eStruct = s.GetZXSTKEYBOARD();
                    block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("KEYB"), 0);
                    block.dwSize = (uint)Marshal.SizeOf(eStruct);
                    buff = MediaConverter.SerializeRaw(block);
                    r.Write(buff);
                    buff = MediaConverter.SerializeRaw(eStruct);
                    r.Write(buff);                    

                    // ZXSTJOYSTICK                    
                    var fStruct = s.GetZXSTJOYSTICK();
                    block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("JOY\0"), 0);
                    block.dwSize = (uint)Marshal.SizeOf(fStruct);
                    buff = MediaConverter.SerializeRaw(block);
                    r.Write(buff);
                    buff = MediaConverter.SerializeRaw(fStruct);
                    r.Write(buff);
                    

                    // ZXSTAYBLOCK
                    if (s._machine.Spectrum.MachineType != MachineType.ZXSpectrum16 && s._machine.Spectrum.MachineType != MachineType.ZXSpectrum48)
                    {
                        var gStruct = s.GetZXSTAYBLOCK();
                        block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("AY\0\0"), 0);
                        block.dwSize = (uint)Marshal.SizeOf(gStruct);
                        buff = MediaConverter.SerializeRaw(block);
                        r.Write(buff);
                        buff = MediaConverter.SerializeRaw(gStruct);
                        r.Write(buff);
                    }

                    // ZXSTRAMPAGE
                    switch (s._machine.Spectrum.MachineType)
                    {
                        // For 16k Spectrums, only page 5 (0x4000 - 0x7fff) is saved.
                        case MachineType.ZXSpectrum16:
                            block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("RAMP"), 0);
                            var rp16 = s.GetZXSTRAMPAGE(5, s._machine.RAM0);
                            block.dwSize = (uint)Marshal.SizeOf(rp16);
                            buff = MediaConverter.SerializeRaw(block);
                            r.Write(buff);
                            buff = MediaConverter.SerializeRaw(rp16);
                            r.Write(buff);
                            break;
                        // For 48k Spectrums and Timex TS/TC models, pages 5, 2 (0x8000 - 0xbfff) and 0 (0xc000 - 0xffff) are saved.
                        case MachineType.ZXSpectrum48:
                            block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("RAMP"), 0);
                            var rp48_0 = s.GetZXSTRAMPAGE(5, s._machine.RAM0);
                            block.dwSize = (uint)Marshal.SizeOf(rp48_0);
                            buff = MediaConverter.SerializeRaw(block);
                            r.Write(buff);
                            buff = MediaConverter.SerializeRaw(rp48_0);
                            r.Write(buff);

                            block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("RAMP"), 0);
                            var rp48_1 = s.GetZXSTRAMPAGE(5, s._machine.RAM1);
                            block.dwSize = (uint)Marshal.SizeOf(rp48_1);
                            buff = MediaConverter.SerializeRaw(block);
                            r.Write(buff);
                            buff = MediaConverter.SerializeRaw(rp48_1);
                            r.Write(buff);

                            block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("RAMP"), 0);
                            var rp48_2 = s.GetZXSTRAMPAGE(5, s._machine.RAM2);
                            block.dwSize = (uint)Marshal.SizeOf(rp48_2);
                            buff = MediaConverter.SerializeRaw(block);
                            r.Write(buff);
                            buff = MediaConverter.SerializeRaw(rp48_2);
                            r.Write(buff);
                            break;
                        // For 128k Spectrums and the Pentagon 128, all pages (0-7) are saved.
                        case MachineType.ZXSpectrum128:
                        case MachineType.ZXSpectrum128Plus2:
                        case MachineType.ZXSpectrum128Plus2a:
                        case MachineType.ZXSpectrum128Plus3:
                            List<byte[]> rams = new List<byte[]>
                            {
                                s._machine.RAM0, s._machine.RAM1, s._machine.RAM2, s._machine.RAM3,
                                s._machine.RAM4, s._machine.RAM5, s._machine.RAM6, s._machine.RAM7
                            };
                            for (byte i = 0; i < 8; i++)
                            {
                                block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("RAMP"), 0);
                                var rp = s.GetZXSTRAMPAGE(i, rams[i]);
                                block.dwSize = (uint)Marshal.SizeOf(rp);
                                buff = MediaConverter.SerializeRaw(block);
                                r.Write(buff);
                                buff = MediaConverter.SerializeRaw(rp);
                                r.Write(buff);
                            }
                            break;
                    }
                    /*
                    // ZXSTPLUS3                   
                    if (s._machine.Spectrum.MachineType == MachineType.ZXSpectrum128Plus3)
                    {
                        var iStruct = s.GetZXSTPLUS3();
                        block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("+3\0\0"), 0);
                        block.dwSize = (uint)Marshal.SizeOf(iStruct);
                        buff = MediaConverter.SerializeRaw(block);
                        r.Write(buff);
                        buff = MediaConverter.SerializeRaw(iStruct);
                        r.Write(buff);

                        // ZXSTDSKFILE
                        if (s._machine.diskImages.Count() > 0)
                        {
                            var jStruct = s.GetZXSTDSKFILE();
                            block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("DSK\0"), 0);
                            block.dwSize = (uint)Marshal.SizeOf(jStruct);
                            buff = MediaConverter.SerializeRaw(block);
                            r.Write(buff);
                            buff = MediaConverter.SerializeRaw(jStruct);
                            r.Write(buff);
                        }
                    }                    

                    // ZXSTTAPE
                    if (s._machine.tapeImages.Count() > 0)
                    {
                        var hStruct = s.GetZXSTTAPE();
                        var tapeData = s._machine.tapeImages[s._machine.TapeMediaIndex];
                        block.dwId = MediaConverter.GetUInt32(Encoding.UTF8.GetBytes("TAPE"), 0);
                        block.dwSize = (uint)Marshal.SizeOf(hStruct) + (uint)tapeData.Length;
                        buff = MediaConverter.SerializeRaw(block);
                        r.Write(buff);
                        buff = MediaConverter.SerializeRaw(hStruct);
                        r.Write(buff);
                        buff = MediaConverter.SerializeRaw(tapeData);
                        r.Write(buff);
                        char[] terminator = "\0".ToCharArray();
                        r.Write(terminator);
                    }
                    */
                   
                }

                result = ms.ToArray();
            }

            return result;
        }

        private ZXSTRAMPAGE GetZXSTRAMPAGE(byte page, byte[] RAM)
        {
            var s = new ZXSTRAMPAGE();
            s.wFlags = 0; // uncompressed only at the moment
            s.chPageNo = page;
            s.ramPage = RAM;
            return s;
        }

        private ZXSTCREATOR GetZXSTCREATOR()
        {
            var s = new ZXSTCREATOR();
            var str = "BIZHAWK EMULATOR".ToCharArray();
            s.szCreator = new char[32];
            for (int i = 0; i < str.Length; i++)
                s.szCreator[i] = str[i];
            s.chMajorVersion = 1;
            s.chMinorVersion = 4;           

            return s;
        }

        private ZXSTZ80REGS GetZXSTZ80REGS()
        {
            var s = new ZXSTZ80REGS();
            s.AF = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["AF"].Value;
            s.BC = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["BC"].Value;
            s.DE = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["DE"].Value;
            s.HL = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["HL"].Value;
            s.AF1 = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["Shadow AF"].Value;
            s.BC1 = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["Shadow BC"].Value;
            s.DE1 = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["Shadow DE"].Value;
            s.HL1 = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["Shadow HL"].Value;
            s.IX = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["IX"].Value;
            s.IY = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["IY"].Value;
            s.SP = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["SP"].Value;
            s.PC = (ushort)_machine.Spectrum.GetCpuFlagsAndRegisters()["PC"].Value;
            s.I = (byte)_machine.CPU.Regs[_machine.CPU.I];
            s.R = (byte)_machine.CPU.Regs[_machine.CPU.R];
            s.IFF1 = (byte)(_machine.CPU.IFF1 ? 1 : 0);
            s.IFF2 = (byte)(_machine.CPU.IFF2 ? 1 : 0);
            s.IM = (byte)_machine.CPU.InterruptMode;
            s.dwCyclesStart = (uint)(_machine.CurrentFrameCycle + _machine.ULADevice.InterruptStartTime);
            s.wMemPtr = (ushort)(_machine.CPU.Regs[_machine.CPU.Z] + (_machine.CPU.Regs[_machine.CPU.W] << 8));
            //s.chHoldIntReqCycles = ?
            
            if (_machine.CPU.EIPending > 0)
            {
                s.chFlags |= ZXSTZF_EILAST;
            }
            else if (_machine.CPU.halted)
            {
                s.chFlags |= ZXSTZF_HALTED;
            }
            
            return s;
        }

        private ZXSTSPECREGS GetZXSTSPECREGS()
        {
            var s = new ZXSTSPECREGS();
            s.chBorder = _machine.ULADevice.BorderColor > 7 ? (byte)0 : (byte)_machine.ULADevice.BorderColor;
            s.chFe = _machine.LastFe;
            byte x7ffd = (byte)_machine.RAMPaged;
            byte x1ffd = 0;
            switch (_machine.Spectrum.MachineType)
            {
                case MachineType.ZXSpectrum16:
                case MachineType.ZXSpectrum48:
                    s.ch7ffd = 0;
                    s.unionPage = 0;
                    break;

                case MachineType.ZXSpectrum128:
                case MachineType.ZXSpectrum128Plus2:
                    // 7FFD
                    if (_machine._ROMpaged == 1)
                        x7ffd |= 0x10;
                    if (_machine.SHADOWPaged)
                        x7ffd |= 0x08;
                    if (_machine.PagingDisabled)
                        x7ffd |= 0x20;
                    break;

                case MachineType.ZXSpectrum128Plus2a:
                case MachineType.ZXSpectrum128Plus3:
                    if (_machine.UPDDiskDevice.FDD_FLAG_MOTOR)
                        x1ffd |= 0x08;
                    if (_machine.SpecialPagingMode)
                    {
                        x1ffd |= 0x01;
                        switch (_machine.PagingConfiguration)
                        {
                            case 1:
                                x1ffd |= 0x02;
                                break;
                            case 2:
                                x1ffd |= 0x04;
                                break;
                            case 3:
                                x1ffd |= 0x02;
                                x1ffd |= 0x04;
                                break; 
                        }
                    }
                    else
                    {
                        if (_machine.ROMhigh)
                            x1ffd |= 0x04;
                    } 
                    if (_machine.ROMlow)
                        x7ffd |= 0x10;                    
                    if (_machine.SHADOWPaged)
                        x7ffd |= 0x08;
                    if (_machine.PagingDisabled)
                        x7ffd |= 0x20;
                    break;
            }

            s.ch7ffd = x7ffd;
            s.unionPage = x1ffd;
            return s;
        }

        private ZXSTKEYBOARD GetZXSTKEYBOARD()
        {
            var s = new ZXSTKEYBOARD();
            s.dwFlags = 0; //no issue 2 emulation
            s.chKeyboardJoystick |= (byte)JoystickTypes.ZXSTKJT_NONE;
            return s;
        }

        private ZXSTJOYSTICK GetZXSTJOYSTICK()
        {
            var s = new ZXSTJOYSTICK();
            s.dwFlags = 0; //depreciated
            s.chTypePlayer1 |= (byte)JoystickTypes.ZXSTKJT_KEMPSTON;
            s.chTypePlayer2 |= (byte)JoystickTypes.ZXSTKJT_SINCLAIR1;
            return s;
        }

        private ZXSTAYBLOCK GetZXSTAYBLOCK()
        {
            var s = new ZXSTAYBLOCK();
            s.cFlags = 0; // no external units
            s.chCurrentRegister = (byte)_machine.AYDevice.SelectedRegister;
            var regs = _machine.AYDevice.ExportRegisters();
            s.chAyRegs = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                s.chAyRegs[i] = (byte)regs[i];
            }
            return s;
        }

        private ZXSTTAPE GetZXSTTAPE()
        {
            var s = new ZXSTTAPE();
            s.wFlags |= (int)CassetteRecorderState.ZXSTTP_EMBEDDED;
            s.wCurrentBlockNo = (ushort)_machine.TapeDevice.CurrentDataBlockIndex;
            s.dwCompressedSize = _machine.tapeImages[_machine.TapeDevice.CurrentDataBlockIndex].Length;
            s.dwUncompressedSize = _machine.tapeImages[_machine.TapeDevice.CurrentDataBlockIndex].Length;
            char[] ext = "tzx".ToCharArray();
            s.szFileExtension = new char[16];
            for (int f = 1; f < ext.Length; f++)
            {
                s.szFileExtension[f - 1] = ext[f];
            }
            return s;
        }

        private ZXSTPLUS3 GetZXSTPLUS3()
        {
            var s = new ZXSTPLUS3();
            s.chNumDrives = 1;
            s.fMotorOn = _machine.UPDDiskDevice.FDD_FLAG_MOTOR ? (byte)1 : (byte)0;
            return s;
        }

        private ZXSTDSKFILE GetZXSTDSKFILE()
        {
            var s = new ZXSTDSKFILE();
            s.wFlags = 0;
            s.chDriveNum = 0;
            s.dwUncompressedSize = 0;
            return s;
        }
    }
}
