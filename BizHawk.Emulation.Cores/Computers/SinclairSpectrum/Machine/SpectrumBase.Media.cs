using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            get { return tapeMediaIndex; }
            set
            {
                int tmp = value;
                int result = value;			              

                if (tapeImages == null || tapeImages.Count() == 0)
                {
                    // no tape images found
                    return;
                } 

				if (value >= tapeImages.Count())
                {
                    // media at this index does not exist - loop back to 0
                    result = 0;
                }
				else if (value < 0)
                {
                    // negative index not allowed - move to last item in the collection
                    result = tapeImages.Count() - 1;
                }
				
                // load the media into the tape device
                tapeMediaIndex = result;
                // fire osd message
                if (!IsLoadState)
                    Spectrum.OSD_TapeInserted();

                LoadTapeMedia();
            }
        }

        /// <summary>
        /// The index of the currently 'loaded' disk image
        /// </summary>
        protected int diskMediaIndex;
        public int DiskMediaIndex
        {
            get { return diskMediaIndex; }
            set
            {
                int tmp = value;
                int result = value;

                if (diskImages == null || diskImages.Count() == 0)
                {
                    // no tape images found
                    return;
                }

                if (value >= diskImages.Count())
                {
                    // media at this index does not exist - loop back to 0
                    result = 0;
                }
                else if (value < 0)
                {
                    // negative index not allowed - move to last item in the collection
                    result = diskImages.Count() - 1;
                }
				                
                // load the media into the disk device
                diskMediaIndex = result;

                // fire osd message
                if (!IsLoadState)
                    Spectrum.OSD_DiskInserted();

                LoadDiskMedia();
            }
        }

        /// <summary>
        /// Called on first instantiation (and subsequent core reboots)
        /// </summary>
        /// <param name="files"></param>
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
                        diskImages.Add(m);
                        Spectrum._diskInfo.Add(Spectrum._gameInfo[cnt]);
                        break;
                    case SpectrumMediaType.DiskDoubleSided:
                        // this is a bit tricky. we will attempt to parse the double sided disk image byte array,
                        // then output two separate image byte arrays
                        List<byte[]> working = new List<byte[]>();
                        foreach (DiskType type in Enum.GetValues(typeof(DiskType)))
                        {
                            bool found = false;

                            switch (type)
                            {
                                case DiskType.CPCExtended:
                                    found = CPCExtendedFloppyDisk.SplitDoubleSided(m, working);
                                    break;
                                case DiskType.CPC:
                                    found = CPCFloppyDisk.SplitDoubleSided(m, working);
                                    break;
                                case DiskType.UDI:
                                    found =  UDI1_0FloppyDisk.SplitDoubleSided(m, working);
                                    break;
                            }

                            if (found)
                            {
                                // add side 1
                                diskImages.Add(working[0]);
                                // add side 2
                                diskImages.Add(working[1]);

                                Common.GameInfo one = new Common.GameInfo();
                                Common.GameInfo two = new Common.GameInfo();
                                var gi = Spectrum._gameInfo[cnt];
                                for (int i = 0; i < 2; i++)
                                {
                                    Common.GameInfo work = new Common.GameInfo();
                                    if (i == 0)
                                    {
                                        work = one;
                                    }
                                    else if (i == 1)
                                    {
                                        work = two;
                                    }

                                    work.FirmwareHash = gi.FirmwareHash;
                                    work.Hash = gi.Hash;
                                    work.Name = gi.Name + " (Parsed Side " + (i + 1) + ")";
                                    work.Region = gi.Region;
                                    work.NotInDatabase = gi.NotInDatabase;
                                    work.Status = gi.Status;
                                    work.System = gi.System;

                                    Spectrum._diskInfo.Add(work);
                                }
                            }
                            else
                            {

                            }
                        }
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
            if (this.GetType() != typeof(ZX128Plus3))
            {
                Spectrum.CoreComm.ShowMessage("You are trying to load one of more disk images.\n\n Please select ZX Spectrum +3 emulation immediately and reboot the core");
                return;
            }
            else
            {
                //Spectrum.CoreComm.ShowMessage("You are attempting to load a disk into the +3 disk drive.\n\nThis DOES NOT currently work properly but IS under active development.");
            }

            UPDDiskDevice.FDD_LoadDisk(diskImages[diskMediaIndex]);
        }

        /// <summary>
        /// Identifies and sorts the various media types
        /// </summary>
        /// <returns></returns>
        private SpectrumMediaType IdentifyMedia(byte[] data)
        {
            // get first 16 bytes as a string
            string hdr = Encoding.ASCII.GetString(data.Take(16).ToArray());

			// disk checking first
			if (hdr.ToUpper().Contains("EXTENDED CPC DSK") || hdr.ToUpper().Contains("MV - CPC"))
            {
                // spectrum .dsk disk file
                // check for number of sides
                var sides = data[0x31];
                if (sides == 1)
                    return SpectrumMediaType.Disk;
                else
                    return SpectrumMediaType.DiskDoubleSided;
            }
			if (hdr.ToUpper().StartsWith("FDI"))
            {
                // spectrum .fdi disk file
                return SpectrumMediaType.Disk;
            }
            if (hdr.ToUpper().StartsWith("CAPS"))
            {
                // IPF format file
                return SpectrumMediaType.Disk;
            }
            if (hdr.ToUpper().StartsWith("UDI!") && data[0x08] == 0)
            {
                // UDI v1.0
                if (hdr.StartsWith("udi!"))
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

            // tape checking
            if (hdr.ToUpper().StartsWith("ZXTAPE!"))
            {
                // spectrum .tzx tape file
                return SpectrumMediaType.Tape;
            }
            if (hdr.ToUpper().StartsWith("PZXT"))
            {
                // spectrum .tzx tape file
                return SpectrumMediaType.Tape;
            }
            if (hdr.ToUpper().StartsWith("COMPRESSED SQ"))
            {
                // spectrum .tzx tape file
                return SpectrumMediaType.Tape;
            }
            if (hdr.ToUpper().Contains("WAVE"))
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
