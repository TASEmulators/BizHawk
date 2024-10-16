using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.Common;
using BizHawk.Client.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Client.EmuHawk
{
	public abstract partial class RetroAchievements
	{
		protected bool AllGamesVerified { get; set; }
		protected abstract uint IdentifyHash(string hash);
		protected abstract uint IdentifyRom(byte[] rom);

		private uint HashDisc(string path, ConsoleID consoleID)
		{
			// this shouldn't throw in practice, this is only called when loading was successful!
			using var disc = DiscExtensions.CreateAnyType(path, e => throw new(e));
			var dsr = new DiscSectorReader(disc)
			{
				Policy =
				{
					UserData2048Mode = DiscSectorReaderPolicy.EUserData2048Mode.InspectSector_AssumeForm1,
					DeterministicClearBuffer = false // let's make this a little faster
				}
			};

			var buf2048 = new byte[2048];
			var buffer = new List<byte>();

			int FirstDataTrackLBA()
			{
				var toc = disc.TOC;
				for (var t = toc.FirstRecordedTrackNumber; t <= toc.LastRecordedTrackNumber; t++)
				{
					if (toc.TOCItems[t]
						.IsData) return toc.TOCItems[t].LBA;
				}

				throw new InvalidOperationException("Could not find first data track for hashing");
			}

			switch (consoleID)
			{
				case ConsoleID.PCEngineCD:
					{
						var slba = FirstDataTrackLBA();
						dsr.ReadLBA_2048(slba + 1, buf2048, 0);
						buffer.AddRange(new ArraySegment<byte>(buf2048, 128 - 22, 22));
						var bootSector = (buf2048[0] << 16) | (buf2048[1] << 8) | buf2048[2];
						var numSectors = buf2048[3];
						for (var i = 0; i < numSectors; i++)
						{
							dsr.ReadLBA_2048(slba + bootSector + i, buf2048, 0);
							buffer.AddRange(buf2048);
						}
						break;
					}
				case ConsoleID.PCFX:
					{
						var slba = FirstDataTrackLBA();
						dsr.ReadLBA_2048(slba + 1, buf2048, 0);
						buffer.AddRange(new ArraySegment<byte>(buf2048, 0, 128));
						var bootSector = (buf2048[35] << 24) | (buf2048[34] << 16) | (buf2048[33] << 8) | buf2048[32];
						var numSectors = (buf2048[39] << 24) | (buf2048[38] << 16) | (buf2048[37] << 8) | buf2048[36];
						for (var i = 0; i < numSectors; i++)
						{
							dsr.ReadLBA_2048(slba + bootSector + i, buf2048, 0);
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
							while (index + 33 + filename.Length < 2048)
							{
								var term = buf2048[index + 33 + filename.Length];
								if (term == ';' || term == '\0')
								{
									var fn = Encoding.ASCII.GetString(buf2048, index + 33, filename.Length);
									if (filename.Equals(fn, StringComparison.OrdinalIgnoreCase))
									{
										filesize = (buf2048[index + 13] << 24) | (buf2048[index + 12] << 16) | (buf2048[index + 11] << 8) | buf2048[index + 10];
										return (buf2048[index + 4] << 16) | (buf2048[index + 3] << 8) | buf2048[index + 2];
									}
								}

								// move on to next sector if filename not found in current sector due to subdirectory
								if (buf2048[index] == 0)
								{
									dsr.ReadLBA_2048(++sector, buf2048, 0);
									index = 0;
									continue;
								}

								index += buf2048[index];
							}

							filesize = 0;
							return -1;
						}

						var exePath = "PSX.EXE";

						// find SYSTEM.CNF sector
						var sector = GetFileSector("SYSTEM.CNF", out _);
						if (sector > 0)
						{
							// read SYSTEM.CNF sector
							dsr.ReadLBA_2048(sector, buf2048, 0);
							exePath = Encoding.ASCII.GetString(buf2048);

							// replace any tab characters with space characters to normalize exePath
							exePath = exePath.Replace('\t', ' ');

							// "BOOT = cdrom:" precedes the path
							var index = exePath.IndexOf("BOOT = cdrom:", StringComparison.Ordinal);
							if (index < -1) break;
							exePath = exePath.Remove(0, index + 13);

							// the path might start with a number of slashes, remove these
							index = 0;
							while (index < exePath.Length && exePath[index] is '\\') index++;

							// end of the path has ;
							var end = exePath.IndexOf(';');
							if (end < 0) break;
							exePath = exePath.Substring(startIndex: index, length: end - index);
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
					var discHasher = new DiscHasher(disc);
					return discHasher.CalculateRAJaguarHash() switch
					{
						string jaguarHash => IdentifyHash(jaguarHash),
						null => 0,
					};
			}

			var hash = MD5Checksum.ComputeDigestHex(buffer.ToArray());
			return IdentifyHash(hash);
		}

		private uint HashArcade(string path)
		{
			// Arcade wants to just hash the filename (with no extension)
			var name = Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(path));
			var hash = MD5Checksum.ComputeDigestHex(name);
			return IdentifyHash(hash);
		}

		// Stuff needed for 3DS hashing...
		private readonly LibRCheevos.rc_hash_3ds_get_cia_normal_key_func _getCiaNormalKeyFunc;
		private readonly LibRCheevos.rc_hash_3ds_get_ncch_normal_keys_func _getNcchNormalKeysFunc;
		// https://github.com/CasualPokePlayer/encore/blob/2b20082581906fe973e26ed36bef695aa1f64527/src/core/hw/aes/key.cpp#L23-L30
		private static readonly BigInteger GENERATOR_CONSTANT = BigInteger.Parse("1FF9E9AAC5FE0408024591DC5D52768A", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
		private static readonly BigInteger U128_MAX = BigInteger.Parse("0FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture);

		private static byte[] Derive3DSNormalKey(BigInteger keyX, BigInteger keyY)
		{
			static BigInteger LeftRot128(BigInteger v, int rot)
			{
				var l = (v << rot) & U128_MAX;
				var r = v >> (128 - rot);
				return l | r;
			}

			static BigInteger Add128(BigInteger v1, BigInteger v2)
				=> (v1 + v2) & U128_MAX;

			var normalKey = LeftRot128(Add128(LeftRot128(keyX, 2) ^ keyY, GENERATOR_CONSTANT), 87);
			var normalKeyBytes = normalKey.ToByteArray();
			if (normalKeyBytes.Length > 17)
			{
				// this shoudn't ever happen
				throw new InvalidOperationException();
			}

			// get rid of a final trailing 0
			// but also make sure we have 0 paddng to 16 bytes
			Array.Resize(ref normalKeyBytes, 16);

			// .ToByteArray() is always in little endian order, but we want big endian order
			Array.Reverse(normalKeyBytes);
			return normalKeyBytes;
		}

		private MemoryStream GetFirmware(FirmwareID id)
		{
			var record = FirmwareDatabase.FirmwareRecords.First(fr => fr.ID == id);
			var resolved = _mainForm.FirmwareManager.Resolve(_getConfig().PathEntries, _getConfig().FirmwareUserSpecifications, record);
			if (resolved?.FilePath == null) throw new InvalidOperationException();
			return new(File.ReadAllBytes(resolved.FilePath), writable: false);
		}

		private (BigInteger Key1, BigInteger Key2) FindAesKeys(string key1Prefix, string key2Prefix)
		{
			using var keys = new StreamReader(GetFirmware(new("3DS", "aes_keys")), Encoding.UTF8);
			string key1Str = null, key2Str = null;
			while ((key1Str is null || key2Str is null) && keys.ReadLine() is { } line)
			{
				if (line.Length == 0 || line.StartsWith('#'))
				{
					continue;
				}

				var eqpos = line.IndexOf('=');
				if (eqpos == -1 || eqpos != line.LastIndexOf('='))
				{
					throw new InvalidOperationException("Malformed key list");
				}

				if (key1Str is null)
				{
					if (line.StartsWithOrdinal(key1Prefix))
					{
						key1Str = line[(eqpos + 1)..];
						if (key1Str.Length != 32)
						{
							throw new InvalidOperationException("Invalid key length");
						}
					}
				}

				if (key2Str is null)
				{
					if (line.StartsWithOrdinal(key2Prefix))
					{
						key2Str = line[(eqpos + 1)..];
						if (key2Str.Length != 32)
						{
							throw new InvalidOperationException("Invalid key length");
						}
					}
				}
			}

			if (key1Str is null || key2Str is null)
			{
				throw new InvalidOperationException("Couldn't find requested keys");
			}

			var key1 = BigInteger.Parse($"0{key1Str}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var key2 = BigInteger.Parse($"0{key2Str}", NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			return (key1, key2);
		}

		private bool GetCiaNormalKeyFunc(byte common_key_index, IntPtr out_normal_key)
		{
			if (common_key_index > 5)
			{
				return false;
			}

			try
			{
				var (keyX, keyY) = FindAesKeys("slot0x3DKeyX=", $"common{common_key_index}=");
				Marshal.Copy(Derive3DSNormalKey(keyX, keyY), 0, out_normal_key, 16);
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return false;
			}
		}

		private bool GetNcchNormalKeysFunc(IntPtr primary_key_y, byte secondary_key_x_slot, IntPtr optional_program_id, IntPtr out_primary_key, IntPtr out_secondary_key)
		{
			if (secondary_key_x_slot is not (0x2C or 0x25 or 0x18 or 0x1B))
			{
				return false;
			}

			try
			{
				var (primaryKeyX, secondaryKeyX) = FindAesKeys("slot0x2CKeyX=", $"slot0x{secondary_key_x_slot:X2}KeyX=");

				var primaryKeyYBytes = new byte[17];
				Marshal.Copy(primary_key_y, primaryKeyYBytes, 1, 16);
				Array.Reverse(primaryKeyYBytes); // convert big endian to little endian
				var primaryKeyY = new BigInteger(primaryKeyYBytes);

				Marshal.Copy(Derive3DSNormalKey(primaryKeyX, primaryKeyY), 0, out_primary_key, 16);

				if (optional_program_id == IntPtr.Zero)
				{
					Marshal.Copy(Derive3DSNormalKey(secondaryKeyX, primaryKeyY), 0, out_secondary_key, 16);
					return true;
				}

				var programId = MemoryMarshal.Read<ulong>(
					Util.UnsafeSpanFromPointer<byte>(ptr: optional_program_id, count: 8));

				FirmwareID seeddbFWID = new("3DS", "seeddb");
				using BinaryReader seeddb = new(GetFirmware(seeddbFWID));
				var count = seeddb.ReadUInt32();
				seeddb.BaseStream.Seek(12, SeekOrigin.Current); // apparently some padding bytes before actual seeds
				for (long i = 0; i < count; i++)
				{
					var titleId = seeddb.ReadUInt64();
					if (titleId != programId)
					{
						seeddb.BaseStream.Seek(24, SeekOrigin.Current);
						continue;
					}

					var sha256Input = new byte[32];
					Marshal.Copy(primary_key_y, sha256Input, 0, 16);
					var bytesRead = seeddb.BaseStream.Read(sha256Input, offset: 16, count: 16);
					Debug.Assert(bytesRead is 16, $"reached end-of-file while reading {seeddbFWID} firmware");
					var sha256Digest = SHA256Checksum.Compute(sha256Input);

					var secondaryKeyYBytes = new byte[17];
					Buffer.BlockCopy(sha256Digest, 0, secondaryKeyYBytes, 1, 16);
					Array.Reverse(secondaryKeyYBytes); // convert big endian to little endian
					var secondaryKeyY = new BigInteger(secondaryKeyYBytes);
					Marshal.Copy(Derive3DSNormalKey(secondaryKeyX, secondaryKeyY), 0, out_secondary_key, 16);
					return true;
				}

				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return false;
			}
		}

		private uint Hash3DS(string path)
		{
			// 3DS is too big to hash as a byte array...
			var hash = new byte[33];
			return RCheevos._lib.rc_hash_generate_from_file(hash, ConsoleID.Nintendo3DS, path)
				? IdentifyHash(Encoding.ASCII.GetString(hash, 0, 32)) : 0;
		}

		protected IReadOnlyList<uint> GetRAGameIds(IOpenAdvanced ioa, ConsoleID consoleID)
		{
			var ret = new List<uint>();
			switch (ioa.TypeName)
			{
				case OpenAdvancedTypes.OpenRom:
					{
						var ext = Path.GetExtension(Path.GetExtension(ioa.SimplePath.Replace("|", "")).ToLowerInvariant());

						if (ext == ".m3u")
						{
							using var file = new HawkFile(ioa.SimplePath);
							using var sr = new StreamReader(file.GetStream());
							var m3u = M3U_File.Read(sr);
							m3u.Rebase(Path.GetDirectoryName(ioa.SimplePath));
							ret.AddRange(m3u.Entries.Select(entry => HashDisc(entry.Path, consoleID)));
						}
						else if (ext == ".xml")
						{
							var xml = XmlGame.Create(new(ioa.SimplePath));
							foreach (var kvp in xml.Assets)
							{
								if (consoleID is ConsoleID.Arcade)
								{
									ret.Add(HashArcade(kvp.Key));
									break;
								}

								if (consoleID is ConsoleID.Nintendo3DS)
								{
									ret.Add(Hash3DS(kvp.Key));
									break;
								}

								ret.Add(Disc.IsValidExtension(Path.GetExtension(kvp.Key))
									? HashDisc(kvp.Key, consoleID)
									: IdentifyRom(kvp.Value));
							}
						}
						else
						{
							if (consoleID is ConsoleID.Arcade)
							{
								ret.Add(HashArcade(ioa.SimplePath));
								break;
							}

							if (consoleID is ConsoleID.Nintendo3DS)
							{
								ret.Add(Hash3DS(ioa.SimplePath));
								break;
							}

							if (Disc.IsValidExtension(Path.GetExtension(ext)))
							{
								ret.Add(HashDisc(ioa.SimplePath, consoleID));
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
