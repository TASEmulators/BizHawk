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
    ///     The IMAGE_IMPORT_BY_NAME structure is used to
    ///     describes imports of functions or symbols by their name.
    ///     The AddressOfData in the IMAGE_THUNK_DATA from the
    ///     IMAGE_IMPORT_DESCRIPTOR points to it.
    /// </summary>
    public class IMAGE_IMPORT_BY_NAME : AbstractStructure
    {
        /// <summary>
        ///     Create new IMAGE_IMPORT_BY_NAME object.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset of the IMAGE_IMPORT_BY_NAME.</param>
        public IMAGE_IMPORT_BY_NAME(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        /// <summary>
        ///     Hint.
        /// </summary>
        public ushort Hint
        {
            get { return Buff.BytesToUInt16(Offset); }
            set { Buff.SetUInt16(Offset, value); }
        }

        /// <summary>
        ///     Name of the function to import as a C-string (null terminated).
        /// </summary>
        public string Name => Buff.GetCString(Offset + 0x2);

        /// <summary>
        ///     Creates a string representation of the objects properties.
        /// </summary>
        /// <returns>The IMAGE_IMPORT_BY_NAME properties as a string.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_IMPORT_BY_NAME\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));
            return sb.ToString();
        }
    }
}