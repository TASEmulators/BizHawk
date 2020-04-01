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
    /// Bound import descriptor.
    /// </summary>
    public class IMAGE_BOUND_IMPORT_DESCRIPTOR : AbstractStructure
    {
        /// <summary>
        /// Create new bound import descriptor structure.
        /// </summary>
        /// <param name="buff">PE file as byte buffer.</param>
        /// <param name="offset">Offset of bound import descriptor in the buffer.</param>
        public IMAGE_BOUND_IMPORT_DESCRIPTOR(byte[] buff, uint offset) 
            : base(buff, offset)
        {
        }

        /// <summary>
        /// Time date stamp.
        /// </summary>
        public uint TimeDateStamp
        {
            get { return Buff.BytesToUInt32(Offset + 0); }
            set { Buff.SetUInt32(Offset + 0, value); }
        }

        /// <summary>
        /// Offset module name.
        /// </summary>
        public ushort OffsetModuleName
        {
            get { return Buff.BytesToUInt16(Offset + 4); }
            set { Buff.SetUInt16(Offset + 2, value); }
        }

        /// <summary>
        /// Number of moduke forwarder references.
        /// </summary>
        public ushort NumberOfModuleForwarderRefs
        {
            get { return Buff.BytesToUInt16(Offset + 6); }
            set { Buff.SetUInt16(Offset + 4, value); }
        }
    }
}