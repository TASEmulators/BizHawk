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
using PeNet.Parser;
using PeNet.Structures;
using PeNet.Utilities;

namespace PeNet
{
    internal class DotNetStructureParsers
    {
        private readonly byte[] _buff;
        private readonly IMAGE_SECTION_HEADER[] _sectionHeaders;
        private readonly IMAGE_COR20_HEADER _imageCor20Header;
        private MetaDataHdrParser _metaDataHdrParser;
        private MetaDataStreamStringParser _metaDataStreamStringParser;
        private MetaDataStreamUSParser _metaDataStreamUSParser;
        private MetaDataStreamTablesHeaderParser _metaDataStreamTablesHeaderParser;

        public METADATAHDR MetaDataHdr => _metaDataHdrParser?.GetParserTarget();
        public List<string> MetaDataStreamString => _metaDataStreamStringParser?.GetParserTarget();
        public List<string> MedaDataStreamUS => _metaDataStreamUSParser?.GetParserTarget();
        public METADATATABLESHDR MetaDataStreamTablesHeader => _metaDataStreamTablesHeaderParser?.GetParserTarget();

        public DotNetStructureParsers(
            byte[] buff,
            IMAGE_COR20_HEADER imageCor20Header,
            IMAGE_SECTION_HEADER[] sectionHeaders)
        {
            _buff = buff;
            _sectionHeaders = sectionHeaders;
            _imageCor20Header = imageCor20Header;

            InitAllParsers();
        }

        private void InitAllParsers()
        {
            _metaDataHdrParser = InitMetaDataParser();
            _metaDataStreamStringParser = InitMetaDataStreamStringParser();
            _metaDataStreamUSParser = InitMetaDataStreamUSParser();
            _metaDataStreamTablesHeaderParser = InitMetaDataStreamTablesHeaderParser();
        }

        private MetaDataHdrParser InitMetaDataParser()
        {
            var rawAddress = _imageCor20Header?.MetaData.VirtualAddress.SafeRVAtoFileMapping(_sectionHeaders);
            return rawAddress == null ? null : new MetaDataHdrParser(_buff, rawAddress.Value);
        }

        private MetaDataStreamStringParser InitMetaDataStreamStringParser()
        {
            var metaDataStream = MetaDataHdr?.MetaDataStreamsHdrs?.FirstOrDefault(x => x.streamName == "#Strings");

            if (metaDataStream == null)
                return null;

            return new MetaDataStreamStringParser(_buff, MetaDataHdr.Offset + metaDataStream.offset, metaDataStream.size);
        }

        private MetaDataStreamUSParser InitMetaDataStreamUSParser()
        {
            var metaDataStream = MetaDataHdr?.MetaDataStreamsHdrs?.FirstOrDefault(x => x.streamName == "#US");

            if (metaDataStream == null)
                return null;

            return new MetaDataStreamUSParser(_buff, MetaDataHdr.Offset + metaDataStream.offset, metaDataStream.size);
        }

        private MetaDataStreamTablesHeaderParser InitMetaDataStreamTablesHeaderParser()
        {
            var metaDataStream = MetaDataHdr?.MetaDataStreamsHdrs?.FirstOrDefault(x => x.streamName == "#~");

            if (metaDataStream == null)
                return null;

            return new MetaDataStreamTablesHeaderParser(_buff, MetaDataHdr.Offset + metaDataStream.offset);
        }
    }
}