using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public sealed class MemoryAccessApi : IMemoryAccess
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomainCore { get; set; }

		private MemoryDomain _currentMemoryDomain;

		private readonly Action<string> _logCallback;

		public MemoryAccessApi(Action<string> logCallback)
		{
			_logCallback = logCallback;
		}

		public MemoryAccessApi() : this(Console.WriteLine) {}

		public bool BigEndian { get; set; }

		private IMemoryDomains Domains
		{
			get
			{
				if (MemoryDomainCore != null) return MemoryDomainCore;
				var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
				_logCallback(error);
				throw new NotImplementedException(error);
			}
		}

		public IList<string> MemoryDomainList => Domains.Select(domain => domain.Name).ToList();

		private MemoryDomain SelectedDomain
		{
			get => _currentMemoryDomain ??= Domains.HasSystemBus ? Domains.SystemBus : Domains.MainMemory;
			set => _currentMemoryDomain = value;
		}

		public string SelectedDomainName => SelectedDomain.Name;

		public uint SelectedDomainSize => (uint) SelectedDomain.Size;

		public uint GetMemoryDomainSize(string name) => (uint) NamedDomainOrCurrent(name).Size;

		/// <exception cref="ArgumentOutOfRangeException">range defined by <paramref name="addr"/> and <paramref name="count"/> extends beyond the bound of <paramref name="domain"/> (or <see cref="SelectedDomain"/> if null)</exception>
		public string HashRegion(long addr, int count, string domain)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!0L.RangeToExclusive(d.Size).Contains(addr))
			{
				var error = $"Address {addr} is outside the bounds of domain {d.Name}";
				_logCallback(error);
				throw new ArgumentOutOfRangeException(error);
			}
			if (addr + count > d.Size)
			{
				var error = $"Address {addr} + count {count} is outside the bounds of domain {d.Name}";
				_logCallback(error);
				throw new ArgumentOutOfRangeException(error);
			}
			var data = new byte[count];
			for (var i = 0; i < count; i++) data[i] = d.PeekByte(addr + i);
			using var hasher = SHA256.Create();
			return hasher.ComputeHash(data).BytesToHexString();
		}

		private MemoryDomain NamedDomainOrCurrent(string name)
		{
			if (!string.IsNullOrEmpty(name))
			{
				try
				{
					var found = Domains[name];
					if (found != null) return found;
				}
				catch
				{
					// ignored
				}
				_logCallback($"Unable to find domain: {name}, falling back to current");
			}
			return SelectedDomain;
		}

		public uint ReadByte(long addr, string domain) => ReadUnsignedByte(addr, domain);

		public IList<byte> ReadByteRange(long addr, int length, string domain)
		{
			var d = NamedDomainOrCurrent(domain);
			if (addr < 0) _logCallback($"Warning: Attempted reads on addresses {addr}..-1 outside range of domain {d.Name} in {nameof(ReadByteRange)}()");
			var lastReqAddr = addr + length - 1;
			var indexAfterLast = Math.Min(lastReqAddr, d.Size - 1) - addr + 1;
			var bytes = new byte[length];
			for (var i = addr < 0 ? -addr : 0; i != indexAfterLast; i++) bytes[i] = d.PeekByte(addr + i);
			if (lastReqAddr >= d.Size) _logCallback($"Warning: Attempted reads on addresses {d.Size}..{lastReqAddr} outside range of domain {d.Name} in {nameof(ReadByteRange)}()");
			return bytes.ToList();
		}

		public float ReadFloat(long addr, string domain)
		{
			var d = NamedDomainOrCurrent(domain);
			if (addr >= d.Size)
			{
				_logCallback($"Warning: Attempted read {addr} outside memory size of {d.Size}");
				return default;
			}
			return BitConverter.ToSingle(BitConverter.GetBytes(d.PeekUint(addr, BigEndian)), 0);
		}

		public int ReadS16(long addr, string domain) => (short) ReadSigned(addr, 2, domain);

		public int ReadS24(long addr, string domain) => ReadSigned(addr, 3, domain);

		public int ReadS32(long addr, string domain) => ReadSigned(addr, 4, domain);

		public int ReadS8(long addr, string domain) => (sbyte) ReadUnsignedByte(addr, domain);

		private int ReadSigned(long addr, int size, string domain) => U2S(ReadUnsigned(addr, size, domain), size);

		public uint ReadU16(long addr, string domain) => (ushort) ReadUnsigned(addr, 2, domain);

		public uint ReadU24(long addr, string domain) => ReadUnsigned(addr, 3, domain);

		public uint ReadU32(long addr, string domain) => ReadUnsigned(addr, 4, domain);

		public uint ReadU8(long addr, string domain) => (byte) ReadUnsignedByte(addr, domain);

		private uint ReadUnsigned(long addr, int size, string domain) => BigEndian ? ReadUnsignedBig(addr, size, domain) : ReadUnsignedLittle(addr, size, domain);

		private uint ReadUnsignedBig(long addr, int size, string domain)
		{
			uint v = 0;
			for (var i = 0; i < size; i++) v |= ReadUnsignedByte(addr + i, domain) << (8 * (size - 1 - i));
			return v;
		}

		private uint ReadUnsignedByte(long addr, string domain)
		{
			var d = NamedDomainOrCurrent(domain);
			if (addr >= d.Size)
			{
				_logCallback($"Warning: attempted read of {addr} outside the memory size of {d.Size}");
				return default;
			}
			return d.PeekByte(addr);
		}

		private uint ReadUnsignedLittle(long addr, int size, string domain)
		{
			uint v = 0;
			for (var i = 0; i < size; i++) v |= ReadUnsignedByte(addr + i, domain) << (8 * i);
			return v;
		}

		private static int U2S(uint u, int size)
		{
			var sh = 8 * (4 - size);
			return ((int) u << sh) >> sh;
		}

		public bool UseMemoryDomain(string domain)
		{
			try
			{
				var found = Domains[domain];
				if (found != null)
				{
					SelectedDomain = found;
					return true;
				}
			}
			catch
			{
				// ignored
			}
			_logCallback($"Unable to find domain: {domain}");
			return false;
		}

		public void WriteByte(long addr, uint value, string domain) => WriteUnsignedByte(addr, value, domain);

		public void WriteByteRange(long addr, IList<byte> memoryblock, string domain)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!d.CanPoke())
			{
				_logCallback($"Error: the domain {d.Name} is not writable");
				return;
			}
			if (addr < 0) _logCallback($"Warning: Attempted reads on addresses {addr}..-1 outside range of domain {d.Name} in {nameof(WriteByteRange)}()");
			var lastReqAddr = addr + memoryblock.Count - 1;
			var indexAfterLast = Math.Min(lastReqAddr, d.Size - 1) - addr + 1;
			for (var i = addr < 0 ? (int) -addr : 0; i != indexAfterLast; i++) d.PokeByte(addr + i, memoryblock[i]);
			if (lastReqAddr >= d.Size) _logCallback($"Warning: Attempted reads on addresses {d.Size}..{lastReqAddr} outside range of domain {d.Name} in {nameof(WriteByteRange)}()");
		}

		public void WriteFloat(long addr, double value, string domain)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!d.CanPoke())
			{
				_logCallback($"Error: the domain {SelectedDomain.Name} is not writable");
				return;
			}
			if (addr >= d.Size)
			{
				_logCallback($"Warning: Attempted write {addr} outside memory size of {d.Size}");
				return;
			}
			d.PokeUint(addr, BitConverter.ToUInt32(BitConverter.GetBytes((float) value), 0), BigEndian);
		}

		public void WriteS16(long addr, int value, string domain) => WriteSigned(addr, value, 2, domain);

		public void WriteS24(long addr, int value, string domain) => WriteSigned(addr, value, 3, domain);

		public void WriteS32(long addr, int value, string domain) => WriteSigned(addr, value, 4, domain);

		public void WriteS8(long addr, int value, string domain) => WriteSigned(addr, value, 1, domain);

		private void WriteSigned(long addr, int value, int size, string domain) => WriteUnsigned(addr, (uint) value, size, domain);

		public void WriteU16(long addr, uint value, string domain) => WriteUnsigned(addr, value, 2, domain);

		public void WriteU24(long addr, uint value, string domain) => WriteUnsigned(addr, value, 3, domain);

		public void WriteU32(long addr, uint value, string domain) => WriteUnsigned(addr, value, 4, domain);

		public void WriteU8(long addr, uint value, string domain) => WriteUnsignedByte(addr, value, domain);

		private void WriteUnsigned(long addr, uint value, int size, string domain)
		{
			if (BigEndian) WriteUnsignedBig(addr, value, size, domain);
			else WriteUnsignedLittle(addr, value, size, domain);
		}

		private void WriteUnsignedBig(long addr, uint v, int size, string domain)
		{
			for (var i = 0; i < size; i++) WriteUnsignedByte(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF, domain);
		}

		private void WriteUnsignedByte(long addr, uint v, string domain)
		{
			var d = NamedDomainOrCurrent(domain);
			if (!d.CanPoke())
			{
				_logCallback($"Error: the domain {d.Name} is not writable");
				return;
			}
			if (addr >= d.Size)
			{
				_logCallback($"Warning: attempted write to {addr} outside the memory size of {d.Size}");
				return;
			}
			d.PokeByte(addr, (byte) v);
		}

		private void WriteUnsignedLittle(long addr, uint v, int size, string domain)
		{
			for (var i = 0; i < size; i++) WriteUnsignedByte(addr + i, (v >> (8 * i)) & 0xFF, domain);
		}
	}
}
