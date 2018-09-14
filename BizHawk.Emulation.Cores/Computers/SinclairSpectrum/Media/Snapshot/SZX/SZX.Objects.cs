using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// Structs, Constants and Enums
    /// http://www.spectaculator.com/docs/zx-state/intro.shtml
    /// </summary>
    public partial class SZX
    {
        #region ZX-State Header

        public enum MachineIdentifier : byte
        {
            ZXSTMID_16K             = 0,
            ZXSTMID_48K             = 1,
            ZXSTMID_128K            = 2,
            ZXSTMID_PLUS2           = 3,
            ZXSTMID_PLUS2A          = 4,
            ZXSTMID_PLUS3           = 5,
            ZXSTMID_PLUS3E          = 6,
            ZXSTMID_PENTAGON128     = 7,
            ZXSTMID_TC2048          = 8,
            ZXSTMID_TC2068          = 9,
            ZXSTMID_SCORPION        = 10,
            ZXSTMID_SE              = 11,
            ZXSTMID_TS2068          = 12,
            ZXSTMID_PENTAGON512     = 13,
            ZXSTMID_PENTAGON1024    = 14,
            ZXSTMID_NTSC48K         = 15,
            ZXSTMID_128KE           = 16
        }

        /// <summary>
        /// If set, the emulated Spectrum uses alternate timings (one cycle later than normal timings). If reset, the emulated Spectrum uses standard timings. 
        /// This flag is only applicable for the ZXSTMID_16K, ZXSTMID_48K and ZXSTMID_128K models.
        /// </summary>
        public const int ZXSTMF_ALTERNATETIMINGS = 1;

        /// <summary>
        /// The zx-state header appears right at the start of a zx-state (.szx) file.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTHEADER
        {
            public uint dwMagic;
            public byte chMajorVersion;
            public byte chMinorVersion;
            public byte chMachineId;
            public byte chFlags;
        }

        #endregion

        #region ZXSTBLOCK Header

        /// <summary>
        /// Block Header. Each real block starts with this header.
        /// </summary>
        public struct ZXSTBLOCK
        {
            public uint dwId;
            public uint dwSize;
        }

        #endregion

        #region ZXSTCREATOR

        /// <summary>
        /// This block identifies the program that created this zx-state file.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTCREATOR
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szCreator;
            public short chMajorVersion;
            public short chMinorVersion;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] chData;
        }

        #endregion

        #region ZXSTZ80REGS

        /// <summary>
        /// The last instruction executed was an EI instruction or an invalid $DD or $FD prefix.
        /// </summary>
        public const int ZXSTZF_EILAST = 1;
        /// <summary>
        /// The last instruction executed was a HALT instruction. The CPU is currently executing NOPs and will continue to do so until the next interrupt occurs. 
        /// This flag is mutually exclusive with ZXSTZF_EILAST.
        /// </summary>
        public const int ZXSTZF_HALTED = 2;

        /// <summary>
        /// Contains the Z80 registers and other internal state values. It does not contain any specific model registers.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTZ80REGS
        {
            public ushort AF, BC, DE, HL;
            public ushort AF1, BC1, DE1, HL1;
            public ushort IX, IY, SP, PC;
            public byte I;
            public byte R;
            public byte IFF1, IFF2;
            public byte IM;
            public uint dwCyclesStart;
            public byte chHoldIntReqCycles;
            public byte chFlags;
            public ushort wMemPtr;
        }

        #endregion

        #region ZXSTSPECREGS

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTSPECREGS
        {
            public byte chBorder;
            public byte ch7ffd;
            public byte unionPage;
            public byte chFe;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] chReserved;
        }

        #endregion

        #region ZXSTAYBLOCK

        /// <summary>
        /// Fuller Box emulation
        /// </summary>
        public const int ZXSTAYF_FULLERBOX = 1;
        /// <summary>
        /// Melodik Soundbox emulation. 
        /// This is essentially an AY chip for older Spectrums that uses the same ports as that found in 128k Spectrums
        /// </summary>
        public const int ZXSTAYF_128AY = 2;

        /// <summary>
        /// The state of the AY chip found in all 128k Spectrums, Pentagons, Scorpions and Timex machines. 
        /// This block may also be present for 16k/48k Spectrums if Fuller Box or Melodik emulation is enabled.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTAYBLOCK
        {
            public byte cFlags;
            public byte chCurrentRegister;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] chAyRegs;
        }

        #endregion

        #region ZXSTRAMPAGE

        /// <summary>
        /// Ram pages are compressed using Zlib
        /// </summary>
        public const int ZXSTRF_COMPRESSED = 1;

        /// <summary>
        /// zx-state files will contain a number of 16KB RAM page blocks, depending on the specific Spectrum model.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTRAMPAGE
        {
            public ushort wFlags;
            public byte chPageNo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4000)]
            public byte[] ramPage;
        }

        #endregion

        #region ZXSTKEYBOARD

        /// <summary>
        /// Keyboard state
        /// </summary>
        public const int ZXSTKF_ISSUE2 = 1;

        /// <summary>
        /// Supported joystick types
        /// </summary>
        public enum JoystickTypes
        {
            ZXSTKJT_KEMPSTON        = 0,
            ZXSTKJT_FULLER          = 1,
            ZXSTKJT_CURSOR          = 2,
            ZXSTKJT_SINCLAIR1       = 3,
            ZXSTKJT_SINCLAIR2       = 4,
            ZXSTKJT_SPECTRUMPLUS    = 5,
            ZXSTKJT_TIMEX1          = 6,
            ZXSTKJT_TIMEX2          = 7,
            ZXSTKJT_NONE            = 8
        }

        /// <summary>
        /// The state of the Spectrum keyboard and any keyboard joystick emulation.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTKEYBOARD
        {
            public uint dwFlags;
            public byte chKeyboardJoystick;
        }

        #endregion

        #region ZXSTJOYSTICK

        /// <summary>
        /// Joystick setup for both players.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTJOYSTICK
        {
            public uint dwFlags;
            public byte chTypePlayer1;
            public byte chTypePlayer2;
        }

        #endregion

        #region ZXSTTAPE

        /// <summary>
        /// Cassette Recorder state
        /// </summary>
        public enum CassetteRecorderState
        {
            ZXSTTP_EMBEDDED     = 1,
            ZXSTTP_COMPRESSED   = 2
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTTAPE
        {
            public ushort wCurrentBlockNo;
            public ushort wFlags;
            public int dwUncompressedSize;
            public int dwCompressedSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public char[] szFileExtension;
        }

        #endregion

        #region ZXSTPLUS3

        /// <summary>
        /// The number of drives connected to the Spectrum +3 and whether their motors are turned on. 
        /// Any blocks specifying which disk files are in which drive will follow this one.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTPLUS3
        {
            public byte chNumDrives;
            public byte fMotorOn;
        }

        #endregion

        #region ZXSTDSKFILE

        /// <summary>
        /// Not implemented. All disk images are currently links to external .dsk or .ipf files
        /// </summary>
        public const int ZXSTDSKF_COMPRESSED = 1;
        /// <summary>
        /// Not implemented. All disk images are currently links to external .dsk or .ipf files
        /// </summary>
        public const int ZXSTDSKF_EMBEDDED = 2;
        /// <summary>
        /// When a double-sided disk is inserted into a single-sided drive, specifies the side being read from/written to. 
        /// If set, Side B is the active side, otherwise it is Side A.
        /// </summary>
        public const int ZXSTDSKF_SIDEB = 3;

        /// <summary>
        /// Each +3 disk drive that has a disk inserted in it will have one of these blocks. 
        /// They follow the ZXSTPLUS3 block which identifies the number of drives.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ZXSTDSKFILE
        {
            public ushort wFlags;
            public byte chDriveNum;
            public int dwUncompressedSize;
        }

        #endregion

        #region Not Yet Implemented

        #region ZXSTATASP

        #endregion

        #region ZXSTATARAM

        #endregion

        #region ZXSTCF

        #endregion

        #region ZXSTCFRAM

        #endregion

        #region ZXSTCOVOX

        #endregion

        #region ZXSTBETA128

        #endregion

        #region ZXSTBETADISK

        #endregion

        #region ZXSTDOCK

        #endregion

        #region ZXSTGS

        #endregion

        #region ZXSTGSRAMPAGE

        #endregion

        #region ZXSTIF1

        #endregion

        #region ZXSTIF2ROM

        #endregion

        #region ZXSTMCART

        #endregion

        #region ZXSTMOUSE

        #endregion

        #region ZXSTMULTIFACE

        #endregion

        #region ZXSTOPUS

        #endregion

        #region ZXSTOPUSDISK

        #endregion

        #region ZXSTPLUSD

        #endregion

        #region ZXSTPLUSDDISK

        #endregion

        #region ZXSTROM

        #endregion

        #region ZXSTSCLDREGS

        #endregion

        #region ZXSTSIDE

        #endregion

        #region ZXSTSPECDRUM

        #endregion

        #region ZXSTUSPEECH

        #endregion

        #region ZXSTZXPRINTER

        #endregion

        #endregion        
    }
}
