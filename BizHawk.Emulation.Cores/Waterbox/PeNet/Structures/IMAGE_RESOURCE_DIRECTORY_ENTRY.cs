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
using System.Text;
using PeNet.Utilities;

namespace PeNet.Structures
{
    /// <summary>
    ///     The resource directory entry represents one entry (e.g. icon)
    ///     in a resource directory.
    /// </summary>
    public class IMAGE_RESOURCE_DIRECTORY_ENTRY : AbstractStructure
    {
        /// <summary>
        ///     Create a new instance of the IMAGE_RESOURCE_DIRECTORY_ENTRY.
        /// </summary>
        /// <param name="buff">A PE file as a byte array.</param>
        /// <param name="offset">Raw offset to the entry.</param>
        /// <param name="resourceDirOffset">Raw offset to the resource directory.</param>
        public IMAGE_RESOURCE_DIRECTORY_ENTRY(byte[] buff, uint offset, uint resourceDirOffset)
            : base(buff, offset)
        {
            // Resolve the Name
            try
            {
                if (IsIdEntry)
                {
                    ResolvedName = Utilities.FlagResolver.ResolveResourceId(ID);
                }
                else if (IsNamedEntry)
                {
                    var nameAddress = resourceDirOffset + (Name & 0x7FFFFFFF);
                    var unicodeName = new IMAGE_RESOURCE_DIR_STRING_U(Buff, nameAddress);
                    ResolvedName = unicodeName.NameString;
                }
            }
            catch (Exception)
            {
                ResolvedName = null;
            }
        }

        /// <summary>
        ///     Get the Resource Directory which the Directory Entry points
        ///     to if the Directory Entry has DataIsDirectory set.
        /// </summary>
        public IMAGE_RESOURCE_DIRECTORY ResourceDirectory { get; internal set; }

        /// <summary>
        ///     Get the Resource Data Entry if the entry is no directory.
        /// </summary>
        public IMAGE_RESOURCE_DATA_ENTRY ResourceDataEntry { get; internal set; }

        /// <summary>
        ///     Address of the name if its a named resource.
        /// </summary>
        public uint Name
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        ///     The resolved name as a string if its a named resource.
        /// </summary>
        public string ResolvedName { get; private set; }

        /// <summary>
        ///     The ID if its a ID resource.
        ///     You can resolve the ID to a string with Utility.ResolveResourceId(id)
        /// </summary>
        public uint ID
        {
            get { return Name & 0xFFFF; }
            set { Name = value & 0xFFFF; }
        }

        /// <summary>
        ///     Offset to the data.
        /// </summary>
        public uint OffsetToData
        {
            get { return Buff.BytesToUInt32(Offset + 0x4); }
            set { Buff.SetUInt32(Offset + 0x4, value); }
        }

        /// <summary>
        ///     Offset to the next directory.
        /// </summary>
        public uint OffsetToDirectory => OffsetToData & 0x7FFFFFFF;

        /// <summary>
        ///     True if the entry data is a directory
        /// </summary>
        public bool DataIsDirectory
        {
            get
            {
                if ((OffsetToData & 0x80000000) == 0x80000000)
                    return true;
                return false;
            }
        }

        /// <summary>
        ///     True if the entry is a resource with a name.
        /// </summary>
        public bool IsNamedEntry
        {
            get
            {
                if ((Name & 0x80000000) == 0x80000000)
                    return true;
                return false;
            }
        }

        /// <summary>
        ///     True if the entry is a resource with an ID instead of a name.
        /// </summary>
        public bool IsIdEntry => !IsNamedEntry;

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        ///     A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_RESOURCE_DIRECTORY_ENTRY\n");
            sb.Append(this.PropertiesToString("{0,-20}:\t{1,10:X}\n"));
            if (ResourceDirectory != null)
                sb.Append(ResourceDirectory);
            return sb.ToString();
        }
    }
}