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

using PeNet.Structures;

namespace PeNet.Parser
{
    internal class ImageSectionHeadersParser : SafeParser<IMAGE_SECTION_HEADER[]>
    {
        private readonly ushort _numOfSections;

        internal ImageSectionHeadersParser(byte[] buff, uint offset, ushort numOfSections)
            : base(buff, offset)
        {
            _numOfSections = numOfSections;
        }

        protected override IMAGE_SECTION_HEADER[] ParseTarget()
        {
            var sh = new IMAGE_SECTION_HEADER[_numOfSections];
            uint secSize = 0x28; // Every section header is 40 bytes in size.
            for (uint i = 0; i < _numOfSections; i++)
            {
                sh[i] = new IMAGE_SECTION_HEADER(_buff, _offset + i*secSize);
            }

            return sh;
        }
    }
}