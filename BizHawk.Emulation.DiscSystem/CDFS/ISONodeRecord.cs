using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Class to represent the file/directory information read from the disk.
	/// </summary>
	public class ISONodeRecord
	{
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
		/// This is the number of blocks at the beginning of the file reserved for extended attribute information
		/// The format of the extended attribute record is not defined and is reserved for application use
		/// </summary>
		public byte ExtendedAttribRecordLength;

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
		public ISONodeRecord()
		{
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
		public bool IsFile()
		{
			return ((this.Flags >> 1) & 0x01) == 0;
		}

		/// <summary>
		/// Return true if the record represents a directory.
		/// </summary>
		/// <returns>True if a directory.</returns>
		public bool IsDirectory()
		{
			return ((this.Flags >> 1) & 0x01) == 1;
		}

		#endregion

		#region Parsing

		/// <summary>
		/// Parse the record from an array and offset.
		/// </summary>
		/// <param name="data">The array to parse from.</param>
		/// <param name="cursor">The offset to start parsing at.</param>
		public void Parse(byte[] data, int cursor)
		{
			// Put the array into a memory stream and pass to the main parsing function
			MemoryStream s = new MemoryStream(data);
			s.Seek(cursor, SeekOrigin.Begin);

			if (ISOFile.Format == ISOFile.ISOFormat.ISO9660)
				this.ParseISO9660(s);

			if (ISOFile.Format == ISOFile.ISOFormat.CDInteractive)
				this.ParseCDInteractive(s);
		}

		/// <summary>
		/// Parse the node record from the given ISO9660 stream.
		/// </summary>
		/// <param name="s">The stream to parse from.</param>
		public void ParseISO9660(Stream s)
		{
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
			if (this.NameLength == 1 && (buffer[0] == 0 || buffer[0] == 1))
			{
				if (buffer[0] == 0)
					this.Name = ISONodeRecord.CURRENT_DIRECTORY;
				else
					this.Name = ISONodeRecord.PARENT_DIRECTORY;
			}
			else
			{
				this.Name = ASCIIEncoding.ASCII.GetString(buffer, 0, this.NameLength);
			}

			// Seek to end
			s.Seek(startPosition + this.Length, SeekOrigin.Begin);
		}

		/// <summary>
		/// Parse the node record from the given CD-I stream.
		/// </summary>
		/// <param name="s">The stream to parse from.</param>
		public void ParseCDInteractive(Stream s)
		{
			/*
			BP      Size in bytes   Description
			1       1               Record length
			2       1               Extended Attribute record length
			3       4               Reserved
			7       4               File beginning LBN
			11      4               Reserved
			15      4               File size
			19      6               Creation date
			25      1               Reserved
			26      1               File flags
			27      2               Interleave
			29      2               Reserved
			31      2               Album Set Sequence number
			33      1               File name size
			34      (n)             File name
			34+n    4               Owner ID
			38+n    2               Attributes
			40+n    2               Reserved
			42+n    1               File number
			43+n    1               Reserved
			        43+n            Total
			*/

			EndianBitConverter bc = EndianBitConverter.CreateForLittleEndian();
			EndianBitConverter bcBig = EndianBitConverter.CreateForBigEndian();
			long startPosition = s.Position;
			byte[] buffer = new byte[ISOFile.SECTOR_SIZE];

			// Read the entire structure
			s.Read(buffer, 0, ISOFile.SECTOR_SIZE);
			s.Position -= ISOFile.SECTOR_SIZE;

			// Get the record length
			this.Length = buffer[0];

			// extended attribute record length
			this.ExtendedAttribRecordLength = buffer[1];

			// Read Data Offset
			this.OffsetOfData = bcBig.ReadIntValue(buffer, 6, 4);

			// Read Data Length
			this.LengthOfData = bcBig.ReadIntValue(buffer, 14, 4);

			// Read the time
			var ti = bc.ReadBytes(buffer, 18, 6);
			this.Year = ti[0];
			this.Month = ti[1];
			this.Day = ti[2];
			this.Hour = ti[3];
			this.Minute = ti[4];
			this.Second = ti[5];

			// read interleave - still to do

			// read album (volume) set sequence number (we are ignoring this)

			// Read the name length
			this.NameLength = buffer[32];            

			// Read the file/directory name
			var name = bc.ReadBytes(buffer, 33, this.NameLength);
			if (this.NameLength == 1 && (name[0] == 0 || name[0] == 1))
			{
				if (name[0] == 0)
					this.Name = ISONodeRecord.CURRENT_DIRECTORY;
				else
					this.Name = ISONodeRecord.PARENT_DIRECTORY;
			}
			else
			{
				this.Name = ASCIIEncoding.ASCII.GetString(name, 0, this.NameLength);
			}

			// skip ownerID for now

			// read the flags - only really interested in the directory attribute (bit 15)
			// (confusingly these are called 'attributes' in CD-I. the CD-I 'File Flags' entry is something else entirely)
			this.Flags = buffer[37 + this.NameLength];

			// skip filenumber
			//this.FileNumber = buffer[41 + this.NameLength];

			// Seek to end
			s.Seek(startPosition + this.Length, SeekOrigin.Begin);
		}

		#endregion
	}
}
