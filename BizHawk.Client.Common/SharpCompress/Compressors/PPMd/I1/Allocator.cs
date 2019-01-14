namespace SharpCompress.Compressors.PPMd.I1
{
    /// Allocate a single, large array and then provide sections of this array to callers.  Callers are provided with
    /// instances of <see cref="Pointer"/> (which simply contain a single address value, representing a location
    /// in the large array).  Callers can then cast <see cref="Pointer"/> to one of the following structures (all
    /// of which also simply contain a single address value):
    internal class Allocator
    {
        private const uint UNIT_SIZE = 12;
        private const uint LOCAL_OFFSET = 4; // reserve the first four bytes for Pointer.Zero
        private const uint NODE_OFFSET = LOCAL_OFFSET + MemoryNode.SIZE; // reserve space for a single memory node

        private const uint HEAP_OFFSET = NODE_OFFSET + INDEX_COUNT * MemoryNode.SIZE;

        // reserve space for the array of memory nodes

        private const uint N1 = 4;
        private const uint N2 = 4;
        private const uint N3 = 4;
        private const uint N4 = (128 + 3 - 1 * N1 - 2 * N2 - 3 * N3) / 4;
        private const uint INDEX_COUNT = N1 + N2 + N3 + N4;

        private static readonly byte[] INDEX_TO_UNITS;
        private static readonly byte[] UNITS_TO_INDEX;

        public uint _allocatorSize;
        public uint _glueCount;
        public Pointer _baseUnit;
        public Pointer _lowUnit;
        public Pointer _highUnit;
        public Pointer _text;
        public Pointer _heap;
        public MemoryNode[] _memoryNodes;

        public byte[] _memory;

        /// <summary>
        /// Initializes static read-only arrays used by the <see cref="Allocator"/>.
        /// </summary>
        static Allocator()
        {
            // Construct the static index to units lookup array.  It will contain the following values.
            //
            // 1 2 3 4 6 8 10 12 15 18 21 24 28 32 36 40 44 48 52 56 60 64 68 72 76 80 84 88 92 96 100 104 108
            // 112 116 120 124 128

            uint index;
            uint unitCount;

            INDEX_TO_UNITS = new byte[INDEX_COUNT];

            for (index = 0, unitCount = 1; index < N1; index++, unitCount += 1)
            {
                INDEX_TO_UNITS[index] = (byte)unitCount;
            }

            for (unitCount++; index < N1 + N2; index++, unitCount += 2)
            {
                INDEX_TO_UNITS[index] = (byte)unitCount;
            }

            for (unitCount++; index < N1 + N2 + N3; index++, unitCount += 3)
            {
                INDEX_TO_UNITS[index] = (byte)unitCount;
            }

            for (unitCount++; index < N1 + N2 + N3 + N4; index++, unitCount += 4)
            {
                INDEX_TO_UNITS[index] = (byte)unitCount;
            }

            // Construct the static units to index lookup array.  It will contain the following values.
            //
            // 00 01 02 03 04 04 05 05 06 06 07 07 08 08 08 09 09 09 10 10 10 11 11 11 12 12 12 12 13 13 13 13
            // 14 14 14 14 15 15 15 15 16 16 16 16 17 17 17 17 18 18 18 18 19 19 19 19 20 20 20 20 21 21 21 21
            // 22 22 22 22 23 23 23 23 24 24 24 24 25 25 25 25 26 26 26 26 27 27 27 27 28 28 28 28 29 29 29 29
            // 30 30 30 30 31 31 31 31 32 32 32 32 33 33 33 33 34 34 34 34 35 35 35 35 36 36 36 36 37 37 37 37

            UNITS_TO_INDEX = new byte[128];

            for (unitCount = index = 0; unitCount < 128; unitCount++)
            {
                index += (uint)((INDEX_TO_UNITS[index] < unitCount + 1) ? 1 : 0);
                UNITS_TO_INDEX[unitCount] = (byte)index;
            }
        }

        #region Public Methods

        public Allocator()
        {
            _memoryNodes = new MemoryNode[INDEX_COUNT];
        }

        /// <summary>
        /// Initialize or reset the memory allocator (so that the single, large array can be re-used without destroying
        /// and re-creating it).
        /// </summary>
        public void Initialize()
        {
            for (int index = 0; index < INDEX_COUNT; index++)
            {
                _memoryNodes[index] = new MemoryNode((uint)(NODE_OFFSET + index * MemoryNode.SIZE), _memory);
                _memoryNodes[index].Stamp = 0;
                _memoryNodes[index].Next = MemoryNode.ZERO;
                _memoryNodes[index].UnitCount = 0;
            }

            _text = _heap;

            uint difference = UNIT_SIZE * (_allocatorSize / 8 / UNIT_SIZE * 7);

            _highUnit = _heap + _allocatorSize;
            _lowUnit = _highUnit - difference;
            _baseUnit = _highUnit - difference;

            _glueCount = 0;
        }

        /// <summary>
        /// Start the allocator (create a single, large array of bytes).
        /// </summary>
        /// <remarks>
        /// Note that .NET will create that array on the large object heap (because it is so large).
        /// </remarks>
        /// <param name="allocatorSize"></param>
        public void Start(int allocatorSize)
        {
            uint size = (uint)allocatorSize;
            if (_allocatorSize != size)
            {
                Stop();
                _memory = new byte[HEAP_OFFSET + size]; // the single, large array of bytes
                _heap = new Pointer(HEAP_OFFSET, _memory); // reserve bytes in the range 0 .. HeapOffset - 1
                _allocatorSize = size;
            }
        }

        /// <summary>
        /// Stop the allocator (free the single, large array of bytes).  This can safely be called multiple times (without
        /// intervening calls to <see cref="Start"/>).
        /// </summary>
        /// <remarks>
        /// Because the array is on the large object heap it may not be freed immediately.
        /// </remarks>
        public void Stop()
        {
            if (_allocatorSize != 0)
            {
                _allocatorSize = 0;
                _memory = null;
                _heap = Pointer.ZERO;
            }
        }

        /// <summary>
        /// Determine how much memory (from the single, large array) is currenly in use.
        /// </summary>
        /// <returns></returns>
        public uint GetMemoryUsed()
        {
            uint memoryUsed = _allocatorSize - (_highUnit - _lowUnit) - (_baseUnit - _text);
            for (uint index = 0; index < INDEX_COUNT; index++)
            {
                memoryUsed -= UNIT_SIZE * INDEX_TO_UNITS[index] * _memoryNodes[index].Stamp;
            }
            return memoryUsed;
        }

        /// <summary>
        /// Allocate a given number of units from the single, large array.  Each unit is <see cref="UNIT_SIZE"/> bytes
        /// in size.
        /// </summary>
        /// <param name="unitCount"></param>
        /// <returns></returns>
        public Pointer AllocateUnits(uint unitCount)
        {
            uint index = UNITS_TO_INDEX[unitCount - 1];
            if (_memoryNodes[index].Available)
            {
                return _memoryNodes[index].Remove();
            }

            Pointer allocatedBlock = _lowUnit;
            _lowUnit += INDEX_TO_UNITS[index] * UNIT_SIZE;
            if (_lowUnit <= _highUnit)
            {
                return allocatedBlock;
            }

            _lowUnit -= INDEX_TO_UNITS[index] * UNIT_SIZE;
            return AllocateUnitsRare(index);
        }

        /// <summary>
        /// Allocate enough space for a PpmContext instance in the single, large array.
        /// </summary>
        /// <returns></returns>
        public Pointer AllocateContext()
        {
            if (_highUnit != _lowUnit)
            {
                return (_highUnit -= UNIT_SIZE);
            }
            if (_memoryNodes[0].Available)
            {
                return _memoryNodes[0].Remove();
            }
            return AllocateUnitsRare(0);
        }

        /// <summary>
        /// Increase the size of an existing allocation (represented by a <see cref="Pointer"/>).
        /// </summary>
        /// <param name="oldPointer"></param>
        /// <param name="oldUnitCount"></param>
        /// <returns></returns>
        public Pointer ExpandUnits(Pointer oldPointer, uint oldUnitCount)
        {
            uint oldIndex = UNITS_TO_INDEX[oldUnitCount - 1];
            uint newIndex = UNITS_TO_INDEX[oldUnitCount];

            if (oldIndex == newIndex)
            {
                return oldPointer;
            }

            Pointer pointer = AllocateUnits(oldUnitCount + 1);

            if (pointer != Pointer.ZERO)
            {
                CopyUnits(pointer, oldPointer, oldUnitCount);
                _memoryNodes[oldIndex].Insert(oldPointer, oldUnitCount);
            }

            return pointer;
        }

        /// <summary>
        /// Decrease the size of an existing allocation (represented by a <see cref="Pointer"/>).
        /// </summary>
        /// <param name="oldPointer"></param>
        /// <param name="oldUnitCount"></param>
        /// <param name="newUnitCount"></param>
        /// <returns></returns>
        public Pointer ShrinkUnits(Pointer oldPointer, uint oldUnitCount, uint newUnitCount)
        {
            uint oldIndex = UNITS_TO_INDEX[oldUnitCount - 1];
            uint newIndex = UNITS_TO_INDEX[newUnitCount - 1];

            if (oldIndex == newIndex)
            {
                return oldPointer;
            }

            if (_memoryNodes[newIndex].Available)
            {
                Pointer pointer = _memoryNodes[newIndex].Remove();
                CopyUnits(pointer, oldPointer, newUnitCount);
                _memoryNodes[oldIndex].Insert(oldPointer, INDEX_TO_UNITS[oldIndex]);
                return pointer;
            }
            SplitBlock(oldPointer, oldIndex, newIndex);
            return oldPointer;
        }

        /// <summary>
        /// Free previously allocated space (the location and amount of space to free must be specified by using
        /// a <see cref="Pointer"/> to indicate the location and a number of units to indicate the amount).
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="unitCount"></param>
        public void FreeUnits(Pointer pointer, uint unitCount)
        {
            uint index = UNITS_TO_INDEX[unitCount - 1];
            _memoryNodes[index].Insert(pointer, INDEX_TO_UNITS[index]);
        }

        public void SpecialFreeUnits(Pointer pointer)
        {
            if (pointer != _baseUnit)
            {
                _memoryNodes[0].Insert(pointer, 1);
            }
            else
            {
                MemoryNode memoryNode = pointer;
                memoryNode.Stamp = uint.MaxValue;
                _baseUnit += UNIT_SIZE;
            }
        }

        public Pointer MoveUnitsUp(Pointer oldPointer, uint unitCount)
        {
            uint index = UNITS_TO_INDEX[unitCount - 1];

            if (oldPointer > _baseUnit + 16 * 1024 || oldPointer > _memoryNodes[index].Next)
            {
                return oldPointer;
            }

            Pointer pointer = _memoryNodes[index].Remove();
            CopyUnits(pointer, oldPointer, unitCount);
            unitCount = INDEX_TO_UNITS[index];

            if (oldPointer != _baseUnit)
            {
                _memoryNodes[index].Insert(oldPointer, unitCount);
            }
            else
            {
                _baseUnit += unitCount * UNIT_SIZE;
            }

            return pointer;
        }

        /// <summary>
        /// Expand the space allocated (in the single, large array) for the bytes of the data (ie. the "text") that is
        /// being encoded or decoded.
        /// </summary>
        public void ExpandText()
        {
            MemoryNode memoryNode;
            uint[] counts = new uint[INDEX_COUNT];

            while ((memoryNode = _baseUnit).Stamp == uint.MaxValue)
            {
                _baseUnit = memoryNode + memoryNode.UnitCount;
                counts[UNITS_TO_INDEX[memoryNode.UnitCount - 1]]++;
                memoryNode.Stamp = 0;
            }

            for (uint index = 0; index < INDEX_COUNT; index++)
            {
                for (memoryNode = _memoryNodes[index]; counts[index] != 0; memoryNode = memoryNode.Next)
                {
                    while (memoryNode.Next.Stamp == 0)
                    {
                        memoryNode.Unlink();
                        _memoryNodes[index].Stamp--;
                        if (--counts[index] == 0)
                        {
                            break;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private Pointer AllocateUnitsRare(uint index)
        {
            if (_glueCount == 0)
            {
                GlueFreeBlocks();
                if (_memoryNodes[index].Available)
                {
                    return _memoryNodes[index].Remove();
                }
            }

            uint oldIndex = index;
            do
            {
                if (++oldIndex == INDEX_COUNT)
                {
                    _glueCount--;
                    oldIndex = INDEX_TO_UNITS[index] * UNIT_SIZE;
                    return (_baseUnit - _text > oldIndex) ? (_baseUnit -= oldIndex) : Pointer.ZERO;
                }
            }
            while (!_memoryNodes[oldIndex].Available);

            Pointer allocatedBlock = _memoryNodes[oldIndex].Remove();
            SplitBlock(allocatedBlock, oldIndex, index);
            return allocatedBlock;
        }

        private void SplitBlock(Pointer pointer, uint oldIndex, uint newIndex)
        {
            uint unitCountDifference = (uint)(INDEX_TO_UNITS[oldIndex] - INDEX_TO_UNITS[newIndex]);
            Pointer newPointer = pointer + INDEX_TO_UNITS[newIndex] * UNIT_SIZE;

            uint index = UNITS_TO_INDEX[unitCountDifference - 1];
            if (INDEX_TO_UNITS[index] != unitCountDifference)
            {
                uint unitCount = INDEX_TO_UNITS[--index];
                _memoryNodes[index].Insert(newPointer, unitCount);
                newPointer += unitCount * UNIT_SIZE;
                unitCountDifference -= unitCount;
            }

            _memoryNodes[UNITS_TO_INDEX[unitCountDifference - 1]].Insert(newPointer, unitCountDifference);
        }

        private void GlueFreeBlocks()
        {
            MemoryNode memoryNode = new MemoryNode(LOCAL_OFFSET, _memory);
            memoryNode.Stamp = 0;
            memoryNode.Next = MemoryNode.ZERO;
            memoryNode.UnitCount = 0;

            MemoryNode memoryNode0;
            MemoryNode memoryNode1;
            MemoryNode memoryNode2;

            if (_lowUnit != _highUnit)
            {
                _lowUnit[0] = 0;
            }

            // Find all unused memory nodes.

            memoryNode1 = memoryNode;
            for (uint index = 0; index < INDEX_COUNT; index++)
            {
                while (_memoryNodes[index].Available)
                {
                    memoryNode0 = _memoryNodes[index].Remove();
                    if (memoryNode0.UnitCount != 0)
                    {
                        while ((memoryNode2 = memoryNode0 + memoryNode0.UnitCount).Stamp == uint.MaxValue)
                        {
                            memoryNode0.UnitCount = memoryNode0.UnitCount + memoryNode2.UnitCount;
                            memoryNode2.UnitCount = 0;
                        }
                        memoryNode1.Link(memoryNode0);
                        memoryNode1 = memoryNode0;
                    }
                }
            }

            // Coalesce the memory represented by the unused memory nodes.

            while (memoryNode.Available)
            {
                memoryNode0 = memoryNode.Remove();
                uint unitCount = memoryNode0.UnitCount;
                if (unitCount != 0)
                {
                    for (; unitCount > 128; unitCount -= 128, memoryNode0 += 128)
                    {
                        _memoryNodes[INDEX_COUNT - 1].Insert(memoryNode0, 128);
                    }

                    uint index = UNITS_TO_INDEX[unitCount - 1];
                    if (INDEX_TO_UNITS[index] != unitCount)
                    {
                        uint unitCountDifference = unitCount - INDEX_TO_UNITS[--index];
                        _memoryNodes[unitCountDifference - 1].Insert(memoryNode0 + (unitCount - unitCountDifference),
                                                                    unitCountDifference);
                    }

                    _memoryNodes[index].Insert(memoryNode0, INDEX_TO_UNITS[index]);
                }
            }

            _glueCount = 1 << 13;
        }

        private void CopyUnits(Pointer target, Pointer source, uint unitCount)
        {
            do
            {
                target[0] = source[0];
                target[1] = source[1];
                target[2] = source[2];
                target[3] = source[3];
                target[4] = source[4];
                target[5] = source[5];
                target[6] = source[6];
                target[7] = source[7];
                target[8] = source[8];
                target[9] = source[9];
                target[10] = source[10];
                target[11] = source[11];
                target += UNIT_SIZE;
                source += UNIT_SIZE;
            }
            while (--unitCount != 0);
        }

        #endregion
    }
}