using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	public sealed class MemApi : IMem
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[OptionalService]
		private IMemoryDomains MemoryDomainCore { get; set; }

		private MemoryDomain _currentMemoryDomain;

		private MemoryDomain Domain
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					if (_currentMemoryDomain == null)
					{
						_currentMemoryDomain = MemoryDomainCore.HasSystemBus
							? MemoryDomainCore.SystemBus
							: MemoryDomainCore.MainMemory;
					}

					return _currentMemoryDomain;
				}

				var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
				LogCallback(error);
				throw new NotImplementedException(error);
			}
		}

		private bool _isBigEndian;

		private IMemoryDomains DomainList
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					return MemoryDomainCore;
				}

				var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
				LogCallback(error);
				throw new NotImplementedException(error);
			}
		}

		public MemApi(Action<string> logCallback)
		{
			LogCallback = logCallback;
		}

		public MemApi() : this(Console.WriteLine) {}

		private readonly Action<string> LogCallback;

		private string VerifyMemoryDomain(string domain)
		{
			try
			{
				if (DomainList[domain] == null)
				{
					LogCallback($"Unable to find domain: {domain}, falling back to current");
					return Domain.Name;
				}

				return domain;
			}
			catch // Just in case
			{
				LogCallback($"Unable to find domain: {domain}, falling back to current");
			}

			return Domain.Name;
		}

		private uint ReadUnsignedByte(long addr, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (addr < d.Size)
			{
				return d.PeekByte(addr);
			}

			LogCallback($"Warning: attempted read of {addr} outside the memory size of {d.Size}");
			return 0;
		}

		private void WriteUnsignedByte(long addr, uint v, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (d.CanPoke())
			{
				if (addr < d.Size)
				{
					d.PokeByte(addr, (byte)v);
				}
				else
				{
					LogCallback($"Warning: attempted write to {addr} outside the memory size of {d.Size}");
				}
			}
			else
			{
				LogCallback($"Error: the domain {d.Name} is not writable");
			}
		}

		private static int U2S(uint u, int size)
		{
			var s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		#region Endian Handling

		private uint ReadUnsignedLittle(long addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i, domain) << (8 * i);
			}

			return v;
		}

		private uint ReadUnsignedBig(long addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i, domain) << (8 * (size - 1 - i));
			}

			return v;
		}

		private void WriteUnsignedLittle(long addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * i)) & 0xFF, domain);
			}
		}

		private void WriteUnsignedBig(long addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF, domain);
			}
		}

		private int ReadSigned(long addr, int size, string domain = null)
		{
			return _isBigEndian
				? U2S(ReadUnsignedBig(addr, size, domain), size)
				: U2S(ReadUnsignedLittle(addr, size, domain), size);
		}

		private uint ReadUnsigned(long addr, int size, string domain = null)
		{
			return _isBigEndian
				? ReadUnsignedBig(addr, size, domain)
				: ReadUnsignedLittle(addr, size, domain);
		}

		private void WriteSigned(long addr, int value, int size, string domain = null)
		{
			if (_isBigEndian) WriteUnsignedBig(addr, (uint)value, size, domain);
			else WriteUnsignedLittle(addr, (uint)value, size, domain);
		}

		private void WriteUnsigned(long addr, uint value, int size, string domain = null)
		{
			if (_isBigEndian) WriteUnsignedBig(addr, value, size, domain);
			else WriteUnsignedLittle(addr, value, size, domain);
		}

		#endregion

		#region Unique Library Methods

		public void SetBigEndian(bool enabled = true)
		{
			_isBigEndian = enabled;
		}

		public List<string> GetMemoryDomainList()
		{
			var list = new List<string>();

			foreach (var domain in DomainList)
			{
				list.Add(domain.Name);
			}

			return list;
		}

		public uint GetMemoryDomainSize(string name = "")
		{
			if (string.IsNullOrEmpty(name))
			{
				return (uint)Domain.Size;
			}

			return (uint)DomainList[VerifyMemoryDomain(name)].Size;
		}

		public string GetCurrentMemoryDomain()
		{
			return Domain.Name;
		}

		public uint GetCurrentMemoryDomainSize()
		{
			return (uint)Domain.Size;
		}

		public bool UseMemoryDomain(string domain)
		{
			try
			{
				if (DomainList[domain] != null)
				{
					_currentMemoryDomain = DomainList[domain];
					return true;
				}

				LogCallback($"Unable to find domain: {domain}");
				return false;
			}
			catch // Just in case
			{
				LogCallback($"Unable to find domain: {domain}");
			}

			return false;
		}

		public string HashRegion(long addr, int count, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];

			// checks
			if (addr < 0 || addr >= d.Size)
			{
				string error = $"Address {addr} is outside the bounds of domain {d.Name}";
				LogCallback(error);
				throw new ArgumentOutOfRangeException(error);
			}
			if (addr + count > d.Size)
			{
				string error = $"Address {addr} + count {count} is outside the bounds of domain {d.Name}";
				LogCallback(error);
				throw new ArgumentOutOfRangeException(error);
			}

			byte[] data = new byte[count];
			for (int i = 0; i < count; i++)
			{
				data[i] = d.PeekByte(addr + i);
			}

			using var hasher = SHA256.Create();
			return hasher.ComputeHash(data).BytesToHexString();
		}

		#endregion

		#region Common Special and Legacy Methods

		public uint ReadByte(long addr, string domain = null)
		{
			return ReadUnsignedByte(addr, domain);
		}

		public void WriteByte(long addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		public List<byte> ReadByteRange(long addr, int length, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			var lastAddr = length + addr;
			var list = new List<byte>();
			for (; addr <= lastAddr; addr++)
			{
				if (addr < d.Size)
					list.Add(d.PeekByte(addr));
				else {
					LogCallback($"Warning: Attempted read {addr} outside memory domain size of {d.Size} in {nameof(ReadByteRange)}()");
					list.Add(0);
				}
			}

			return list;
		}

		public void WriteByteRange(long addr, List<byte> memoryblock, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (d.CanPoke())
			{
				foreach (var m in memoryblock)
				{
					if (addr < d.Size)
					{
						d.PokeByte(addr++, m);
					}
					else
					{
						LogCallback($"Warning: Attempted write {addr} outside memory domain size of {d.Size} in {nameof(WriteByteRange)}()");
					}
				}
			}
			else
			{
				LogCallback($"Error: the domain {d.Name} is not writable");
			}
		}

		public float ReadFloat(long addr, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (addr < d.Size)
			{
				var val = d.PeekUint(addr, _isBigEndian);
				var bytes = BitConverter.GetBytes(val);
				return BitConverter.ToSingle(bytes, 0);
			}

			LogCallback($"Warning: Attempted read {addr} outside memory size of {d.Size}");

			return 0;
		}

		public void WriteFloat(long addr, double value, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (d.CanPoke())
			{
				if (addr < d.Size)
				{
					var dv = (float)value;
					var bytes = BitConverter.GetBytes(dv);
					var v = BitConverter.ToUInt32(bytes, 0);
					d.PokeUint(addr, v, _isBigEndian);
				}
				else
				{
					LogCallback($"Warning: Attempted write {addr} outside memory size of {d.Size}");
				}
			}
			else
			{
				LogCallback($"Error: the domain {Domain.Name} is not writable");
			}
		}

		#endregion

		#region 1 Byte

		public int ReadS8(long addr, string domain = null)
		{
			return (sbyte)ReadUnsignedByte(addr, domain);
		}

		public uint ReadU8(long addr, string domain = null)
		{
			return (byte)ReadUnsignedByte(addr, domain);
		}

		public void WriteS8(long addr, int value, string domain = null)
		{
			WriteSigned(addr, value, 1, domain);
		}

		public void WriteU8(long addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		#endregion

		#region 2 Byte

		public int ReadS16(long addr, string domain = null)
		{
			return (short)ReadSigned(addr, 2, domain);
		}

		public void WriteS16(long addr, int value, string domain = null)
		{
			WriteSigned(addr, value, 2, domain);
		}

		public uint ReadU16(long addr, string domain = null)
		{
			return (ushort)ReadUnsigned(addr, 2, domain);
		}

		public void WriteU16(long addr, uint value, string domain = null)
		{
			WriteUnsigned(addr, value, 2, domain);
		}
		#endregion

		#region 3 Byte

		public int ReadS24(long addr, string domain = null)
		{
			return ReadSigned(addr, 3, domain);
		}
		public void WriteS24(long addr, int value, string domain = null)
		{
			WriteSigned(addr, value, 3, domain);
		}

		public uint ReadU24(long addr, string domain = null)
		{
			return ReadUnsigned(addr, 3, domain);
		}

		public void WriteU24(long addr, uint value, string domain = null)
		{
			WriteUnsigned(addr, value, 3, domain);
		}

		#endregion

		#region 4 Byte

		public int ReadS32(long addr, string domain = null)
		{
			return ReadSigned(addr, 4, domain);
		}

		public void WriteS32(long addr, int value, string domain = null)
		{
			WriteSigned(addr, value, 4, domain);
		}

		public uint ReadU32(long addr, string domain = null)
		{
			return ReadUnsigned(addr, 4, domain);
		}

		public void WriteU32(long addr, uint value, string domain = null)
		{
			WriteUnsigned(addr, value, 4, domain);
		}

		#endregion
	}
}
