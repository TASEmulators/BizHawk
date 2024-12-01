using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.DiscSystem;

#pragma warning disable BHI1007 // target-typed Exception TODO don't

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private static readonly LibRCheevos.rc_hash_message_callback _errorMessageCallback, _verboseMessageCallback;

		private static void ErrorMessageCallback(string message)
			=> Console.WriteLine($"[RC HASH ERROR] {message}");

		private static void VerboseMessageCallback(string message)
			=> Console.WriteLine($"[RC HASH VERBOSE] {message}");

		private static readonly LibRCheevos.rc_hash_filereader _filereader;
		private static readonly LibRCheevos.rc_hash_cdreader _cdreader;

		private static IntPtr OpenFileCallback(string utf8_path)
		{
			var file = new HawkFile(utf8_path);

			// this probably shouldn't ever happen
			if (!file.Exists || !file.IsBound || !file.GetStream().CanSeek || !file.GetStream().CanRead)
			{
				file.Dispose();
				return IntPtr.Zero;
			}

			var handle = GCHandle.Alloc(file, GCHandleType.Normal);
			return GCHandle.ToIntPtr(handle);
		}

		private static void SeekFileCallback(IntPtr file_handle, long offset, SeekOrigin origin)
		{
			var handle = GCHandle.FromIntPtr(file_handle);
			var file = (HawkFile)handle.Target;
			file.GetStream().Seek(offset, origin);
		}

		private static long TellFileCallback(IntPtr file_handle)
		{
			var handle = GCHandle.FromIntPtr(file_handle);
			var file = (HawkFile)handle.Target;
			return file.GetStream().Position;
		}

		private static nuint ReadFileCallback(IntPtr file_handle, IntPtr buffer, nuint requested_bytes)
		{
			var handle = GCHandle.FromIntPtr(file_handle);
			var file = (HawkFile)handle.Target;
			var stream = file.GetStream();

			// this is poop without spans
			const int TMP_BUFFER_LEN = 65536;
			var tmp = ArrayPool<byte>.Shared.Rent(TMP_BUFFER_LEN);
			try
			{
				var remainingBytes = requested_bytes;
				while (remainingBytes != 0)
				{
					var numRead = stream.Read(tmp, 0, (int)Math.Min(remainingBytes, TMP_BUFFER_LEN));
					if (numRead == 0) // reached end of stream
					{
						break;
					}

					Marshal.Copy(tmp, 0, buffer, numRead);
					buffer += numRead;
					remainingBytes -= (uint)numRead;
				}

				return requested_bytes - remainingBytes;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(tmp);
			}
		}

		private static void CloseFileCallback(IntPtr file_handle)
		{
			var handle = GCHandle.FromIntPtr(file_handle);
			var file = (HawkFile)handle.Target;
			file.Dispose();
			handle.Free();
		}

		private class RCTrack : IDisposable
		{
			private readonly Disc _disc;
			private readonly DiscSectorReader _dsr;
			private readonly DiscTrack _track;
			private readonly byte[] _buf2352;

			public bool IsAvailable => _disc != null;
			public int LBA => _track.LBA;

			private const int RC_HASH_CDTRACK_FIRST_DATA = -1;
			private const int RC_HASH_CDTRACK_LAST = -2;
			private const int RC_HASH_CDTRACK_LARGEST = -3;
			private const int RC_HASH_CDTRACK_FIRST_OF_SECOND_SESSION = -4;

			public RCTrack(string path, int tracknum)
			{
				try
				{
					_disc = DiscExtensions.CreateAnyType(path, e => throw new(e));
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					return;
				}

				switch (tracknum) // implicitly, this checks the first session only, except for RC_HASH_CDTRACK_FIRST_OF_SECOND_SESSION
				{
					case RC_HASH_CDTRACK_FIRST_DATA:
						for (var i = 1; i <= _disc.Session1.InformationTrackCount; i++)
						{
							var track = _disc.Session1.Tracks[i];
							if (track.IsData)
							{
								_track = track;
								break;
							}
						}
						break;
					case RC_HASH_CDTRACK_LAST:
						for (var i = _disc.Session1.InformationTrackCount; i >= 1; i--)
						{
							var track = _disc.Session1.Tracks[i];
							if (track.IsData)
							{
								_track = track;
								break;
							}
						}
						break;
					case RC_HASH_CDTRACK_LARGEST or 0: // 0 is same meaning
						for (var i = 1; i <= _disc.Session1.InformationTrackCount; i++)
						{
							var track = _disc.Session1.Tracks[i];
							if (track.IsData)
							{
								if (_track is null)
								{
									_track = track;
								}
								else
								{
									var curTrackLen = _track.NextTrack.LBA - _track.LBA;
									var nextTrackLen = track.NextTrack.LBA - track.LBA;
									if (nextTrackLen > curTrackLen)
									{
										_track = track;
									}
								}
							}
						}
						break;
					case RC_HASH_CDTRACK_FIRST_OF_SECOND_SESSION:
						if (_disc.Sessions.Count >= 2)
						{
							// we don't care about IsData here, as this is used for Jaguar CD
							// Jaguar CD is an audio CD in this regard, with no "data" tracks
							_track = _disc.Sessions[2].FirstInformationTrack;
						}
						break;
					case > 0:
						if (tracknum <= _disc.Session1.InformationTrackCount)
						{
							_track = _disc.Session1.Tracks[tracknum];
						}
						break;
				}

				if (_track == null)
				{
					_disc.Dispose();
					_disc = null;
					return;
				}

				_dsr = new(_disc)
				{
					Policy =
					{
						UserData2048Mode = DiscSectorReaderPolicy.EUserData2048Mode.InspectSector_AssumeForm1,
						ThrowExceptions2048 = false
					}
				};

				_buf2352 = new byte[2352];
			}

			public int ReadSector(int lba, IntPtr buffer, nuint requestedBytes)
			{
				if (lba < _track.LBA || lba >= _track.NextTrack.LBA)
				{
					return 0;
				}

				var numRead = _track.IsAudio
					? _dsr.ReadLBA_2352(lba, _buf2352, 0)
					: _dsr.ReadLBA_2048(lba, _buf2352, 0);
				var numCopied = (int)Math.Min((ulong)numRead, requestedBytes);
				Marshal.Copy(_buf2352, 0, buffer, numCopied);
				return numCopied;
			}

			public void Dispose()
			{
				_disc.Dispose();
			}
		}

		private static IntPtr OpenTrackCallback(string path, int tracknum)
		{
			var track = new RCTrack(path, tracknum);

			if (!track.IsAvailable)
			{
				return IntPtr.Zero;
			}

			var handle = GCHandle.Alloc(track, GCHandleType.Normal);
			return GCHandle.ToIntPtr(handle);
		}

		private static nuint ReadSectorCallback(IntPtr track_handle, uint sector, IntPtr buffer, nuint requested_bytes)
		{
			var handle = GCHandle.FromIntPtr(track_handle);
			var track = (RCTrack)handle.Target;
			return (uint)track.ReadSector((int)sector, buffer, requested_bytes);
		}

		private static void CloseTrackCallback(IntPtr track_handle)
		{
			var handle = GCHandle.FromIntPtr(track_handle);
			var track = (RCTrack)handle.Target;
			track.Dispose();
			handle.Free();
		}

		private static uint FirstTrackSectorCallback(IntPtr track_handle)
		{
			var handle = GCHandle.FromIntPtr(track_handle);
			var track = (RCTrack)handle.Target;
			return (uint)track.LBA;
		}

		// debug method for hashing a file purely using librcheevos
		// outputs results in the console
		public static void DebugHash()
		{
			using var ofd = new OpenFileDialog();

			ofd.CheckFileExists = true;
			ofd.CheckPathExists = true;
			ofd.InitialDirectory = PathUtils.ExeDirectoryPath;

			string path = null;
			if (ofd.ShowDialog()
				.IsOk())
			{
				path = ofd.FileName;
			}

			if (string.IsNullOrEmpty(path))
			{
				return;
			}

			var ext = Path.GetExtension(Path.GetExtension(path.Replace("|", "")).ToLowerInvariant());

			switch (ext)
			{
				case ".m3u":
				{
					using var file = new HawkFile(path);
					using var sr = new StreamReader(file.GetStream());
					var m3u = M3U_File.Read(sr);
					m3u.Rebase(Path.GetDirectoryName(path));
					foreach (var entry in m3u.Entries)
					{
						InternalDebugHash(entry.Path);
					}

					break;
				}
				case ".xml":
				{
					var xml = XmlGame.Create(new(path));
					foreach (var kvp in xml.Assets)
					{
						InternalDebugHash(kvp.Key);
					}

					break;
				}
				default:
					InternalDebugHash(path);
					break;
			}
		}

		private static void InternalDebugHash(string path)
		{
			static string ResolvePath(string path)
			{
				if (!HawkFile.PathContainsPipe(path) && Disc.IsValidExtension(Path.GetExtension(path)))
				{
					return path; // nothing to do in this case
				}

				if (MAMEMachineDB.IsMAMEMachine(path))
				{
					// the actual file isn't used here, the file name is hashed
					// to keep things consistent, let's just make it all lowercase (like done internally)
					return Path.GetFileName(path).ToLowerInvariant();
				}

				using var file = new HawkFile(path);
				if (file.IsArchive && !file.IsBound)
				{
					using var ac = new ArchiveChooser(file);
					if (ac.ShowDialog().IsOk())
					{
						file.BindArchiveMember(ac.SelectedMemberIndex);
					}
				}

				if (!file.IsBound)
				{
					file.BindFirst();
				}

				return file.CanonicalFullPath;
			}

			static ConsoleID IdentifyConsole(string path)
			{
				if (!HawkFile.PathContainsPipe(path) && Disc.IsValidExtension(Path.GetExtension(path)))
				{
					using var disc = DiscExtensions.CreateAnyType(path, Console.WriteLine);
					if (disc is null)
					{
						return ConsoleID.UnknownConsoleID;
					}

					return new DiscIdentifier(disc).DetectDiscType() switch
					{
						DiscType.AudioDisc => ConsoleID.UnknownConsoleID,
						DiscType.UnknownFormat => ConsoleID.UnknownConsoleID,
						DiscType.UnknownCDFS => ConsoleID.UnknownConsoleID,
						DiscType.SonyPSX => ConsoleID.PlayStation,
						DiscType.SonyPSP => ConsoleID.PSP,
						DiscType.SegaSaturn => ConsoleID.Saturn,
						DiscType.TurboCD => ConsoleID.PCEngineCD,
						DiscType.TurboGECD => ConsoleID.PCEngineCD,
						DiscType.MegaCD => ConsoleID.SegaCD,
						DiscType.PCFX => ConsoleID.PCFX,
						DiscType.Panasonic3DO => ConsoleID.ThreeDO,
						DiscType.CDi => ConsoleID.CDi,
						DiscType.GameCube => ConsoleID.GameCube,
						DiscType.Wii => ConsoleID.WII,
						DiscType.NeoGeoCD => ConsoleID.NeoGeoCD,
						DiscType.Playdia => ConsoleID.UnknownConsoleID,
						DiscType.Amiga => ConsoleID.Amiga,
						DiscType.Dreamcast => ConsoleID.Dreamcast,
						DiscType.SonyPS2 => ConsoleID.PlayStation2,
						DiscType.JaguarCD => ConsoleID.JaguarCD,
						_ => throw new InvalidOperationException()
					};
				}

				if (MAMEMachineDB.IsMAMEMachine(path))
				{
					return ConsoleID.Arcade;
				}

				using var file = new HawkFile(path);
				var rom = new RomGame(file);
				return rom.GameInfo.System switch
				{
					VSystemID.Raw.A26 => ConsoleID.Atari2600,
					VSystemID.Raw.A78 => ConsoleID.Atari7800,
					VSystemID.Raw.Amiga => ConsoleID.Amiga,
					VSystemID.Raw.AmstradCPC => ConsoleID.AmstradCPC,
					VSystemID.Raw.AppleII => ConsoleID.AppleII,
					VSystemID.Raw.Arcade => ConsoleID.Arcade,
					VSystemID.Raw.C64 => ConsoleID.C64,
					VSystemID.Raw.ChannelF => ConsoleID.FairchildChannelF,
					VSystemID.Raw.Coleco => ConsoleID.Colecovision,
					VSystemID.Raw.DEBUG => ConsoleID.UnknownConsoleID,
					VSystemID.Raw.Dreamcast => ConsoleID.Dreamcast,
					VSystemID.Raw.GameCube => ConsoleID.GameCube,
					VSystemID.Raw.GB => ConsoleID.GB,
					VSystemID.Raw.GBA => ConsoleID.GBA,
					VSystemID.Raw.GBC => ConsoleID.GBC,
					VSystemID.Raw.GBL => ConsoleID.GB,
					VSystemID.Raw.GEN when rom.GameInfo.GetBool("32X", false) => ConsoleID.Sega32X,
					VSystemID.Raw.GEN => ConsoleID.MegaDrive,
					VSystemID.Raw.GG => ConsoleID.GameGear,
					VSystemID.Raw.GGL => ConsoleID.GameGear,
					VSystemID.Raw.INTV => ConsoleID.Intellivision,
					VSystemID.Raw.Jaguar => ConsoleID.Jaguar,
					VSystemID.Raw.Libretro => ConsoleID.UnknownConsoleID,
					VSystemID.Raw.Lynx => ConsoleID.Lynx,
					VSystemID.Raw.MSX => ConsoleID.MSX,
					VSystemID.Raw.N64 => ConsoleID.N64,
					VSystemID.Raw.NDS => ConsoleID.DS,
					VSystemID.Raw.NeoGeoCD => ConsoleID.NeoGeoCD,
					VSystemID.Raw.NES => ConsoleID.NES,
					VSystemID.Raw.NGP => ConsoleID.NeoGeoPocket,
					VSystemID.Raw.NULL => ConsoleID.UnknownConsoleID,
					VSystemID.Raw.O2 => ConsoleID.MagnavoxOdyssey,
					VSystemID.Raw.Panasonic3DO => ConsoleID.ThreeDO,
					VSystemID.Raw.PCE => ConsoleID.PCEngine,
					VSystemID.Raw.PCECD => ConsoleID.PCEngineCD,
					VSystemID.Raw.PCFX => ConsoleID.PCFX,
					VSystemID.Raw.PhillipsCDi => ConsoleID.CDi,
					VSystemID.Raw.Playdia => ConsoleID.UnknownConsoleID,
					VSystemID.Raw.PS2 => ConsoleID.PlayStation2,
					VSystemID.Raw.PSP => ConsoleID.PSP,
					VSystemID.Raw.PSX => ConsoleID.PlayStation,
					VSystemID.Raw.SAT => ConsoleID.Saturn,
					VSystemID.Raw.Sega32X => ConsoleID.Sega32X,
					VSystemID.Raw.SG => ConsoleID.SG1000,
					VSystemID.Raw.SGB => ConsoleID.GB,
					VSystemID.Raw.SGX => ConsoleID.PCEngine,
					VSystemID.Raw.SGXCD => ConsoleID.PCEngineCD,
					VSystemID.Raw.SMS => ConsoleID.MasterSystem,
					VSystemID.Raw.SNES => ConsoleID.SNES,
					VSystemID.Raw.TI83 => ConsoleID.UnknownConsoleID,
					VSystemID.Raw.TIC80 => ConsoleID.Tic80,
					VSystemID.Raw.UZE => ConsoleID.UnknownConsoleID,
					VSystemID.Raw.VB => ConsoleID.VirtualBoy,
					VSystemID.Raw.VEC => ConsoleID.Vectrex,
					VSystemID.Raw.Wii => ConsoleID.WII,
					VSystemID.Raw.WSWAN => ConsoleID.WonderSwan,
					VSystemID.Raw.ZXSpectrum => ConsoleID.ZXSpectrum,
					_ => ConsoleID.UnknownConsoleID,
				};
			}

			path = ResolvePath(path);
			var consoleID = IdentifyConsole(path);
			var hash = new byte[33];
			var success = _lib.rc_hash_generate_from_file(hash, consoleID, path);
			Console.WriteLine(path);
			Console.WriteLine(success
				? $"Generated RC Hash: {Encoding.ASCII.GetString(hash, 0, 32)}"
				: "Failed to generate RC Hash");
		}
	}
}