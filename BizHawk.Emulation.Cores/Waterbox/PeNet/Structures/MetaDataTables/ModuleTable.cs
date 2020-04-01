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

using System.Collections.Generic;
using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures.MetaDataTables
{
    /// <summary>
    /// Module Table from the Meta Data Table Header of the 
    /// .Net header. Contains information about the current 
    /// assembly. Has only one row.
    /// </summary>
    public class ModuleTable : AbstractMetaDataTable<ModuleTableRow>
    {
        private readonly HeapOffsetBasedIndexSizes _heapOffsetIndexSizes;

        /// <summary>
        /// Create a new instance of the ModuleTable.
        /// </summary>
        /// <param name="buff">Buffer containing the ModuleTable.</param>
        /// <param name="offset">Offset to the ModuleTable in the buffer.</param>
        /// <param name="numberOfRows">Number of rows of the table.</param>
        /// <param name="heapOffsetSizes">The HeapOffsetSizes flag of the Meta Data Tables Header.</param>
        public ModuleTable(byte[] buff, uint offset, uint numberOfRows, byte heapOffsetSizes) 
            : base(buff, offset, numberOfRows)
        {
            _heapOffsetIndexSizes = new HeapOffsetBasedIndexSizes(heapOffsetSizes);
        }

        /// <summary>
        /// Parse the rows of the table.
        /// </summary>
        /// <returns>List with rows.</returns>
        protected override List<ModuleTableRow> ParseRows()
        {
            var currentOffset = Offset;
            var rows = new List<ModuleTableRow>((int) NumberOfRows);
            for (var i = 0; i < NumberOfRows; i++)
            {
                var row = new ModuleTableRow(Buff, currentOffset, _heapOffsetIndexSizes);
                rows.Add(row);
                currentOffset += row.Length;
            }
            return rows;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder("ModuleTable\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));
            foreach (var moduleTableRow in Rows)
                sb.Append(moduleTableRow);
            return sb.ToString();
        }
    }
}