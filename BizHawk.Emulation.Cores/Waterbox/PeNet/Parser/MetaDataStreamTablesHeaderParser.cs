using PeNet.Structures;

namespace PeNet.Parser
{
    internal class MetaDataStreamTablesHeaderParser : SafeParser<METADATATABLESHDR>
    {
        public MetaDataStreamTablesHeaderParser(byte[] buff, uint offset) 
            : base(buff, offset)
        {
        }

        protected override METADATATABLESHDR ParseTarget()
        {
            return new METADATATABLESHDR(_buff, _offset);
        }
    }
}