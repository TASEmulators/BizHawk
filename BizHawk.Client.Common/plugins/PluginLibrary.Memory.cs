using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Client.Common
{
	public sealed class MemoryPluginLibrary : PluginMemoryBase
	{
		private MemoryDomain _currentMemoryDomain;
		private bool _isBigEndian;
		public MemoryPluginLibrary(bool Big = false)
			: base()
		{
			_isBigEndian = Big;
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

		public string HashRegion(int addr, int count, string domain = null)
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

			using (var hasher = System.Security.Cryptography.SHA256.Create())
			{
				return hasher.ComputeHash(data).BytesToHexString();
			}
		}

		#endregion

		#region Endian Handling

		private int ReadSigned(int addr, int size, string domain = null)
		{
			if (_isBigEndian) return ReadSignedBig(addr, size, domain);
			else return ReadSignedLittle(addr, size, domain);
		}

		private uint ReadUnsigned(int addr, int size, string domain = null)
		{
			if (_isBigEndian) return ReadUnsignedBig(addr, size, domain);
			else return ReadUnsignedLittle(addr, size, domain);
		}

		private void WriteSigned(int addr, int value, int size, string domain = null)
		{
			if (_isBigEndian) WriteSignedBig(addr, value, size, domain);
			else WriteSignedLittle(addr, value, size, domain);
		}

		private void WriteUnsigned(int addr, uint value, int size, string domain = null)
		{
			if (_isBigEndian) WriteUnsignedBig(addr, value, size, domain);
			else WriteUnsignedLittle(addr, value, size, domain);
		}

		#endregion

		#region Common Special and Legacy Methods

		public uint ReadByte(int addr, string domain = null)
		{
			return ReadUnsignedByte(addr, domain);
		}

		public void WriteByte(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		public new List<byte> ReadByteRange(int addr, int length, string domain = null)
		{
			return base.ReadByteRange(addr, length, domain);
		}

		public new void WriteByteRange(int addr, List<byte> memoryblock, string domain = null)
		{
			base.WriteByteRange(addr, memoryblock, domain);
		}

		public float ReadFloat(int addr, string domain = null)
		{
			return base.ReadFloat(addr, _isBigEndian, domain);
		}

		public void WriteFloat(int addr, double value, string domain = null)
		{
			base.WriteFloat(addr, value, _isBigEndian, domain);
		}

		#endregion

		#region 1 Byte

		public int ReadS8(int addr, string domain = null)
		{
			return (sbyte)ReadUnsignedByte(addr, domain);
		}

		public void WriteS8(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		public uint ReadU8(int addr, string domain = null)
		{
			return ReadUnsignedByte(addr, domain);
		}

		public void WriteU8(int addr, uint value, string domain = null)
		{
			WriteUnsignedByte(addr, value, domain);
		}

		#endregion

		#region 2 Byte
		public int ReadS16(int addr, string domain = null)
		{
			return ReadSigned(addr, 2, domain);
		}

		public void WriteS16(int addr, int value, string domain = null)
		{
			WriteSigned(addr, value, 2, domain);
		}

		public uint ReadU16(int addr, string domain = null)
		{
			return ReadUnsigned(addr, 2, domain);
		}

		public void WriteU16(int addr, uint value, string domain = null)
		{
			WriteUnsigned(addr, value, 2, domain);
		}
		#endregion

		#region 3 Byte

		public int ReadS24(int addr, string domain = null)
		{
			return ReadSigned(addr, 3, domain);
		}
		public void WriteS24(int addr, int value, string domain = null)
		{
			WriteSigned(addr, value, 3, domain);
		}

		public uint ReadU24(int addr, string domain = null)
		{
			return ReadUnsigned(addr, 3, domain);
		}

		public void WriteU24(int addr, uint value, string domain = null)
		{
			WriteUnsigned(addr, value, 3, domain);
		}

		#endregion

		#region 4 Byte

		public int ReadS32(int addr, string domain = null)
		{
			return ReadSigned(addr, 4, domain);
		}

		public void WriteS32(int addr, int value, string domain = null)
		{
			WriteSigned(addr, value, 4, domain);
		}

		public uint ReadU32(int addr, string domain = null)
		{
			return ReadUnsigned(addr, 4, domain);
		}

		public void WriteU32(int addr, uint value, string domain = null)
		{
			WriteUnsigned(addr, value, 4, domain);
		}

		#endregion
	}
}