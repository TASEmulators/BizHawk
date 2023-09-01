using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public partial class Citra
	{
		private IMemoryDomains _memoryDomains;

		private MemoryDomainIntPtr _fcram;
		private MemoryDomainIntPtr _vram;
		private MemoryDomainIntPtr _dspRam;
		private MemoryDomainIntPtr _n3dsExRam;

		private void InitMemoryDomains()
		{
			List<MemoryDomain> domains = new List<MemoryDomain>()
			{
				(_fcram = new("FCRAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4)),
				(_vram = new("VRAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4)),
				(_dspRam = new("DSP RAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4)),
			};

			_n3dsExRam = new("N3DS Extra RAM", MemoryDomain.Endian.Little, IntPtr.Zero, 0, true, 4);
			if (_syncSettings.IsNew3ds)
			{
				domains.Add(_n3dsExRam);
			}

			_memoryDomains = new MemoryDomainList(domains);
			_serviceProvider.Register(_memoryDomains);
			WireMemoryDomains();
		}

		private void WireMemoryDomains()
		{
			void WireDomain(LibCitra.MemoryRegion region, MemoryDomainIntPtr domain)
			{
				_core.Citra_GetMemoryRegion(_context, region, out var ptr, out int size);
				domain.Data = ptr;
				domain.SetSize(size);
			}

			WireDomain(LibCitra.MemoryRegion.FCRAM, _fcram);
			WireDomain(LibCitra.MemoryRegion.VRAM, _vram);
			WireDomain(LibCitra.MemoryRegion.DSP, _dspRam);
			WireDomain(LibCitra.MemoryRegion.N3DS, _n3dsExRam);
		}
	}
}