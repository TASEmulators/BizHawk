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
    internal class RuntimeFunctionsParser : SafeParser<RUNTIME_FUNCTION[]>
    {
        private readonly uint _directorySize;
        private readonly bool _is32Bit;
        private readonly IMAGE_SECTION_HEADER[] _sectionHeaders;

        public RuntimeFunctionsParser(
            byte[] buff,
            uint offset,
            bool is32Bit,
            uint directorySize,
            IMAGE_SECTION_HEADER[] sectionHeaders
            )
            : base(buff, offset)
        {
            _is32Bit = is32Bit;
            _directorySize = directorySize;
            _sectionHeaders = sectionHeaders;
        }

        protected override RUNTIME_FUNCTION[] ParseTarget()
        {
            if (_is32Bit || _offset == 0)
                return null;

            var sizeOfRuntimeFunction = 0xC;
            var rf = new RUNTIME_FUNCTION[_directorySize/sizeOfRuntimeFunction];

            for (var i = 0; i < rf.Length; i++)
            {
                rf[i] = new RUNTIME_FUNCTION(_buff, (uint) (_offset + i*sizeOfRuntimeFunction), _sectionHeaders);
            }

            return rf;
        }
    }
}