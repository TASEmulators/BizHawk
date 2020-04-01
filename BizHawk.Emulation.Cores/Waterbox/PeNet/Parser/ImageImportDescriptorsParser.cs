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

namespace PeNet.Parser
{
    internal class ImageImportDescriptorsParser : SafeParser<IMAGE_IMPORT_DESCRIPTOR[]>
    {
        public ImageImportDescriptorsParser(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        protected override IMAGE_IMPORT_DESCRIPTOR[] ParseTarget()
        {
            if (_offset == 0)
                return null;

            var idescs = new List<IMAGE_IMPORT_DESCRIPTOR>();
            uint idescSize = 20; // Size of IMAGE_IMPORT_DESCRIPTOR (5 * 4 Byte)
            uint round = 0;

            while (true)
            {
                var idesc = new IMAGE_IMPORT_DESCRIPTOR(_buff, _offset + idescSize*round);

                // Found the last IMAGE_IMPORT_DESCRIPTOR which is completely null (except TimeDateStamp).
                if (idesc.OriginalFirstThunk == 0
                    //&& idesc.TimeDateStamp == 0
                    && idesc.ForwarderChain == 0
                    && idesc.Name == 0
                    && idesc.FirstThunk == 0)
                {
                    break;
                }

                idescs.Add(idesc);
                round++;
            }


            return idescs.ToArray();
        }
    }
}