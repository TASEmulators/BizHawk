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
    /// The IMAGE_DELAY_IMPORT_DESCRIPTOR describes delayed imports.
    /// </summary>
    public class IMAGE_DELAY_IMPORT_DESCRIPTOR : AbstractStructure
    {
        /// <summary>
        /// Create a new IMAGE_DELAY_IMPORT_DESCRIPTOR object.
        /// </summary>
        /// <param name="buff">PE binary as byte buffer.</param>
        /// <param name="offset">Offset to the delay import descriptor.</param>
        public IMAGE_DELAY_IMPORT_DESCRIPTOR(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public uint grAttrs
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint szName
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint phmod
        {
            get { return Buff.BytesToUInt32(Offset + 0x8); }
            set { Buff.SetUInt32(Offset + 0x8, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint pIAT
        {
            get { return Buff.BytesToUInt32(Offset + 0xc); }
            set { Buff.SetUInt32(Offset + 0xc, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint pINT
        {
            get { return Buff.BytesToUInt32(Offset + 0x10); }
            set { Buff.SetUInt32(Offset + 0x10, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint pBoundIAT
        {
            get { return Buff.BytesToUInt32(Offset + 0x14); }
            set { Buff.SetUInt32(Offset + 0x14, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint pUnloadIAT
        {
            get { return Buff.BytesToUInt32(Offset + 0x18); }
            set { Buff.SetUInt32(Offset + 0x16, value); }
        }

        /// <summary>
        /// 
        /// </summary>
        public uint dwTimeStamp
        {
            get { return Buff.BytesToUInt32(Offset + 0x1c); }
            set { Buff.SetUInt32(Offset + 0x1c, value); }
        }

        /// <summary>
        ///     Convert all object properties to strings.
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_DELAY_IMPORT_DESCRIPTOR\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}