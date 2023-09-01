using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Media
{
	public class DiskBuilder
	{
		public enum FileType
		{
			Deleted = 0,
			Sequential = 1,
			Program = 2,
			User = 3,
			Relative = 4
		}

		protected class BamEntry
		{
			public int Data { get; private set; }
			public int Sectors { get; }

			public BamEntry(int sectors)
			{
				Data = 0;
				for (int i = 0; i < sectors; i++)
				{
					Data >>= 1;
					Data |= 0x800000;
				}

				Data |= (sectors << 24);
				Sectors = sectors;
			}

			private int GetBit(int sector)
			{
				if (sector < 0 || sector >= Sectors)
				{
					return 0;
				}
				return 0x800000 >> sector;
			}

			public void Allocate(int sector)
			{
				int bit = GetBit(sector);
				if (bit != 0 && (Data & bit) != 0)
				{
					Data &= ~bit;
					Data -= 0x1000000;
				}
			}

			public void Free(int sector)
			{
				int bit = GetBit(sector);
				if (bit != 0 && (Data & bit) == 0)
				{
					Data |= bit;
					Data += 0x1000000;
				}
			}

			public int SectorsRemaining => (Data >> 24) & 0xFF;

			public bool this[int sector]
			{
				get => (Data & (1 << sector)) != 0;
				set
				{
					if (value)
					{
						Free(sector);
					}
					else
					{
						Allocate(sector);
					}
				}
			}

			public byte[] GetBytes() => GetBytesEnumerable().ToArray();

			private IEnumerable<byte> GetBytesEnumerable()
			{
				yield return unchecked((byte)(Data >> 24));
				yield return unchecked((byte)(Data >> 16));
				yield return unchecked((byte)(Data >> 8));
				yield return unchecked((byte)Data);
			}

			public IEnumerable<bool> Entries
			{
				get
				{
					int d = Data;
					for (int i = 0; i < Sectors; i++)
					{
						d <<= 1;
						yield return (d & 0x1000000) != 0;
					}
				}
			}
		}

		protected class LocatedEntry
		{
			public Entry Entry { get; set; }
			public int DirectoryTrack { get; set; }
			public int DirectorySector { get; set; }
			public int Track { get; set; }
			public int Sector { get; set; }
			public int SideTrack { get; set; }
			public int SideSector { get; set; }
			public int LengthInSectors { get; set; }
		}

		public class Entry
		{
			public FileType Type { get; set; }
			public bool Locked { get; set; }
			public bool Closed { get; set; }
			public string Name { get; set; }
			public int RecordLength { get; set; }
			public byte[] Data { get; set; }
		}

		private static readonly int[] SectorsPerTrack =
		{
			21, 21, 21, 21, 21,
			21, 21, 21, 21, 21,
			21, 21, 21, 21, 21,
			21, 21, 19, 19, 19,
			19, 19, 19, 19, 18,
			18, 18, 18, 18, 18,
			17, 17, 17, 17, 17,
			17, 17, 17, 17, 17
		};

		public List<Entry> Entries { get; set; }
		public int VersionType { get; set; }
		public string Title { get; set; }

		public DiskBuilder()
		{
			Entries = new List<Entry>();
			VersionType = 0x41;
		}

		public Disk Build()
		{
			const int tracks = 35;
			int[] trackByteOffsets = new int[tracks];
			BamEntry[] bam = new BamEntry[tracks];
			bool diskFull = false;

			for (int i = 0; i < tracks; i++)
			{
				bam[i] = new BamEntry(SectorsPerTrack[i]);
				if (i > 0)
				{
					trackByteOffsets[i] = trackByteOffsets[i - 1] + (SectorsPerTrack[i - 1] * 256);
				}
			}
			byte[] bytes = new byte[trackByteOffsets[tracks - 1] + (SectorsPerTrack[tracks - 1] * 256)];

			int currentTrack = 16;
			int currentSector = 0;
			int interleaveStart = 0;
			int sectorInterleave = 3;
			List<LocatedEntry> directory = new();

			int GetOutputOffset(int t, int s) => trackByteOffsets[t] + (s * 256);

			foreach (var entry in Entries)
			{
				int sourceOffset = 0;
				int dataLength = entry.Data == null ? 0 : entry.Data.Length;
				int lengthInSectors = dataLength / 254;
				int dataRemaining = dataLength;
				LocatedEntry directoryEntry = new()
				{
					Entry = entry,
					LengthInSectors = lengthInSectors + 1,
					Track = currentTrack,
					Sector = currentSector
				};
				directory.Add(directoryEntry);

				while (!diskFull)
				{
					int outputOffset = GetOutputOffset(currentTrack, currentSector);

					if (dataRemaining > 254)
					{
						Array.Copy(entry.Data, sourceOffset, bytes, outputOffset + 2, 254);
						dataRemaining -= 254;
						sourceOffset += 254;
					}
					else
					{
						if (dataRemaining > 0)
						{
							Array.Copy(entry.Data, sourceOffset, bytes, outputOffset + 2, dataRemaining);
							bytes[outputOffset + 0] = 0;
							bytes[outputOffset + 1] = (byte)(dataRemaining + 1);
							dataRemaining = 0;
						}
					}

					bam[currentTrack].Allocate(currentSector);
					currentSector += sectorInterleave;
					if (currentSector >= SectorsPerTrack[currentTrack])
					{
						interleaveStart++;
						if (interleaveStart >= sectorInterleave)
						{
							interleaveStart = 0;
							if (currentTrack >= 17)
							{
								currentTrack++;
								if (currentTrack >= 35)
								{
									diskFull = true;
									break;
								}
							}
							else
							{
								currentTrack--;
								if (currentTrack < 0)
									currentTrack = 18;
							}
						}
						currentSector = interleaveStart;
					}

					if (dataRemaining <= 0)
					{
						break;
					}

					bytes[outputOffset + 0] = (byte)(currentTrack + 1);
					bytes[outputOffset + 1] = (byte)currentSector;
				}

				if (diskFull)
				{
					break;
				}
			}

			// write Directory
			int directoryOffset = -(0x20);
			currentTrack = 17;
			currentSector = 1;
			int directoryOutputOffset = GetOutputOffset(currentTrack, currentSector);
			int fileIndex = 0;
			bam[currentTrack].Allocate(currentSector);
			foreach (var entry in directory)
			{
				directoryOffset += 0x20;
				if (directoryOffset == 0x100)
				{
					directoryOffset = 0;
					currentSector += 3;
					bytes[directoryOutputOffset] = (byte)currentTrack;
					bytes[directoryOutputOffset + 1] = (byte)currentSector;
					directoryOutputOffset = GetOutputOffset(currentTrack, currentSector);
					bam[currentTrack].Allocate(currentSector);
				}
				bytes[directoryOutputOffset + directoryOffset + 0x00] = 0x00;
				bytes[directoryOutputOffset + directoryOffset + 0x01] = 0x00;
				bytes[directoryOutputOffset + directoryOffset + 0x02] = (byte)((int)entry.Entry.Type | (entry.Entry.Locked ? 0x40 : 0x00) | (entry.Entry.Closed ? 0x80 : 0x00));
				bytes[directoryOutputOffset + directoryOffset + 0x03] = (byte)(entry.Track + 1);
				bytes[directoryOutputOffset + directoryOffset + 0x04] = (byte)entry.Sector;
				for (int i = 0x05; i <= 0x14; i++)
				{
					bytes[directoryOutputOffset + directoryOffset + i] = 0xA0;
				}

				byte[] fileNameBytes = Encoding.ASCII.GetBytes(entry.Entry.Name ?? $"FILE{fileIndex:D3}");
				Array.Copy(fileNameBytes, 0, bytes, directoryOutputOffset + directoryOffset + 0x05, Math.Min(fileNameBytes.Length, 0x10));
				bytes[directoryOutputOffset + directoryOffset + 0x1E] = (byte)(entry.LengthInSectors & 0xFF);
				bytes[directoryOutputOffset + directoryOffset + 0x1F] = (byte)((entry.LengthInSectors >> 8) & 0xFF);
				fileIndex++;
			}
			bytes[directoryOutputOffset + 0x00] = 0x00;
			bytes[directoryOutputOffset + 0x01] = 0xFF;

			// write BAM
			int bamOutputOffset = GetOutputOffset(17, 0);
			bytes[bamOutputOffset + 0x00] = 18;
			bytes[bamOutputOffset + 0x01] = 1;
			bytes[bamOutputOffset + 0x02] = (byte)VersionType;
			for (int i = 0; i < 35; i++)
			{
				Array.Copy(bam[i].GetBytes(), 0, bytes, bamOutputOffset + 4 + (i * 4), 4);
			}

			for (int i = 0x90; i <= 0xAA; i++)
			{
				bytes[bamOutputOffset + i] = 0xA0;
			}

			byte[] titleBytes = Encoding.ASCII.GetBytes(Title ?? "UNTITLED");
			Array.Copy(titleBytes, 0, bytes, bamOutputOffset + 0x90, Math.Min(titleBytes.Length, 0x10));

			return D64.Read(bytes);
		}
	}
}
