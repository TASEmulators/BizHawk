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
using System.Linq;
using System.Xml.Schema;

namespace PeNet.Structures.MetaDataTables
{
    /// <summary>
    /// Represents an Table of the Meta Data Tables Header in 
    /// the .Net header.
    /// </summary>
    public abstract class AbstractMetaDataTable<T> : AbstractStructure
        where T : AbstractMetaDataTableRow
    {
        private List<T> _rows;

        /// <summary>
        /// Create a new AbstractMetaDataTable instance.
        /// </summary>
        /// <param name="buff">Buffer which contains the table.</param>
        /// <param name="offset">Offset of the table in the buffer.</param>
        /// <param name="numberOfRows">Number of rows of the table.</param>
        protected AbstractMetaDataTable(byte[] buff, uint offset, uint numberOfRows) 
            : base(buff, offset)
        {
            NumberOfRows = numberOfRows;
        }

        /// <summary>
        /// Number of rows of the table.
        /// </summary>
        public uint NumberOfRows { get; }

        /// <summary>
        /// Access the rows of the Meta Data Table.
        /// </summary>
        public List<T> Rows
        {
            get
            {
                if (_rows != null)
                    return _rows;

                _rows = ParseRows();
                return _rows;
            }
        }

        /// <summary>
        /// Parse the rows of the table.
        /// </summary>
        /// <returns>List with table rows.</returns>
        protected abstract List<T> ParseRows();

        /// <summary>
        /// Length of the complete table in bytes.
        /// </summary>
        public uint Length => (uint) Rows.Sum(x => x.Length);
    }
}