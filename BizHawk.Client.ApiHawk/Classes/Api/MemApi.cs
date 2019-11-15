using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Client.ApiHawk
{
	public sealed class MemApi : MemApiBase, IMem
	{
		private MemoryDomain _currentMemoryDomain;
		private bool _isBigEndian;
		public MemApi()
			: base()
		{
		}

		protected override MemoryDomain Domain
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
				Console.WriteLine(error);
				throw new NotImplementedException(error);
			}
		}

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

				Console.WriteLine($"Unable to find domain: {domain}");
				return false;
			}
			catch // Just in case
			{
				Console.WriteLine($"Unable to find domain: {domain}");
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
				Console.WriteLine(error);
				throw new ArgumentOutOfRangeException(error);
			}
			if (addr + count > d.Size)
			{
				string error = $"Address {addr} + count {count} is outside the bounds of domain {d.Name}";
				Console.WriteLine(error);
				throw new ArgumentOutOfRangeException(error);
			}

			byte[] data = new byte[count];
			for (int i = 0; i < count; i++)
			{
				data[i] = d.PeekByte(addr + i);
			}

			using var hasher = System.Security.Cryptography.SHA256.Create();
			return hasher.ComputeHash(data).BytesToHexString();
		}

		#endregion

		#region Endian Handling

		private int ReadSigned(long addr, int size, string domain = null)
		{
			return _isBigEndian
				? ReadSignedBig(addr, size, domain)
				: ReadSignedLittle(addr, size, domain);
		}

		private uint ReadUnsigned(long addr, int size, string domain = null)
		{
			return _isBigEndian
				? ReadUnsignedBig(addr, size, domain)
				: ReadUnsignedLittle(addr, size, domain);
		}

		private void WriteSigned(long addr, int value, int size, string domain = null)
		{
			if (_isBigEndian) WriteSignedBig(addr, value, size, domain);
			else WriteSignedLittle(addr, value, size, domain);
		}

		private void WriteUnsigned(long addr, uint value, int size, string domain = null)
		{
			if (_isBigEndian) WriteUnsignedBig(addr, value, size, domain);
			else WriteUnsignedLittle(addr, value, size, domain);
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

		public new List<byte> ReadByteRange(long addr, int length, string domain = null)
		{
			return base.ReadByteRange(addr, length, domain);
		}

		public new void WriteByteRange(long addr, List<byte> memoryblock, string domain = null)
		{
			base.WriteByteRange(addr, memoryblock, domain);
		}

		public float ReadFloat(long addr, string domain = null)
		{
			return base.ReadFloat(addr, _isBigEndian, domain);
		}

		public void WriteFloat(long addr, double value, string domain = null)
		{
			base.WriteFloat(addr, value, _isBigEndian, domain);
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