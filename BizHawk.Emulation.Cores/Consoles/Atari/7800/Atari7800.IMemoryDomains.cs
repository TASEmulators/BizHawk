using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using EMU7800.Core;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	public partial class Atari7800
	{
		private List<MemoryDomain> _MemoryDomains;

		private IMemoryDomains MemoryDomains;

		public void SetupMemoryDomains(HSC7800 hsc7800)
		{
			// reset memory domains
			if (_MemoryDomains == null)
			{
				_MemoryDomains = new List<MemoryDomain>();
				if (theMachine is Machine7800)
				{
					_MemoryDomains.Add(new MemoryDomainDelegate(
						"RAM", 0x1000, MemoryDomain.Endian.Unknown,
						delegate(long addr)
						{
							if (addr < 0 || addr >= 0x1000)
							{
								throw new ArgumentOutOfRangeException();
							}

							if (addr < 0x800)
							{
								return ((Machine7800)theMachine).RAM1[(ushort)addr];
							}

							return ((Machine7800)theMachine).RAM2[(ushort)addr];
						},

						delegate(long addr, byte val)
						{
							if (addr < 0 || addr >= 0x1000)
							{
								throw new ArgumentOutOfRangeException();
							}
							else if (addr < 0x800)
							{
								((Machine7800)theMachine).RAM1[(ushort)(addr & 0x800)] = val;
							}
							else
							{
								((Machine7800)theMachine).RAM2[(ushort)addr] = val;
							}
						}, 1));

					_MemoryDomains.Add(new MemoryDomainByteArray(
						"BIOS ROM", MemoryDomain.Endian.Unknown,
						bios, false, 1));

					if (hsc7800 != null)
					{
						_MemoryDomains.Add(new MemoryDomainByteArray(
							"HSC ROM", MemoryDomain.Endian.Unknown, hsbios, false, 1));

						_MemoryDomains.Add(new MemoryDomainByteArray(
							"HSC RAM", MemoryDomain.Endian.Unknown, hsram, true, 1));
					}

					_MemoryDomains.Add(new MemoryDomainDelegate(
						"System Bus", 65536, MemoryDomain.Endian.Unknown,
						delegate(long addr)
						{
							if (addr < 0 || addr >= 0x10000)
								throw new ArgumentOutOfRangeException();
							return theMachine.Mem[(ushort)addr];
						},
						delegate(long addr, byte val)
						{
							if (addr < 0 || addr >= 0x10000)
								throw new ArgumentOutOfRangeException();
							theMachine.Mem[(ushort)addr] = val;
						}, 1));
				}
				else // todo 2600?
				{
				}

				MemoryDomains = new MemoryDomainList(_MemoryDomains);
				(ServiceProvider as BasicServiceProvider).Register<IMemoryDomains>(MemoryDomains);
			}
		}
	}
}
