using System.Collections.Generic;
using PeNet.Utilities;
using static System.String;

namespace PeNet.Parser
{
    internal class MetaDataStreamUSParser : SafeParser<List<string>>
    {
        private readonly uint _size;

        public MetaDataStreamUSParser(byte[] buff, uint offset, uint size) 
            : base(buff, offset)
        {
            _size = size;
        }

        protected override List<string> ParseTarget()
        {
            var stringList = new List<string>();

            for (var i = _offset; i < _offset + _size; i++)
            {
                var tmpString = _buff.GetUnicodeString(i);
                i += (uint) tmpString.Length * 2 + 1 ;

                if (IsNullOrWhiteSpace(tmpString))
                    continue;

                stringList.Add(tmpString);
            }

            return stringList;
        }
    }
}