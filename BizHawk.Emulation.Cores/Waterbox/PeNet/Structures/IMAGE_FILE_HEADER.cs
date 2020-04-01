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
    ///     The File header contains information about the structure
    ///     and properties of the PE file.
    /// </summary>
    public class IMAGE_FILE_HEADER : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_FILE_HEADER object.
        /// </summary>
        /// <param name="buff">A PE file as byte array.</param>
        /// <param name="offset">Raw offset to the file header.</param>
        public IMAGE_FILE_HEADER(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     The machine (CPU type) the PE file is intended for.
        ///     Can be resolved with Utility.ResolveTargetMachine(machine).
        /// </summary>
        public ushort Machine
        {
            get { return Buff.BytesToUInt16(Offset); }
            set { Buff.SetUInt16(Offset, value); }
        }

        /// <summary>
        ///     The number of sections in the PE file.
        /// </summary>
        public ushort NumberOfSections
        {
            get { return Buff.BytesToUInt16(Offset + 0x2); }
            set { Buff.SetUInt16(Offset + 0x2, value); }
        }

        /// <summary>
        ///     Time and date stamp.
        /// </summary>
        public uint TimeDateStamp
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        ///     Pointer to COFF symbols table. They are rare in PE files,
        ///     but often in obj files.
        /// </summary>
        public uint PointerToSymbolTable
        {
            get { return Buff.BytesToUInt32(Offset + 0x8); }
            set { Buff.SetUInt32(Offset + 0x8, value); }
        }

        /// <summary>
        ///     The number of COFF symbols.
        /// </summary>
        public uint NumberOfSymbols
        {
            get { return Buff.BytesToUInt32(Offset + 0xC); }
            set { Buff.SetUInt32(Offset + 0xC, value); }
        }

        /// <summary>
        ///     The size of the optional header which follow the file header.
        /// </summary>
        public ushort SizeOfOptionalHeader
        {
            get { return Buff.BytesToUInt16(Offset + 0x10); }
            set { Buff.SetUInt16(Offset + 0x10, value); }
        }

        /// <summary>
        ///     Set of flags which describe the PE file in detail.
        ///     Can be resolved with Utility.ResolveCharacteristics(characteristics).
        /// </summary>
        public ushort Characteristics
        {
            get { return Buff.BytesToUInt16(Offset + 0x12); }
            set { Buff.SetUInt16(Offset + 0x12, value); }
        }

        /// <summary>
        ///     Creates a string representation of all object properties.
        /// </summary>
        /// <returns>The file header properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_FILE_HEADER\n");
            sb.Append(this.PropertiesToString("{0,-15}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}