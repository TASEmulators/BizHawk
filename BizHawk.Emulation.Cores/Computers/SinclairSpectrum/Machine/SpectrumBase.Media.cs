using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public abstract partial class SpectrumBase
    {
		// until +3 disk drive is emulated, we assume that incoming files are tape images

		/// <summary>
        /// The tape or disk image(s) that are passed in from the main ZXSpectrum class
        /// </summary>
		protected List<byte[]> mediaImages { get; set; }

		/// <summary>
        /// Tape images
        /// </summary>
		protected List<byte[]> tapeImages { get; set; }

		/// <summary>
        /// Disk images
        /// </summary>
		protected List<byte[]> diskImages { get; set; }

		/// <summary>
        /// The index of the currently 'loaded' tape or disk image
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
                Spectrum.OSD_TapeInserted();
                LoadTapeMedia();
            }
        }

        /// <summary>
        /// The index of the currently 'loaded' tape or disk image
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
            Spectrum.OSD_TapeInit();
        }

		/// <summary>
        /// Attempts to load all media into the relevant structures
        /// </summary>
		protected void LoadAllMedia()
        {
            tapeImages = new List<byte[]>();
            diskImages = new List<byte[]>();

			foreach (var m in mediaImages)
            {
				switch (IdentifyMedia(m))
                {
                    case SpectrumMediaType.Tape:
                        tapeImages.Add(m);
                        break;
                    case SpectrumMediaType.Disk:
                        diskImages.Add(m);
                        break;
                }
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
            throw new NotImplementedException("+3 disk drive device not yet implemented");
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
			if (hdr.ToUpper().Contains("EXTENDED CPC DSK"))
            {
                // spectrum .dsk disk file
                return SpectrumMediaType.Disk;
            }
			if (hdr.ToUpper().StartsWith("FDI"))
            {
                // spectrum .fdi disk file
                return SpectrumMediaType.Disk;
            }

            // tape checking
            if (hdr.ToUpper().StartsWith("ZXTAPE!"))
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
		Disk
    }
}
