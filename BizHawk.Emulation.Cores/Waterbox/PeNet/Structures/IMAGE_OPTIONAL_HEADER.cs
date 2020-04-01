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
using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     Represents the optional header in
    ///     the NT header.
    /// </summary>
    public class IMAGE_OPTIONAL_HEADER : AbstractStructure
    {
        private readonly bool _is64Bit;

        /// <summary>
        ///     The Data Directories.
        /// </summary>
        public readonly IMAGE_DATA_DIRECTORY[] DataDirectory;

        /// <summary>
        ///     Create a new IMAGE_OPTIONAL_HEADER object.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset to the optional header.</param>
        /// <param name="is64Bit">Set to true, if header is for a x64 application.</param>
        public IMAGE_OPTIONAL_HEADER(byte[] buff, uint offset, bool is64Bit)
            : base(buff, offset)
        {
            _is64Bit = is64Bit;

            DataDirectory = new IMAGE_DATA_DIRECTORY[16];

            for (uint i = 0; i < 16; i++)
            {
                if (!_is64Bit)
                    DataDirectory[i] = new IMAGE_DATA_DIRECTORY(buff, offset + 0x60 + i*0x8);
                else
                    DataDirectory[i] = new IMAGE_DATA_DIRECTORY(buff, offset + 0x70 + i*0x8);
            }
        }

        /// <summary>
        ///     Flag if the file is x32, x64 or a ROM image.
        /// </summary>
        public ushort Magic
        {
            get { return Buff.BytesToUInt16(Offset); }
            set { Buff.SetUInt16(Offset, value); }
        }

        /// <summary>
        ///     Major linker version.
        /// </summary>
        public byte MajorLinkerVersion
        {
            get { return Buff[Offset + 0x2]; }
            set { Buff[Offset + 0x2] = value; }
        }

        /// <summary>
        ///     Minor linker version.
        /// </summary>
        public byte MinorLinkerVersion
        {
            get { return Buff[Offset + 0x3]; }
            set { Buff[Offset + 03] = value; }
        }

        /// <summary>
        ///     Size of all code sections together.
        /// </summary>
        public uint SizeOfCode
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        ///     Size of all initialized data sections together.
        /// </summary>
        public uint SizeOfInitializedData
        {
            get { return Buff.BytesToUInt32(Offset + 0x8); }
            set { Buff.SetUInt32(Offset + 0x8, value); }
        }

        /// <summary>
        ///     Size of all uninitialized data sections together.
        /// </summary>
        public uint SizeOfUninitializedData
        {
            get { return Buff.BytesToUInt32(Offset + 0xC); }
            set { Buff.SetUInt32(Offset + 0xC, value); }
        }

        /// <summary>
        ///     RVA of the entry point function.
        /// </summary>
        public uint AddressOfEntryPoint
        {
            get { return Buff.BytesToUInt32(Offset + 0x10); }
            set { Buff.SetUInt32(Offset + 0x10, value); }
        }

        /// <summary>
        ///     RVA to the beginning of the code section.
        /// </summary>
        public uint BaseOfCode
        {
            get { return Buff.BytesToUInt32(Offset + 0x14); }
            set { Buff.SetUInt32(Offset + 0x14, value); }
        }

        /// <summary>
        ///     RVA to the beginning of the data section.
        /// </summary>
        public uint BaseOfData
        {
            get { return _is64Bit ? 0 : Buff.BytesToUInt32(Offset + 0x18); }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x18, value);
                else
                    throw new Exception("IMAGE_OPTIONAL_HEADER->BaseOfCode does not exist in 64 bit applications.");
            }
        }

        /// <summary>
        ///     Preferred address of the image when it's loaded to memory.
        /// </summary>
        public ulong ImageBase
        {
            get
            {
                return _is64Bit
                    ? Buff.BytesToUInt64(Offset + 0x18)
                    : Buff.BytesToUInt32(Offset + 0x1C);
            }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x1C, (uint) value);
                else
                    Buff.SetUInt64(Offset + 0x18, value);
            }
        }

        /// <summary>
        ///     Section alignment in memory in bytes. Must be greater or equal to the file alignment.
        /// </summary>
        public uint SectionAlignment
        {
            get { return Buff.BytesToUInt32(Offset + 0x20); }
            set { Buff.SetUInt32(Offset + 0x20, value); }
        }

        /// <summary>
        ///     File alignment of the raw data of the sections in bytes.
        /// </summary>
        public uint FileAlignment
        {
            get { return Buff.BytesToUInt32(Offset + 0x24); }
            set { Buff.SetUInt32(Offset + 0x24, value); }
        }

        /// <summary>
        ///     Major operation system version to run the file.
        /// </summary>
        public ushort MajorOperatingSystemVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x28); }
            set { Buff.SetUInt16(Offset + 0x28, value); }
        }

        /// <summary>
        ///     Minor operation system version to run the file.
        /// </summary>
        public ushort MinorOperatingSystemVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x2A); }
            set { Buff.SetUInt16(Offset + 0x2A, value); }
        }

        /// <summary>
        ///     Major image version.
        /// </summary>
        public ushort MajorImageVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x2C); }
            set { Buff.SetUInt16(Offset + 0x2C, value); }
        }

        /// <summary>
        ///     Minor image version.
        /// </summary>
        public ushort MinorImageVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x2E); }
            set { Buff.SetUInt16(Offset + 0x2E, value); }
        }

        /// <summary>
        ///     Major version of the subsystem.
        /// </summary>
        public ushort MajorSubsystemVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x30); }
            set { Buff.SetUInt16(Offset + 0x30, value); }
        }

        /// <summary>
        ///     Minor version of the subsystem.
        /// </summary>
        public ushort MinorSubsystemVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x32); }
            set { Buff.SetUInt16(Offset + 0x32, value); }
        }

        /// <summary>
        ///     Reserved and must be 0.
        /// </summary>
        public uint Win32VersionValue
        {
            get { return Buff.BytesToUInt32(Offset + 0x34); }
            set { Buff.SetUInt32(Offset + 0x34, value); }
        }

        /// <summary>
        ///     Size of the image including all headers in bytes. Must be a multiple of
        ///     the section alignment.
        /// </summary>
        public uint SizeOfImage
        {
            get { return Buff.BytesToUInt32(Offset + 0x38); }
            set { Buff.SetUInt32(Offset + 0x38, value); }
        }

        /// <summary>
        ///     Sum of the e_lfanwe from the DOS header, the 4 byte signature, size of
        ///     the file header, size of the optional header and size of all section.
        ///     Rounded to the next file alignment.
        /// </summary>
        public uint SizeOfHeaders
        {
            get { return Buff.BytesToUInt32(Offset + 0x3C); }
            set { Buff.SetUInt32(Offset + 0x3C, value); }
        }

        /// <summary>
        ///     Image checksum validated at runtime for drivers, DLLs loaded at boot time and
        ///     DLLs loaded into a critical system.
        /// </summary>
        public uint CheckSum
        {
            get { return Buff.BytesToUInt32(Offset + 0x40); }
            set { Buff.SetUInt32(Offset + 0x40, value); }
        }

        /// <summary>
        ///     The subsystem required to run the image e.g., Windows GUI, XBOX etc.
        ///     Can be resolved to a string with Utility.ResolveSubsystem(subsystem=
        /// </summary>
        public ushort Subsystem
        {
            get { return Buff.BytesToUInt16(Offset + 0x44); }
            set { Buff.SetUInt16(Offset + 0x44, value); }
        }

        /// <summary>
        ///     DLL characteristics of the image.
        /// </summary>
        public ushort DllCharacteristics
        {
            get { return Buff.BytesToUInt16(Offset + 0x46); }
            set { Buff.SetUInt16(Offset + 0x46, value); }
        }

        /// <summary>
        ///     Size of stack reserve in bytes.
        /// </summary>
        public ulong SizeOfStackReserve
        {
            get
            {
                return _is64Bit
                    ? Buff.BytesToUInt64(Offset + 0x48)
                    : Buff.BytesToUInt32(Offset + 0x48);
            }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x48, (uint) value);
                else
                    Buff.SetUInt64(Offset + 0x48, value);
            }
        }

        /// <summary>
        ///     Size of bytes committed for the stack in bytes.
        /// </summary>
        public ulong SizeOfStackCommit
        {
            get
            {
                return _is64Bit
                    ? Buff.BytesToUInt64(Offset + 0x50)
                    : Buff.BytesToUInt32(Offset + 0x4C);
            }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x4C, (uint) value);
                else
                    Buff.SetUInt64(Offset + 0x50, value);
            }
        }

        /// <summary>
        ///     Size of the heap to reserve in bytes.
        /// </summary>
        public ulong SizeOfHeapReserve
        {
            get
            {
                return _is64Bit
                    ? Buff.BytesToUInt64(Offset + 0x58)
                    : Buff.BytesToUInt32(Offset + 0x50);
            }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x50, (uint) value);
                else
                    Buff.SetUInt64(Offset + 0x58, value);
            }
        }

        /// <summary>
        ///     Size of the heap commit in bytes.
        /// </summary>
        public ulong SizeOfHeapCommit
        {
            get
            {
                return _is64Bit
                    ? Buff.BytesToUInt64(Offset + 0x60)
                    : Buff.BytesToUInt32(Offset + 0x54);
            }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x54, (uint) value);
                else
                    Buff.SetUInt64(Offset + 0x60, value);
            }
        }

        /// <summary>
        ///     Obsolete
        /// </summary>
        public uint LoaderFlags
        {
            get
            {
                return _is64Bit
                    ? Buff.BytesToUInt32(Offset + 0x68)
                    : Buff.BytesToUInt32(Offset + 0x58);
            }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x58, value);
                else
                    Buff.SetUInt32(Offset + 0x68, value);
            }
        }

        /// <summary>
        ///     Number of directory entries in the remainder of the optional header.
        /// </summary>
        public uint NumberOfRvaAndSizes
        {
            get
            {
                return _is64Bit
                    ? Buff.BytesToUInt32(Offset + 0x6C)
                    : Buff.BytesToUInt32(Offset + 0x5C);
            }
            set
            {
                if (!_is64Bit)
                    Buff.SetUInt32(Offset + 0x5C, value);
                else
                    Buff.SetUInt32(Offset + 0x6C, value);
            }
        }

        /// <summary>
        ///     Creates a string representation of the objects
        ///     properties.
        /// </summary>
        /// <returns>Optional header properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_OPTIONAL_HEADER\n");
            sb.Append(this.PropertiesToString("{0,-15}:\t{1,10:X}\n"));
            foreach (var dd in DataDirectory)
                sb.Append(dd);
            return sb.ToString();
        }
    }
}