#nullable disable

using System.Linq;
using System.Text;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A slightly convoluted way of determining the required System based on a *.dsk file
	/// This is here because (for probably good reason) there does not appear to be a route
	/// to BizHawk.Emulation.Cores from BizHawk.Emulation.Common
	/// </summary>
	public class DskIdentifier
	{
		private readonly byte[] _data;
		private string _possibleIdent = "";

		/// <summary>
		/// Default fallthrough to AppleII - the AppleII *.dsk format seems to be very simple with no ident strings
		/// </summary>
		public string IdentifiedSystem { get; set; } = VSystemID.Raw.AppleII;

		// dsk header
		public byte NumberOfTracks { get; set; }
		public byte NumberOfSides { get; set; }
		public int[] TrackSizes { get; set; }

		// state
		public int SideCount { get; set; }
		public int BytesPerTrack { get; set; }

		public Track[] Tracks { get; set; }

		public DskIdentifier(byte[] imageData)
		{
			_data = imageData;
			ParseDskImage();
		}

		private void ParseDskImage()
		{
			string ident = Encoding.ASCII.GetString(_data, 0, 16).ToUpperInvariant();
			if (ident.Contains("MV - CPC"))
			{
				ParseDsk();
			}
			else if (ident.Contains("EXTENDED CPC DSK"))
			{
				ParseEDsk();
			}
			else
			{
				// fall through
				return;
			}

			CalculateFormat();
		}

		private void CalculateFormat()
		{
			// uses some of the work done here: https://github.com/damieng/DiskImageManager
			var trk = Tracks[0];

			// look for standard speccy bootstart
			if (trk.Sectors[0].SectorData != null && trk.Sectors[0].SectorData.Length > 0)
			{
				if (trk.Sectors[0].SectorData[0] == 0 && trk.Sectors[0].SectorData[1] == 0
					&& trk.Sectors[0].SectorData[2] == 40)
				{
					_possibleIdent = VSystemID.Raw.ZXSpectrum;
				}
			}

			// search for PLUS3DOS string
			foreach (var t in Tracks)
			{
				foreach (var s in t.Sectors)
				{
					if (s.SectorData == null || s.SectorData.Length == 0)
						continue;

					string str = Encoding.ASCII.GetString(s.SectorData, 0, s.SectorData.Length);
					if (str.Contains("PLUS3DOS", StringComparison.OrdinalIgnoreCase))
					{
						IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
						return;
					}
				}
			}

			// check for bootable status
			if (trk.Sectors[0].SectorData != null && trk.Sectors[0].SectorData.Length > 0)
			{
				switch (trk.Sectors[0].GetModChecksum256())
				{
					case 3:
						IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
						return;
					case 1:
					case 255:
						// different Amstrad PCW boot records
						// just return CPC for now
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						return;
				}

				switch (trk.GetLowestSectorID())
				{
					case 65:
					case 193:
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						return;
				}
			}

			// at this point the disk is not standard bootable
			// try format analysis			
			if (trk.Sectors.Length == 9 && trk.Sectors[0].SectorSize == 2)
			{
				switch (trk.GetLowestSectorID())
				{
					case 1:
						switch (trk.Sectors[0].GetModChecksum256())
						{
							case 3:
								IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
								return;
							case 1:
							case 255:
								// different Amstrad PCW checksums
								// just return CPC for now
								IdentifiedSystem = VSystemID.Raw.AmstradCPC;
								return;
						}
						break;
					case 65:
					case 193:
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						return;
				}
			}

			// could be an odd format disk
			switch (trk.GetLowestSectorID())
			{
				case 1:
					if (trk.Sectors.Length == 8)
					{
						// CPC IBM
						IdentifiedSystem = VSystemID.Raw.AmstradCPC;
						return;
					}
					break;
				case 65:
				case 193:
					// possible CPC custom
					_possibleIdent = VSystemID.Raw.AmstradCPC;
					break;
			}

			// other custom ZX Spectrum formats
			if (NumberOfSides == 1 && trk.Sectors.Length == 10)
			{
				if (trk.Sectors[0].SectorData.Length > 10)
				{
					if (trk.Sectors[0].SectorData[2] == 42 && trk.Sectors[0].SectorData[8] == 12)
					{
						switch (trk.Sectors[0].SectorData[5])
						{
							case 0:
								if (trk.Sectors[1].SectorID == 8)
								{
									switch (Tracks[1].Sectors[0].SectorID)
									{
										case 7:
											IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
											return;
										default:
											_possibleIdent = VSystemID.Raw.ZXSpectrum;
											break;
									}
								}
								else
								{
									_possibleIdent = VSystemID.Raw.ZXSpectrum;
								}
								break;
							case 1:
								if (trk.Sectors[1].SectorID == 8)
								{
									switch (Tracks[1].Sectors[0].SectorID)
									{
										case 7:
										case 1:
											IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
											return;
									}
								}
								else
								{
									_possibleIdent = VSystemID.Raw.ZXSpectrum;
								}
								break;
						}
					}

					if (trk.Sectors[0].SectorData[7] is 3
						&& trk.Sectors[0].SectorData[9] is 23
						&& trk.Sectors[0].SectorData[2] is 40)
					{
						IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
						return;
					}
				}
			}

			// last chance. use the possible value
			if (IdentifiedSystem == VSystemID.Raw.AppleII && _possibleIdent != "")
			{
				IdentifiedSystem = VSystemID.Raw.ZXSpectrum;
			}
		}

		private void ParseDsk()
		{
			NumberOfTracks = _data[0x30];
			NumberOfSides = _data[0x31];
			TrackSizes = new int[NumberOfTracks * NumberOfSides];
			Tracks = new Track[NumberOfTracks * NumberOfSides];
			int pos = 0x32;
			for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
			{
				TrackSizes[i] = (ushort)(_data[pos] | _data[pos + 1] << 8);
			}
			pos = 0x100;
			for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
			{
				if (TrackSizes[i] == 0)
				{
					Tracks[i] = new() { Sectors = Array.Empty<Sector>() };
					continue;
				}
				int p = pos;
				Tracks[i] = new Track
				{
					TrackIdent = Encoding.ASCII.GetString(_data, p, 12)
				};
				p += 16;
				Tracks[i].TrackNumber = _data[p++];
				Tracks[i].SideNumber = _data[p++];
				p += 2;
				Tracks[i].SectorSize = _data[p++];
				Tracks[i].NumberOfSectors = _data[p++];
				Tracks[i].Gap3Length = _data[p++];
				Tracks[i].FillerByte = _data[p++];
				int dPos = pos + 0x100;
				Tracks[i].Sectors = new Sector[Tracks[i].NumberOfSectors];
				for (int s = 0; s < Tracks[i].NumberOfSectors; s++)
				{
					Tracks[i].Sectors[s] = new Sector
					{
						TrackNumber = _data[p++],
						SideNumber = _data[p++],
						SectorID = _data[p++],
						SectorSize = _data[p++],
						Status1 = _data[p++],
						Status2 = _data[p++],
						ActualDataByteLength = (ushort) (_data[p] | _data[p + 1] << 8)
					};

					p += 2;
					if (Tracks[i].Sectors[s].SectorSize == 0)
					{
						Tracks[i].Sectors[s].ActualDataByteLength = TrackSizes[i];
					}
					else if (Tracks[i].Sectors[s].SectorSize > 6)
					{
						Tracks[i].Sectors[s].ActualDataByteLength = TrackSizes[i];
					}
					else if (Tracks[i].Sectors[s].SectorSize == 6)
					{
						Tracks[i].Sectors[s].ActualDataByteLength = 0x1800;
					}
					else
					{
						Tracks[i].Sectors[s].ActualDataByteLength = 0x80 << Tracks[i].Sectors[s].SectorSize;
					}
					Tracks[i].Sectors[s].SectorData = new byte[Tracks[i].Sectors[s].ActualDataByteLength];
					for (int b = 0; b < Tracks[i].Sectors[s].ActualDataByteLength; b++)
					{
						Tracks[i].Sectors[s].SectorData[b] = _data[dPos + b];
					}
					dPos += Tracks[i].Sectors[s].ActualDataByteLength;
				}
				pos += TrackSizes[i];
			}
		}

		private void ParseEDsk()
		{
			NumberOfTracks = _data[0x30];
			NumberOfSides = _data[0x31];
			TrackSizes = new int[NumberOfTracks * NumberOfSides];
			Tracks = new Track[NumberOfTracks * NumberOfSides];
			int pos = 0x34;
			for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
			{
				TrackSizes[i] = _data[pos++] * 256;
			}
			pos = 0x100;
			for (int i = 0; i < NumberOfTracks * NumberOfSides; i++)
			{
				if (TrackSizes[i] == 0)
				{
					Tracks[i] = new() { Sectors = Array.Empty<Sector>() };
					continue;
				}
				int p = pos;
				Tracks[i] = new Track { TrackIdent = Encoding.ASCII.GetString(_data, p, 12) };
				p += 16;
				Tracks[i].TrackNumber = _data[p++];
				Tracks[i].SideNumber = _data[p++];
				Tracks[i].DataRate = _data[p++];
				Tracks[i].RecordingMode = _data[p++];
				Tracks[i].SectorSize = _data[p++];
				Tracks[i].NumberOfSectors = _data[p++];
				Tracks[i].Gap3Length = _data[p++];
				Tracks[i].FillerByte = _data[p++];
				int dPos = pos + 0x100;
				Tracks[i].Sectors = new Sector[Tracks[i].NumberOfSectors];
				for (int s = 0; s < Tracks[i].NumberOfSectors; s++)
				{
					Tracks[i].Sectors[s] = new Sector
					{
						TrackNumber = _data[p++],
						SideNumber = _data[p++],
						SectorID = _data[p++],
						SectorSize = _data[p++],
						Status1 = _data[p++],
						Status2 = _data[p++],
						ActualDataByteLength = (ushort) (_data[p] | _data[p + 1] << 8)
					};

					p += 2;
					Tracks[i].Sectors[s].SectorData = new byte[Tracks[i].Sectors[s].ActualDataByteLength];
					for (int b = 0; b < Tracks[i].Sectors[s].ActualDataByteLength; b++)
					{
						Tracks[i].Sectors[s].SectorData[b] = _data[dPos + b];
					}
					if (Tracks[i].Sectors[s].SectorSize <= 7)
					{
						int specifiedSize = 0x80 << Tracks[i].Sectors[s].SectorSize;
						if (specifiedSize < Tracks[i].Sectors[s].ActualDataByteLength)
						{
							if (Tracks[i].Sectors[s].ActualDataByteLength % specifiedSize != 0)
							{
								Tracks[i].Sectors[s].ContainsMultipleWeakSectors = true;
							}
						}
					}
					dPos += Tracks[i].Sectors[s].ActualDataByteLength;
				}
				pos += TrackSizes[i];
			}
		}

		public class Track
		{
			public string TrackIdent { get; set; }
			public byte TrackNumber { get; set; }
			public byte SideNumber { get; set; }
			public byte DataRate { get; set; }
			public byte RecordingMode { get; set; }
			public byte SectorSize { get; set; }
			public byte NumberOfSectors { get; set; }
			public byte Gap3Length { get; set; }
			public byte FillerByte { get; set; }
			public Sector[] Sectors { get; set; }

			public byte GetLowestSectorID()
			{
				byte res = 0xFF;
				foreach (var s in Sectors)
				{
					if (s.SectorID < res)
					{
						res = s.SectorID;
					}
				}

				return res;
			}
		}

		public class Sector
		{
			public byte TrackNumber { get; set; }
			public byte SideNumber { get; set; }
			public byte SectorID { get; set; }
			public byte SectorSize { get; set; }
			public byte Status1 { get; set; }
			public byte Status2 { get; set; }
			public int ActualDataByteLength { get; set; }
			public byte[] SectorData { get; set; }
			public bool ContainsMultipleWeakSectors { get; set; }

			public byte GetModChecksum256() => (byte) (SectorData.Sum(static b => b) & 0xFF);
		}
	}
}
