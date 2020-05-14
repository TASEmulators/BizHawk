using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Represents a volume descriptor for a disk image.
	/// </summary>
	public class ISOVolumeDescriptor
	{


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





		private EndianBitConverter bc = EndianBitConverter.CreateForLittleEndian();
		private EndianBitConverter bcBig = EndianBitConverter.CreateForBigEndian();





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
		/// (ISO9660 only) Sector offset of the first path table
		/// </summary>
		public int OffsetOfFirstLittleEndianPathTable;
		/// <summary>
		/// (ISO9660 only) Sector offset of the second path table
		/// </summary>
		public int OffsetOfSecondLittleEndianPathTable;
		/// <summary>
		/// (ISO9660 only) Sector offset of the first path table
		/// </summary>
		public int OffsetOfFirstBigEndianPathTable;
		/// <summary>
		/// (ISO9660 only) Sector offset of the second path table
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
		/// (ISO9660 only) Extra reserved data
		/// </summary>
		public byte[] Reserved;


		// CD-Interactive only
		
		/// <summary>
		/// The bits of this field are numbered from 0 to 7 starting with the least significant bit
		/// BitPosition 0:  A value of 0 = the coded character set identifier field specifies only an escape sequence registered according to ISO 2375
		///                 A value of 1 = the coded character set identifier field specifies only an escape sequence NOT registered according to ISO 2375
		/// BitPostion 1-7:  All bits are 0 (reserved for future standardization)
		/// </summary>
		public byte VolumeFlags;
		/// <summary>
		/// This field specifies one escape sequence according to the International Register of Coded Character Sets to be used with escape 
		/// sequence standards for recording.The ESC character, which is the first character of all sequences, shall be omitted when recording this field
		/// </summary>
		public byte[] CodedCharSetIdent;
		/// <summary>
		/// The block address of the first block of the system Path Table is kept in this field
		/// </summary>
		public int AddressOfPathTable;





		/// <summary>
		/// Constructor.
		/// </summary>
		public ISOVolumeDescriptor()
		{
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

			// CD-I specific
			this.VolumeFlags = 0;
			this.CodedCharSetIdent = new byte[LENGTH_SHORT_IDENTIFIER];
			this.AddressOfPathTable = 0;            
		}





		/// <summary>
		/// Start parsing the volume descriptor header.
		/// </summary>
		/// <param name="s">The stream to parse from.</param>
		public bool Parse(Stream s)
		{
			long startPosition = s.Position;
			byte[] buffer = new byte[ISOFile.SECTOR_SIZE];

			// Read the entire structure
			s.Read(buffer, 0, ISOFile.SECTOR_SIZE);

			// Parse based on format
			byte[] header = bc.ReadBytes(buffer, 0, ISOFile.SECTOR_SIZE);
			if (GetISO9660(header))
			{
				ParseISO9660(s);
				return true;
			}
			if (GetCDI(header))
			{
				ParseCDInteractive(s);
				return true;
			}

			return false;
		}

		public void ParseISO9660(Stream s)
		{
			long startPosition = s.Position;
			byte[] buffer = new byte[ISOFile.SECTOR_SIZE];
			s.Position = startPosition - ISOFile.SECTOR_SIZE;

			// Read the entire structure
			s.Read(buffer, 0, ISOFile.SECTOR_SIZE);

			// Get the type
			this.Type = buffer[0];

			// Handle the primary volume information
			if (this.Type == 1)
			{
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
		}

		public void ParseCDInteractive(Stream s)
		{
			/* From the Green Book Spec
			* BP (byte position) obviously is n+1

			 BP Size in Bytes Description
			 1 1 Disc Label Record Type
			 2 5 Volume Structure Standard ID
			 7 1 Volume Structure Version number
			 8 1 Volume flags
			 9 32 System identifier
			 41 32 Volume identifier
			 73 12 Reserved
			 85 4 Volume space size
			 89 32 Coded Character Set identifier
			 121 2 Reserved
			 123 2 Number of Volumes in Album
			 125 2 Reserved
			 127 2 Album Set Sequence number
			 129 2 Reserved
			 131 2 Logical Block size
			 133 4 Reserved
			 137 4 Path Table size
			 141 8 Reserved
			 149 4 Address of Path Table
			 153 38 Reserved
			 191 128 Album identifier
			 319 128 Publisher identifier
			 447 128 Data Preparer identifier
			 575 128 Application identifier
			 703 32 Copyright file name
			 735 5 Reserved
			 740 32 Abstract file name
			 772 5 Reserved
			 777 32 Bibliographic file name
			 809 5 Reserved
			 814 16 Creation date and time
			 830 1 Reserved
			 831 16 Modification date and time
			 847 1 Reserved
			 848 16 Expiration date and time
			 864 1 Reserved
			 865 16 Effective date and time
			 881 1 Reserved
			 882 1 File Structure Standard Version number
			 883 1 Reserved
			 884 512 Application use
			1396 653 Reserved                      */

			long startPosition = s.Position;
			byte[] buffer = new byte[ISOFile.SECTOR_SIZE];
			s.Position = startPosition - ISOFile.SECTOR_SIZE;

			// Read the entire structure
			s.Read(buffer, 0, ISOFile.SECTOR_SIZE);

			// Get the type
			this.Type = buffer[0];

			// Handle the primary volume information
			if (this.Type == 1)
			{
				this.VolumeFlags = buffer[7];
				this.SystemIdentifier = bc.ReadBytes(buffer, 8, LENGTH_SHORT_IDENTIFIER);
				this.VolumeIdentifier = bc.ReadBytes(buffer, 40, LENGTH_SHORT_IDENTIFIER);
				this.NumberOfSectors = bcBig.ReadIntValue(buffer, 84, 4);
				this.CodedCharSetIdent = bc.ReadBytes(buffer, 88, LENGTH_SHORT_IDENTIFIER);
				this.VolumeSetSize = bcBig.ReadIntValue(buffer, 122, 2);
				this.VolumeSequenceNumber = bcBig.ReadIntValue(buffer, 126, 2);
				this.SectorSize = bcBig.ReadIntValue(buffer, 130, 2);
				this.PathTableSize = bcBig.ReadIntValue(buffer, 136, 4);
				this.AddressOfPathTable = bcBig.ReadIntValue(buffer, 148, 4);

				this.VolumeSetIdentifier = bc.ReadBytes(buffer, 190, LENGTH_LONG_IDENTIFIER);
				this.PublisherIdentifier = bc.ReadBytes(buffer, 318, LENGTH_LONG_IDENTIFIER);
				this.DataPreparerIdentifier = bc.ReadBytes(buffer, 446, LENGTH_LONG_IDENTIFIER);
				this.ApplicationIdentifier = bc.ReadBytes(buffer, 574, LENGTH_LONG_IDENTIFIER);

				this.CopyrightFileIdentifier = bc.ReadBytes(buffer, 702, LENGTH_SHORT_IDENTIFIER);
				this.AbstractFileIdentifier = bc.ReadBytes(buffer, 739, LENGTH_SHORT_IDENTIFIER);
				this.BibliographicalFileIdentifier = bc.ReadBytes(buffer, 776, LENGTH_SHORT_IDENTIFIER);

				this.VolumeCreationDateTime = bc.ReadBytes(buffer, 813, 16);
				this.LastModifiedDateTime = bc.ReadBytes(buffer, 830, 16);
				this.ExpirationDateTime = bc.ReadBytes(buffer, 847, 16);
				this.EffectiveDateTime = bc.ReadBytes(buffer, 864, 16);

				// save current position
				long pos = s.Position;
				
				// get path table records
				s.Position = ISOFile.SECTOR_SIZE * this.AddressOfPathTable;
				ISOFile.CDIPathTable = CDIPathNode.ParsePathTable(s, this.PathTableSize);

				// read the root dir record
				s.Position = ISOFile.SECTOR_SIZE * ISOFile.CDIPathTable[0].DirectoryBlockAddress;
				s.Read(buffer, 0, ISOFile.SECTOR_SIZE);
				this.RootDirectoryRecord.Parse(buffer, 0);

				// go back to where we were
				s.Position = pos;
			}
		}

		/// <summary>
		/// Detect ISO9660
		/// </summary>
		public bool GetISO9660(byte[] buffer)
		{
			//zero 24-jun-2013 - validate ISO9660
			//  "CD001\x01"
			if (buffer[1] == 'C' && buffer[2] == 'D' && buffer[3] == '0' && buffer[4] == '0' && buffer[5] == '1' && buffer[6] == 0x01)
			{
				ISOFile.Format = ISOFile.ISOFormat.ISO9660;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Detect CD-I
		/// </summary>
		public bool GetCDI(byte[] buffer)
		{
			// CD-Interactive
			if (Encoding.ASCII.GetString(bc.ReadBytes(buffer, 1, 5)).Contains("CD-I"))
			{
				ISOFile.Format = ISOFile.ISOFormat.CDInteractive;
				return true;
			}

			return false;
		}        





		/// <summary>
		/// Returns true if this is the terminator volume descriptor.
		/// </summary>
		/// <returns>True if the terminator.</returns>
		public bool IsTerminator()
		{
			return (this.Type == 255);
		}


	}

	/// <summary>
	/// Represents a Directory Path Table entry on a CD-I disc
	/// </summary>
	public class CDIPathNode
	{


		/// <summary>
		/// The length of the directory name.
		/// </summary>
		public byte NameLength;

		/// <summary>
		/// This is the length of the Extended Attribute record
		/// </summary>
		public byte ExtendedAttribRecordLength;

		/// <summary>
		/// This field contains the beginning logical block number (LBN) of the directory file on disc
		/// </summary>
		public int DirectoryBlockAddress;

		/// <summary>
		/// This is the number (relative to the beginning of the Path Table) of this directory's parent
		/// </summary>
		public int ParentDirectoryNumber;

		/// <summary>
		/// The directory name.
		/// This variable length field is used to store the actual text representing the name of the directory.
		/// If the length of the file name is odd, a null padding byte is added to make the size of the Path Table record even.
		/// The padding byte is not included in the name size field.
		/// </summary>
		public string Name;





		/// <summary>
		/// Empty Constructor
		/// </summary>
		public CDIPathNode()
		{

		}





		/*
			BP  Size in bytes   Description
			1   1               Name size
			2   1               Extended Attribute record length
			3   4               Directory block address
			7   2               Parent Directory number
			9   n               Directory file name 
		*/

		public static List<CDIPathNode> ParsePathTable(Stream s, int PathTableSize)
		{
			EndianBitConverter bc = EndianBitConverter.CreateForLittleEndian();
			EndianBitConverter bcBig = EndianBitConverter.CreateForBigEndian();

			byte[] buffer = new byte[ISOFile.SECTOR_SIZE];

			// Read the entire structure
			s.Read(buffer, 0, ISOFile.SECTOR_SIZE);

			int startCursor = 0;

			List<CDIPathNode> pathNodes = new List<CDIPathNode>();

			int pad = 0;

			do
			{
				CDIPathNode node = new CDIPathNode();
				byte[] data = bc.ReadBytes(buffer, startCursor, ISOFile.SECTOR_SIZE - startCursor);
				node.NameLength = data[0];                

				node.ExtendedAttribRecordLength = data[1];
				node.DirectoryBlockAddress = bcBig.ReadIntValue(data, 2, 4);
				node.ParentDirectoryNumber = bcBig.ReadIntValue(data, 6, 2);
				node.Name = Encoding.ASCII.GetString(bc.ReadBytes(data, 8, data[0]));

				// if nameLength is odd a padding byte must be added
				
				if (node.NameLength % 2 != 0)
					pad = 1;

				pathNodes.Add(node);

				startCursor += node.NameLength + 8;

			} while (startCursor < PathTableSize + pad);


			return pathNodes;
		}


	}
}
