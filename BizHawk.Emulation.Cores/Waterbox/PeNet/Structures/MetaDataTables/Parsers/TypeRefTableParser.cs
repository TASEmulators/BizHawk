using System;
using PeNet.Parser;

namespace PeNet.Structures.MetaDataTables.Parsers
{
    internal class TypeRefTableParser : SafeParser<TypeRefTable>
    {
        private readonly uint _numOfRows;
        private readonly uint _heapOffsetSizes;

        public TypeRefTableParser(byte[] buff, uint offset, uint numOfRows, byte heapOffsetSizes) 
            : base(buff, offset)
        {
            _numOfRows = numOfRows;
            _heapOffsetSizes = heapOffsetSizes;
        }

        protected override TypeRefTable ParseTarget()
        {
            return null;
        }
    }
}