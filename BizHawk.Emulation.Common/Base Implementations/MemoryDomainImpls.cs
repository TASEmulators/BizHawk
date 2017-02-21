using System;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomainDelegate : MemoryDomain
	{
		private Func<long, byte> _peek;
		private Action<long, byte> _poke;

		public Func<long, byte> Peek { get { return _peek; } set { _peek = value; } }
		public Action<long, byte> Poke { get { return _poke; } set { _poke = value; Writable = value != null; } }

		public override byte PeekByte(long addr)
		{
			return _peek(addr);
		}

		public override void PokeByte(long addr, byte val)
		{
			if (_poke != null)
				_poke(addr, val);
		}

		public MemoryDomainDelegate(string name, long size, Endian endian, Func<long, byte> peek, Action<long, byte> poke, int wordSize)
		{
			Name = name;
			EndianType = endian;
			Size = size;
			_peek = peek;
			_poke = poke;
			Writable = poke != null;
			WordSize = wordSize;
		}
	}

	public class MemoryDomainByteArray : MemoryDomain
	{
		private byte[] _data;

		public byte[] Data { get { return _data; } set { _data = value; Size = _data.LongLength; } }

		public override byte PeekByte(long addr)
		{
			return Data[addr];
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
				Data[addr] = val;
		}

		public MemoryDomainByteArray(string name, Endian endian, byte[] data, bool writable, int wordSize)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Writable = writable;
			WordSize = wordSize;
		}
	}

	public unsafe class MemoryDomainIntPtr : MemoryDomain
	{
		public IntPtr Data { get; set; }

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
				return ((byte*)Data)[addr];
			else
				throw new ArgumentOutOfRangeException("addr");
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
					((byte*)Data)[addr] = val;
				else
					throw new ArgumentOutOfRangeException("addr");
			}
		}

		public void SetSize(long size)
		{
			Size = size;
		}

		public MemoryDomainIntPtr(string name, Endian endian, IntPtr data, long size, bool writable, int wordSize)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = wordSize;
		}
	}

	public unsafe class MemoryDomainIntPtrSwap16 : MemoryDomain
	{
		public IntPtr Data { get; set; }

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
				return ((byte*)Data)[addr ^ 1];
			else
				throw new ArgumentOutOfRangeException("addr");
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
					((byte*)Data)[addr ^ 1] = val;
				else
					throw new ArgumentOutOfRangeException("addr");
			}
		}

		public MemoryDomainIntPtrSwap16(string name, Endian endian, IntPtr data, long size, bool writable)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = 2;
		}
	}
}
