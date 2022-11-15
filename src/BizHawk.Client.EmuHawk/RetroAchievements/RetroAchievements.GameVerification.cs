using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Client.EmuHawk
{
	public abstract partial class RetroAchievements
	{
		protected bool AllGamesVerified { get; set; }
		protected abstract int IdentifyHash(string hash);
		protected abstract int IdentifyRom(byte[] rom);

		private int? HashDisc(string path, ConsoleID consoleID, int discCount)
		{
			// this shouldn't throw in practice, this is only called when loading was successful!
			using var disc = DiscExtensions.CreateAnyType(path, e => throw new Exception(e));
			var dsr = new DiscSectorReader(disc)
			{
				Policy = { DeterministicClearBuffer = false } // let's make this a little faster
			};

			var buf2048 = new byte[2048];
			var buffer = new List<byte>();

			switch (consoleID)
			{
				case ConsoleID.PCEngineCD:
					{
						dsr.ReadLBA_2048(1, buf2048, 0);
						buffer.AddRange(new ArraySegment<byte>(buf2048, 128 - 22, 22));
						var bootSector = (buf2048[2] << 16) | (buf2048[1] << 8) | buf2048[0];
						var numSectors = buf2048[3];
						for (int i = 0; i < numSectors; i++)
						{
							dsr.ReadLBA_2048(bootSector + i, buf2048, 0);
							buffer.AddRange(buf2048);
						}
						break;
					}
				case ConsoleID.PCFX:
					{
						dsr.ReadLBA_2048(1, buf2048, 0);
						buffer.AddRange(new ArraySegment<byte>(buf2048, 0, 128));
						var bootSector = (buf2048[35] << 24) | (buf2048[34] << 16) | (buf2048[33] << 8) | buf2048[32];
						var numSectors = (buf2048[39] << 24) | (buf2048[38] << 16) | (buf2048[37] << 8) | buf2048[36];
						for (int i = 0; i < numSectors; i++)
						{
							dsr.ReadLBA_2048(bootSector + i, buf2048, 0);
							buffer.AddRange(buf2048);
						}
						break;
					}
				case ConsoleID.PlayStation:
					{
						int GetFileSector(string filename, out int filesize)
						{
							dsr.ReadLBA_2048(16, buf2048, 0);
							var sector = (buf2048[160] << 16) | (buf2048[159] << 8) | buf2048[158];
							dsr.ReadLBA_2048(sector, buf2048, 0);
							var index = 0;
							while ((index + 33 + filename.Length) < 2048)
							{
								var term = buf2048[index + 33 + filename.Length];
								if (term == ';' || term == '\0')
								{
									var fn = Encoding.ASCII.GetString(buf2048, index + 33, filename.Length);
									if (filename == fn)
									{
										filesize = (buf2048[index + 13] << 24) | (buf2048[index + 12] << 16) | (buf2048[index + 11] << 8) | buf2048[index + 10];
										return (buf2048[index + 4] << 16) | (buf2048[index + 3] << 8) | buf2048[index + 2];
									}
								}
								index += buf2048[index];
							}

							filesize = 0;
							return -1;
						}

						string exePath = "PSX.EXE";

						// find SYSTEM.CNF sector
						var sector = GetFileSector("SYSTEM.CNF", out _);
						if (sector > 0)
						{
							// read SYSTEM.CNF sector
							dsr.ReadLBA_2048(sector, buf2048, 0);
							exePath = Encoding.ASCII.GetString(buf2048);

							// "BOOT = cdrom:\" precedes the path
							var index = exePath.IndexOf("BOOT = cdrom:\\");
							if (index < 0) break;
							exePath = exePath.Remove(0, index + 14);

							// end of the path has ;
							var end = exePath.IndexOf(';');
							if (end < 0) break;
							exePath = exePath.Substring(0, end);
						}

						buffer.AddRange(Encoding.ASCII.GetBytes(exePath));

						// get the filename
						// valid too if -1, as that means we already have the filename
						var start = exePath.LastIndexOf('\\');
						if (start > 0)
						{
							exePath = exePath.Remove(0, start + 1);
						}

						// get sector for exe
						sector = GetFileSector(exePath, out var exeSize);
						if (sector < 0) break;

						dsr.ReadLBA_2048(sector++, buf2048, 0);

						if ("PS-X EXE" == Encoding.ASCII.GetString(buf2048, 0, 8))
						{
							exeSize = ((buf2048[31] << 24) | (buf2048[30] << 16) | (buf2048[29] << 8) | buf2048[28]) + 2048;
						}

						buffer.AddRange(new ArraySegment<byte>(buf2048, 0, Math.Min(2048, exeSize)));
						exeSize -= 2048;

						while (exeSize > 0)
						{
							dsr.ReadLBA_2048(sector++, buf2048, 0);
							buffer.AddRange(new ArraySegment<byte>(buf2048, 0, Math.Min(2048, exeSize)));
							exeSize -= 2048;
						}

						break;
					}
				case ConsoleID.SegaCD:
				case ConsoleID.Saturn:
					dsr.ReadLBA_2048(0, buf2048, 0);
					buffer.AddRange(new ArraySegment<byte>(buf2048, 0, 512));
					break;
				case ConsoleID.JaguarCD:
					if (discCount == 2) // we want to hash the second session of the disc (which is hacked to be disc 2)
					{
						const string _jaguarHeader = "ATARI APPROVED DATA HEADER ATRI ";
						const string _jaguarBSHeader = "TARA IPARPVODED TA AEHDAREA RT I";
						var buf2352 = new byte[2352];

						// find the boot track header
						// see https://github.com/TASEmulators/BizHawk/blob/f29113287e88c6a644dbff30f92a9833307aad20/waterbox/virtualjaguar/src/cdhle.cpp#L109-L145
						var startLba = disc.Session1.FirstInformationTrack.LBA;
						var numLbas = disc.Session1.FirstInformationTrack.NextTrack.LBA - disc.Session1.FirstInformationTrack.LBA;
						int bootLen = 0, bootLba = 0, bootOff = 0;
						bool byteswapped = false, foundHeader = false;
						for (int i = 0; i < numLbas; i++)
						{
							dsr.ReadLBA_2352(startLba + i, buf2352, 0);

							for (int j = 0; j < (2352 - 32 - 4 - 4); j++)
							{
								if (buf2352[j] == _jaguarHeader[0])
								{
									if (_jaguarHeader == Encoding.ASCII.GetString(buf2352, j, 32))
									{
										bootLen = (buf2352[j + 36] << 24) | (buf2352[j + 37] << 16) | (buf2352[j + 38] << 8) | buf2352[j + 39];
										bootLba = startLba + i;
										bootOff = j + 32 + 4 + 4;
										byteswapped = false;
										foundHeader = true;
										break;
									}
								}
								else if (buf2352[j] == _jaguarBSHeader[0])
								{
									if (_jaguarBSHeader == Encoding.ASCII.GetString(buf2352, j, 32))
									{
										bootLen = (buf2352[j + 37] << 24) | (buf2352[j + 36] << 16) | (buf2352[j + 39] << 8) | buf2352[j + 38];
										bootLba = startLba + i;
										bootOff = j + 32 + 4 + 4;
										byteswapped = true;
										foundHeader = true;
										break;
									}
								}
							}

							if (foundHeader)
							{
								break;
							}
						}

						if (!foundHeader)
						{
							return 0;
						}

						dsr.ReadLBA_2352(bootLba++, buf2352, 0);

						if (byteswapped)
						{
							EndiannessUtils.MutatingByteSwap16(buf2352.AsSpan());
						}

						buffer.AddRange(new ArraySegment<byte>(buf2352, bootOff, Math.Min(2352 - bootOff, bootLen)));
						bootLen -= 2352 - bootOff;

						while (bootLen > 0)
						{
							dsr.ReadLBA_2352(bootLba++, buf2352, 0);

							if (byteswapped)
							{
								EndiannessUtils.MutatingByteSwap16(buf2352.AsSpan());
							}

							buffer.AddRange(new ArraySegment<byte>(buf2352, 0, Math.Min(2352, bootLen)));
							bootLen -= 2352;
						}

						break;
					}
					else
					{
						return null; // other sessions aren't hashed, ignore them
					}
			}

			var hash = MD5Checksum.ComputeDigestHex(buffer.ToArray());
			return IdentifyHash(hash);
		}

		private int HashArcade(string path)
		{
			// Arcade wants to just hash the filename (with no extension)
			var name = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(path));
			var hash = MD5Checksum.ComputeDigestHex(name);
			return IdentifyHash(hash);
		}

		protected IReadOnlyList<int> GetRAGameIds(IOpenAdvanced ioa, ConsoleID consoleID)
		{
			var ret = new List<int>();
			switch (ioa.TypeName)
			{
				case OpenAdvancedTypes.OpenRom:
					{
						var ext = Path.GetExtension(Path.GetExtension(ioa.SimplePath.Replace("|", "")).ToLowerInvariant());
						var discCount = 0;

						if (ext == ".m3u")
						{
							using var file = new HawkFile(ioa.SimplePath);
							using var sr = new StreamReader(file.GetStream());
							var m3u = M3U_File.Read(sr);
							m3u.Rebase(Path.GetDirectoryName(ioa.SimplePath));
							foreach (var entry in m3u.Entries)
							{
								var id = HashDisc(entry.Path, consoleID, ++discCount);
								if (id.HasValue)
								{
									ret.Add(id.Value);
								}
							}
						}
						else if (ext == ".xml")
						{
							var xml = XmlGame.Create(new HawkFile(ioa.SimplePath));
							foreach (var kvp in xml.Assets)
							{
								if (consoleID is ConsoleID.Arcade)
								{
									ret.Add(HashArcade(kvp.Key));
									break;
								}

								if (Disc.IsValidExtension(Path.GetExtension(kvp.Key)))
								{
									var id = HashDisc(kvp.Key, consoleID, ++discCount);
									if (id.HasValue)
									{
										ret.Add(id.Value);
									}
								}
								else
								{
									ret.Add(IdentifyRom(kvp.Value));
								}
							}
						}
						else
						{
							if (consoleID is ConsoleID.Arcade)
							{
								ret.Add(HashArcade(ioa.SimplePath));
								break;
							}

							if (Disc.IsValidExtension(Path.GetExtension(ext)))
							{
								var id = HashDisc(ioa.SimplePath, consoleID, ++discCount);
								if (id.HasValue)
								{
									ret.Add(id.Value);
								}
							}
							else
							{
								using var file = new HawkFile(ioa.SimplePath);
								var rom = file.ReadAllBytes();
								ret.Add(IdentifyRom(rom));
							}
						}
						break;
					}
				case OpenAdvancedTypes.MAME:
					{
						ret.Add(HashArcade(ioa.SimplePath));
						break;
					}
				case OpenAdvancedTypes.LibretroNoGame:
					// nothing to hash here
					break;
				case OpenAdvancedTypes.Libretro:
					{
						// can't know what's here exactly, so we'll just hash the entire thing
						using var file = new HawkFile(ioa.SimplePath);
						var rom = file.ReadAllBytes();
						ret.Add(IdentifyRom(rom));
						break;
					}
			}

			return ret.AsReadOnly();
		}
	}
}
