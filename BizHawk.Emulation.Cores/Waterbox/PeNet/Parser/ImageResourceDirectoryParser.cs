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
    internal class ImageResourceDirectoryParser : SafeParser<IMAGE_RESOURCE_DIRECTORY>
    {
        internal ImageResourceDirectoryParser(byte[] buff, uint offset)
            : base(buff, offset)
        {
        }

        protected override IMAGE_RESOURCE_DIRECTORY ParseTarget()
        {
            if (_offset == 0)
                return null;

            // Parse the root directory.
            var root = new IMAGE_RESOURCE_DIRECTORY(_buff, _offset, _offset);

            // Parse the second stage (type)
            foreach (var de in root.DirectoryEntries)
            {
                de.ResourceDirectory = new IMAGE_RESOURCE_DIRECTORY(
                    _buff,
                    _offset + de.OffsetToDirectory,
                    _offset
                    );

                // Parse the third stage (name/IDs)
                foreach (var de2 in de.ResourceDirectory.DirectoryEntries)
                {
                    de2.ResourceDirectory = new IMAGE_RESOURCE_DIRECTORY(
                        _buff,
                        _offset + de2.OffsetToDirectory,
                        _offset
                        );

                    // Parse the forth stage (language) with the data.
                    foreach (var de3 in de2.ResourceDirectory.DirectoryEntries)
                    {
                        de3.ResourceDataEntry = new IMAGE_RESOURCE_DATA_ENTRY(_buff,
                            _offset + de3.OffsetToData);
                    }
                }
            }

            return root;
        }
    }
}