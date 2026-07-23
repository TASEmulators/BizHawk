using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// A whole floppy represented as flux: one MfmTrack per (cylinder, side). Disk-image formats
	/// (DSK/EDSK and later HFE/IPF) convert into this. Part of the shared floppy disk subsystem.
	/// </summary>
	public sealed class FluxDisk
	{
		private readonly Dictionary<int, MfmTrack> _tracks = new Dictionary<int, MfmTrack>();

		public int Cylinders { get; private set; }
		public int Sides { get; private set; }
		public bool WriteProtected { get; set; }

		private static int Key(int cylinder, int side) => (cylinder << 1) | (side & 1);

		public void SetTrack(int cylinder, int side, MfmTrack track)
		{
			_tracks[Key(cylinder, side)] = track;
			if (cylinder + 1 > Cylinders) Cylinders = cylinder + 1;
			if (side + 1 > Sides) Sides = side + 1;
		}

		/// <summary>
		/// The flux for a (cylinder, side), or null if that track is not present/formatted.
		/// </summary>
		public MfmTrack GetTrack(int cylinder, int side)
			=> _tracks.TryGetValue(Key(cylinder, side), out var t) ? t : null;

		/// <summary>
		/// Extract one side of a double-sided disk as a new single-sided disk (that side's tracks placed at
		/// side 0). This is how a double-sided image is split into two selectable disks for the single-headed
		/// +3/CPC drive - format-agnostic, since it works on the shared flux representation.
		/// </summary>
		public FluxDisk ExtractSide(int side)
		{
			var one = new FluxDisk { WriteProtected = WriteProtected };
			for (int cyl = 0; cyl < Cylinders; cyl++)
			{
				var t = GetTrack(cyl, side);
				if (t != null) one.SetTrack(cyl, 0, t);
			}
			return one;
		}

		/// <summary>
		/// Build a flux disk from a CPC DSK/EDSK image (each track synthesized into MFM cells).
		/// </summary>
		public static FluxDisk FromCpcDsk(byte[] dsk)
		{
			var parsed = CpcDskConverter.Parse(dsk);
			CpcDskConverter.ApplySpeedlockWeakSynthesis(parsed); // reproduce weak sectors a plain DSK omits
			var disk = new FluxDisk();
			foreach (var pt in parsed.Tracks)
				disk.SetTrack(pt.Cylinder, pt.Side, pt.BuildFlux());
			return disk;
		}

		/// <summary>
		/// Build a flux disk from an HxC HFE image (raw cell bitstream, de-interleaved per side).
		/// </summary>
		public static FluxDisk FromHfe(byte[] hfe) => HfeConverter.ToFluxDisk(hfe);

		/// <summary>
		/// Build a flux disk from a ZX Spectrum FDI sector image.
		/// </summary>
		public static FluxDisk FromFdi(byte[] fdi) => FdiConverter.ToFluxDisk(fdi);

		/// <summary>
		/// Build a flux disk from a ZX Spectrum UDI v1.0 track image.
		/// </summary>
		public static FluxDisk FromUdi(byte[] udi) => UdiConverter.ToFluxDisk(udi);

		/// <summary>
		/// Build a flux disk from a SuperCard Pro (.scp) flux image (flux quantized to MFM cells).
		/// </summary>
		public static FluxDisk FromScp(byte[] scp) => ScpConverter.ToFluxDisk(scp);

		/// <summary>
		/// Build a flux disk from a headerless raw sector image using an explicit geometry.
		/// </summary>
		public static FluxDisk FromRawSectors(byte[] data, DiskGeometry geometry) => RawSectorConverter.ToFluxDisk(data, geometry);

		/// <summary>
		/// Build a flux disk from an IPF image (each formatted track rolled into MFM cells).
		/// </summary>
		public static FluxDisk FromIpf(byte[] ipf)
		{
			var parsed = IpfConverter.Parse(ipf);
			var disk = new FluxDisk();
			foreach (var img in parsed.Images)
			{
				if (!parsed.Data.TryGetValue(img.DataKey, out var data)) continue;
				var track = IpfConverter.BuildFluxTrack(img, data);
				if (track != null) disk.SetTrack(img.Track, img.Side, track);
			}
			return disk;
		}
	}
}
