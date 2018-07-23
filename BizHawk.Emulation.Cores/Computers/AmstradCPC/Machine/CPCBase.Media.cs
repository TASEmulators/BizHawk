using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Imported media *
    /// </summary>
    public abstract partial class CPCBase
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
                //Spectrum.OSD_TapeInserted();
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
                //Spectrum.OSD_DiskInserted();

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
                    case CPCMediaType.Tape:
                        tapeImages.Add(m);
                        CPC._tapeInfo.Add(CPC._gameInfo[cnt]);
                        break;
                    case CPCMediaType.Disk:
                        diskImages.Add(m);
                        CPC._diskInfo.Add(CPC._gameInfo[cnt]);
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
            if (this.GetType() == typeof(CPC464))
            {
                CPC.CoreComm.ShowMessage("You are trying to load one of more disk images.\n\n Please select something other than CPC 464 emulation immediately and reboot the core");
                return;
            }

            UPDDiskDevice.FDD_LoadDisk(diskImages[diskMediaIndex]);
        }

        /// <summary>
        /// Identifies and sorts the various media types
        /// </summary>
        /// <returns></returns>
        private CPCMediaType IdentifyMedia(byte[] data)
        {
            // get first 16 bytes as a string
            string hdr = Encoding.ASCII.GetString(data.Take(16).ToArray());

            // disk checking first
            if (hdr.ToUpper().Contains("EXTENDED CPC DSK") || hdr.ToUpper().Contains("MV - CPC"))
            {
                // amstrad .dsk disk file
                return CPCMediaType.Disk;
            }

            // tape checking
            if (hdr.ToUpper().StartsWith("ZXTAPE!"))
            {
                // cdt tape file
                return CPCMediaType.Tape;
            }

            // not found
            return CPCMediaType.None;
        }
    }

    public enum CPCMediaType
    {
        None,
        Tape,
        Disk
    }
}

