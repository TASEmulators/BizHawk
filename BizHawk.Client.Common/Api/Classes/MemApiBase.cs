using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Base class for the Memory and MainMemory plugin libraries
	/// </summary>
	public abstract class MemApiBase : IExternalApi
	{
		[RequiredService]
		protected IEmulator Emulator { get; set; }

		[OptionalService]
		protected IMemoryDomains MemoryDomainCore { get; set; }

		protected abstract MemoryDomain Domain { get; }

		protected MemApiBase()
		{ }

		protected IMemoryDomains DomainList
		{
			get
			{
				if (MemoryDomainCore != null)
				{
					return MemoryDomainCore;
				}

				var error = $"Error: {Emulator.Attributes().CoreName} does not implement memory domains";
				Console.WriteLine(error);
				throw new NotImplementedException(error);
			}
		}

		public string VerifyMemoryDomain(string domain)
		{
			try
			{
				if (DomainList[domain] == null)
				{
					Console.WriteLine($"Unable to find domain: {domain}, falling back to current");
					return Domain.Name;
				}

				return domain;
			}
			catch // Just in case
			{
				Console.WriteLine($"Unable to find domain: {domain}, falling back to current");
			}

			return Domain.Name;
		}

		protected uint ReadUnsignedByte(long addr, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (addr < d.Size)
			{
				return d.PeekByte(addr);
			}

			Console.WriteLine($"Warning: attempted read of {addr} outside the memory size of {d.Size}");
			return 0;
		}

		protected void WriteUnsignedByte(long addr, uint v, string domain = null)
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
					Console.WriteLine($"Warning: attempted write to {addr} outside the memory size of {d.Size}");
				}
			}
			else
			{
				Console.WriteLine($"Error: the domain {d.Name} is not writable");
			}
		}

		protected static int U2S(uint u, int size)
		{
			var s = (int)u;
			s <<= 8 * (4 - size);
			s >>= 8 * (4 - size);
			return s;
		}

		protected int ReadSignedLittle(long addr, int size, string domain = null)
		{
			return U2S(ReadUnsignedLittle(addr, size, domain), size);
		}

		protected uint ReadUnsignedLittle(long addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i, domain) << (8 * i);
			}

			return v;
		}

		protected int ReadSignedBig(long addr, int size, string domain = null)
		{
			return U2S(ReadUnsignedBig(addr, size, domain), size);
		}

		protected uint ReadUnsignedBig(long addr, int size, string domain = null)
		{
			uint v = 0;
			for (var i = 0; i < size; ++i)
			{
				v |= ReadUnsignedByte(addr + i, domain) << (8 * (size - 1 - i));
			}

			return v;
		}

		protected void WriteSignedLittle(long addr, int v, int size, string domain = null)
		{
			WriteUnsignedLittle(addr, (uint)v, size, domain);
		}

		protected void WriteUnsignedLittle(long addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * i)) & 0xFF, domain);
			}
		}

		protected void WriteSignedBig(long addr, int v, int size, string domain = null)
		{
			WriteUnsignedBig(addr, (uint)v, size, domain);
		}

		protected void WriteUnsignedBig(long addr, uint v, int size, string domain = null)
		{
			for (var i = 0; i < size; ++i)
			{
				WriteUnsignedByte(addr + i, (v >> (8 * (size - 1 - i))) & 0xFF, domain);
			}
		}

		#region public Library implementations

		protected List<byte> ReadByteRange(long addr, int length, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			var lastAddr = length + addr;
			var list = new List<byte>();
			for (; addr <= lastAddr; addr++)
			{
				if (addr < d.Size)
					list.Add(d.PeekByte(addr));
				else {
					Console.WriteLine($"Warning: Attempted read {addr} outside memory domain size of {d.Size} in {nameof(ReadByteRange)}()");
					list.Add(0);
				}
			}

			return list;
		}

		protected void WriteByteRange(long addr, List<byte> memoryblock, string domain = null)
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
						Console.WriteLine($"Warning: Attempted write {addr} outside memory domain size of {d.Size} in {nameof(WriteByteRange)}()");
					}
				}
			}
			else
			{
				Console.WriteLine($"Error: the domain {d.Name} is not writable");
			}
		}

		protected float ReadFloat(long addr, bool bigEndian, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (addr < d.Size)
			{
				var val = d.PeekUint(addr, bigEndian);
				var bytes = BitConverter.GetBytes(val);
				return BitConverter.ToSingle(bytes, 0);
			}

			Console.WriteLine($"Warning: Attempted read {addr} outside memory size of {d.Size}");

			return 0;
		}

		protected void WriteFloat(long addr, double value, bool bigEndian, string domain = null)
		{
			var d = string.IsNullOrEmpty(domain) ? Domain : DomainList[VerifyMemoryDomain(domain)];
			if (d.CanPoke())
			{
				if (addr < d.Size)
				{
					var dv = (float)value;
					var bytes = BitConverter.GetBytes(dv);
					var v = BitConverter.ToUInt32(bytes, 0);
					d.PokeUint(addr, v, bigEndian);
				}
				else
				{
					Console.WriteLine($"Warning: Attempted write {addr} outside memory size of {d.Size}");
				}
			}
			else
			{
				Console.WriteLine($"Error: the domain {Domain.Name} is not writable");
			}
		}

		#endregion
	}
}
