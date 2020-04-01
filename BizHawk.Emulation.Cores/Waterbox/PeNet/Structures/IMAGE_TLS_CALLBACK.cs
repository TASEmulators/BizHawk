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

using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    /// Thread Local Storage callback.
    /// </summary>
    public class IMAGE_TLS_CALLBACK : AbstractStructure
    {
        private readonly bool _is64Bit;

        /// <summary>
        /// Create a new TLS callback structure.
        /// </summary>
        /// <param name="buff">PE file as byte buffer.</param>
        /// <param name="offset">Offset of the TLS callback structure in the buffer.</param>
        /// <param name="is64Bit">Flag is the PE file is 64 Bit.</param>
        public IMAGE_TLS_CALLBACK(byte[] buff, uint offset, bool is64Bit) 
            : base(buff, offset)
        {
            _is64Bit = is64Bit;
        }

        /// <summary>
        /// Address of actual callback code.
        /// </summary>
        public ulong Callback
        {
            get
            {
                return _is64Bit ? Buff.BytesToUInt64(Offset + 0) : Buff.BytesToUInt32(Offset + 0);
            }
            set
            {
                if(_is64Bit)
                    Buff.SetUInt64(Offset + 0, value);
                else
                    Buff.SetUInt32(Offset + 0, (uint) value);
            }
        }
    }
}