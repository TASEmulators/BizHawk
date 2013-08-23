using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ISOParser {
    /// <summary>
    /// Class to represent the file/directory information read from the disk.
    /// </summary>
    public class ISONodeRecord {
        #region Constants

        /// <summary>
        /// String representing the current directory entry
        /// </summary>
        public const string CURRENT_DIRECTORY = ".";

        /// <summary>
        /// String representing the parent directory entry
        /// </summary>
        public const string PARENT_DIRECTORY = "..";

        #endregion

        #region Public Properties

        /// <summary>
        /// The length of the record in bytes.
        /// </summary>
        public byte Length;

        /// <summary>
        /// The file offset of the data for this file/directory (in sectors).
        /// </summary>
        public long OffsetOfData;
        /// <summary>
        /// The length of the data for this file/directory (in bytes).
        /// </summary>
        public long LengthOfData;

        /// <summary>
        /// The file/directory creation year since 1900.
        /// </summary>
        public byte Year;
        /// <summary>
        /// The file/directory creation month.
        /// </summary>
        public byte Month;
        /// <summary>
        /// The file/directory creation day.
        /// </summary>
        public byte Day;
        /// <summary>
        /// The file/directory creation hour.
        /// </summary>
        public byte Hour;
        /// <summary>
        /// The file/directory creation minute.
        /// </summary>
        public byte Minute;
        /// <summary>
        /// The file/directory creation second.
        /// </summary>
        public byte Second;
        /// <summary>
        /// The file time offset from GMT.
        /// </summary>
        public byte TimeZoneOffset;

        /// <summary>
        /// Flags representing the attributes of this file/directory.
        /// </summary>
        public byte Flags;

        /// <summary>
        /// The length of the file/directory name.
        /// </summary>
        public byte NameLength;
        /// <summary>
        /// The file/directory name.
        /// </summary>
        public string Name;

        #endregion

        #region Construction

        /// <summary>
        /// Constructor
        /// </summary>
        public ISONodeRecord() {
            // Set initial values
            this.Length = 0;
            
            this.OffsetOfData = 0;
            this.LengthOfData = 0;
            
            this.Year = 0;            
            this.Month = 0;
            this.Day = 0;
            this.Hour = 0;
            this.Minute = 0;
            this.Second = 0;
            this.TimeZoneOffset = 0;

            this.Flags = 0;

            this.NameLength = 0;
            this.Name = null;
        }

        #endregion

        #region File/Directory Methods

        /// <summary>
        /// Return true if the record represents a file.
        /// </summary>
        /// <returns>True if a file.</returns>
        public bool IsFile() {
            return ((this.Flags >> 1) & 0x01) == 0;
        }

        /// <summary>
        /// Return true if the record represents a directory.
        /// </summary>
        /// <returns>True if a directory.</returns>
        public bool IsDirectory() {
            return ((this.Flags >> 1) & 0x01) == 1;
        }

        #endregion

        #region Parsing

        /// <summary>
        /// Parse the record from an array and offset.
        /// </summary>
        /// <param name="data">The array to parse from.</param>
        /// <param name="cursor">The offset to start parsing at.</param>
        public void Parse(byte[] data, int cursor) {
            // Put the array into a memory stream and pass to the main parsing function
            MemoryStream s = new MemoryStream(data);
            s.Seek(cursor, SeekOrigin.Begin);
            this.Parse(s);
        }

        /// <summary>
        /// Parse the node record from the given stream.
        /// </summary>
        /// <param name="s">The stream to parse from.</param>
        public void Parse(Stream s) {
            EndianBitConverter bc = EndianBitConverter.CreateForLittleEndian();
            long startPosition = s.Position;
            byte[] buffer = new byte[ISOFile.SECTOR_SIZE];

            // Get the length
            s.Read(buffer, 0, 1);
            this.Length = buffer[0];

            //the number of sectors in the attribute record
            s.Read(buffer, 0, 1);

            // Read Data Offset
            s.Read(buffer, 0, 8);
            this.OffsetOfData = (long)bc.ToInt32(buffer);

            // Read Data Length
            s.Read(buffer, 0, 8);
            this.LengthOfData = (long)bc.ToInt32(buffer);

            // Read the time and flags
            s.Read(buffer, 0, 8);
            this.Year = buffer[0];
            this.Month = buffer[1];
            this.Day = buffer[2];
            this.Hour = buffer[3];
            this.Minute = buffer[4];
            this.Second = buffer[5];
            this.TimeZoneOffset = buffer[6];

            this.Flags = buffer[7];

            s.Read(buffer, 0, 6);

            // Read the name length
            s.Read(buffer, 0, 1);
            this.NameLength = buffer[0];

            // Read the directory name
            s.Read(buffer, 0, this.NameLength);
            if (this.NameLength == 1 && (buffer[0] == 0 || buffer[0] == 1)) {
                if (buffer[0] == 0)
                    this.Name = ISONodeRecord.CURRENT_DIRECTORY;
                else
                    this.Name = ISONodeRecord.PARENT_DIRECTORY;
            }
            else {
                this.Name = ASCIIEncoding.ASCII.GetString(buffer, 0, this.NameLength);
            }

            // Seek to end
            s.Seek(startPosition + this.Length, SeekOrigin.Begin);
        }

        #endregion
    }
}
