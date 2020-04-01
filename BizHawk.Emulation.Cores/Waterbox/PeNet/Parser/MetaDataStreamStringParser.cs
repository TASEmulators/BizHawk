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

using System;
using System.Collections.Generic;
using PeNet.Utilities;

namespace PeNet.Parser
{
    internal class MetaDataStreamStringParser : SafeParser<List<string>>
    {
        private readonly uint _size;

        public MetaDataStreamStringParser(
            byte[] buff, 
            uint offset,
            uint size
            ) 
            : base(buff, offset)
        {
            _size = size;
        }

        protected override List<string> ParseTarget()
        {
            var stringList = new List<string>();

            for (var i = _offset; i < _offset + _size; i++)
            {
                var tmpString = _buff.GetCString(i);
                i += (uint) tmpString.Length;

                if(String.IsNullOrWhiteSpace(tmpString))
                    continue;

                stringList.Add(tmpString);
            }

            return stringList;
        }
    }
}