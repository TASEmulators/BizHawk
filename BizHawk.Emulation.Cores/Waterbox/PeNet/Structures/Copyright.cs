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
using ExtensionMethods = PeNet.Utilities.ExtensionMethods;

namespace PeNet.Structures
{
    /// <summary>
    ///     The copyright ASCII (not 0-terminated) string of the PE file
    ///     if any is given.
    /// </summary>
    public class Copyright : AbstractStructure
    {
        /// <summary>
        ///     Create a new copyright object.
        /// </summary>
        /// <param name="buff">PE binary as byte array.</param>
        /// <param name="offset">Offset to the copyright string in the binary.</param>
        /// <param name="size">Size of the copyright string.</param>
        public Copyright(byte[] buff, uint offset, uint size)
            : base(buff, offset)
        {
            CopyrightString = ParseCopyrightString(buff, offset, size);
        }

        /// <summary>
        ///     The copyright string.
        /// </summary>
        public string CopyrightString { get; private set; }

        private string ParseCopyrightString(byte[] buff, uint offset, uint size)
        {
            return Encoding.ASCII.GetString(buff, (int) offset, (int) size);
        }


        /// <summary>
        ///     Convert all object properties to strings.
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("Copyright\n");
            sb.Append(ExtensionMethods.PropertiesToString(this, "{0,-10}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}