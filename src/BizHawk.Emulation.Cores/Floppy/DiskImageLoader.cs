namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Detects a disk image's format by signature and converts it into the shared flux model. Recognizes
	/// CPC DSK/EDSK, IPF, HxC HFE, SuperCard Pro SCP and ZX Spectrum FDI; a headerless image falls back to a
	/// supplied raw geometry (default the standard +3 layout).
	/// </summary>
	public static class DiskImageLoader
	{
		public static FluxDisk ToFluxDisk(byte[] data, DiskGeometry rawFallback = null)
		{
			if (data == null || data.Length < 8) throw new System.ArgumentException("empty or too-small disk image", nameof(data));

			if (IpfConverter.IsIpf(data)) return FluxDisk.FromIpf(data);
			if (HfeConverter.IsHfe(data) || HfeConverter.IsHfeV3(data)) return FluxDisk.FromHfe(data);
			if (ScpConverter.IsScp(data)) return FluxDisk.FromScp(data);
			if (UdiConverter.IsUdi(data)) return FluxDisk.FromUdi(data);
			if (FdiConverter.IsFdi(data)) return FluxDisk.FromFdi(data);
			if (CpcDskConverter.IsCpcDsk(data)) return FluxDisk.FromCpcDsk(data);

			// headerless: treat as a raw sector image with the given (or standard +3) geometry
			return RawSectorConverter.ToFluxDisk(data, rawFallback ?? DiskGeometry.Plus3);
		}
	}
}
