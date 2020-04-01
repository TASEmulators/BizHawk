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

namespace PeNet.Structures.MetaDataTables
{
    public class TypeRefTable : AbstractStructure
    {
        private readonly uint _numOfRows;
        private List<TypeRefTableRow> _rows;

        public TypeRefTable(byte[] buff, uint offset, uint numOfRows) 
            : base(buff, offset)
        {
            _numOfRows = numOfRows;
        }

        public List<TypeRefTableRow> Rows => _rows ?? (_rows = ParseRows(_numOfRows));

        private List<TypeRefTableRow> ParseRows(uint numOfRows)
        {
            var rows = new List<TypeRefTableRow>((int) numOfRows);
            uint rowLength = 0; // TODO: Compute row length
            uint resolutionScopeSize = 0; // TODO: Compute size (2 or 4 bytes) based on the number of elements where the index points to
            uint stringSize = 4;


            for (var i = 0; i < numOfRows; i++)
            {
                rows.Add(new TypeRefTableRow(Buff, Offset + rowLength, resolutionScopeSize, stringSize));
            }

            return rows;
        }
     
    }
}