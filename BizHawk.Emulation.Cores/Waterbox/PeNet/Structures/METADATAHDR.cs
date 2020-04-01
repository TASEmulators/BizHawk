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
using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    /// The Meta Data Header is part of the .Net/CLI (COM+ 2) header and is reachable
    /// from the .Net/CLI (COM+2) header IMAGE_COR20_HEADER. It contains information
    /// about embedded streams (sections) in the .Net assembly.
    /// </summary>
    public class METADATAHDR : AbstractStructure
    {
        /// <summary>
        /// Create a new Meta Data Header from a byte array.
        /// </summary>
        /// <param name="buff">Byte buffer which contains a Meta Data Header.</param>
        /// <param name="offset">Offset of the header start in the byte buffer.</param>
        public METADATAHDR(byte[] buff, uint offset) 
            : base(buff, offset)
        {
        }

        /// <summary>
        /// Signature. Always 0x424A5342 in a valid .Net assembly.
        /// </summary>
        public uint Signature
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        /// Major version.
        /// </summary>
        public ushort MajorVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x4); }
            set { Buff.SetUInt16(Offset + 0x4, value); }
        }

        /// <summary>
        /// Minor version.
        /// </summary>
        public ushort MinorVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x6); }
            set { Buff.SetUInt16(Offset + 0x6, value); }
        }

        /// <summary>
        /// Reserved. Always 0.
        /// </summary>
        public uint Reserved
        {
            get { return Buff.BytesToUInt32(Offset + 0x8); }
            set { Buff.SetUInt32(Offset + 0x8, value); }
        }

        /// <summary>
        /// Length of the UTF-8 version string rounded up to a multiple of 4.
        /// For e.g., v1.3.4323
        /// </summary>
        public uint VersionLength
        {
            get { return Buff.BytesToUInt32(Offset + 0xC); }
            set { Buff.SetUInt32(Offset + 0xC, value); }
        }

        /// <summary>
        /// Version number as an UTF-8 string.
        /// </summary>
        public string Version => ParseVersionString(Offset + 0x10, VersionLength);

        /// <summary>
        /// Reserved flags field. Always 0.
        /// </summary>
        public ushort Flags
        {
            get { return Buff.BytesToUInt16(VersionLength + Offset +  0x10); }
            set { Buff.SetUInt16(VersionLength + Offset + 0x10, value); }
        }

        /// <summary>
        /// Number of streams (sections) to follow. 
        /// </summary>
        public ushort Streams
        {
            get { return Buff.BytesToUInt16(VersionLength + Offset + 0x12); }
            set { Buff.SetUInt16(VersionLength + Offset + 0x12, value); }
        }

        /// <summary>
        /// Array with all Meta Data Stream Headers.
        /// </summary>
        public METADATASTREAMHDR[] MetaDataStreamsHdrs => ParseMetaDataStreamHdrs(VersionLength + Offset + 0x14);

        private METADATASTREAMHDR[] ParseMetaDataStreamHdrs(uint offset)
        {
            var metaDataStreamHdrs = new List<METADATASTREAMHDR>();
            var tmpOffset = offset;
            for (var i = 0; i < Streams; i++)
            {
                var metaDataStreamHdr = new METADATASTREAMHDR(Buff, tmpOffset);
                metaDataStreamHdrs.Add(metaDataStreamHdr);
                tmpOffset += metaDataStreamHdr.HeaderLength;
            }

            return metaDataStreamHdrs.ToArray();
        }

        private string ParseVersionString(uint offset, uint versionLength)
        {
            var bytes = new byte[versionLength];
            Array.Copy(Buff, offset, bytes, 0, versionLength);
            var paddedString = Encoding.UTF8.GetString(bytes);

            // Remove padding and return.
            return paddedString.Replace("\0", string.Empty);
        }

        /// <summary>
        ///     Convert all object properties to strings.
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("METADATAHDR\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

            if (MetaDataStreamsHdrs != null)
            {
                sb.AppendLine("Meta Data Stream Headers");
                foreach (var metaDataStreamsHdr in MetaDataStreamsHdrs)
                {
                    sb.Append(metaDataStreamsHdr);
                }
            }
            

            return sb.ToString();
        }
    }
}