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

using PeNet.Utilities;

namespace PeNet.Structures.MetaDataTables
{
    /// <summary>
    /// Abstract Meta Data Table Row.
    /// </summary>
    public abstract class AbstractMetaDataTableRow : AbstractStructure
    {
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="buff">Buffer containing the row.</param>
        /// <param name="offset">Offset in the buffer where the row starts.</param>
        protected AbstractMetaDataTableRow(byte[] buff, uint offset) 
            : base(buff, offset)
        {
        }

        /// <summary>
        /// Length of the row in bytes.
        /// </summary>
        public abstract uint Length { get; }
    }
}