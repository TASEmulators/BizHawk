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

namespace PeNet.Structures.MetaDataTables
{
    public class MetaDataTableIndexComputation
    {
        private readonly IMETADATATABLESHDR _metaDataTablesHeader;

        public MetaDataTableIndexComputation(IMETADATATABLESHDR metaDataTablesHeader)
        {
            _metaDataTablesHeader = metaDataTablesHeader;
        }

        public Tuple<string, uint> GetTableNameAndIndex(uint index)
        {
            // TODO: return the name of the table to which the index points and the index
            return null;
        }

        public uint GetTableIndexSize(Type indexEnumType)
        {
            if(!indexEnumType.IsEnum)
                throw new ArgumentException("Generic parameter must be of type enum.");

            var names = Enum.GetNames(indexEnumType);
            var maxRows = GetMaxRows(names);
            return GetIndexSize(names.Length, maxRows);
        }

        private uint GetIndexSize(int numOfChoices, uint maxRows)
        {
            var numOfTagBits = (int) Math.Ceiling(Math.Log(numOfChoices, 2));
            var numOfIndexBits = sizeof(ushort) * 8 - numOfTagBits;
            var numOfIndexableRows = (uint) Math.Pow(numOfIndexBits, 2);

            return (uint) (maxRows > numOfIndexableRows ? 4 : 2);
        }

        private uint GetMaxRows(IEnumerable<string> names)
        {
            return names
                .Select(name => _metaDataTablesHeader.TableDefinitions.FirstOrDefault(x => x.Name == name))
                .Where(tableDef => tableDef != null)
                .Max(x => x.NumOfRows);
        }
    }
}