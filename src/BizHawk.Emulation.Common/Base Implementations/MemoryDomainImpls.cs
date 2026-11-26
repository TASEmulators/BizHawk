#nullable disable

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomainDelegate : MemoryDomain
	{
		private Action<long, byte> _poke;

		public delegate void BulkBytePeekDel(long startAddress, Span<byte> values);

		public delegate void BulkUshortPeekDel(long startAddress, Span<ushort> values, bool bigEndian);

		public delegate void BulkUintPeekDel(long startAddress, Span<uint> values, bool bigEndian);

		// TODO: use an array of Ranges
		private BulkBytePeekDel _bulkPeekByte { get; set; }
		private BulkUshortPeekDel _bulkPeekUshort { get; set; }
		private BulkUintPeekDel _bulkPeekUint { get; set; }

		public Func<long, byte> Peek { get; set; }

		public Action<long, byte> Poke
		{
			get => _poke;
			set
			{
				_poke = value;
				Writable = value != null;
			}
		}

		public override byte PeekByte(long addr)
		{
			return Peek(addr);
		}

		public override void PokeByte(long addr, byte val)
		{
			_poke?.Invoke(addr, val);
		}

		public override void BulkPeekByte(long startAddress, Span<byte> values)
		{
			if (_bulkPeekByte != null)
			{
				_bulkPeekByte.Invoke(startAddress, values);
			}
			else
			{
				base.BulkPeekByte(startAddress, values);
			}
		}

		public override void BulkPeekUshort(long startAddress, Span<ushort> values, bool bigEndian)
		{
			if (_bulkPeekUshort != null)
			{
				_bulkPeekUshort.Invoke(startAddress, values, bigEndian);
			}
			else
			{
				base.BulkPeekUshort(startAddress, values, bigEndian);
			}
		}

		public override void BulkPeekUint(long startAddress, Span<uint> values, bool bigEndian)
		{
			if (_bulkPeekUint != null)
			{
				_bulkPeekUint.Invoke(startAddress, values, bigEndian);
			}
			else
			{
				base.BulkPeekUint(startAddress, values, bigEndian);
			}
		}

		public MemoryDomainDelegate(
			string name,
			long size,
			Endian endian,
			Func<long, byte> peek,
			Action<long, byte> poke,
			int wordSize,
			BulkBytePeekDel bulkPeekByte = null,
			BulkUshortPeekDel bulkPeekUshort = null,
			BulkUintPeekDel bulkPeekUint = null)
		{
			Name = name;
			EndianType = endian;
			Size = size;
			Peek = peek;
			_poke = poke;
			Writable = poke != null;
			WordSize = wordSize;
			_bulkPeekByte = bulkPeekByte;
			_bulkPeekUshort = bulkPeekUshort;
			_bulkPeekUint = bulkPeekUint;
		}
	}

	public class MemoryDomainByteArray : MemoryDomain
	{
		private byte[] _data;

		public byte[] Data
		{
			get => _data;
			set
			{
				_data = value;
				Size = _data.LongLength;
			}
		}

		public override byte PeekByte(long addr)
		{
			return Data[addr];
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				Data[addr] = val;
			}
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


	public class MemoryDomainUshortArray : MemoryDomain
	{
		private ushort[] _data;

		public ushort[] Data
		{
			get => _data;
			set
			{
				_data = value;
				Size = _data.LongLength*2;
			}
		}

		public override byte PeekByte(long addr)
		{
			long bit0 = addr & 1;
			addr >>= 1;
			if(bit0==0)
				return (byte)(_data[addr] & 0xFF);
			else
				return (byte)((_data[addr]>>8)&0xFF);
		}

		public override void PokeByte(long addr, byte val)
		{
			if (!Writable)
				return;
			long bit0 = addr & 1;
			addr >>= 1;
			if (bit0 == 0)
				Data[addr] = (ushort)((_data[addr] & 0xFF00) | val);
			else
				Data[addr] = (ushort)((_data[addr] & 0x00FF) | (val<<8));
		}

		public MemoryDomainUshortArray(string name, Endian endian, ushort[] data, bool writable)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Writable = writable;
			WordSize = 2;
		}
	}

	public unsafe class MemoryDomainIntPtr : MemoryDomain
	{
		public IntPtr Data { get; set; }

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				return ((byte*)Data)[addr];
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					((byte*)Data)[addr] = val;
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public override void BulkPeekByte(long startAddress, Span<byte> values)
		{
			if (startAddress < Size && (startAddress + values.Length) <= Size)
			{
				Span<byte> data = new(((IntPtr)((long)Data + startAddress)).ToPointer(), values.Length);
				data.CopyTo(values);
			}
			else if (startAddress < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(startAddress));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(values));
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

	public unsafe class MemoryDomainIntPtrMonitor : MemoryDomain
	{
		public IntPtr Data { get; set; }
		private readonly IMonitor _monitor;

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					return ((byte*)Data)[addr];
				}
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					using (_monitor.EnterExit())
					{
						((byte*)Data)[addr] = val;
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public void SetSize(long size)
		{
			Size = size;
		}

		public MemoryDomainIntPtrMonitor(string name, Endian endian, IntPtr data, long size, bool writable, int wordSize,
			IMonitor monitor)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = wordSize;
			_monitor = monitor;
		}

		public override void Enter()
			=> _monitor.Enter();

		public override void Exit()
			=> _monitor.Exit();
	}

	public unsafe class MemoryDomainIntPtrSwap16 : MemoryDomain
	{
		public IntPtr Data { get; set; }

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				return ((byte*)Data)[addr ^ 1];
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					((byte*)Data)[addr ^ 1] = val;
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
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

	public unsafe class MemoryDomainIntPtrSwap16Monitor : MemoryDomain
	{
		public IntPtr Data { get; set; }
		private readonly IMonitor _monitor;

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					return ((byte*)Data)[addr ^ 1];
				}
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					using (_monitor.EnterExit())
					{
						((byte*)Data)[addr ^ 1] = val;
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public MemoryDomainIntPtrSwap16Monitor(string name, Endian endian, IntPtr data, long size, bool writable,
			IMonitor monitor)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = 2;
			_monitor = monitor;
		}

		public override void Enter()
			=> _monitor.Enter();

		public override void Exit()
			=> _monitor.Exit();
	}

	public class MemoryDomainDelegateSysBusNES : MemoryDomainDelegate
	{
		private readonly Action<int, byte, int, int> sendcheattocore;

		public override void SendCheatToCore(int addr, byte value, int compare, int comparetype)
		{
			if (sendcheattocore != null)
			{
				sendcheattocore.Invoke(addr, value, compare, comparetype);
			}
			else
			{
				base.SendCheatToCore(addr, value, compare, comparetype);
			}
		}

		public MemoryDomainDelegateSysBusNES(
			string name,
			long size,
			Endian endian,
			Func<long, byte> peek,
			Action<long, byte> poke,
			int wordSize,
			Action<int, byte, int, int> nescheatpoke = null)
				: base(name, size, endian, peek, poke, wordSize)
					=> sendcheattocore = nescheatpoke;
	}
}
