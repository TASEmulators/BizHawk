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
    internal class ImageTlsDirectoryParser : SafeParser<IMAGE_TLS_DIRECTORY>
    {
        private readonly bool _is64Bit;
        private readonly IMAGE_SECTION_HEADER[] _sectionsHeaders;

        internal ImageTlsDirectoryParser(
            byte[] buff, 
            uint offset, 
            bool is64Bit, 
            IMAGE_SECTION_HEADER[] sectionHeaders
            ) 
            : base(buff, offset)
        {
            _is64Bit = is64Bit;
            _sectionsHeaders = sectionHeaders;
        }

        protected override IMAGE_TLS_DIRECTORY ParseTarget()
        {
            var tlsDir = new IMAGE_TLS_DIRECTORY(_buff, _offset, _is64Bit);
            tlsDir.TlsCallbacks = ParseTlsCallbacks(tlsDir.AddressOfCallBacks);
            return tlsDir;
        }

        private IMAGE_TLS_CALLBACK[] ParseTlsCallbacks(ulong addressOfCallBacks)
        {
            var callbacks = new List<IMAGE_TLS_CALLBACK>();
            var rawAddressOfCallbacks = (uint) addressOfCallBacks.VAtoFileMapping(_sectionsHeaders);

            uint count = 0;
            while (true)
            {
                if (_is64Bit)
                {
                    var cb = new IMAGE_TLS_CALLBACK(_buff, rawAddressOfCallbacks + count*8, _is64Bit);
                    if (cb.Callback == 0)
                        break;

                    callbacks.Add(cb);
                    count++;
                }
                else
                {
                    var cb = new IMAGE_TLS_CALLBACK(_buff, rawAddressOfCallbacks + count*4, _is64Bit);
                    if (cb.Callback == 0)
                        break;

                    callbacks.Add(cb);
                    count++;
                }
            }

            return callbacks.ToArray();
        }
    }
}