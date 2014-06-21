using System;
using System.IO.MemoryMappedFiles;

namespace BizHawk.Common
{
	public unsafe class SharedMemoryBlock : IDisposable
	{
		public string Name;
		public string BlockName;
		public int Size;
		public MemoryMappedFile mmf;
		public MemoryMappedViewAccessor mmva;
		public byte* Ptr;

		public void Allocate()
		{
			//we can't allocate 0 bytes here.. so just allocate 1 byte here if 0 was requested. it should be OK, and we dont have to handle cases where blocks havent been allocated
			int sizeToAlloc = Size;
			if (sizeToAlloc == 0) sizeToAlloc = 1;
			mmf = MemoryMappedFile.CreateNew(BlockName, sizeToAlloc);
			mmva = mmf.CreateViewAccessor();
			mmva.SafeMemoryMappedViewHandle.AcquirePointer(ref Ptr);
		}

		public void Dispose()
		{
			if (mmf == null) return;
			mmva.Dispose();
			mmf.Dispose();
			mmf = null;
		}
	}
}