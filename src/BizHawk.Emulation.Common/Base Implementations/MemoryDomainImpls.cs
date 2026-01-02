#nullable disable

using System.Runtime.InteropServices;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomainDelegate : MemoryDomain
	{
		private Action<long, byte> _poke;

		// TODO: use an array of Ranges
		private Action<Range<long>, byte[]> _bulkPeekByte { get; set; }
		private Action<Range<long>, bool, ushort[]> _bulkPeekUshort { get; set; }
		private Action<Range<long>, bool, uint[]> _bulkPeekUint { get; set; }

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
			if (addr < 0 || Size <= addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			return Peek(addr);
		}

		public override void PokeByte(long addr, byte val)
		{
			if (_poke is null)
			{
				FailPokingNotAllowed();
				return;
			}
			if (addr < 0 || Size <= addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			_poke.Invoke(addr, val);
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (_bulkPeekByte != null)
			{
				_bulkPeekByte.Invoke(addresses, values);
			}
			else
			{
				base.BulkPeekByte(addresses, values);
			}
		}

		public override void BulkPeekUshort(Range<long> addresses, bool bigEndian, ushort[] values)
		{
			if (_bulkPeekUshort != null)
			{
				var start = addresses.Start;
				if (start < 0 || Size <= start) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), start, message: string.Format(ERR_FMT_STR_START_OOR, Size));
				var endExcl = start + checked((long) addresses.Count());
				if (Size < endExcl) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), endExcl, message: string.Format(ERR_FMT_STR_END_OOR, Size));
				_bulkPeekUshort.Invoke(addresses, bigEndian, values);
			}
			else
			{
				base.BulkPeekUshort(addresses, bigEndian, values);
			}
		}

		public override void BulkPeekUint(Range<long> addresses, bool bigEndian, uint[] values)
		{
			if (_bulkPeekUint != null)
			{
				var start = addresses.Start;
				if (start < 0 || Size <= start) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), start, message: string.Format(ERR_FMT_STR_START_OOR, Size));
				var endExcl = start + checked((long) addresses.Count());
				if (Size < endExcl) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), endExcl, message: string.Format(ERR_FMT_STR_END_OOR, Size));
				_bulkPeekUint.Invoke(addresses, bigEndian, values);
			}
			else
			{
				base.BulkPeekUint(addresses, bigEndian, values);
			}
		}

		public MemoryDomainDelegate(
			string name,
			long size,
			Endian endian,
			Func<long, byte> peek,
			Action<long, byte> poke,
			int wordSize,
			Action<Range<long>, byte[]> bulkPeekByte = null,
			Action<Range<long>, bool, ushort[]> bulkPeekUshort = null,
			Action<Range<long>, bool, uint[]> bulkPeekUint = null)
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
			if (!Writable)
			{
				FailPokingNotAllowed();
				return;
			}
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
			{
				FailPokingNotAllowed();
				return;
			}
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

	public class MemoryDomainIntPtr : MemoryDomain
	{
		public IntPtr Data { get; set; }

		public unsafe override byte PeekByte(long addr)
		{
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			return ((byte*) Data)[addr];
		}

		public override unsafe void PokeByte(long addr, byte val)
		{
			if (!Writable)
			{
				FailPokingNotAllowed();
				return;
			}
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			((byte*) Data)[addr] = val;
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			//TODO why are we casting `long`s to `ulong` here?
			var start = (ulong)addresses.Start;
			if ((ulong) Size <= start) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), start, message: string.Format(ERR_FMT_STR_START_OOR, Size));
			var count = addresses.Count();
			var endExcl = start + count;
			if ((ulong) Size < endExcl) throw new ArgumentOutOfRangeException(paramName: nameof(addresses), endExcl, message: string.Format(ERR_FMT_STR_END_OOR, Size));
			Marshal.Copy(source: (IntPtr) ((ulong) Data + start), destination: values, startIndex: 0, length: (int) count);
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
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			using (_monitor.EnterExit()) return ((byte*) Data)[addr];
		}

		public override void PokeByte(long addr, byte val)
		{
			if (!Writable)
			{
				FailPokingNotAllowed();
				return;
			}
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			using (_monitor.EnterExit()) ((byte*) Data)[addr] = val;
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
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			return ((byte*) Data)[addr ^ 1L];
		}

		public override void PokeByte(long addr, byte val)
		{
			if (!Writable)
			{
				FailPokingNotAllowed();
				return;
			}
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			((byte*) Data)[addr ^ 1L] = val;
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
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			using (_monitor.EnterExit()) return ((byte*) Data)[addr ^ 1L];
		}

		public override void PokeByte(long addr, byte val)
		{
			if (!Writable)
			{
				FailPokingNotAllowed();
				return;
			}
			//TODO why are we casting `long`s to `ulong` here?
			if ((ulong) Size <= (ulong) addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_ADDR_OOR, Size));
			using (_monitor.EnterExit()) ((byte*) Data)[addr ^ 1L] = val;
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
