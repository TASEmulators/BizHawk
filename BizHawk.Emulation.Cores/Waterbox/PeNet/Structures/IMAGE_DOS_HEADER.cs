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
    ///     The IMAGE_DOS_HEADER with which every PE file starts.
    /// </summary>
    public class IMAGE_DOS_HEADER : AbstractStructure
    {
        /// <summary>
        ///     Create a new IMAGE_DOS_HEADER object.
        /// </summary>
        /// <param name="buff">Byte buffer containing a PE file.</param>
        /// <param name="offset">Offset in the buffer to the DOS header.</param>
        public IMAGE_DOS_HEADER(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Magic "MZ" header.
        /// </summary>
        public ushort e_magic
        {
            get { return Buff.BytesToUInt16(Offset + 0x00); }
            set { Buff.SetUInt16(Offset + 0x00, value); }
        }

        /// <summary>
        ///     Bytes on the last page of the file.
        /// </summary>
        public ushort e_cblp
        {
            get { return Buff.BytesToUInt16(Offset + 0x02); }
            set { Buff.SetUInt16(Offset + 0x02, value); }
        }

        /// <summary>
        ///     Pages in the file.
        /// </summary>
        public ushort e_cp
        {
            get { return Buff.BytesToUInt16(Offset + 0x04); }
            set { Buff.SetUInt16(Offset + 0x04, value); }
        }

        /// <summary>
        ///     Relocations.
        /// </summary>
        public ushort e_crlc
        {
            get { return Buff.BytesToUInt16(Offset + 0x06); }
            set { Buff.SetUInt16(Offset + 0x06, value); }
        }

        /// <summary>
        ///     Size of the header in paragraphs.
        /// </summary>
        public ushort e_cparhdr
        {
            get { return Buff.BytesToUInt16(Offset + 0x08); }
            set { Buff.SetUInt16(Offset + 0x08, value); }
        }

        /// <summary>
        ///     Minimum extra paragraphs needed.
        /// </summary>
        public ushort e_minalloc
        {
            get { return Buff.BytesToUInt16(Offset + 0x0A); }
            set { Buff.SetUInt16(Offset + 0x0A, value); }
        }

        /// <summary>
        ///     Maximum extra paragraphs needed.
        /// </summary>
        public ushort e_maxalloc
        {
            get { return Buff.BytesToUInt16(Offset + 0x0C); }
            set { Buff.SetUInt16(Offset + 0x0C, value); }
        }

        /// <summary>
        ///     Initial (relative) SS value.
        /// </summary>
        public ushort e_ss
        {
            get { return Buff.BytesToUInt16(Offset + 0x0E); }
            set { Buff.SetUInt16(Offset + 0x0E, value); }
        }

        /// <summary>
        ///     Initial SP value.
        /// </summary>
        public ushort e_sp
        {
            get { return Buff.BytesToUInt16(Offset + 0x10); }
            set { Buff.SetUInt16(Offset + 0x10, value); }
        }

        /// <summary>
        ///     Checksum
        /// </summary>
        public ushort e_csum
        {
            get { return Buff.BytesToUInt16(Offset + 0x12); }
            set { Buff.SetUInt16(Offset + 0x12, value); }
        }

        /// <summary>
        ///     Initial IP value.
        /// </summary>
        public ushort e_ip
        {
            get { return Buff.BytesToUInt16(Offset + 0x14); }
            set { Buff.SetUInt16(Offset + 0x14, value); }
        }

        /// <summary>
        ///     Initial (relative) CS value.
        /// </summary>
        public ushort e_cs
        {
            get { return Buff.BytesToUInt16(Offset + 0x16); }
            set { Buff.SetUInt16(Offset + 0x16, value); }
        }

        /// <summary>
        ///     Raw address of the relocation table.
        /// </summary>
        public ushort e_lfarlc
        {
            get { return Buff.BytesToUInt16(Offset + 0x18); }
            set { Buff.SetUInt16(Offset + 0x18, value); }
        }

        /// <summary>
        ///     Overlay number.
        /// </summary>
        public ushort e_ovno
        {
            get { return Buff.BytesToUInt16(Offset + 0x1A); }
            set { Buff.SetUInt16(Offset + 0x1A, value); }
        }

        /// <summary>
        ///     Reserved.
        /// </summary>
        public ushort[] e_res // 4 * UInt16
        {
            get
            {
                return new[]
                {
                    Buff.BytesToUInt16(Offset + 0x1C),
                    Buff.BytesToUInt16(Offset + 0x1E),
                    Buff.BytesToUInt16(Offset + 0x20),
                    Buff.BytesToUInt16(Offset + 0x22)
                };
            }
            set
            {
                Buff.SetUInt16(Offset + 0x1C, value[0]);
                Buff.SetUInt16(Offset + 0x1E, value[1]);
                Buff.SetUInt16(Offset + 0x20, value[2]);
                Buff.SetUInt16(Offset + 0x22, value[3]);
            }
        }

        /// <summary>
        ///     OEM identifier.
        /// </summary>
        public ushort e_oemid
        {
            get { return Buff.BytesToUInt16(Offset + 0x24); }
            set { Buff.SetUInt16(Offset + 0x24, value); }
        }

        /// <summary>
        ///     OEM information.
        /// </summary>
        public ushort e_oeminfo
        {
            get { return Buff.BytesToUInt16(Offset + 0x26); }
            set { Buff.SetUInt16(Offset + 0x26, value); }
        }

        /// <summary>
        ///     Reserved.
        /// </summary>
        public ushort[] e_res2 // 10 * UInt16
        {
            get
            {
                return new[]
                {
                    Buff.BytesToUInt16(Offset + 0x28),
                    Buff.BytesToUInt16(Offset + 0x2A),
                    Buff.BytesToUInt16(Offset + 0x2C),
                    Buff.BytesToUInt16(Offset + 0x2E),
                    Buff.BytesToUInt16(Offset + 0x30),
                    Buff.BytesToUInt16(Offset + 0x32),
                    Buff.BytesToUInt16(Offset + 0x34),
                    Buff.BytesToUInt16(Offset + 0x36),
                    Buff.BytesToUInt16(Offset + 0x38),
                    Buff.BytesToUInt16(Offset + 0x3A)
                };
            }
            set
            {
                Buff.SetUInt16(Offset + 0x28, value[0]);
                Buff.SetUInt16(Offset + 0x2A, value[1]);
                Buff.SetUInt16(Offset + 0x2C, value[2]);
                Buff.SetUInt16(Offset + 0x2E, value[3]);
                Buff.SetUInt16(Offset + 0x30, value[4]);
                Buff.SetUInt16(Offset + 0x32, value[5]);
                Buff.SetUInt16(Offset + 0x34, value[6]);
                Buff.SetUInt16(Offset + 0x36, value[7]);
                Buff.SetUInt16(Offset + 0x38, value[8]);
                Buff.SetUInt16(Offset + 0x3A, value[9]);
            }
        }

        /// <summary>
        ///     Raw address of the NT header.
        /// </summary>
        public uint e_lfanew
        {
            get { return Buff.BytesToUInt32(Offset + 0x3C); }
            set { Buff.SetUInt32(Offset + 0x3C, value); }
        }

        /// <summary>
        ///     Creates a string representation of all properties.
        /// </summary>
        /// <returns>The header properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_DOS_HEADER\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}