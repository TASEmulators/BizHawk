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
using PeNet.Structures;
using PeNet.Utilities;

namespace PeNet.Parser
{
    internal class ImportedFunctionsParser : SafeParser<ImportFunction[]>
    {
        private readonly IMAGE_IMPORT_DESCRIPTOR[] _importDescriptors;
        private readonly bool _is64Bit;
        private readonly IMAGE_SECTION_HEADER[] _sectionHeaders;

        internal ImportedFunctionsParser(
            byte[] buff,
            IMAGE_IMPORT_DESCRIPTOR[] importDescriptors,
            IMAGE_SECTION_HEADER[] sectionHeaders,
            bool is64Bit) :
                base(buff, 0)
        {
            _importDescriptors = importDescriptors;
            _sectionHeaders = sectionHeaders;
            _is64Bit = is64Bit;
        }

        protected override ImportFunction[] ParseTarget()
        {
            if (_importDescriptors == null)
                return null;

            var impFuncs = new List<ImportFunction>();
            var sizeOfThunk = (uint) (_is64Bit ? 0x8 : 0x4); // Size of IMAGE_THUNK_DATA
            var ordinalBit = _is64Bit ? 0x8000000000000000 : 0x80000000;
            var ordinalMask = (ulong) (_is64Bit ? 0x7FFFFFFFFFFFFFFF : 0x7FFFFFFF);

            foreach (var idesc in _importDescriptors)
            {
                var dllAdr = idesc.Name.RVAtoFileMapping(_sectionHeaders);
                var dll = _buff.GetCString(dllAdr);
                var tmpAdr = /*idesc.OriginalFirstThunk != 0 ?*/ idesc.OriginalFirstThunk /*: idesc.FirstThunk*/;
                if (tmpAdr == 0)
                    continue;

                var thunkAdr = tmpAdr.RVAtoFileMapping(_sectionHeaders);
				var thunkDestAdr = idesc.FirstThunk;
                uint round = 0;
                while (true)
                {
                    var t = new IMAGE_THUNK_DATA(_buff, thunkAdr + round*sizeOfThunk, _is64Bit);

                    if (t.AddressOfData == 0)
                        break;

					var td = thunkDestAdr + round * sizeOfThunk;
					
					// Check if import by name or by ordinal.
                    // If it is an import by ordinal, the most significant bit of "Ordinal" is "1" and the ordinal can
                    // be extracted from the least significant bits.
                    // Else it is an import by name and the link to the IMAGE_IMPORT_BY_NAME has to be followed

                    if ((t.Ordinal & ordinalBit) == ordinalBit) // Import by ordinal
                    {
                        impFuncs.Add(new ImportFunction(null, dll, (ushort) (t.Ordinal & ordinalMask), td));
                    }
                    else // Import by name
                    {
                        var ibn = new IMAGE_IMPORT_BY_NAME(_buff,
                            ((uint) t.AddressOfData).RVAtoFileMapping(_sectionHeaders));
                        impFuncs.Add(new ImportFunction(ibn.Name, dll, ibn.Hint, td));
                    }

                    round++;
                }
            }


            return impFuncs.ToArray();
        }
    }
}