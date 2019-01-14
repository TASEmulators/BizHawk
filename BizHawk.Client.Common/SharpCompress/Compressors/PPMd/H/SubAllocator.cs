using System;
using System.Text;

namespace SharpCompress.Compressors.PPMd.H
{
    internal class SubAllocator
    {
        public virtual int FakeUnitsStart { get => _fakeUnitsStart; set => _fakeUnitsStart = value; }

        public virtual int HeapEnd => _heapEnd;

        public virtual int PText { get => _pText; set => _pText = value; }

        public virtual int UnitsStart { get => _unitsStart; set => _unitsStart = value; }

        public virtual byte[] Heap => _heap;

        //UPGRADE_NOTE: Final was removed from the declaration of 'N4 '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        public const int N1 = 4;
        public const int N2 = 4;
        public const int N3 = 4;
        public static readonly int N4 = (128 + 3 - 1 * N1 - 2 * N2 - 3 * N3) / 4;

        //UPGRADE_NOTE: Final was removed from the declaration of 'N_INDEXES '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        public static readonly int N_INDEXES = N1 + N2 + N3 + N4;

        //UPGRADE_NOTE: Final was removed from the declaration of 'UNIT_SIZE '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        //UPGRADE_NOTE: The initialization of  'UNIT_SIZE' was moved to static method 'SharpCompress.Unpack.PPM.SubAllocator'. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1005'"
        public static readonly int UNIT_SIZE;

        public const int FIXED_UNIT_SIZE = 12;

        private int _subAllocatorSize;

        // byte Indx2Units[N_INDEXES], Units2Indx[128], GlueCount;
        private readonly int[] _indx2Units = new int[N_INDEXES];
        private readonly int[] _units2Indx = new int[128];
        private int _glueCount;

        // byte *HeapStart,*LoUnit, *HiUnit;
        private int _heapStart, _loUnit, _hiUnit;

        //UPGRADE_NOTE: Final was removed from the declaration of 'freeList '. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1003'"
        private readonly RarNode[] _freeList = new RarNode[N_INDEXES];

        // byte *pText, *UnitsStart,*HeapEnd,*FakeUnitsStart;
        private int _pText, _unitsStart, _heapEnd, _fakeUnitsStart;

        private byte[] _heap;

        private int _freeListPos;

        private int _tempMemBlockPos;

        // Temp fields
        private RarNode _tempRarNode;
        private RarMemBlock _tempRarMemBlock1;
        private RarMemBlock _tempRarMemBlock2;
        private RarMemBlock _tempRarMemBlock3;

        public SubAllocator()
        {
            Clean();
        }

        public virtual void Clean()
        {
            _subAllocatorSize = 0;
        }

        private void InsertNode(int p, int indx)
        {
            RarNode temp = _tempRarNode;
            temp.Address = p;
            temp.SetNext(_freeList[indx].GetNext());
            _freeList[indx].SetNext(temp);
        }

        public virtual void IncPText()
        {
            _pText++;
        }

        private int RemoveNode(int indx)
        {
            int retVal = _freeList[indx].GetNext();
            RarNode temp = _tempRarNode;
            temp.Address = retVal;
            _freeList[indx].SetNext(temp.GetNext());
            return retVal;
        }

        private int U2B(int nu)
        {
            return UNIT_SIZE * nu;
        }

        /* memblockptr */

        private int MbPtr(int basePtr, int items)
        {
            return (basePtr + U2B(items));
        }

        private void SplitBlock(int pv, int oldIndx, int newIndx)
        {
            int i, uDiff = _indx2Units[oldIndx] - _indx2Units[newIndx];
            int p = pv + U2B(_indx2Units[newIndx]);
            if (_indx2Units[i = _units2Indx[uDiff - 1]] != uDiff)
            {
                InsertNode(p, --i);
                p += U2B(i = _indx2Units[i]);
                uDiff -= i;
            }
            InsertNode(p, _units2Indx[uDiff - 1]);
        }

        public virtual void StopSubAllocator()
        {
            if (_subAllocatorSize != 0)
            {
                _subAllocatorSize = 0;

                //ArrayFactory.BYTES_FACTORY.recycle(heap);
                _heap = null;
                _heapStart = 1;

                // rarfree(HeapStart);
                // Free temp fields
                _tempRarNode = null;
                _tempRarMemBlock1 = null;
                _tempRarMemBlock2 = null;
                _tempRarMemBlock3 = null;
            }
        }

        public virtual int GetAllocatedMemory()
        {
            return _subAllocatorSize;
        }

        public virtual bool StartSubAllocator(int saSize)
        {
            int t = saSize;
            if (_subAllocatorSize == t)
            {
                return true;
            }
            StopSubAllocator();
            int allocSize = t / FIXED_UNIT_SIZE * UNIT_SIZE + UNIT_SIZE;

            // adding space for freelist (needed for poiters)
            // 1+ for null pointer
            int realAllocSize = 1 + allocSize + 4 * N_INDEXES;

            // adding space for an additional memblock
            _tempMemBlockPos = realAllocSize;
            realAllocSize += RarMemBlock.SIZE;

            _heap = new byte[realAllocSize];
            _heapStart = 1;
            _heapEnd = _heapStart + allocSize - UNIT_SIZE;
            _subAllocatorSize = t;

            // Bug fixed
            _freeListPos = _heapStart + allocSize;

            //UPGRADE_ISSUE: The following fragment of code could not be parsed and was not converted. "ms-help://MS.VSCC.v80/dv_commoner/local/redirect.htm?index='!DefaultContextWindowIndex'&keyword='jlca1156'"
            //assert(realAllocSize - tempMemBlockPos == RarMemBlock.size): realAllocSize 
            //+   + tempMemBlockPos +   + RarMemBlock.size;

            // Init freeList
            for (int i = 0, pos = _freeListPos; i < _freeList.Length; i++, pos += RarNode.SIZE)
            {
                _freeList[i] = new RarNode(_heap);
                _freeList[i].Address = pos;
            }

            // Init temp fields
            _tempRarNode = new RarNode(_heap);
            _tempRarMemBlock1 = new RarMemBlock(_heap);
            _tempRarMemBlock2 = new RarMemBlock(_heap);
            _tempRarMemBlock3 = new RarMemBlock(_heap);

            return true;
        }

        private void GlueFreeBlocks()
        {
            RarMemBlock s0 = _tempRarMemBlock1;
            s0.Address = _tempMemBlockPos;
            RarMemBlock p = _tempRarMemBlock2;
            RarMemBlock p1 = _tempRarMemBlock3;
            int i, k, sz;
            if (_loUnit != _hiUnit)
            {
                _heap[_loUnit] = 0;
            }
            for (i = 0, s0.SetPrev(s0), s0.SetNext(s0); i < N_INDEXES; i++)
            {
                while (_freeList[i].GetNext() != 0)
                {
                    p.Address = RemoveNode(i); // =(RAR_MEM_BLK*)RemoveNode(i);
                    p.InsertAt(s0); // p->insertAt(&s0);
                    p.Stamp = 0xFFFF; // p->Stamp=0xFFFF;
                    p.SetNu(_indx2Units[i]); // p->NU=Indx2Units[i];
                }
            }
            for (p.Address = s0.GetNext(); p.Address != s0.Address; p.Address = p.GetNext())
            {
                // while ((p1=MBPtr(p,p->NU))->Stamp == 0xFFFF && int(p->NU)+p1->NU
                // < 0x10000)
                // Bug fixed
                p1.Address = MbPtr(p.Address, p.GetNu());
                while (p1.Stamp == 0xFFFF && p.GetNu() + p1.GetNu() < 0x10000)
                {
                    p1.Remove();
                    p.SetNu(p.GetNu() + p1.GetNu()); // ->NU += p1->NU;
                    p1.Address = MbPtr(p.Address, p.GetNu());
                }
            }

            // while ((p=s0.next) != &s0)
            // Bug fixed
            p.Address = s0.GetNext();
            while (p.Address != s0.Address)
            {
                for (p.Remove(), sz = p.GetNu(); sz > 128; sz -= 128, p.Address = MbPtr(p.Address, 128))
                {
                    InsertNode(p.Address, N_INDEXES - 1);
                }
                if (_indx2Units[i = _units2Indx[sz - 1]] != sz)
                {
                    k = sz - _indx2Units[--i];
                    InsertNode(MbPtr(p.Address, sz - k), k - 1);
                }
                InsertNode(p.Address, i);
                p.Address = s0.GetNext();
            }
        }

        private int AllocUnitsRare(int indx)
        {
            if (_glueCount == 0)
            {
                _glueCount = 255;
                GlueFreeBlocks();
                if (_freeList[indx].GetNext() != 0)
                {
                    return RemoveNode(indx);
                }
            }
            int i = indx;
            do
            {
                if (++i == N_INDEXES)
                {
                    _glueCount--;
                    i = U2B(_indx2Units[indx]);
                    int j = FIXED_UNIT_SIZE * _indx2Units[indx];
                    if (_fakeUnitsStart - _pText > j)
                    {
                        _fakeUnitsStart -= j;
                        _unitsStart -= i;
                        return _unitsStart;
                    }
                    return (0);
                }
            }
            while (_freeList[i].GetNext() == 0);
            int retVal = RemoveNode(i);
            SplitBlock(retVal, i, indx);
            return retVal;
        }

        public virtual int AllocUnits(int nu)
        {
            int indx = _units2Indx[nu - 1];
            if (_freeList[indx].GetNext() != 0)
            {
                return RemoveNode(indx);
            }
            int retVal = _loUnit;
            _loUnit += U2B(_indx2Units[indx]);
            if (_loUnit <= _hiUnit)
            {
                return retVal;
            }
            _loUnit -= U2B(_indx2Units[indx]);
            return AllocUnitsRare(indx);
        }

        public virtual int AllocContext()
        {
            if (_hiUnit != _loUnit)
            {
                return (_hiUnit -= UNIT_SIZE);
            }
            if (_freeList[0].GetNext() != 0)
            {
                return RemoveNode(0);
            }
            return AllocUnitsRare(0);
        }

        public virtual int ExpandUnits(int oldPtr, int oldNu)
        {
            int i0 = _units2Indx[oldNu - 1];
            int i1 = _units2Indx[oldNu - 1 + 1];
            if (i0 == i1)
            {
                return oldPtr;
            }
            int ptr = AllocUnits(oldNu + 1);
            if (ptr != 0)
            {
                // memcpy(ptr,OldPtr,U2B(OldNU));
                Array.Copy(_heap, oldPtr, _heap, ptr, U2B(oldNu));
                InsertNode(oldPtr, i0);
            }
            return ptr;
        }

        public virtual int ShrinkUnits(int oldPtr, int oldNu, int newNu)
        {
            // System.out.println("SubAllocator.shrinkUnits(" + OldPtr + ", " +
            // OldNU + ", " + NewNU + ")");
            int i0 = _units2Indx[oldNu - 1];
            int i1 = _units2Indx[newNu - 1];
            if (i0 == i1)
            {
                return oldPtr;
            }
            if (_freeList[i1].GetNext() != 0)
            {
                int ptr = RemoveNode(i1);

                // memcpy(ptr,OldPtr,U2B(NewNU));
                // for (int i = 0; i < U2B(NewNU); i++) {
                // heap[ptr + i] = heap[OldPtr + i];
                // }
                Array.Copy(_heap, oldPtr, _heap, ptr, U2B(newNu));
                InsertNode(oldPtr, i0);
                return ptr;
            }
            SplitBlock(oldPtr, i0, i1);
            return oldPtr;
        }

        public virtual void FreeUnits(int ptr, int oldNu)
        {
            InsertNode(ptr, _units2Indx[oldNu - 1]);
        }

        public virtual void DecPText(int dPText)
        {
            PText = PText - dPText;
        }

        public virtual void InitSubAllocator()
        {
            int i, k;
            Utility.Fill(_heap, _freeListPos, _freeListPos + SizeOfFreeList(), (byte)0);

            _pText = _heapStart;

            int size2 = FIXED_UNIT_SIZE * (_subAllocatorSize / 8 / FIXED_UNIT_SIZE * 7);
            int realSize2 = size2 / FIXED_UNIT_SIZE * UNIT_SIZE;
            int size1 = _subAllocatorSize - size2;
            int realSize1 = size1 / FIXED_UNIT_SIZE * UNIT_SIZE + size1 % FIXED_UNIT_SIZE;
            _hiUnit = _heapStart + _subAllocatorSize;
            _loUnit = _unitsStart = _heapStart + realSize1;
            _fakeUnitsStart = _heapStart + size1;
            _hiUnit = _loUnit + realSize2;

            for (i = 0, k = 1; i < N1; i++, k += 1)
            {
                _indx2Units[i] = k & 0xff;
            }
            for (k++; i < N1 + N2; i++, k += 2)
            {
                _indx2Units[i] = k & 0xff;
            }
            for (k++; i < N1 + N2 + N3; i++, k += 3)
            {
                _indx2Units[i] = k & 0xff;
            }
            for (k++; i < (N1 + N2 + N3 + N4); i++, k += 4)
            {
                _indx2Units[i] = k & 0xff;
            }

            for (_glueCount = 0, k = 0, i = 0; k < 128; k++)
            {
                i += ((_indx2Units[i] < (k + 1)) ? 1 : 0);
                _units2Indx[k] = i & 0xff;
            }
        }

        private int SizeOfFreeList()
        {
            return _freeList.Length * RarNode.SIZE;
        }

        // Debug
        // public void dumpHeap() {
        // File file = new File("P:\\test\\heapdumpj");
        // OutputStream out = null;
        // try {
        // out = new FileOutputStream(file);
        // out.write(heap, heapStart, heapEnd - heapStart);
        // out.flush();
        // System.out.println("Heap dumped to " + file.getAbsolutePath());
        // }
        // catch (IOException e) {
        // e.printStackTrace();
        // }
        // finally {
        // FileUtil.close(out);
        // }
        // }

        // Debug
        public override String ToString()
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("SubAllocator[");
            buffer.Append("\n  subAllocatorSize=");
            buffer.Append(_subAllocatorSize);
            buffer.Append("\n  glueCount=");
            buffer.Append(_glueCount);
            buffer.Append("\n  heapStart=");
            buffer.Append(_heapStart);
            buffer.Append("\n  loUnit=");
            buffer.Append(_loUnit);
            buffer.Append("\n  hiUnit=");
            buffer.Append(_hiUnit);
            buffer.Append("\n  pText=");
            buffer.Append(_pText);
            buffer.Append("\n  unitsStart=");
            buffer.Append(_unitsStart);
            buffer.Append("\n]");
            return buffer.ToString();
        }

        static SubAllocator()
        {
            UNIT_SIZE = Math.Max(PpmContext.SIZE, RarMemBlock.SIZE);
        }
    }
}