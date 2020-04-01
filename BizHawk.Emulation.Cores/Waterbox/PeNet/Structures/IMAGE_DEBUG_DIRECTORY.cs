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
    ///     The IMAGE_DEBUG_DIRECTORY hold debug information
    ///     about the PE file.
    /// </summary>
    public class IMAGE_DEBUG_DIRECTORY : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_DEBUG_DIRECTORY object.
        /// </summary>
        /// <param name="buff">PE binary as byte array.</param>
        /// <param name="offset">Offset to the debug struct in the binary.</param>
        public IMAGE_DEBUG_DIRECTORY(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Characteristics of the debug information.
        /// </summary>
        public uint Characteristics
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        ///     Time and date stamp
        /// </summary>
        public uint TimeDateStamp
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        ///     Major Version.
        /// </summary>
        public ushort MajorVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x8); }
            set { Buff.SetUInt16(Offset + 0x8, value); }
        }

        /// <summary>
        ///     Minor Version.
        /// </summary>
        public ushort MinorVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0xa); }
            set { Buff.SetUInt16(Offset + 0xa, value); }
        }

        /// <summary>
        ///     Type
        ///     1: Coff
        ///     2: CV-PDB
        ///     9: Borland
        /// </summary>
        public uint Type
        {
            get { return Buff.BytesToUInt32(Offset + 0xc); }
            set { Buff.SetUInt32(Offset + 0xc, value); }
        }

        /// <summary>
        ///     Size of data.
        /// </summary>
        public uint SizeOfData
        {
            get { return Buff.BytesToUInt32(Offset + 0x10); }
            set { Buff.SetUInt32(Offset + 0x10, value); }
        }

        /// <summary>
        ///     Address of raw data.
        /// </summary>
        public uint AddressOfRawData
        {
            get { return Buff.BytesToUInt32(Offset + 0x14); }
            set { Buff.SetUInt32(Offset + 0x14, value); }
        }

        /// <summary>
        ///     Pointer to raw data.
        /// </summary>
        public uint PointerToRawData
        {
            get { return Buff.BytesToUInt32(Offset + 0x18); }
            set { Buff.SetUInt32(Offset + 0x18, value); }
        }

        /// <summary>
        ///     Convert all object properties to strings.
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_DEBUG_DIRECTORY\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}