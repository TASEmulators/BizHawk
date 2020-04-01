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

using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     The runtime function struct is represents
    ///     a function in the exception header for x64
    ///     applications.
    /// </summary>
    public class RUNTIME_FUNCTION : AbstractStructure
    {
        private UNWIND_INFO _resolvedUnwindInfo;
        private readonly IMAGE_SECTION_HEADER[] _sectionHeaders;

        /// <summary>
        ///     Create a new RUNTIME_FUNCTION object.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset of the runtime function struct.</param>
        /// <param name="sh">Section Headers of the PE file.</param>
        public RUNTIME_FUNCTION(byte[] buff, uint offset, IMAGE_SECTION_HEADER[] sh)
            : base(buff, offset)
        {
            _sectionHeaders = sh;
        }

        /// <summary>
        ///     RVA Start of the function in code.
        /// </summary>
        public uint FunctionStart
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        ///     RVA End of the function in code.
        /// </summary>
        public uint FunctionEnd
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        ///     Pointer to the unwind information.
        /// </summary>
        public uint UnwindInfo
        {
            get { return Buff.BytesToUInt32(Offset + 0x8); }
            set { Buff.SetUInt32(Offset + 0x8, value); }
        }

        /// <summary>
        ///     Unwind Info object belonging to this Runtime Function.
        /// </summary>
        public UNWIND_INFO ResolvedUnwindInfo {
            get
            {
                if (_resolvedUnwindInfo != null)
                    return _resolvedUnwindInfo;

                _resolvedUnwindInfo = GetUnwindInfo(_sectionHeaders);
                return _resolvedUnwindInfo;
            }
        }

        /// <summary>
        ///     Get the UNWIND_INFO from a runtime function form the
        ///     Exception header in x64 applications.
        /// </summary>
        /// <param name="sh">Section Headers of the PE file.</param>
        /// <returns>UNWIND_INFO for the runtime function.</returns>
        private UNWIND_INFO GetUnwindInfo(IMAGE_SECTION_HEADER[] sh)
        {
            // Check if the last bit is set in the UnwindInfo. If so, it is a chained 
            // information.
            var uwAddress = (UnwindInfo & 0x1) == 0x1
                ? UnwindInfo & 0xFFFE
                : UnwindInfo;

            var uw = new UNWIND_INFO(Buff, uwAddress.RVAtoFileMapping(sh));
            return uw;
        }

        /// <summary>
        ///     Creates a string representation of the objects
        ///     properties.
        /// </summary>
        /// <returns>The runtime function properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("RUNTIME_FUNCTION\n");
            sb.Append(this.PropertiesToString("{0,-20}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}