using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ISOParser {
    /// <summary>
    /// This class is meant to parse disk images as specified by ISO9660. 
    /// Specifically, it should work for most disk images that are created 
    /// by the stanard disk imaging software. This class is by no means
    /// robust to all variations of ISO9660.
    /// Also, this class does not currently support the UDF file system.
    /// 
    /// TODO: Add functions to enumerate a directory or visit a file...
    /// 
    /// The information for building class came from three primary sources:
    /// 1. The ISO9660 wikipedia article:
    ///     http://en.wikipedia.org/wiki/ISO_9660
    /// 2. ISO9660 Simplified for DOS/Windows
    ///     http://alumnus.caltech.edu/~pje/iso9660.html
    /// 3. The ISO 9660 File System
    ///     http://users.telenet.be/it3.consultants.bvba/handouts/ISO9960.html
    /// </summary>
    public class ISOFile {
        #region Constants

        /// <summary>
        /// We are hard coding the SECTOR_SIZE
        /// </summary>
        public const int SECTOR_SIZE = 2048;

        #endregion

        #region Public Members

        /// <summary>
        /// This is a list of all the volume descriptors in the disk image.
        /// NOTE: The first entry should be the primary volume.
        /// </summary>
        public List<ISOVolumeDescriptor> VolumeDescriptors;

        /// <summary>
        /// The Directory that is the root of this file system
        /// </summary>
        public ISODirectoryNode Root;

        #endregion

        #region Construction

        /// <summary>
        /// Construct the ISO file data structures, but leave everything
        /// blank.
        /// </summary>
        public ISOFile() {
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parse the given stream to populate the iso information
        /// </summary>
        /// <param name="s">The stream which we are using to parse the image. 
        /// Should already be located at the start of the image.</param>
        public bool Parse(Stream s, int startSector=16)
				{
					this.VolumeDescriptors = new List<ISOVolumeDescriptor>();
					Root = null;

            long startPosition = s.Position;
            byte[] buffer = new byte[ISOFile.SECTOR_SIZE];

            // Seek through the first volume descriptor
						s.Seek(startPosition + (SECTOR_SIZE * startSector), SeekOrigin.Begin);

            // Read one of more volume descriptors
            do {
								//zero 24-jun-2013 - improved validity checks

                ISOVolumeDescriptor desc = new ISOVolumeDescriptor();
                bool isValid = desc.Parse(s);
								if (!isValid) return false;

								if (desc.IsTerminator())
									break;
								else if (desc.Type < 4)
									this.VolumeDescriptors.Add(desc);
								else
									//found a volume descriptor of incorrect type.. maybe this isnt a cdfs
									//supposedly these exist.. wait for one to show up
									return false;

            } while(true);

					//zero 24-jun-2013 - well, my very first test iso had 2 volume descriptors.
            // Check to make sure we only read one volume descriptor
            // Finding more could be an error with the disk.
						//if (this.VolumeDescriptors.Count != 1) {
						//    Console.WriteLine("Strange ISO format...");
						//    return;
						//}

						//zero 24-jun-2013 - if theres no volume descriptors, we're gonna call this not a cdfs
						if (VolumeDescriptors.Count == 0) return false;

            // Visit all the directories and get the offset of each directory/file

            // We need to keep track of the directories and files we have visited in case there are loops.
            Dictionary<long, ISONode> visitedNodes = new Dictionary<long,ISONode>();

            // Create (and visit) the root node
            this.Root = new ISODirectoryNode(this.VolumeDescriptors[0].RootDirectoryRecord);
            visitedNodes.Add(this.Root.Offset, this.Root);
            this.Root.Parse(s, visitedNodes);

						return true;
        }

        #endregion

        #region Printing

        /// <summary>
        /// Print the directory tree for the image.
        /// </summary>
        public void Print() {
            // DEBUGGING: Now print out the directory structure
            this.Root.Print(0);
        }

        #endregion
    }
}
