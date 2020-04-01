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
    /// COM+ 2.0 (CLI) Header
    /// https://www.codeproject.com/Articles/12585/The-NET-File-Format
    /// </summary>
    public class IMAGE_COR20_HEADER : AbstractStructure
    {
        private IMAGE_DATA_DIRECTORY _metaData;
        private IMAGE_DATA_DIRECTORY _resources;
        private IMAGE_DATA_DIRECTORY _strongSignatureNames;
        private IMAGE_DATA_DIRECTORY _codeManagerTable;
        private IMAGE_DATA_DIRECTORY _vTableFixups;
        private IMAGE_DATA_DIRECTORY _exportAddressTableJumps;
        private IMAGE_DATA_DIRECTORY _managedNativeHeader;

        /// <summary>
        /// Create a new instance of an COM+ 2 (CLI) header.
        /// </summary>
        /// <param name="buff">PE binary as byte array.</param>
        /// <param name="offset">Offset to the COM+ 2 (CLI) header in the byte array.</param>
        public IMAGE_COR20_HEADER(byte[] buff, uint offset) 
            : base(buff, offset)
        {
        }

        /// <summary>
        /// Size of the structure.
        /// </summary>
        public uint cb
        {
            get { return Buff.BytesToUInt32(Offset); }
            set { Buff.SetUInt32(Offset, value); }
        }

        /// <summary>
        /// Major runtime version of the CRL.
        /// </summary>
        public ushort MajorRuntimeVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x4); }
            set { Buff.SetUInt16(Offset + 0x4, value); }
        }

        /// <summary>
        /// Minor runtime version of the CRL.
        /// </summary>
        public ushort MinorRuntimeVersion
        {
            get { return Buff.BytesToUInt16(Offset + 0x6); }
            set { Buff.SetUInt16(Offset + 0x6, value); }
        }

        /// <summary>
        /// Meta data directory.
        /// </summary>
        public IMAGE_DATA_DIRECTORY MetaData
        {
            get
            {
                if (_metaData != null)
                    return _metaData;

                _metaData = SetImageDataDirectory(Buff, Offset + 0x8);
                return _metaData;
            }
        }
        
        /// <summary>
        /// COM image flags.
        /// </summary>
        public uint Flags
        {
            get { return Buff.BytesToUInt32(Offset + 0x10); }
            set { Buff.SetUInt32(Offset + 0x10, value); }
        }

        /// <summary>
        /// Represents the managed entry point if COMIMAGE_FLAGS_NATIVE_ENTRYPOINT is not set.
        /// Union with EntryPointRVA.
        /// </summary>
        public uint EntryPointToken
        {
            get { return Buff.BytesToUInt32(Offset + 0x14); }
            set { Buff.SetUInt32(Offset + 0x14, value); }
        }

        /// <summary>
        /// Represents an RVA to an native entry point if the COMIMAGE_FLAGS_NATIVE_ENTRYPOINT is set.
        /// Union with EntryPointToken.
        /// </summary>
        public uint EntryPointRVA
        {
            get { return EntryPointToken; }
            set { EntryPointToken = value; }
        }

        /// <summary>
        /// Resource data directory.
        /// </summary>
        public IMAGE_DATA_DIRECTORY Resources
        {
            get
            {
                if (_resources != null)
                    return _resources;

                _resources = SetImageDataDirectory(Buff, Offset+0x18);
                return _resources;
            }
        }

        /// <summary>
        /// Strong names signature directory.
        /// </summary>
        public IMAGE_DATA_DIRECTORY StrongNameSignature
        {
            get
            {
                if (_strongSignatureNames != null)
                    return _strongSignatureNames;

                _strongSignatureNames = SetImageDataDirectory(Buff, Offset + 0x20);
                return _strongSignatureNames;
            }
        }

        /// <summary>
        /// Code manager table directory.
        /// </summary>
        public IMAGE_DATA_DIRECTORY CodeManagerTable
        {
            get
            {
                if (_codeManagerTable != null)
                    return _codeManagerTable;

                _codeManagerTable = SetImageDataDirectory(Buff, Offset + 0x28);
                return _codeManagerTable;
            }
        }

        /// <summary>
        /// Virtual table fix up directory.
        /// </summary>
        public IMAGE_DATA_DIRECTORY VTableFixups
        {
            get
            {
                if (_vTableFixups != null)
                    return _vTableFixups;

                _vTableFixups = SetImageDataDirectory(Buff, Offset + 0x30);
                return _vTableFixups;
            }
        }

        /// <summary>
        /// Export address table jump directory.
        /// </summary>
        public IMAGE_DATA_DIRECTORY ExportAddressTableJumps
        {
            get
            {
                if (_exportAddressTableJumps != null)
                    return _exportAddressTableJumps;

                _exportAddressTableJumps = SetImageDataDirectory(Buff, Offset + 0x38);
                return _exportAddressTableJumps;
            }
        }

        /// <summary>
        /// Managed native header directory.
        /// </summary>
        public IMAGE_DATA_DIRECTORY ManagedNativeHeader
        {
            get
            {
                if (_managedNativeHeader != null)
                    return _managedNativeHeader;

                _managedNativeHeader = SetImageDataDirectory(Buff, Offset + 0x40);
                return _managedNativeHeader;
            }
        }

        private IMAGE_DATA_DIRECTORY SetImageDataDirectory(byte[] buff, uint offset)
        {
            try
            {
                return new IMAGE_DATA_DIRECTORY(buff, offset);
            }
            catch (Exception)
            {
                return null;
            }
        }


        /// <summary>
        ///     Convert all object properties to strings.
        /// </summary>
        /// <returns>String representation of the object</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("IMAGE_COR20_HEADER\n");
            sb.Append(this.PropertiesToString("{0,-10}:\t{1,10:X}\n"));

            return sb.ToString();
        }
    }
}