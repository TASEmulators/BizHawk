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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     The IMAGE_BASE_RELOCATION structure holds information needed to relocate
    ///     the image to another virtual address.
    /// </summary>
    public class IMAGE_BASE_RELOCATION : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_BASE_RELOCATION object.
        /// </summary>
        /// <param name="buff">PE binary as byte array.</param>
        /// <param name="offset">Offset to the relocation struct in the binary.</param>
        /// <param name="relocSize">Size of the complete relocation directory.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     If the SizeOfBlock is bigger than the size
        ///     of the Relocation Directory.
        /// </exception>
        public IMAGE_BASE_RELOCATION(byte[] buff, uint offset, uint relocSize)
            : base(buff, offset)
        {
            if (SizeOfBlock > relocSize)
                throw new ArgumentOutOfRangeException(nameof(relocSize),
                    "SizeOfBlock cannot be bigger than size of the Relocation Directory.");

            if(SizeOfBlock < 8)
                throw new Exception("SizeOfBlock cannot be smaller than 8.");

            ParseTypeOffsets();
        }

        /// <summary>
        ///     RVA of the relocation block.
        /// </summary>
        public uint VirtualAddress
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        ///     SizeOfBlock-8 indicates how many TypeOffsets follow the SizeOfBlock.
        /// </summary>
        public uint SizeOfBlock
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        ///     Array with the TypeOffsets for the relocation block.
        /// </summary>
        public TypeOffset[] TypeOffsets { get; private set; }

        private void ParseTypeOffsets()
        {
            var list = new List<TypeOffset>();
            for (uint i = 0; i < (SizeOfBlock - 8)/2; i++)
            {
                list.Add(new TypeOffset(Buff, Offset + 8 + i*2));
            }
            TypeOffsets = list.ToArray();
        }

        /// <summary>
        ///     Convert all object properties to strings.
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_BASE_RELOCATION\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));
            TypeOffsets.ToList().ForEach(to => sb.AppendLine(to.ToString()));

            return sb.ToString();
        }

        /// <summary>
        ///     Represents the type and offset in an
        ///     IMAGE_BASE_RELOCATION structure.
        /// </summary>
        public class TypeOffset
        {
            private readonly byte[] _buff;
            private readonly uint _offset;

            /// <summary>
            ///     Create a new TypeOffset object.
            /// </summary>
            /// <param name="buff">PE binary as byte array.</param>
            /// <param name="offset">Offset of the TypeOffset in the array.</param>
            public TypeOffset(byte[] buff, uint offset)
            {
                _buff = buff;
                _offset = offset;
            }

            /// <summary>
            ///     The type is described in the 4 lower bits of the
            ///     TypeOffset word.
            /// </summary>
            public byte Type
            {
                get
                {
                    var to = _buff.BytesToUInt16(_offset);
                    return (byte) (to >> 12);
                }
            }

            /// <summary>
            ///     The offset is described in the 12 higher bits of the
            ///     TypeOffset word.
            /// </summary>
            public ushort Offset
            {
                get
                {
                    var to = _buff.BytesToUInt16(_offset);
                    return (ushort) (to & 0xFFF);
                }
            }

            /// <summary>
            ///     Convert all object properties to strings.
            /// </summary>
            /// <returns>String representation of the object</returns>
            public override string ToString()
            {
                var sb = new StringBuilder("TypeOffset\n");
                sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

                return sb.ToString();
            }
        }
    }
}