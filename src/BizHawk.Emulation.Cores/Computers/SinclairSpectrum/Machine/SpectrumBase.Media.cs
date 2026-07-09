using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Cores.Floppy;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// The abstract class that all emulated models will inherit from
	/// * Imported media *
	/// </summary>
	public abstract partial class SpectrumBase
	{
		/// <summary>
		/// The tape or disk image(s) that are passed in from the main ZXSpectrum class
		/// </summary>
		protected List<byte[]> mediaImages { get; set; }

		/// <summary>
		/// Tape images
		/// </summary>
		public List<byte[]> tapeImages { get; set; }

		/// <summary>
		/// Disk images
		/// </summary>
		public List<byte[]> diskImages { get; set; }

		/// <summary>
		/// The side to present for each entry in diskImages: 0 or 1 selects one side of a
		/// double-sided image (which is registered as two disks), -1 loads the image as-is.
		/// </summary>
		public List<int> diskSides { get; set; }

		/// <summary>
		/// Set when a savestate is loaded
		/// (Used to cancel any tape/disk load messages after a loadstate)
		/// </summary>
		public bool IsLoadState;

		/// <summary>
		/// The index of the currently 'loaded' tape image
		/// </summary>
		protected int tapeMediaIndex;
		public int TapeMediaIndex
		{
			get => tapeMediaIndex;
			set
			{
				int tmp = value;
				int result = value;

				if (tapeImages == null || tapeImages.Count == 0)
				{
					// no tape images found
					return;
				}

				if (value >= tapeImages.Count)
				{
					// media at this index does not exist - loop back to 0
					result = 0;
				}
				else if (value < 0)
				{
					// negative index not allowed - move to last item in the collection
					result = tapeImages.Count - 1;
				}

				// load the media into the tape device
				tapeMediaIndex = result;

				// load first so the tape blocks (and detected loader) are current, then fire the osd message
				LoadTapeMedia();

				// fire osd message
				if (!IsLoadState)
					Spectrum.OSD_TapeInserted();
			}
		}

		/// <summary>
		/// The index of the currently 'loaded' disk image
		/// </summary>
		protected int diskMediaIndex;
		public int DiskMediaIndex
		{
			get => diskMediaIndex;
			set
			{
				int tmp = value;
				int result = value;

				if (diskImages == null || diskImages.Count == 0)
				{
					// no tape images found
					return;
				}

				if (value >= diskImages.Count)
				{
					// media at this index does not exist - loop back to 0
					result = 0;
				}
				else if (value < 0)
				{
					// negative index not allowed - move to last item in the collection
					result = diskImages.Count - 1;
				}

				// load the media into the disk device
				diskMediaIndex = result;

				LoadDiskMedia();

				// fire osd message (after load, so it can report the detected protection)
				if (!IsLoadState)
					Spectrum.OSD_DiskInserted();
			}
		}

		/// <summary>
		/// Called on first instantiation (and subsequent core reboots)
		/// </summary>
		protected void InitializeMedia(List<byte[]> files)
		{
			mediaImages = files;
			LoadAllMedia();
		}

		/// <summary>
		/// Attempts to load all media into the relevant structures
		/// </summary>
		protected void LoadAllMedia()
		{
			tapeImages = new List<byte[]>();
			diskImages = new List<byte[]>();
			diskSides = new List<int>();

			int cnt = 0;
			foreach (var m in mediaImages)
			{
				switch (IdentifyMedia(m))
				{
					case SpectrumMediaType.Tape:
						tapeImages.Add(m);
						Spectrum._tapeInfo.Add(Spectrum._gameInfo[cnt]);
						break;
					case SpectrumMediaType.Disk:
					case SpectrumMediaType.DiskDoubleSided:
						AddDiskImage(m, Spectrum._gameInfo[cnt]);
						break;
				}

				cnt++;
			}

			if (tapeImages.Count > 0)
				LoadTapeMedia();

			if (diskImages.Count > 0)
				LoadDiskMedia();
		}

		/// <summary>
		/// Registers a disk image, splitting a double-sided disk into two selectable single-sided disks. This
		/// works for every supported format (DSK/EDSK/IPF/HFE/SCP/FDI/UDI) because the double-sidedness is
		/// determined from the shared flux model rather than per-format byte layouts.
		/// </summary>
		private void AddDiskImage(byte[] image, Common.GameInfo gameInfo)
		{
			int sides;
			try { sides = DiskImageLoader.ToFluxDisk(image).Sides; }
			catch { sides = 1; }

			// The +3's 3" drive is single-headed, so a double-sided image is split into two selectable disks.
			// The Beta 128 drive (Pentagon) is double-headed and reads both sides of one disk, so never split.
			if (sides <= 1 || this is Pentagon128)
			{
				diskImages.Add(image);
				diskSides.Add(-1);
				Spectrum._diskInfo.Add(gameInfo);
				return;
			}

			// double-sided: register both sides as separate disks (the +3 drive is single-headed)
			for (int s = 0; s < 2; s++)
			{
				diskImages.Add(image);
				diskSides.Add(s);
				Spectrum._diskInfo.Add(new Common.GameInfo
				{
					FirmwareHash = gameInfo.FirmwareHash,
					Hash = gameInfo.Hash,
					Name = gameInfo.Name + " (Side " + (s + 1) + ")",
					Region = gameInfo.Region,
					NotInDatabase = gameInfo.NotInDatabase,
					Status = gameInfo.Status,
					System = gameInfo.System,
				});
			}
		}

		/// <summary>
		/// Attempts to load a tape into the tape device based on tapeMediaIndex
		/// </summary>
		protected void LoadTapeMedia()
		{
			TapeDevice.LoadTape(tapeImages[tapeMediaIndex]);
		}

		/// <summary>
		/// Attempts to load a disk into the disk device based on diskMediaIndex
		/// </summary>
		protected void LoadDiskMedia()
		{
			var image = diskImages[diskMediaIndex];

			// route by format to the machine that can actually read it, and warn (like the +3 gate) if the
			// wrong model is selected: TR-DOS .trd/.scl need the Pentagon's Beta 128, every other disk image
			// (+3/PCW .dsk/.edsk and the flux formats) needs the +3's uPD765.
			bool isTrDos = Floppy.TrdConverter.IsTrd(image) || Floppy.SclConverter.IsScl(image);

			if (isTrDos && this is not Pentagon128)
			{
				Spectrum.CoreComm.ShowMessage("You are trying to load a TR-DOS (.trd/.scl) disk image.\n\n Please select Pentagon 128 emulation immediately and reboot the core");
				return;
			}

			if (!isTrDos && this is not ZX128Plus3)
			{
				Spectrum.CoreComm.ShowMessage("You are trying to load a +3 disk image.\n\n Please select ZX Spectrum +3 emulation immediately and reboot the core");
				return;
			}

			UPDDiskDevice.FDD_LoadDisk(image, diskSides[diskMediaIndex]);
		}

		/// <summary>
		/// Identifies and sorts the various media types
		/// </summary>
		private SpectrumMediaType IdentifyMedia(byte[] data)
		{
			// get first 16 bytes as a string
			string hdr = Encoding.ASCII.GetString(data.Take(16).ToArray());

			// disk checking first
			if (hdr.ContainsIgnoreCase("MV - CPC") || hdr.ContainsIgnoreCase("EXTENDED CPC DSK"))
			{
				// spectrum .dsk disk file
				// check for number of sides
				var sides = data[0x31];
				if (sides == 1)
					return SpectrumMediaType.Disk;
				else
					return SpectrumMediaType.DiskDoubleSided;
			}
			if (hdr.StartsWithIgnoreCase("FDI"))
			{
				// spectrum .fdi disk file
				return SpectrumMediaType.Disk;
			}
			if (hdr.StartsWithIgnoreCase("CAPS"))
			{
				// IPF format file
				return SpectrumMediaType.Disk;
			}
			if (hdr.StartsWithIgnoreCase("UDI!") && data[0x08] is 0)
			{
				// UDI v1.0
				if (hdr.StartsWithOrdinal("udi!"))
				{
					throw new NotSupportedException("ZXHawk currently does not supported UDIv1.0 with compression.");
				}
				else
				{
					if (data[0x0A] == 0x01)
						return SpectrumMediaType.DiskDoubleSided;
					else
						return SpectrumMediaType.Disk;
				}
			}

			// SuperCard Pro (.scp) and HxC (.hfe) flux images. These carry sidedness inside the flux rather
			// than in a fixed header field, so AddDiskImage derives the side count from the decoded flux;
			// classifying as Disk here is enough to route them down the disk path.
			if (Floppy.ScpConverter.IsScp(data)
				|| Floppy.HfeConverter.IsHfe(data) || Floppy.HfeConverter.IsHfeV3(data))
			{
				return SpectrumMediaType.Disk;
			}

			// TR-DOS disk images: .trd is headerless (validated structurally), .scl carries a SINCLAIR header
			if (Floppy.TrdConverter.IsTrd(data) || hdr.StartsWithIgnoreCase("SINCLAIR"))
			{
				return SpectrumMediaType.Disk;
			}

			// tape checking
			if (hdr.StartsWithIgnoreCase("ZXTAPE!"))
			{
				// spectrum .tzx tape file
				return SpectrumMediaType.Tape;
			}
			if (hdr.StartsWithIgnoreCase("PZXT"))
			{
				// spectrum .tzx tape file
				return SpectrumMediaType.Tape;
			}
			if (hdr.StartsWithIgnoreCase("COMPRESSED SQ"))
			{
				// spectrum .tzx tape file
				return SpectrumMediaType.Tape;
			}
			if (hdr.ContainsIgnoreCase("WAVE"))
			{
				// spectrum .tzx tape file
				return SpectrumMediaType.Tape;
			}

			// if we get this far, assume a .tap file
			return SpectrumMediaType.Tape;
		}
	}

	public enum SpectrumMediaType
	{
		None,
		Tape,
		Disk,
		DiskDoubleSided
	}
}
