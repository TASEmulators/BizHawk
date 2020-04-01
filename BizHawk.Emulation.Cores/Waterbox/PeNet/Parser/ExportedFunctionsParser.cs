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
using PeNet.Utilities;

namespace PeNet.Parser
{
    internal class ExportedFunctionsParser : SafeParser<ExportFunction[]>
    {
        private readonly IMAGE_EXPORT_DIRECTORY _exportDirectory;
        private readonly IMAGE_SECTION_HEADER[] _sectionHeaders;

        internal ExportedFunctionsParser(
            byte[] buff,
            IMAGE_EXPORT_DIRECTORY exportDirectory,
            IMAGE_SECTION_HEADER[] sectionHeaders
            )
            : base(buff, 0)
        {
            _exportDirectory = exportDirectory;
            _sectionHeaders = sectionHeaders;
        }

        protected override ExportFunction[] ParseTarget()
        {
            if (_exportDirectory == null || _exportDirectory.AddressOfFunctions == 0)
                return null;

            var expFuncs = new ExportFunction[_exportDirectory.NumberOfFunctions];

            var funcOffsetPointer = _exportDirectory.AddressOfFunctions.RVAtoFileMapping(_sectionHeaders);
            var ordOffset = _exportDirectory.AddressOfNameOrdinals.RVAtoFileMapping(_sectionHeaders);
            var nameOffsetPointer = _exportDirectory.AddressOfNames.RVAtoFileMapping(_sectionHeaders);

            //Get addresses
            for (uint i = 0; i < expFuncs.Length; i++)
            {
                var ordinal = i + _exportDirectory.Base;
                var address = _buff.BytesToUInt32(funcOffsetPointer + sizeof(uint)*i);

                expFuncs[i] = new ExportFunction(null, address, (ushort) ordinal);
            }

            //Associate names
            for (uint i = 0; i < _exportDirectory.NumberOfNames; i++)
            {
                var namePtr = _buff.BytesToUInt32(nameOffsetPointer + sizeof(uint)*i);
                var nameAdr = namePtr.RVAtoFileMapping(_sectionHeaders);
                var name = _buff.GetCString(nameAdr);
                var ordinalIndex = (uint) _buff.GetOrdinal(ordOffset + sizeof(ushort)*i);

                expFuncs[ordinalIndex] = new ExportFunction(name, expFuncs[ordinalIndex].Address,
                    expFuncs[ordinalIndex].Ordinal);
            }

            return expFuncs;
        }
    }
}