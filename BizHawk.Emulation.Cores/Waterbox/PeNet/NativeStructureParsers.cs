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

using PeNet.Parser;
using PeNet.Structures;
using PeNet.Utilities;

namespace PeNet
{
    internal class NativeStructureParsers
    {
        private readonly byte[] _buff;
        private ImageDosHeaderParser _imageDosHeaderParser;
        private ImageNtHeadersParser _imageNtHeadersParser;
        private ImageSectionHeadersParser _imageSectionHeadersParser;

        internal NativeStructureParsers(byte[] buff)
        {
            _buff = buff;
            InitAllParsers();
        }

        public IMAGE_DOS_HEADER ImageDosHeader => _imageDosHeaderParser?.GetParserTarget();
        public IMAGE_NT_HEADERS ImageNtHeaders => _imageNtHeadersParser?.GetParserTarget();
        public IMAGE_SECTION_HEADER[] ImageSectionHeaders => _imageSectionHeadersParser?.GetParserTarget();

        private bool Is64Bit => _buff.BytesToUInt16(ImageDosHeader.e_lfanew + 0x4) ==
                                (ushort) Constants.FileHeaderMachine.IMAGE_FILE_MACHINE_AMD64;

        private void InitAllParsers()
        {
            _imageDosHeaderParser = InitImageDosHeaderParser();
            _imageNtHeadersParser = InitNtHeadersParser();
            _imageSectionHeadersParser = InitImageSectionHeadersParser();
        }

        private ImageSectionHeadersParser InitImageSectionHeadersParser()
        {
            return new ImageSectionHeadersParser(_buff, GetSecHeaderOffset(), ImageNtHeaders.FileHeader.NumberOfSections);
        }

        private ImageNtHeadersParser InitNtHeadersParser()
        {
            return new ImageNtHeadersParser(_buff, ImageDosHeader.e_lfanew, Is64Bit);
        }

        private ImageDosHeaderParser InitImageDosHeaderParser()
        {
            return new ImageDosHeaderParser(_buff, 0);
        }

        private uint GetSecHeaderOffset()
        {
            var x = (uint) ImageNtHeaders.FileHeader.SizeOfOptionalHeader + 0x18;
            return ImageDosHeader.e_lfanew + x;
        }
    }
}