using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public partial class Encore
	{
		private IMemoryDomains _memoryDomains;

		private MemoryDomainIntPtr _fcram;
		private MemoryDomainIntPtr _vram;
		private MemoryDomainIntPtr _dspRam;
		private MemoryDomainIntPtr _n3dsExRam;

		private void InitMemoryDomains()
		{
			var domains = new List<MemoryDomain>()
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
			void WireDomain(LibEncore.MemoryRegion region, MemoryDomainIntPtr domain)
			{
				_core.Encore_GetMemoryRegion(_context, region, out var ptr, out var size);
				domain.Data = ptr;
				domain.SetSize(size);
			}

			WireDomain(LibEncore.MemoryRegion.FCRAM, _fcram);
			WireDomain(LibEncore.MemoryRegion.VRAM, _vram);
			WireDomain(LibEncore.MemoryRegion.DSP, _dspRam);
			WireDomain(LibEncore.MemoryRegion.N3DS, _n3dsExRam);
		}
	}
}