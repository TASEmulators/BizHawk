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
using PeNet.Structures;

namespace PeNet.Parser
{
    internal class ImageBaseRelocationsParser : SafeParser<IMAGE_BASE_RELOCATION[]>
    {
        private readonly uint _directorySize;

        public ImageBaseRelocationsParser(
            byte[] buff,
            uint offset,
            uint directorySize
            )
            : base(buff, offset)
        {
            _directorySize = directorySize;
        }

        protected override IMAGE_BASE_RELOCATION[] ParseTarget()
        {
            if (_offset == 0)
                return null;

            var imageBaseRelocations = new List<IMAGE_BASE_RELOCATION>();
            var currentBlock = _offset;


            while (true)
            {
                if (currentBlock >= _offset + _directorySize - 8)
                    break;

                imageBaseRelocations.Add(new IMAGE_BASE_RELOCATION(_buff, currentBlock, _directorySize));
                currentBlock += imageBaseRelocations.Last().SizeOfBlock;
            }

            return imageBaseRelocations.ToArray();
        }
    }
}