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

namespace PeNet.Utilities
{
    /// <summary>
    /// Computes the index sizes of #String, #GUID and #Blob
    /// based on the HeapOffsetSizes value in the Meta Data Tables Header.
    /// </summary>
    public class HeapOffsetBasedIndexSizes
    {
        private readonly byte _heapOffsetSizes;

        /// <summary>
        /// Size of the #String index (4 or 2 bytes).
        /// </summary>
        public uint StringIndexSize => (uint) ((_heapOffsetSizes & 0x1) == 0x1 ? 4 : 2);

        /// <summary>
        /// Size of the #GUID index (4 or 2 bytes).
        /// </summary>
        public uint GuidIndexSize => (uint) (((_heapOffsetSizes >> 1) & 0x1) == 0x1 ? 4 : 2);

        /// <summary>
        /// Size of the #Blob index (4 or 2 bytes).
        /// </summary>
        public uint BlobSize => (uint) (((_heapOffsetSizes >> 2) & 0x1) == 0x1 ? 4 : 2);

        /// <summary>
        /// Create a new HeapOffsetBasedIndexSizes instance based
        /// on the HeapOffsetSizes byte from the Meta Data Tables Header.
        /// </summary>
        /// <param name="heapOffsetSizes"></param>
        public HeapOffsetBasedIndexSizes(byte heapOffsetSizes)
        {
            _heapOffsetSizes = heapOffsetSizes;
        }
    }
}