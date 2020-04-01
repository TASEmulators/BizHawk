/***********************************************************************
Copyright 2016 Stefan Hausotte

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*************************************************************************/

using System;

namespace PeNet
{
    /// <summary>
    ///     This class contains constants and flags which are used in a PE file.
    ///     The constants can be used to map a numeric value to an understandable string.
    /// </summary>
    public static class Constants
    {
        ////////////////////////
        // IMAGE_DATA_DIRECTORY
        ////////////////////////

        /// <summary>
        ///     The data directory indices used to resolve
        ///     which directory is which.
        /// </summary>
        public enum DataDirectoryIndex
        {
            /// <summary>
            ///     Export directory.
            /// </summary>
            Export = 0,

            /// <summary>
            ///     Import directory.
            /// </summary>
            Import = 1,

            /// <summary>
            ///     Resource directory.
            /// </summary>
            Resource = 2,

            /// <summary>
            ///     Exception directory for x64.
            /// </summary>
            Exception = 3,

            /// <summary>
            ///     Security directory.
            /// </summary>
            Security = 4,

            /// <summary>
            ///     Relocation directory.
            /// </summary>
            BaseReloc = 5,

            /// <summary>
            ///     Debug directory.
            /// </summary>
            Debug = 6,

            /// <summary>
            ///     Copyright directory (useless).
            /// </summary>
            Copyright = 7,

            /// <summary>
            ///     Global Pointer directory. Only interesting for Itanium systems.
            /// </summary>
            Globalptr = 8,

            /// <summary>
            ///     Thread Local Storage directory.
            /// </summary>
            TLS = 9,

            /// <summary>
            ///     Load Config directory.
            /// </summary>
            LoadConfig = 0xA,

            /// <summary>
            ///     Bound Import directory. Precomputed import addresses
            ///     to speed up module loading.
            /// </summary>
            BoundImport = 0xB,

            /// <summary>
            ///     Import Address Table directory.
            /// </summary>
            IAT = 0xC,

            /// <summary>
            ///     Delayed Import directory. Imports which are loaded
            ///     with a delay for performance reasons.
            /// </summary>
            DelayImport = 0xD,

            /// <summary>
            ///     COM Descriptor directory. For the .Net Header
            /// </summary>
            COM_Descriptor = 0xE,

            /// <summary>
            ///     Reserved for future use.
            /// </summary>
            Reserved = 0xF
        }

        /////////////////////////////
        // PE File characteristics
        /////////////////////////////

        /// <summary>
        ///     File characteristics from the file header.
        /// </summary>
        [Flags]
        public enum FileHeaderCharacteristics : ushort
        {
            /// <summary>
            ///     Relocation stripped.
            /// </summary>
            IMAGE_FILE_RELOCS_STRIPPED = 0x01,

            /// <summary>
            ///     Executable image.
            /// </summary>
            IMAGE_FILE_EXECUTABLE_IMAGE = 0x02,

            /// <summary>
            ///     Line numbers stripped.
            /// </summary>
            IMAGE_FILE_LINE_NUMS_STRIPPED = 0x04,

            /// <summary>
            ///     Local symbols stripped.
            /// </summary>
            IMAGE_FILE_LOCAL_SYMS_STRIPPED = 0x08,

            /// <summary>
            ///     (OBSOLTETE) Aggressively trim the working set.
            /// </summary>
            IMAGE_FILE_AGGRESIVE_WS_TRIM = 0x10,

            /// <summary>
            ///     Application can handle addresses larger than 2 GB.
            /// </summary>
            IMAGE_FILE_LARGE_ADDRESS_AWARE = 0x20,

            /// <summary>
            ///     (OBSOLTETE) Bytes of word are reversed.
            /// </summary>
            IMAGE_FILE_BYTES_REVERSED_LO = 0x80,

            /// <summary>
            ///     Supports 32 Bit words.
            /// </summary>
            IMAGE_FILE_32BIT_MACHINE = 0x100,

            /// <summary>
            ///     Debug stripped and stored in a separate file.
            /// </summary>
            IMAGE_FILE_DEBUG_STRIPPED = 0x200,

            /// <summary>
            ///     If the image is on a removable media, copy and run it from the swap file.
            /// </summary>
            IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP = 0x400,

            /// <summary>
            ///     If the image is on the network, copy and run it from the swap file.
            /// </summary>
            IMAGE_FILE_NET_RUN_FROM_SWAP = 0x800,

            /// <summary>
            ///     The image is a system file.
            /// </summary>
            IMAGE_FILE_SYSTEM = 0x1000,

            /// <summary>
            ///     Is a dynamic loaded library and executable but cannot
            ///     be run on its own.
            /// </summary>
            IMAGE_FILE_DLL = 0x2000,

            /// <summary>
            ///     Image should be run only on uniprocessor.
            /// </summary>
            IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000,

            /// <summary>
            ///     (OBSOLETE) Reserved.
            /// </summary>
            IMAGE_FILE_BYTES_REVERSED_HI = 0x8000
        }

        //////////////////////////////
        // IMAGE_FILE_HEADER constants
        //////////////////////////////

        /// <summary>
        ///     IMAGE_FILE_HEADER machine constants which define
        ///     for which CPU type the PE file is.
        /// </summary>
        [Flags]
        public enum FileHeaderMachine : ushort
        {
            /// <summary>
            ///     File header -> machine (CPU): unknown
            /// </summary>
            IMAGE_FILE_MACHINE_UNKNOWN = 0x0,

            /// <summary>
            ///     File header -> machine (CPU): Intel 386
            /// </summary>
            IMAGE_FILE_MACHINE_I386 = 0x14c,

            /// <summary>
            ///     File header -> machine (CPU): Intel i860
            /// </summary>
            IMAGE_FILE_MACHINE_I860 = 0x14d,

            /// <summary>
            ///     File header -> machine (CPU): MIPS R3000
            /// </summary>
            IMAGE_FILE_MACHINE_R3000 = 0x162,

            /// <summary>
            ///     File header -> machine (CPU): MIPS little endian (R4000)
            /// </summary>
            IMAGE_FILE_MACHINE_R4000 = 0x166,

            /// <summary>
            ///     File header -> machine (CPU): MIPS R10000
            /// </summary>
            IMAGE_FILE_MACHINE_R10000 = 0x168,

            /// <summary>
            ///     File header -> machine (CPU): MIPS little endian WCI v2
            /// </summary>
            IMAGE_FILE_MACHINE_WCEMIPSV2 = 0x169,

            /// <summary>
            ///     File header -> machine (CPU): old Alpha AXP
            /// </summary>
            IMAGE_FILE_MACHINE_OLDALPHA = 0x183,

            /// <summary>
            ///     File header -> machine (CPU): Alpha AXP
            /// </summary>
            IMAGE_FILE_MACHINE_ALPHA = 0x184,

            /// <summary>
            ///     File header -> machine (CPU): Hitachi SH3
            /// </summary>
            IMAGE_FILE_MACHINE_SH3 = 0x1a2,

            /// <summary>
            ///     File header -> machine (CPU): Hitachi SH3 DSP
            /// </summary>
            IMAGE_FILE_MACHINE_SH3DSP = 0x1a3,

            /// <summary>
            ///     File header -> machine (CPU): unknown
            /// </summary>
            IMAGE_FILE_MACHINE_SH3E = 0x1a4,

            /// <summary>
            ///     File header -> machine (CPU): Hitachi SH4
            /// </summary>
            IMAGE_FILE_MACHINE_SH4 = 0x1a6,

            /// <summary>
            ///     File header -> machine (CPU): Hitachi SH5
            /// </summary>
            IMAGE_FILE_MACHINE_SH5 = 0x1a8,

            /// <summary>
            ///     File header -> machine (CPU): ARM little endian
            /// </summary>
            IMAGE_FILE_MACHINE_ARM = 0x1c0,

            /// <summary>
            ///     File header -> machine (CPU): Thumb
            /// </summary>
            IMAGE_FILE_MACHINE_THUMB = 0x1c2,

            /// <summary>
            ///     File header -> machine (CPU): Matsushita AM33
            /// </summary>
            IMAGE_FILE_MACHINE_AM33 = 0x1d3,

            /// <summary>
            ///     File header -> machine (CPU): PowerPC little endian
            /// </summary>
            IMAGE_FILE_MACHINE_POWERPC = 0x1f0,

            /// <summary>
            ///     File header -> machine (CPU): PowerPC with floating point support
            /// </summary>
            IMAGE_FILE_MACHINE_POWERPCFP = 0x1f1,

            /// <summary>
            ///     File header -> machine (CPU): Intel IA64
            /// </summary>
            IMAGE_FILE_MACHINE_IA64 = 0x200,

            /// <summary>
            ///     File header -> machine (CPU): MIPS16
            /// </summary>
            IMAGE_FILE_MACHINE_MIPS16 = 0x266,

            /// <summary>
            ///     File header -> machine (CPU): Motorola 68000 series
            /// </summary>
            IMAGE_FILE_MACHINE_M68K = 0x268,

            /// <summary>
            ///     File header -> machine (CPU): Alpha AXP 64-bit
            /// </summary>
            IMAGE_FILE_MACHINE_ALPHA64 = 0x284,

            /// <summary>
            ///     File header -> machine (CPU): MIPS with FPU
            /// </summary>
            IMAGE_FILE_MACHINE_MIPSFPU = 0x366,

            /// <summary>
            ///     File header -> machine (CPU): MIPS16 with FPU
            /// </summary>
            IMAGE_FILE_MACHINE_MIPSFPU16 = 0x466,

            /// <summary>
            ///     File header -> machine (CPU): Alpha AXP 64-bit
            /// </summary>
            IMAGE_FILE_MACHINE_AXP64 = IMAGE_FILE_MACHINE_ALPHA64,

            /// <summary>
            ///     File header -> machine (CPU): unknown
            /// </summary>
            IMAGE_FILE_MACHINE_TRICORE = 0x520,

            /// <summary>
            ///     File header -> machine (CPU): unknown
            /// </summary>
            IMAGE_FILE_MACHINE_CEF = 0xcef,

            /// <summary>
            ///     File header -> machine (CPU): EFI Byte Code
            /// </summary>
            IMAGE_FILE_MACHINE_EBC = 0xebc,

            /// <summary>
            ///     File header -> machine (CPU): AMD AMD64 (Used for Intel x64, too)
            /// </summary>
            IMAGE_FILE_MACHINE_AMD64 = 0x8664,

            /// <summary>
            ///     File header -> machine (CPU): Mitsubishi M32R little endian
            /// </summary>
            IMAGE_FILE_MACHINE_M32R = 0x9041,

            /// <summary>
            ///     File header -> machine (CPU): clr pure MSIL (.Net)
            /// </summary>
            IMAGE_FILE_MACHINE_CEE = 0xc0ee
        }

        /// <summary>
        ///     Constants for the Optional header DllCharacteristics
        ///     property.
        /// </summary>
        [Flags]
        public enum OptionalHeaderDllCharacteristics : ushort
        {
            /// <summary>
            ///     DLL can be relocated at load time.
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE = 0x40,

            /// <summary>
            ///     Enforces integrity checks.
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_FORCE_INTEGRITY = 0x80,

            /// <summary>
            ///     Image is compatible with Data Execution Prevention (DEP).
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_NX_COMPAT = 0x100,

            /// <summary>
            ///     Image is isolation aware but should not be isolated.
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_NO_ISOLATION = 0x200,

            /// <summary>
            ///     No Secure Exception Handling (SEH)
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_NO_SEH = 0x400,

            /// <summary>
            ///     Do not bind the image.
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_NO_BIND,

            /// <summary>
            ///     Image is a WDM driver.
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_WDM_DRIVER = 0x2000,

            /// <summary>
            ///     Terminal server aware.
            /// </summary>
            IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE = 0x8000
        }


        //////////////////////////
        // IMAGE_OPTIONAL_HEADER
        //////////////////////////

        /// <summary>
        ///     Constants for the Optional header magic property.
        /// </summary>
        [Flags]
        public enum OptionalHeaderMagic : ushort
        {
            /// <summary>
            ///     The file is an 32 bit executable.
            /// </summary>
            IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10b,

            /// <summary>
            ///     The file is an 64 bit executable.
            /// </summary>
            IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20b,

            /// <summary>
            ///     The file is a ROM image.
            /// </summary>
            IMAGE_ROM_OPTIONAL_HDR_MAGIC = 0x107
        }

        /// <summary>
        ///     Constants for the Optional header subsystem property.
        /// </summary>
        [Flags]
        public enum OptionalHeaderSubsystem : ushort
        {
            /// <summary>
            ///     System driver.
            /// </summary>
            IMAGE_SUBSYSTEM_NATIVE = 0x01,

            /// <summary>
            ///     GUI Subsystem.
            /// </summary>
            IMAGE_SUBSYSTEM_WINDOWS_GUI = 0x02,

            /// <summary>
            ///     Console Subsystem.
            /// </summary>
            IMAGE_SUBSYSTEM_WINDOWS_CUI = 0x03
        }


        //////////////////////////////////
        // IMAGE_RESOURCE_DIRECTORY_ENTRY
        //////////////////////////////////

        /// <summary>
        ///     Mapping from Resources Group ID to a meaningful
        ///     string. Used for ID resources (opposite to named resource).
        /// </summary>
        public enum ResourceGroupIDs : uint
        {
            /// <summary>
            ///     Cursor resource.
            /// </summary>
            Cursor = 1,

            /// <summary>
            ///     Bitmap resource.
            /// </summary>
            Bitmap = 2,

            /// <summary>
            ///     Icon resource.
            /// </summary>
            Icon = 3,

            /// <summary>
            ///     Menu resource.
            /// </summary>
            Menu = 4,

            /// <summary>
            ///     Dialog resource.
            /// </summary>
            Dialog = 5,

            /// <summary>
            ///     String resource.
            /// </summary>
            String = 6,

            /// <summary>
            ///     Font Directory resource.
            /// </summary>
            FontDirectory = 7,

            /// <summary>
            ///     Fonst resource.
            /// </summary>
            Fonst = 8,

            /// <summary>
            ///     Accelerator resource.
            /// </summary>
            Accelerator = 9,

            /// <summary>
            ///     RC Data resource.
            /// </summary>
            RcData = 10,

            /// <summary>
            ///     Message Table resource.
            /// </summary>
            MessageTable = 11,

            /// <summary>
            ///     Group Icon resource.
            /// </summary>
            GroupIcon = 14,

            /// <summary>
            ///     Version resource.
            /// </summary>
            Version = 16,

            /// <summary>
            ///     Dlg Include resource.
            /// </summary>
            DlgInclude = 17,

            /// <summary>
            ///     Plug and Play resource.
            /// </summary>
            PlugAndPlay = 19,

            /// <summary>
            ///     VXD resource.
            /// </summary>
            VXD = 20,

            /// <summary>
            ///     Animated Cursor resource.
            /// </summary>
            AnimatedCurser = 21,

            /// <summary>
            ///     Animated Icon resource.
            /// </summary>
            AnimatedIcon = 22,

            /// <summary>
            ///     HTML resource.
            /// </summary>
            HTML = 23,

            /// <summary>
            ///     Manifest resource.
            /// </summary>
            Manifest = 24
        }

        ////////////////////////
        // IMAGE_SECTION_HEADER
        ////////////////////////

        /// <summary>
        ///     The SectionFlags enumeration lists all possible flags which can
        ///     be set in the section characteristics.
        /// </summary>
        [Flags]
        public enum SectionFlags : uint
        {
            /// <summary>
            ///     Reserved.
            /// </summary>
            IMAGE_SCN_TYPE_NO_PAD = 0x00000008,

            /// <summary>
            ///     Section contains code.
            /// </summary>
            IMAGE_SCN_CNT_CODE = 0x00000020,

            /// <summary>
            ///     Section contains initialized data.
            /// </summary>
            IMAGE_SCN_CNT_INITIALIZED_DATA = 0x00000040,

            /// <summary>
            ///     Section contains uninitialized data.
            /// </summary>
            IMAGE_SCN_CNT_UNINITIALIZED_DATA = 0x00000080,

            /// <summary>
            ///     Reserved.
            /// </summary>
            IMAGE_SCN_LNK_OTHER = 0x00000100,

            /// <summary>
            ///     Section contains comments or some  other type of information.
            /// </summary>
            IMAGE_SCN_LNK_INFO = 0x00000200,

            /// <summary>
            ///     Section contents will not become part of image.
            /// </summary>
            IMAGE_SCN_LNK_REMOVE = 0x00000800,

            /// <summary>
            ///     Section contents comdat.
            /// </summary>
            IMAGE_SCN_LNK_COMDAT = 0x00001000,

            /// <summary>
            ///     Reset speculative exceptions handling bits in the TLB entries for this section.
            /// </summary>
            IMAGE_SCN_NO_DEFER_SPEC_EXC = 0x00004000,

            /// <summary>
            ///     Section content can be accessed relative to GP.
            /// </summary>
            IMAGE_SCN_GPREL = 0x00008000,

            /// <summary>
            ///     Unknown.
            /// </summary>
            IMAGE_SCN_MEM_FARDATA = 0x00008000,

            /// <summary>
            ///     Unknown.
            /// </summary>
            IMAGE_SCN_MEM_PURGEABLE = 0x00020000,

            /// <summary>
            ///     Unknown.
            /// </summary>
            IMAGE_SCN_MEM_16BIT = 0x00020000,

            /// <summary>
            ///     Unknown.
            /// </summary>
            IMAGE_SCN_MEM_LOCKED = 0x00040000,

            /// <summary>
            ///     Unknown.
            /// </summary>
            IMAGE_SCN_MEM_PRELOAD = 0x00080000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_1BYTES = 0x00100000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_2BYTES = 0x00200000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_4BYTES = 0x00300000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_8BYTES = 0x00400000,

            /// <summary>
            ///     Default alignment if no others are specified.
            /// </summary>
            IMAGE_SCN_ALIGN_16BYTES = 0x00500000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_32BYTES = 0x00600000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_64BYTES = 0x00700000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_128BYTES = 0x00800000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_256BYTES = 0x00900000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_512BYTES = 0x00A00000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_1024BYTES = 0x00B00000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_2048BYTES = 0x00C00000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_4096BYTES = 0x00D00000,

            /// <summary>
            ///     Section alignment.
            /// </summary>
            IMAGE_SCN_ALIGN_8192BYTES = 0x00E00000,

            /// <summary>
            ///     Alignment mask.
            /// </summary>
            IMAGE_SCN_ALIGN_MASK = 0x00F00000,

            /// <summary>
            ///     Section contains extended relocations.
            /// </summary>
            IMAGE_SCN_LNK_NRELOC_OVFL = 0x01000000,

            /// <summary>
            ///     Section can be discarded.
            /// </summary>
            IMAGE_SCN_MEM_DISCARDABLE = 0x02000000,

            /// <summary>
            ///     Section is not cache-able.
            /// </summary>
            IMAGE_SCN_MEM_NOT_CACHED = 0x04000000,

            /// <summary>
            ///     Section is not page-able.
            /// </summary>
            IMAGE_SCN_MEM_NOT_PAGED = 0x08000000,

            /// <summary>
            ///     Section is shareable.
            /// </summary>
            IMAGE_SCN_MEM_SHARED = 0x10000000,

            /// <summary>
            ///     Section is executable.
            /// </summary>
            IMAGE_SCN_MEM_EXECUTE = 0x20000000,

            /// <summary>
            ///     Section is readable.
            /// </summary>
            IMAGE_SCN_MEM_READ = 0x40000000,

            /// <summary>
            ///     Section is write-able.
            /// </summary>
            IMAGE_SCN_MEM_WRITE = 0x80000000
        }

        /////////////////
        // UNWINDE_CODE
        /////////////////

        /// <summary>
        ///     UnwindOp Codes for the unwind information
        ///     used to walk the stack in x64 applications.
        /// </summary>
        public enum UnwindOpCodes : byte
        {
            /// <summary>
            ///     Push a non volatile integer.
            /// </summary>
            UWOP_PUSH_NONVOL = 0,

            /// <summary>
            ///     Allocate large size on stack.
            /// </summary>
            UWOP_ALLOC_LARGE = 1,

            /// <summary>
            ///     Allocate small size on stack.
            /// </summary>
            UWOP_ALLOC_SMALL = 2,

            /// <summary>
            ///     Establish frame pointer register.
            /// </summary>
            UWOP_SET_FPREG = 3,

            /// <summary>
            ///     Save non volatile register to stack by a MOV.
            /// </summary>
            UWOP_SAVE_NONVOL = 4,

            /// <summary>
            ///     Save non volatile register to stack with
            ///     a long offset by a MOV.
            /// </summary>
            UWOP_SAVE_NONVOL_FAR = 5,

            /// <summary>
            ///     Save a XMM (128 bit) register to the stack.
            /// </summary>
            UWOP_SAVE_XMM128 = 8,

            /// <summary>
            ///     Save a XMM (128 bit) register to the stack
            ///     with a long offset.
            /// </summary>
            UWOP_SAVE_XMM128_FAR = 9,

            /// <summary>
            ///     Push a machine frame, which is used to record the effect
            ///     of a hardware interrupt.
            /// </summary>
            UWOP_PUSH_MACHFRAME = 10
        }

        //////////////////////////////////////
        // WIN_CERTIFICATE wCertificateType
        //////////////////////////////////////

        /// <summary>
        ///     WIN_CERTIFICATE wCertificateType constants.
        /// </summary>
        [Flags]
        public enum WinCertificateType : ushort
        {
            /// <summary>
            ///     Certificate is X509 standard.
            /// </summary>
            WIN_CERT_TYPE_X509 = 0x0001,

            /// <summary>
            ///     Certificate is PKCS signed data.
            /// </summary>
            WIN_CERT_TYPE_PKCS_SIGNED_DATA = 0x0002,

            /// <summary>
            ///     Reserved
            /// </summary>
            WIN_CERT_TYPE_RESERVED_1 = 0x0003,

            /// <summary>
            ///     Certificate is PKCS1 signature.
            /// </summary>
            WIN_CERT_TYPE_PKCS1_SIGN = 0x0009
        }
    }
}