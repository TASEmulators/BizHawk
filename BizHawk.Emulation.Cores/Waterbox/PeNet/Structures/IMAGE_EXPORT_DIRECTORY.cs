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
    ///     The export directory contains all exported function, symbols etc.
    ///     which can be used by other module.
    /// </summary>
    public class IMAGE_EXPORT_DIRECTORY : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_EXPORT_DIRECTORY object.
        /// </summary>
        /// <param name="buff">PE file as a byte array.</param>
        /// <param name="offset">Raw offset of the export directory in the PE file.</param>
        public IMAGE_EXPORT_DIRECTORY(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     The characteristics of the export directory.
        /// </summary>
        public uint Characteristics
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
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
            get { return Buff.BytesToUInt16(Offset + 0xA); }
            set { Buff.SetUInt16(Offset + 0xA, value); }
        }

        /// <summary>
        ///     Name.
        /// </summary>
        public uint Name
        {
            get { return Buff.BytesToUInt32(Offset + 0xC); }
            set { Buff.SetUInt32(Offset + 0xC, value); }
        }

        /// <summary>
        ///     Base.
        /// </summary>
        public uint Base
        {
            get { return Buff.BytesToUInt32(Offset + 0x10); }
            set { Buff.SetUInt32(Offset + 0x10, value); }
        }

        /// <summary>
        ///     Number of exported functions.
        /// </summary>
        public uint NumberOfFunctions
        {
            get { return Buff.BytesToUInt32(Offset + 0x14); }
            set { Buff.SetUInt32(Offset + 0x14, value); }
        }

        /// <summary>
        ///     Number of exported names.
        /// </summary>
        public uint NumberOfNames
        {
            get { return Buff.BytesToUInt32(Offset + 0x18); }
            set { Buff.SetUInt32(Offset + 0x18, value); }
        }

        /// <summary>
        ///     RVA to the addresses of the functions.
        /// </summary>
        public uint AddressOfFunctions
        {
            get { return Buff.BytesToUInt32(Offset + 0x1C); }
            set { Buff.SetUInt32(Offset + 0x1C, value); }
        }

        /// <summary>
        ///     RVA to the addresses of the names.
        /// </summary>
        public uint AddressOfNames
        {
            get { return Buff.BytesToUInt32(Offset + 0x20); }
            set { Buff.SetUInt32(Offset + 0x20, value); }
        }

        /// <summary>
        ///     RVA to the name ordinals.
        /// </summary>
        public uint AddressOfNameOrdinals
        {
            get { return Buff.BytesToUInt32(Offset + 0x24); }
            set { Buff.SetUInt32(Offset + 0x24, value); }
        }

        /// <summary>
        ///     Creates a string representation of all object
        ///     properties.
        /// </summary>
        /// <returns>The export directory properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_EXPORT_DIRECTORY\n");
            sb.Append(this.PropertiesToString("{0,-15}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}