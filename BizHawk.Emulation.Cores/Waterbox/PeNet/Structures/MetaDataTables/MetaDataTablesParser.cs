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

using System.Linq;
using PeNet.Structures.MetaDataTables.Parsers;

namespace PeNet.Structures.MetaDataTables
{
    /// <summary>
    /// Parser for all Meta Data Tables in the Meta Data Tables Header 
    /// of the .Net header.
    /// </summary>
    public class MetaDataTablesParser
    {
        private readonly byte[] _buff;
        private readonly METADATATABLESHDR _metaDataTablesHdr;
        private ModuleTableParser _moduleTableParser;
        private TypeRefTableParser _typeRefTableParser;

        /// <summary>
        /// Access the Module Table.
        /// </summary>
        public ModuleTable ModuleTable => _moduleTableParser?.GetParserTarget();

        /// <summary>
        /// Create a new MetaDataTablesParser instance.
        /// </summary>
        /// <param name="buff">Buffer containing all Meta Data Tables.</param>
        /// <param name="metaDataTablesHdr">The Meta Data Tables Header structure of the .Net header.</param>
        public MetaDataTablesParser(byte[] buff, METADATATABLESHDR metaDataTablesHdr)
        {
            _buff = buff;
            _metaDataTablesHdr = metaDataTablesHdr;
            InitParsers();
        }

        private void InitParsers()
        {
            var currentTableOffset = (uint) (_metaDataTablesHdr.Offset + 0x18 + _metaDataTablesHdr.TableDefinitions.Count*0x4);
            _moduleTableParser = InitModuleTableParser(currentTableOffset);
        }

        private ModuleTableParser InitModuleTableParser(uint offset)
        {
            var tableDef =
                _metaDataTablesHdr.TableDefinitions.FirstOrDefault(
                    x => x.Name == DotNetConstants.MaskValidFlags.Module.ToString());

            return tableDef == null ? null : new ModuleTableParser(_buff, offset, tableDef.NumOfRows, _metaDataTablesHdr.HeapOffsetSizes);
        }
    }
}