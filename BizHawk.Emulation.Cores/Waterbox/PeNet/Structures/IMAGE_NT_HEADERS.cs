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
    ///     The NT header is the main header for modern Windows applications.
    ///     It contains the file header and the optional header.
    /// </summary>
    public class IMAGE_NT_HEADERS : AbstractStructure
    {
        /// <summary>
        ///     Access to the File header.
        /// </summary>
        public readonly IMAGE_FILE_HEADER FileHeader;

        /// <summary>
        ///     Access to the Optional header.
        /// </summary>
        public readonly IMAGE_OPTIONAL_HEADER OptionalHeader;

        /// <summary>
        ///     Create a new IMAGE_NT_HEADERS object.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset of the NT header.</param>
        /// <param name="is64Bit">Flag if the header is for a x64 application.</param>
        public IMAGE_NT_HEADERS(byte[] buff, uint offset, bool is64Bit)
            : base(buff, offset)
        {
            FileHeader = new IMAGE_FILE_HEADER(buff, offset + 0x4);
            OptionalHeader = new IMAGE_OPTIONAL_HEADER(buff, offset + 0x18, is64Bit);
        }

        /// <summary>
        ///     NT header signature.
        /// </summary>
        public uint Signature
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        ///     Creates a string representation of the objects properties.
        /// </summary>
        /// <returns>The NT header properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_NT_HEADERS\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));
            sb.Append(FileHeader);
            sb.Append(OptionalHeader);

            return sb.ToString();
        }
    }
}