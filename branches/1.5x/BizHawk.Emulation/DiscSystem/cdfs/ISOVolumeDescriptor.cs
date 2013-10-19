using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ISOParser {
    /// <summary>
    /// Represents a volume descriptor for a disk image.
    /// </summary>
    public class ISOVolumeDescriptor {
        #region Constants

        /// <summary>
        /// We are handling the parsing by reading the entire header and
        /// extracting the appropriate bytes.
        /// 
        /// This is done for performance reasons.
        /// </summary>
        private const int LENGTH_SHORT_IDENTIFIER = 32;
        private const int LENGTH_IDENTIFIER = 37;
        private const int LENGTH_LONG_IDENTIFIER = 128;
        private const int LENGTH_ROOT_DIRECTORY_RECORD = 34;
        private const int LENGTH_TIME = 17;
        private const int LENGTH_RESERVED = 512;

        #endregion

        #region Public Properties

        /// <summary>
        /// The type of this volume description, only 1 and 255 are supported
        /// </summary>
        public byte Type;
        
        /// <summary>
        /// The system identifier
        /// </summary>
        public byte[] SystemIdentifier;
        /// <summary>
        /// The volume identifier
        /// </summary>
        public byte[] VolumeIdentifier;
        
        /// <summary>
        /// The number of sectors on the disk
        /// </summary>
        public int NumberOfSectors;
        
        /// <summary>
        /// Volume Set Size (should be 1)
        /// </summary>
        public int VolumeSetSize;
        /// <summary>
        /// Volume Sequence Number (should be 1)
        /// </summary>
        public int VolumeSequenceNumber;
        /// <summary>
        /// Sector Size (should be 2048)
        /// </summary>
        public int SectorSize;

        /// <summary>
        /// Size of the path table
        /// </summary>
        public int PathTableSize;
        /// <summary>
        /// Sector offset of the first path table
        /// </summary>
        public int OffsetOfFirstLittleEndianPathTable;
        /// <summary>
        /// Sector offset of the second path table
        /// </summary>
        public int OffsetOfSecondLittleEndianPathTable;
        /// <summary>
        /// Sector offset of the first path table
        /// </summary>
        public int OffsetOfFirstBigEndianPathTable;
        /// <summary>
        /// Sector offset of the second path table
        /// </summary>
        public int OffsetOfSecondBigEndianPathTable;
        
        /// <summary>
        /// The root directory record
        /// </summary>
        public ISONodeRecord RootDirectoryRecord;

        /// <summary>
        /// The volumen set identifier
        /// </summary>
        public byte[] VolumeSetIdentifier;
        /// <summary>
        /// The publisher identifier
        /// </summary>
        public byte[] PublisherIdentifier;
        /// <summary>
        /// The data preparer identifier
        /// </summary>
        public byte[] DataPreparerIdentifier;
        /// <summary>
        /// The application identifier
        /// </summary>
        public byte[] ApplicationIdentifier;

        /// <summary>
        ///  The copyright identifier
        /// </summary>
        public byte[] CopyrightFileIdentifier;
        /// <summary>
        /// The abstract file identifier
        /// </summary>
        public byte[] AbstractFileIdentifier;
        /// <summary>
        /// The bibliographical file identifier
        /// </summary>
        public byte[] BibliographicalFileIdentifier;

        /// <summary>
        /// The time and date the volume was created
        /// </summary>
        public byte[] VolumeCreationDateTime;
        /// <summary>
        /// The time and date the volume was last modified
        /// </summary>
        public byte[] LastModifiedDateTime;
        /// <summary>
        /// The time and date the volume expires
        /// </summary>
        public byte[] ExpirationDateTime;
        /// <summary>
        /// The time and data when the volume is effective
        /// </summary>
        public byte[] EffectiveDateTime;
        
        /// <summary>
        /// Extra reserved data
        /// </summary>
        public byte[] Reserved;

        #endregion

        #region Construction

        /// <summary>
        /// Constructor.
        /// </summary>
        public ISOVolumeDescriptor() {
            // Set everything to the default value
            this.Type = 0;
            
            this.SystemIdentifier = new byte[LENGTH_SHORT_IDENTIFIER];
            this.VolumeIdentifier = new byte[LENGTH_SHORT_IDENTIFIER];
            
            this.NumberOfSectors = 0;
            
            this.VolumeSetSize = 1;
            this.VolumeSequenceNumber = 1;
            this.SectorSize = ISOFile.SECTOR_SIZE;

            this.PathTableSize = 0;
            this.OffsetOfFirstLittleEndianPathTable = 0;
            this.OffsetOfSecondLittleEndianPathTable = 0;
            this.OffsetOfFirstBigEndianPathTable = 0;
            this.OffsetOfSecondBigEndianPathTable = 0;

            this.RootDirectoryRecord = new ISONodeRecord();

            this.VolumeSetIdentifier = new byte[LENGTH_LONG_IDENTIFIER];
            this.PublisherIdentifier = new byte[LENGTH_LONG_IDENTIFIER];
            this.DataPreparerIdentifier = new byte[LENGTH_LONG_IDENTIFIER];
            this.ApplicationIdentifier = new byte[LENGTH_LONG_IDENTIFIER];
        
            this.CopyrightFileIdentifier = new byte[LENGTH_IDENTIFIER];
            this.AbstractFileIdentifier = new byte[LENGTH_IDENTIFIER];
            this.BibliographicalFileIdentifier = new byte[LENGTH_IDENTIFIER];

            this.VolumeCreationDateTime = new byte[LENGTH_TIME];
            this.LastModifiedDateTime = new byte[LENGTH_TIME];
            this.ExpirationDateTime = new byte[LENGTH_TIME];
            this.EffectiveDateTime = new byte[LENGTH_TIME];

            this.Reserved = new byte[LENGTH_RESERVED];        
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parse the volume descriptor header.
        /// </summary>
        /// <param name="s">The stream to parse from.</param>
        public bool Parse(Stream s) {
            EndianBitConverter bc = EndianBitConverter.CreateForLittleEndian();
            EndianBitConverter bcBig = EndianBitConverter.CreateForBigEndian();
            long startPosition = s.Position;
            byte[] buffer = new byte[ISOFile.SECTOR_SIZE];

            // Read the entire structure
            s.Read(buffer, 0, ISOFile.SECTOR_SIZE);

            // Get the type
            this.Type = buffer[0];

						//zero 24-jun-2013 - validate
            //  "CD001" + 0x01
						if (buffer[1] == 'C' && buffer[2] == 'D' && buffer[3] == '0' && buffer[4] == '0' && buffer[5] == '1' && buffer[6] == 0x01)
						{
							//it seems to be a valid volume descriptor
						}
						else
						{
							return false;
						}

            // Handle the primary volume information
            if (this.Type == 1) {
                int cursor = 8;
                // Get the system identifier 
                Array.Copy(buffer, cursor,
                    this.SystemIdentifier, 0, LENGTH_SHORT_IDENTIFIER);
                cursor += LENGTH_SHORT_IDENTIFIER;

                // Get the volume identifier
                Array.Copy(buffer, cursor,
                    this.VolumeIdentifier, 0, LENGTH_SHORT_IDENTIFIER);
                cursor += LENGTH_SHORT_IDENTIFIER;

                cursor += 8;

                // Get the total number of sectors
                this.NumberOfSectors = bc.ToInt32(buffer, cursor);
                cursor += 8;

                cursor += 32;

                this.VolumeSetSize = bc.ToInt16(buffer, cursor);
                cursor += 4;
                this.VolumeSequenceNumber = bc.ToInt16(buffer, cursor);
                cursor += 4;
                this.SectorSize = bc.ToInt16(buffer, cursor);
                cursor += 4;

                this.PathTableSize = bc.ToInt32(buffer, cursor);
                cursor += 8;
                this.OffsetOfFirstLittleEndianPathTable = bc.ToInt32(buffer, cursor);
                cursor += 4;
                this.OffsetOfSecondLittleEndianPathTable = bc.ToInt32(buffer, cursor);
                cursor += 4;
                this.OffsetOfFirstLittleEndianPathTable = bcBig.ToInt32(buffer, cursor);
                cursor += 4;
                this.OffsetOfSecondLittleEndianPathTable = bcBig.ToInt32(buffer, cursor);
                cursor += 4;

                this.RootDirectoryRecord.Parse(buffer, cursor);
                cursor += LENGTH_ROOT_DIRECTORY_RECORD;

                Array.Copy(buffer, cursor,
                    this.VolumeSetIdentifier, 0, LENGTH_LONG_IDENTIFIER);
                cursor += LENGTH_LONG_IDENTIFIER;
                Array.Copy(buffer, cursor,
                    this.PublisherIdentifier, 0, LENGTH_LONG_IDENTIFIER);
                cursor += LENGTH_LONG_IDENTIFIER;
                Array.Copy(buffer, cursor,
                    this.DataPreparerIdentifier, 0, LENGTH_LONG_IDENTIFIER);
                cursor += LENGTH_LONG_IDENTIFIER;
                Array.Copy(buffer, cursor,
                    this.ApplicationIdentifier, 0, LENGTH_LONG_IDENTIFIER);
                cursor += LENGTH_LONG_IDENTIFIER;

                Array.Copy(buffer, cursor,
                    this.CopyrightFileIdentifier, 0, LENGTH_IDENTIFIER);
                cursor += LENGTH_IDENTIFIER;
                Array.Copy(buffer, cursor,
                    this.AbstractFileIdentifier, 0, LENGTH_IDENTIFIER);
                cursor += LENGTH_IDENTIFIER;
                Array.Copy(buffer, cursor,
                    this.BibliographicalFileIdentifier, 0, LENGTH_IDENTIFIER);
                cursor += LENGTH_IDENTIFIER;

                Array.Copy(buffer, cursor,
                    this.VolumeCreationDateTime, 0, LENGTH_TIME);
                cursor += LENGTH_TIME;
                Array.Copy(buffer, cursor,
                    this.LastModifiedDateTime, 0, LENGTH_TIME);
                cursor += LENGTH_TIME;
                Array.Copy(buffer, cursor,
                    this.ExpirationDateTime, 0, LENGTH_TIME);
                cursor += LENGTH_TIME;
                Array.Copy(buffer, cursor,
                    this.EffectiveDateTime, 0, LENGTH_TIME);
                cursor += LENGTH_TIME;

                cursor += 1;

                cursor += 1;

                Array.Copy(buffer, cursor,
                    this.Reserved, 0, LENGTH_RESERVED);
                cursor += LENGTH_RESERVED;
            }

						return true;
        }

        #endregion

        #region Type Information

        /// <summary>
        /// Returns true if this is the terminator volume descriptor.
        /// </summary>
        /// <returns>True if the terminator.</returns>
        public bool IsTerminator() {
            return (this.Type == 255);
        }

        #endregion
    }
}
