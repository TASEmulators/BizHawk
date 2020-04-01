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

namespace PeNet.Structures.MetaDataTables
{
    /// <summary>
    /// A row in the Module Table of the Meta Data Tables Header
    /// in the .Net header.
    /// </summary>
    public class ModuleTableRow : AbstractMetaDataTableRow
    {
        private readonly HeapOffsetBasedIndexSizes _heapIndexSizes;

        /// <summary>
        /// Create a new ModuleTableRow instance.
        /// </summary>
        /// <param name="buff">Buffer which contains the row.</param>
        /// <param name="offset">Offset in the buff, where the header starts.</param>
        /// <param name="heapOffsetSizes">Computes sizes of the heap bases indexes.</param>
        public ModuleTableRow(byte[] buff, uint offset, HeapOffsetBasedIndexSizes heapOffsetSizes) 
            : base(buff, offset)
        {
            _heapIndexSizes = heapOffsetSizes;
        }

        /// <summary>
        /// Reserved, should be 0.
        /// </summary>
        public ushort Generation
        {
            get { return Buff.BytesToUInt16(Offset); }
            set { Buff.SetUInt16(Offset, value); }
        }

        /// <summary>
        /// Index into #String heap which contains the assembly name.
        /// </summary>
        public uint Name => Buff.BytesToUInt32(Offset + 0x2, _heapIndexSizes.StringIndexSize);

        /// <summary>
        /// Index into #GUID heap which contains the module version ID.
        /// </summary>
        public uint Mvid
            => Buff.BytesToUInt32(Offset + 0x2 + _heapIndexSizes.StringIndexSize, _heapIndexSizes.GuidIndexSize);

        /// <summary>
        /// Index into GUID heap. Reserved, should be 0.
        /// </summary>
        public uint EncId
            =>
            Buff.BytesToUInt32(Offset + 0x2 + _heapIndexSizes.StringIndexSize + _heapIndexSizes.GuidIndexSize,
                _heapIndexSizes.GuidIndexSize);

        /// <summary>
        /// Index into GUID heap. Reserved, should be 0.
        /// </summary>
        public uint EncBaseId
            =>
            Buff.BytesToUInt32(Offset + 0x2 + _heapIndexSizes.StringIndexSize + _heapIndexSizes.GuidIndexSize*2,
                _heapIndexSizes.GuidIndexSize);

        /// <summary>
        /// Length of the row in bytes.
        /// </summary>
        public override uint Length => 0x2 + _heapIndexSizes.StringIndexSize + _heapIndexSizes.GuidIndexSize*3;

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder("ModuleTableRow\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}