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

using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     Represents the section header for one section.
    /// </summary>
    public class IMAGE_SECTION_HEADER : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_SECTION_HEADER object.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset to the section header.</param>
        public IMAGE_SECTION_HEADER(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Max. 8 byte long UTF-8 string that names
        ///     the section.
        /// </summary>
        public byte[] Name
        {
            get
            {
                return new[]
                {
                    Buff[Offset + 0],
                    Buff[Offset + 1],
                    Buff[Offset + 2],
                    Buff[Offset + 3],
                    Buff[Offset + 4],
                    Buff[Offset + 5],
                    Buff[Offset + 6],
                    Buff[Offset + 7]
                };
            }

            set
            {
                Buff[Offset + 0] = value[0];
                Buff[Offset + 1] = value[1];
                Buff[Offset + 2] = value[2];
                Buff[Offset + 3] = value[3];
                Buff[Offset + 4] = value[4];
                Buff[Offset + 5] = value[5];
                Buff[Offset + 6] = value[7];
                Buff[Offset + 7] = value[8];
            }
        }

        /// <summary>
        ///     The raw (file) address of the section.
        /// </summary>
        public uint PhysicalAddress
        {
            get { return Buff.BytesToUInt32(Offset + 0x8); }
            set { Buff.SetUInt32(Offset + 0x8, value); }
        }

        /// <summary>
        ///     Size of the section when loaded into memory. If it's bigger than
        ///     the raw data size, the rest of the section is filled with zeros.
        /// </summary>
        public uint VirtualSize
        {
            get { return PhysicalAddress; }
            set { PhysicalAddress = value; }
        }

        /// <summary>
        ///     RVA of the section start in memory.
        /// </summary>
        public uint VirtualAddress
        {
            get { return Buff.BytesToUInt32(Offset + 0xC); }
            set { Buff.SetUInt32(Offset + 0xC, value); }
        }

        /// <summary>
        ///     Size of the section in raw on disk. Must be a multiple of the file alignment
        ///     specified in the optional header. If its less than the virtual size, the rest
        ///     is filled with zeros.
        /// </summary>
        public uint SizeOfRawData
        {
            get { return Buff.BytesToUInt32(Offset + 0x10); }
            set { Buff.SetUInt32(Offset + 0x10, value); }
        }

        /// <summary>
        ///     Raw address of the section in the file.
        /// </summary>
        public uint PointerToRawData
        {
            get { return Buff.BytesToUInt32(Offset + 0x14); }
            set { Buff.SetUInt32(Offset + 0x14, value); }
        }

        /// <summary>
        ///     Pointer to the beginning of the relocation. If there are none, the
        ///     value is zero.
        /// </summary>
        public uint PointerToRelocations
        {
            get { return Buff.BytesToUInt32(Offset + 0x18); }
            set { Buff.SetUInt32(Offset + 0x18, value); }
        }

        /// <summary>
        ///     Pointer to the beginning of the line-numbers in the file.
        ///     Zero if there are no line-numbers in the file.
        /// </summary>
        public uint PointerToLinenumbers
        {
            get { return Buff.BytesToUInt32(Offset + 0x1C); }
            set { Buff.SetUInt32(Offset + 0x1C, value); }
        }

        /// <summary>
        ///     The number of relocations for the section. Is zero for executable images.
        /// </summary>
        public ushort NumberOfRelocations
        {
            get { return Buff.BytesToUInt16(Offset + 0x20); }
            set { Buff.SetUInt16(Offset + 0x20, value); }
        }

        /// <summary>
        ///     The number of line-number entries for the section.
        /// </summary>
        public ushort NumberOfLinenumbers
        {
            get { return Buff.BytesToUInt16(Offset + 0x22); }
            set { Buff.SetUInt16(Offset + 0x22, value); }
        }

        /// <summary>
        ///     Section characteristics. Can be resolved with
        /// </summary>
        public uint Characteristics
        {
            get { return Buff.BytesToUInt32(Offset + 0x24); }
            set { Buff.SetUInt32(Offset + 0x24, value); }
        }

        /// <summary>
        ///     Create a string from all object properties.
        /// </summary>
        /// <returns>Section header properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_SECTION_HEADER\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}