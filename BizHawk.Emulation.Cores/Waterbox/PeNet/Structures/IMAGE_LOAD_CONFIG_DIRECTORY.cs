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

using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    /// The IMAGE_LOAD_CONFIG_DIRECTORY hold information
    /// important to load the PE file correctly.
    /// </summary>
    public class IMAGE_LOAD_CONFIG_DIRECTORY : AbstractStructure
    {
        private readonly bool _is64Bit;

        /// <summary>
        /// Create a new IMAGE_LOAD_CONFIG_DIRECTORY object.
        /// </summary>
        /// <param name="buff">Byte buffer with a PE file as content.</param>
        /// <param name="offset">Offset of the structure in the buffer.</param>
        /// <param name="is64Bit">Flag if the PE file is 64 Bit.</param>
        public IMAGE_LOAD_CONFIG_DIRECTORY(byte[] buff, uint offset, bool is64Bit) 
            : base(buff, offset)
        {
            _is64Bit = is64Bit;
        }

        /// <summary>
        /// SIze of the IMAGE_LOAD_CONFIG_DIRECTORY structure.
        /// </summary>
        public uint Size
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        /// Time and date stamp. Shows seconds elapsed since 00:00:00, January 1, 1970
        /// in UCT.
        /// </summary>
        public uint TimeDateStamp
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        /// Major version number.
        /// </summary>
        public ushort MajorVesion
        {
            get { return Buff.BytesToUInt16(Offset + 0x8); }
            set { Buff.SetUInt16(Offset + 0x8, value); }
        }

        /// <summary>
        /// Minor version number.
        /// </summary>
        public ushort MinorVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0xA); }
            set { Buff.SetUInt16(Offset + 0xA, value); }
        }

        /// <summary>
        /// GLobal flags to control system behavior.
        /// </summary>
        public uint GlobalFlagsClear
        {
            get { return Buff.BytesToUInt32(Offset + 0xC); }
            set { Buff.SetUInt32(Offset + 0xC, value); }
        }

        /// <summary>
        /// Global flags to control system behavior.
        /// </summary>
        public uint GlobalFlagsSet
        {
            get { return Buff.BytesToUInt32(Offset + 0x10); }
            set { Buff.SetUInt32(Offset + 0x10, value); }
        }

        /// <summary>
        /// Default time-out value for critical sections.
        /// </summary>
        public uint CriticalSectionDefaultTimeout
        {
            get { return Buff.BytesToUInt32(Offset + 0x14); }
            set { Buff.SetUInt32(Offset + 0x14, value); }
        }

        /// <summary>
        /// The size of the minimum block that has to be freed before it's freed in bytes.
        /// </summary>
        public ulong DeCommitFreeBlockThreshold
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x18) : Buff.BytesToUInt32(Offset + 0x18); }
            set
            {
                if (_is64Bit)
                    Buff.SetUInt64(Offset + 0x18, value);
                else
                    Buff.SetUInt32(Offset + 0x18, (uint) value);
            }
        }

        /// <summary>
        /// SIze of the minimum total heap memory that has to be freed before it is freed in bytes.
        /// </summary>
        public ulong DeCommitTotalFreeThreshold
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x20) : Buff.BytesToUInt32(Offset + 0x1c); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x20, value);
                else
                    Buff.SetUInt32(Offset + 0x1C, (uint) value);
            }
        }

        /// <summary>
        /// Virtual Address of a list with addresses where the LOCK prefix is used.
        /// Will be replaced by NOP instructions on single-processor systems. Only available on x86.
        /// </summary>
        public ulong LockPrefixTable
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x28) : Buff.BytesToUInt32(Offset + 0x20); }
            set
            {
                if (_is64Bit)
                    Buff.SetUInt64(Offset + 0x28, value);
                else
                    Buff.SetUInt32(Offset + 0x20, (uint) value);
            }
        }

        /// <summary>
        /// The maximum allocation size in bytes. Only used for debugging purposes.
        /// </summary>
        public ulong MaximumAllocationSize
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x30) : Buff.BytesToUInt32(Offset + 0x24); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x30, value);
                else
                    Buff.SetUInt32(Offset + 0x24, (uint) value);
            }
        }

        /// <summary>
        /// The maximum block size that can be allocated from heap segments in bytes.
        /// </summary>
        public ulong VirtualMemoryThershold
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x38) : Buff.BytesToUInt32(Offset + 0x28); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x38, value);
                else
                    Buff.SetUInt32(Offset + 0x28, (uint) value);
            }
        }

        /// <summary>
        /// The processor affinity mask defines on which CPU the executable should run.
        /// </summary>
        public ulong ProcessAffinityMask
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x40) : Buff.BytesToUInt32(Offset + 0x30); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x40, value);
                else
                    Buff.SetUInt32(Offset + 0x30, (uint) value);
            }
        }

        /// <summary>
        /// The process heap flags.
        /// </summary>
        public uint ProcessHeapFlags
        {
            get { return _is64Bit ? Buff.BytesToUInt32(Offset + 0x48) : Buff.BytesToUInt32(Offset + 0x2C); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt32(Offset + 0x48, value);
                else
                    Buff.SetUInt32(Offset + 0x2C, value);
            }
        }

        /// <summary>
        /// Service pack version.
        /// </summary>
        public ushort CSDVersion
        {
            get { return _is64Bit ? Buff.BytesToUInt16(Offset + 0x4C) : Buff.BytesToUInt16(Offset + 0x34); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt16(Offset + 0x4C, value);
                else
                    Buff.SetUInt16(Offset + 0x34, value);
            }
        }

        /// <summary>
        /// Reserved for use by the operating system.
        /// </summary>
        public ushort Reserved1
        {
            get { return _is64Bit ? Buff.BytesToUInt16(Offset + 0x4E) : Buff.BytesToUInt16(Offset + 0x36); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt16(Offset + 0x4E, value);
                else
                    Buff.SetUInt16(Offset + 0x36, value);
            }
        }

        /// <summary>
        /// Reserved for use by the operating system.
        /// </summary>
        public ulong EditList
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x50) : Buff.BytesToUInt32(Offset + 0x38); }
            set
            {
                if (_is64Bit)
                    Buff.SetUInt64(Offset + 0x50, value);
                else
                    Buff.SetUInt32(Offset + 0x38, (uint) value);
            }
        }

        /// <summary>
        /// Pointer to a cookie used by Visual C++ or GS implementation.
        /// </summary>
        public ulong SecurityCoockie
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x58) : Buff.BytesToUInt32(Offset + 0x3C); }
            set
            {
                if (_is64Bit)
                    Buff.SetUInt64(Offset + 0x58, value);
                else
                    Buff.SetUInt32(Offset + 0x3C, (uint) value);
            }
        }

        /// <summary>
        /// Virtual Address of a sorted table of RVAs of each valid and unique handler in the image.
        /// Only available on x86.
        /// </summary>
        public ulong SEHandlerTable
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x60) : Buff.BytesToUInt32(Offset + 0x40); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x60, value);
                else
                    Buff.SetUInt32(Offset + 0x40, (uint) value);
            }
        }

        /// <summary>
        /// Count of unique exception handlers in the table. Only available on x86.
        /// </summary>
        public ulong SEHandlerCount
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x68) : Buff.BytesToUInt32(Offset + 0x44); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x68, value);
                else
                    Buff.SetUInt32(Offset + 0x44, (uint) value);
            }
        }

        /// <summary>
        /// Control flow guard (Win 8.1 and up) function pointer.
        /// </summary>
        public ulong GuardCFCheckFunctionPointer
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x70) : Buff.BytesToUInt32(Offset + 0x48); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x70, value);
                else
                    Buff.SetUInt32(Offset + 0x4C, (uint) value);
            }
        }

        /// <summary>
        /// Reserved
        /// </summary>
        public ulong Reserved2
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x78) : Buff.BytesToUInt32(Offset + 0x4C); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x78, value);
                else
                    Buff.SetUInt32(Offset + 0x4C, (uint) value);
            }
        }

        /// <summary>
        /// Pointer to the control flow guard function table. Only on Win 8.1 and up.
        /// </summary>
        public ulong GuardCFFunctionTable
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x80) : Buff.BytesToUInt32(Offset + 0x50); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x80, value);
                else
                    Buff.SetUInt32(Offset + 0x50, (uint) value);
            }
        }

        /// <summary>
        /// Count of functions under control flow guard. Only on Win 8.1 and up.
        /// </summary>
        public ulong GuardCFFunctionCount
        {
            get { return _is64Bit ? Buff.BytesToUInt64(Offset + 0x88) : Buff.BytesToUInt32(Offset + 0x54); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0x88, value);
                else
                    Buff.SetUInt32(Offset + 0x54, (uint) value);
            }
        }

        /// <summary>
        /// Flags for the control flow guard. Only on Win 8.1 and up.
        /// </summary>
        public uint GuardFlags
        {
            get { return _is64Bit ? Buff.BytesToUInt32(Offset + 0x90) : Buff.BytesToUInt32(Offset + 0x58); }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt32(Offset + 0x90, value);
                else
                    Buff.SetUInt32(Offset + 0x58, value);
            }
        }
    }
}