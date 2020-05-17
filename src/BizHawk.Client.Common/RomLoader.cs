using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Arcades.MAME;
using BizHawk.Emulation.Cores.Calculators;
using BizHawk.Emulation.Cores.Computers.AmstradCPC;
using BizHawk.Emulation.Cores.Computers.AppleII;
using BizHawk.Emulation.Cores.Computers.Commodore64;
using BizHawk.Emulation.Cores.Computers.SinclairSpectrum;
using BizHawk.Emulation.Cores.Consoles.ChannelF;
using BizHawk.Emulation.Cores.Consoles.NEC.PCFX;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Libretro;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink3x;
using BizHawk.Emulation.Cores.Nintendo.GBHawkLink4x;
using BizHawk.Emulation.Cores.Nintendo.SNES;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Sega.GGHawkLink;
using BizHawk.Emulation.Cores.Sony.PSX;
using BizHawk.Emulation.DiscSystem;

using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace BizHawk.Client.Common
{
	public class RomLoader
	{
		public enum LoadErrorType
		{
			Unknown, MissingFirmware, Xml, DiscError
		}

		// helper methods for the settings events
		private object GetCoreSettings<T>()
			where T : IEmulator
		{
			return GetCoreSettings(typeof(T));
		}

		private object GetCoreSyncSettings<T>()
			where T : IEmulator
		{
			return GetCoreSyncSettings(typeof(T));
		}

		private object GetCoreSettings(Type t)
		{
			var e = new SettingsLoadArgs(t);
			OnLoadSettings?.Invoke(this, e);
			return e.Settings;
		}

		private object GetCoreSyncSettings(Type t)
		{
			var e = new SettingsLoadArgs(t);
			OnLoadSyncSettings?.Invoke(this, e);
			return e.Settings;
		}

		// For not throwing errors but simply outputting information to the screen
		public Action<string> MessageCallback { get; set; }

		private void DoMessageCallback(string message)
		{
			MessageCallback?.Invoke(message);
		}

		// TODO: reconsider the need for exposing these;
		public IEmulator LoadedEmulator { get; private set; }
		public GameInfo Game { get; private set; }
		public RomGame Rom { get; private set; }
		public string CanonicalFullPath { get; private set; }

		public bool Deterministic { get; set; }

		public class RomErrorArgs : EventArgs
		{
			// TODO: think about naming here, what to pass, a lot of potential good information about what went wrong could go here!
			public RomErrorArgs(string message, string systemId, LoadErrorType type)
			{
				Message = message;
				AttemptedCoreLoad = systemId;
				Type = type;
			}

			public RomErrorArgs(string message, string systemId, string path, bool? det, LoadErrorType type)
				: this(message, systemId, type)
			{
				Deterministic = det;
				RomPath = path;
			}

			public string Message { get; }
			public string AttemptedCoreLoad { get; }
			public string RomPath { get; }
			public bool? Deterministic { get; set; }
			public bool Retry { get; set; }
			public LoadErrorType Type { get; }
		}

		public class SettingsLoadArgs : EventArgs
		{
			public object Settings { get; set; }
			public Type Core { get; }
			public SettingsLoadArgs(Type t)
			{
				Core = t;
				Settings = null;
			}
		}

		public delegate void SettingsLoadEventHandler(object sender, SettingsLoadArgs e);
		public event SettingsLoadEventHandler OnLoadSettings;
		public event SettingsLoadEventHandler OnLoadSyncSettings;

		public delegate void LoadErrorEventHandler(object sender, RomErrorArgs e);
		public event LoadErrorEventHandler OnLoadError;

		public Func<HawkFile, int?> ChooseArchive { get; set; }

		public Func<RomGame, string> ChoosePlatform { get; set; }

		// in case we get sent back through the picker more than once, use the same choice the second time
		private int? _previousChoice;
		private int? HandleArchive(HawkFile file)
		{
			if (_previousChoice.HasValue)
			{
				return _previousChoice;
			}

			if (ChooseArchive != null)
			{
				_previousChoice = ChooseArchive(file);
				return _previousChoice;
			}

			return null;
		}

		// May want to phase out this method in favor of the overload with more parameters
		private void DoLoadErrorCallback(string message, string systemId, LoadErrorType type = LoadErrorType.Unknown)
		{
			OnLoadError?.Invoke(this, new RomErrorArgs(message, systemId, type));
		}

		private void DoLoadErrorCallback(string message, string systemId, string path, bool det, LoadErrorType type = LoadErrorType.Unknown)
		{
			OnLoadError?.Invoke(this, new RomErrorArgs(message, systemId, path, det, type));
		}

		private bool PreferredPlatformIsDefined(string extension)
		{
			if (Global.Config.PreferredPlatformsForExtensions.ContainsKey(extension))
			{
				return !string.IsNullOrEmpty(Global.Config.PreferredPlatformsForExtensions[extension]);
			}

			return false;
		}

		public IOpenAdvanced OpenAdvanced { get; set; }

		private bool HandleArchiveBinding(HawkFile file)
		{
			var romExtensions = new[] { "SMS", "SMC", "SFC", "PCE", "SGX", "GG", "SG", "BIN", "GEN", "MD", "SMD", "GB", "NES", "FDS", "ROM", "INT", "GBC", "UNF", "A78", "CRT", "COL", "XML", "Z64", "V64", "N64", "WS", "WSC", "GBA", "32X", "VEC", "O2" };

			// try binding normal rom extensions first
			if (!file.IsBound)
			{
				file.BindSoleItemOf(romExtensions);
			}

			// if we have an archive and need to bind something, then pop the dialog
			if (file.IsArchive && !file.IsBound)
			{
				int? result = HandleArchive(file);
				if (result.HasValue)
				{
					file.BindArchiveMember(result.Value);
				}
				else
				{
					return false;
				}
			}

			CanonicalFullPath = file.CanonicalFullPath;

			return true;
		}

		private List<Disc> DiscsFromXml(XmlGame xmlGame, string systemId, DiscType diskType)
		{
			var discs = new List<Disc>();
			foreach (var e in xmlGame.AssetFullPaths.Where(a => Disc.IsValidExtension(Path.GetExtension(a))))
			{
				var disc = diskType.Create(e, str => { DoLoadErrorCallback(str, systemId, LoadErrorType.DiscError); });
				if (disc != null)
				{
					discs.Add(disc);
				}
			}

			return discs;
		}

		private string SystemFromDiscType(DiscType dt, string fileExt)
		{
			switch (dt)
			{
				default:
				case DiscType.SonyPSX:
					return "PSX";
				case DiscType.SegaSaturn:
					return "SAT";
				case DiscType.SonyPSP:
					return "PSP";
				case DiscType.MegaCD:
					return "GEN";
				case DiscType.PCFX:
					return "PCFX";
				case DiscType.TurboCD:
					return "PCECD";

				case DiscType.Amiga:
				case DiscType.CDi:
				case DiscType.Dreamcast:
				case DiscType.GameCube:
				case DiscType.NeoGeoCD:
				case DiscType.Panasonic3DO:
				case DiscType.Playdia:
				case DiscType.Wii:
					throw new NoAvailableCoreException(dt.ToString()); // no supported emulator core for these (yet)

				case DiscType.AudioDisc:
				case DiscType.UnknownCDFS:
				case DiscType.UnknownFormat:
					return PreferredPlatformIsDefined(fileExt)
						? Global.Config.PreferredPlatformsForExtensions[fileExt]
						: "NULL";
			}
		}

		private bool TryLoadFromDiscFormatRom(ref (RomGame Rom, IEmulator NextEmulator, GameInfo Game) result, string path, CoreComm nextComm, HawkFile file, string fileExt)
		{
			if (file.IsArchive)
			{
				throw new InvalidOperationException("Can't load CD files from archives!");
			}

			//--- load the disc in a context which will let us abort if it's going to take too long
			var discMountJob = new DiscMountJob { IN_FromPath = path, IN_SlowLoadAbortThreshold = 8 };
			discMountJob.Run();

			if (discMountJob.OUT_SlowLoadAborted)
			{
				DoLoadErrorCallback("This disc would take too long to load. Run it through DiscoHawk first, or find a new rip because this one is probably junk", "", LoadErrorType.DiscError);
				return false;
			}

			if (discMountJob.OUT_ErrorLevel)
			{
				throw new InvalidOperationException($"\r\n{discMountJob.OUT_Log}");
			}

			var disc = discMountJob.OUT_Disc;

			// -----------
			// TODO - use more sophisticated IDer
			var discType = new DiscIdentifier(disc).DetectDiscType();
			var discHash = discType == DiscType.SonyPSX
				? new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8")
				: new DiscHasher(disc).OldHash();
			result.Game = Database.CheckDatabase(discHash) ?? new GameInfo
			{
				Name = Path.GetFileNameWithoutExtension(file.Name),
				Hash = discHash,
				System = SystemFromDiscType(new DiscIdentifier(disc).DetectDiscType(), fileExt) // try to use our wizard methods
			};

			switch (result.Game.System)
			{
				case "GEN":
					result.NextEmulator = new GPGX(
						nextComm,
						result.Game,
						null,
						new[] { disc },
						GetCoreSettings<GPGX>(),
						GetCoreSyncSettings<GPGX>()
					);
					break;
				case "SAT":
					result.NextEmulator = new Saturnus(
						nextComm,
						new[] { disc },
						Deterministic,
						(Saturnus.Settings) GetCoreSettings<Saturnus>(),
						(Saturnus.SyncSettings) GetCoreSyncSettings<Saturnus>()
					);
					break;
				case "PSX":
					result.NextEmulator = new Octoshock(
						nextComm,
						new List<Disc> { disc },
						new List<string> { Path.GetFileNameWithoutExtension(path) },
						null,
						GetCoreSettings<Octoshock>(),
						GetCoreSyncSettings<Octoshock>(),
						result.Game.IsRomStatusBad() || result.Game.Status == RomStatus.NotInDatabase
							? "Disc could not be identified as known-good. Look for a better rip."
							: string.Join("\n",
								$"Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{discHash}",
								"Nonetheless it could be an unrecognized romhack or patched version.",
								$"According to redump.org, the ideal hash for entire disc is: CRC32:{result.Game.GetStringValue("dh")}",
								"The file you loaded hasn't been hashed entirely (it would take too long)",
								"Compare it with the full hash calculated by the PSX menu's Hash Discs tool"
							)
					);
					break;
				case "PCFX":
					result.NextEmulator = new Tst(
						nextComm,
						new[] { disc },
						(Tst.Settings) GetCoreSettings<Tst>(),
						(Tst.SyncSettings) GetCoreSyncSettings<Tst>()
					);
					break;
				case "PCE":
				case "PCECD":
					result.NextEmulator = new PCEngine(
						nextComm,
						result.Game,
						disc,
						GetCoreSettings<PCEngine>(),
						GetCoreSyncSettings<PCEngine>()
					);
					break;
			}
			return true;
		}

		private bool TryLoadFromM3UFormatRom(ref (RomGame Rom, IEmulator NextEmulator, GameInfo Game) result, string path, CoreComm nextComm, HawkFile file)
		{
			// HACK ZONE - currently only psx supports m3u
			M3U_File m3u;
			using (var sr = new StreamReader(path))
			{
				m3u = M3U_File.Read(sr);
			}

			if (m3u.Entries.Count == 0)
			{
				throw new InvalidOperationException("Can't load an empty M3U");
			}

			// load discs for all the m3u
			m3u.Rebase(Path.GetDirectoryName(path));
			var discs = new List<Disc>();
			var discNames = new List<string>();
			var sw = new StringWriter();
			foreach (var e in m3u.Entries)
			{
				var disc = DiscType.SonyPSX.Create(e.Path, str => DoLoadErrorCallback(str, "PSX", LoadErrorType.DiscError));
				var discName = Path.GetFileNameWithoutExtension(e.Path);
				discNames.Add(discName);
				discs.Add(disc);

				sw.WriteLine(Path.GetFileName(e.Path));

				var discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
				var game = Database.CheckDatabase(discHash);
				if (game == null || game.IsRomStatusBad() || game.Status == RomStatus.NotInDatabase)
				{
					sw.WriteLine("Disc could not be identified as known-good. Look for a better rip.");
				}
				else
				{
					sw.WriteLine($"Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{discHash}");
					sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
					sw.WriteLine($"According to redump.org, the ideal hash for entire disc is: CRC32:{game.GetStringValue("dh")}");
					sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
					sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
				}

				sw.WriteLine("-------------------------");
			}

			result.NextEmulator = new Octoshock(
				nextComm,
				discs,
				discNames,
				null,
				GetCoreSettings<Octoshock>(),
				GetCoreSyncSettings<Octoshock>(),
				sw.ToString()
			);
			result.Game = new GameInfo
			{
				Name = Path.GetFileNameWithoutExtension(file.Name),
				System = "PSX"
			};
			return true;
		}

		private bool TryLoadFromMiscFormatRom(ref (RomGame Rom, IEmulator NextEmulator, GameInfo Game) result, ref bool cancel, string path, CoreComm nextComm, bool forceAccurateCore, HawkFile file, string fileExt)
		{
			result.Rom = new RomGame(file);

			// hacky for now
			if (fileExt == ".exe")
			{
				result.Rom.GameInfo.System = "PSX";
			}
			else if (fileExt == ".nsf")
			{
				result.Rom.GameInfo.System = "NES";
			}

			Console.WriteLine(result.Rom.GameInfo.System);

			if (string.IsNullOrEmpty(result.Rom.GameInfo.System))
			{
				// Has the user picked a preference for this extension?
				var romExt = result.Rom.Extension.ToLowerInvariant();
				if (PreferredPlatformIsDefined(romExt))
				{
					result.Rom.GameInfo.System = Global.Config.PreferredPlatformsForExtensions[romExt];
				}
				else if (ChoosePlatform != null)
				{
					var platform = ChoosePlatform(result.Rom);
					if (!string.IsNullOrEmpty(platform))
					{
						result.Rom.GameInfo.System = platform;
					}
					else
					{
						cancel = true;
					}
				}
			}

			result.Game = result.Rom.GameInfo;

			// other xml has already been handled
			var isXml = string.Equals(file.Extension, ".xml", StringComparison.InvariantCultureIgnoreCase);
			if (isXml) result.Game.System = "SNES";

			CoreInventory.Core core = null;

			switch (result.Game.System)
			{
				default:
					core = CoreInventory.Instance[result.Game.System];
					break;

				case null:
					// The user picked nothing in the Core picker
					break;
				case "83P":
					var ti83Bios = nextComm.CoreFileProvider.GetFirmware("TI83", "Rom", true);
					//TODO make the TI-83 a proper firmware file
					var ti83BiosPath = Global.FirmwareManager.Request(Global.Config.PathEntries, Global.Config.FirmwareUserSpecifications, "TI83", "Rom");
					using (var ti83AsHawkFile = new HawkFile(ti83BiosPath))
					{
						var ti83BiosAsRom = new RomGame(ti83AsHawkFile);
						var ti83 = new TI83(ti83BiosAsRom.GameInfo, ti83Bios, GetCoreSettings<TI83>());
						ti83.LinkPort.SendFileToCalc(File.OpenRead(path.SubstringBefore('|')), false);
						result.NextEmulator = ti83;
					}
					break;
				case "SNES":
					var useSnes9x = Global.Config.PreferredCores["SNES"] == CoreNames.Snes9X;
					if (Global.Config.CoreForcingViaGameDb && !string.IsNullOrEmpty(result.Game.ForcedCore))
					{
						var forced = result.Game.ForcedCore.ToLowerInvariant();
						if (forced == "snes9x") useSnes9x = true;
						else if (forced == "bsnes") useSnes9x = false;
					}
					if (useSnes9x)
					{
						core = CoreInventory.Instance["SNES", CoreNames.Snes9X];
					}
					else
					{
						//HACK need to get rid of this at some point
						result.NextEmulator = new LibsnesCore(
							result.Game,
							isXml ? null : result.Rom.FileData,
							isXml ? result.Rom.FileData : null,
							Path.GetDirectoryName(path.SubstringBefore('|')), // since we are just getting the directory path, it's safe to remove the archive sub-file (everything after '|')
							nextComm,
							GetCoreSettings<LibsnesCore>(),
							GetCoreSyncSettings<LibsnesCore>()
						);
					}
					break;
				case "NES":
					// apply main spur-of-the-moment switcheroo as lowest priority
					string preference = Global.Config.PreferredCores["NES"];

					// if user has saw fit to override in gamedb, apply that
					if (Global.Config.CoreForcingViaGameDb && !string.IsNullOrEmpty(result.Game.ForcedCore))
					{
						preference = result.Game.ForcedCore.ToLower() switch
						{
							"quicknes" => CoreNames.QuickNes,
							_ => CoreNames.NesHawk
						};
					}

					// but only neshawk is accurate
					if (forceAccurateCore)
					{
						preference = CoreNames.NesHawk;
					}

					core = CoreInventory.Instance["NES", preference];
					break;

				case "GB":
				case "GBC":
					if (!Global.Config.GbAsSgb)
					{
						core = CoreInventory.Instance["GB", Global.Config.PreferredCores["GB"]];
					}
					else
					{
						if (Global.Config.SgbUseBsnes)
						{
							result.Game.System = "SNES";
							result.Game.AddOption("SGB");
							result.NextEmulator = new LibsnesCore(
								result.Game,
								result.Rom.FileData,
								null,
								null,
								nextComm,
								GetCoreSettings<LibsnesCore>(),
								GetCoreSyncSettings<LibsnesCore>()
							);
						}
						else
						{
							core = CoreInventory.Instance["SGB", CoreNames.SameBoy];
						}
					}
					break;
				case "C64":
					result.NextEmulator = new C64(
						nextComm,
						new[] { result.Rom.FileData },
						result.Rom.GameInfo,
						GetCoreSettings<C64>(),
						GetCoreSyncSettings<C64>()
					);
					break;
				case "ZXSpectrum":
					result.NextEmulator = new ZXSpectrum(
						nextComm,
						new[] { result.Rom.RomData },
						new List<GameInfo> { result.Rom.GameInfo },
						GetCoreSettings<ZXSpectrum>(),
						GetCoreSyncSettings<ZXSpectrum>(),
						Deterministic
					);
					break;
				case "ChannelF":
					result.NextEmulator = new ChannelF(
						nextComm,
						result.Game,
						result.Rom.FileData,
						GetCoreSettings<ChannelF>(),
						GetCoreSyncSettings<ChannelF>()
					);
					break;
				case "AmstradCPC":
					result.NextEmulator = new AmstradCPC(
						nextComm,
						Enumerable.Repeat(result.Rom.RomData, 1),
						Enumerable.Repeat(result.Rom.GameInfo, 1).ToList(),
						GetCoreSettings<AmstradCPC>(),
						GetCoreSyncSettings<AmstradCPC>()
					);
					break;
				case "PSX":
					result.NextEmulator = new Octoshock(
						nextComm,
						null,
						null,
						result.Rom.FileData,
						GetCoreSettings<Octoshock>(),
						GetCoreSyncSettings<Octoshock>(),
						"PSX etc."
					);
					break;
				case "Arcade":
					result.NextEmulator = new MAME(
						file.Directory,
						file.CanonicalName,
						GetCoreSyncSettings<MAME>(),
						out var gameName
					);
					result.Rom.GameInfo.Name = gameName;
					break;
				case "GEN":
					core = CoreInventory.Instance["GEN",
						Global.Config.CoreForcingViaGameDb && result.Game.ForcedCore?.ToLower() == "pico"
							? CoreNames.PicoDrive
							: CoreNames.Gpgx
					];
					break;
				case "32X":
					core = CoreInventory.Instance["GEN", CoreNames.PicoDrive];
					break;
			}

			if (core != null)
			{
				// use CoreInventory
				result.NextEmulator = core.Create(
					nextComm,
					result.Game,
					result.Rom.RomData,
					result.Rom.FileData,
					Deterministic,
					GetCoreSettings(core.Type),
					GetCoreSyncSettings(core.Type)
				);
			}

			return true;
		}

		private bool TryLoadFromPSFFormatRom(ref (RomGame Rom, IEmulator NextEmulator, GameInfo Game) result, string path, CoreComm nextComm, HawkFile file)
		{
			var psf = new PSF();
			psf.Load(path, (instream, size) =>
			{
				var ret = new MemoryStream();
				new InflaterInputStream(instream, new Inflater(false)).CopyTo(ret);
				return ret.ToArray();
			});
			result.NextEmulator = new Octoshock(
				nextComm,
				psf,
				GetCoreSettings<Octoshock>(),
				GetCoreSyncSettings<Octoshock>()
			);
			// total garbage, this
			result.Rom = new RomGame(file);
			result.Game = result.Rom.GameInfo;
			return true;
		}

		private bool TryLoadFromXMLFormatRom(ref (RomGame Rom, IEmulator NextEmulator, GameInfo Game) result, string path, CoreComm nextComm, HawkFile file)
		{
			try
			{
				var xmlGame = XmlGame.Create(file); // if load fails, are we supposed to retry as a bsnes XML????????
				result.Game = xmlGame.GI;

				switch (result.Game.System)
				{
					case "GB":
					case "DGB":
						// adelikat: remove need for tags to be hardcoded to left and right, we should clean this up, also maybe the DGB core should just take the xml file and handle it itself
						var leftBytes = xmlGame.Assets.First().Value;
						var rightBytes = xmlGame.Assets.Skip(1).First().Value;

						var left = Database.GetGameInfo(leftBytes, "left.gb");
						var right = Database.GetGameInfo(rightBytes, "right.gb");
						if (Global.Config.PreferredCores["GB"] == CoreNames.GbHawk)
						{
							result.NextEmulator = new GBHawkLink(
								nextComm,
								left,
								leftBytes,
								right,
								rightBytes,
								GetCoreSettings<GBHawkLink>(),
								GetCoreSyncSettings<GBHawkLink>());
						}
						else
						{
							result.NextEmulator = new GambatteLink(
								nextComm,
								left,
								leftBytes,
								right,
								rightBytes,
								GetCoreSettings<GambatteLink>(),
								GetCoreSyncSettings<GambatteLink>(),
								Deterministic);
						}

						// other stuff todo
						break;
					case "GB3x":
						var leftBytes3x = xmlGame.Assets.First().Value;
						var centerBytes3x = xmlGame.Assets.Skip(1).First().Value;
						var rightBytes3x = xmlGame.Assets.Skip(2).First().Value;

						var left3x = Database.GetGameInfo(leftBytes3x, "left.gb");
						var center3x = Database.GetGameInfo(centerBytes3x, "center.gb");
						var right3x = Database.GetGameInfo(rightBytes3x, "right.gb");

						result.NextEmulator = new GBHawkLink3x(
							nextComm,
							left3x,
							leftBytes3x,
							center3x,
							centerBytes3x,
							right3x,
							rightBytes3x,
							GetCoreSettings<GBHawkLink3x>(),
							GetCoreSyncSettings<GBHawkLink3x>());

						break;
					case "GB4x":
						var A_Bytes4x = xmlGame.Assets.First().Value;
						var B_Bytes4x = xmlGame.Assets.Skip(1).First().Value;
						var C_Bytes4x = xmlGame.Assets.Skip(2).First().Value;
						var D_Bytes4x = xmlGame.Assets.Skip(3).First().Value;

						var A_4x = Database.GetGameInfo(A_Bytes4x, "A.gb");
						var B_4x = Database.GetGameInfo(B_Bytes4x, "B.gb");
						var C_4x = Database.GetGameInfo(C_Bytes4x, "C.gb");
						var D_4x = Database.GetGameInfo(D_Bytes4x, "D.gb");

						result.NextEmulator = new GBHawkLink4x(
							nextComm,
							A_4x,
							A_Bytes4x,
							B_4x,
							B_Bytes4x,
							C_4x,
							C_Bytes4x,
							D_4x,
							D_Bytes4x,
							GetCoreSettings<GBHawkLink4x>(),
							GetCoreSyncSettings<GBHawkLink4x>());

						break;
					case "AppleII":
						var roms = xmlGame.Assets.Select(a => a.Value);
						result.NextEmulator = new AppleII(
							nextComm,
							roms,
							(AppleII.Settings)GetCoreSettings<AppleII>());
						break;
					case "C64":
						result.NextEmulator = new C64(
							nextComm,
							xmlGame.Assets.Select(a => a.Value),
							GameInfo.NullInstance,
							(C64.C64Settings)GetCoreSettings<C64>(),
							(C64.C64SyncSettings)GetCoreSyncSettings<C64>());
						break;
					case "ZXSpectrum":

						var zxGI = new List<GameInfo>();
						foreach (var a in xmlGame.Assets)
						{
							zxGI.Add(new GameInfo { Name = Path.GetFileNameWithoutExtension(a.Key) });
						}

						result.NextEmulator = new ZXSpectrum(
							nextComm,
							xmlGame.Assets.Select(a => a.Value),
							zxGI,
							(ZXSpectrum.ZXSpectrumSettings)GetCoreSettings<ZXSpectrum>(),
							(ZXSpectrum.ZXSpectrumSyncSettings)GetCoreSyncSettings<ZXSpectrum>(),
							Deterministic);
						break;
					case "AmstradCPC":

						var cpcGI = new List<GameInfo>();
						foreach (var a in xmlGame.Assets)
						{
							cpcGI.Add(new GameInfo { Name = Path.GetFileNameWithoutExtension(a.Key) });
						}

						result.NextEmulator = new AmstradCPC(
							nextComm,
							xmlGame.Assets.Select(a => a.Value),
							cpcGI,
							(AmstradCPC.AmstradCPCSettings)GetCoreSettings<AmstradCPC>(),
							(AmstradCPC.AmstradCPCSyncSettings)GetCoreSyncSettings<AmstradCPC>());
						break;
					case "PSX":
						var entries = xmlGame.AssetFullPaths;
						var discs = new List<Disc>();
						var discNames = new List<string>();
						var sw = new StringWriter();
						foreach (var e in entries)
						{
							var disc = DiscType.SonyPSX.Create(e, str => { DoLoadErrorCallback(str, "PSX", LoadErrorType.DiscError); });

							var discName = Path.GetFileNameWithoutExtension(e);
							discNames.Add(discName);
							discs.Add(disc);

							sw.WriteLine("{0}", Path.GetFileName(e));

							string discHash = new DiscHasher(disc).Calculate_PSX_BizIDHash().ToString("X8");
							result.Game = Database.CheckDatabase(discHash);
							if (result.Game == null || result.Game.IsRomStatusBad() || result.Game.Status == RomStatus.NotInDatabase)
							{
								sw.WriteLine("Disc could not be identified as known-good. Look for a better rip.");
							}
							else
							{
								sw.WriteLine($"Disc was identified (99.99% confidently) as known good with disc id hash CRC32:{discHash}");
								sw.WriteLine("Nonetheless it could be an unrecognized romhack or patched version.");
								sw.WriteLine($"According to redump.org, the ideal hash for entire disc is: CRC32:{result.Game.GetStringValue("dh")}");
								sw.WriteLine("The file you loaded hasn't been hashed entirely (it would take too long)");
								sw.WriteLine("Compare it with the full hash calculated by the PSX menu's Hash Discs tool");
							}

							sw.WriteLine("-------------------------");
						}

						// todo: copy pasta from PSX .cue section
						result.NextEmulator = new Octoshock(nextComm, discs, discNames, null, GetCoreSettings<Octoshock>(), GetCoreSyncSettings<Octoshock>(), sw.ToString());
						result.Game = new GameInfo
						{
							Name = Path.GetFileNameWithoutExtension(file.Name),
							System = "PSX"
						};
						break;
					case "SAT":
						var saturnDiscs = DiscsFromXml(xmlGame, "SAT", DiscType.SegaSaturn);
						if (!saturnDiscs.Any())
						{
							return false;
						}

						result.NextEmulator = new Saturnus(nextComm, saturnDiscs, Deterministic,
							(Saturnus.Settings)GetCoreSettings<Saturnus>(), (Saturnus.SyncSettings)GetCoreSyncSettings<Saturnus>());
						break;
					case "PCFX":
						var pcfxDiscs = DiscsFromXml(xmlGame, "PCFX", DiscType.PCFX);
						if (!pcfxDiscs.Any())
						{
							return false;
						}

						result.NextEmulator = new Tst(nextComm, pcfxDiscs,
							(Tst.Settings)GetCoreSettings<Tst>(), (Tst.SyncSettings)GetCoreSyncSettings<Tst>());
						break;
					case "GEN":
						var genDiscs = DiscsFromXml(xmlGame, "GEN", DiscType.MegaCD);
						var romBytes = xmlGame.Assets
							.Where(a => !Disc.IsValidExtension(a.Key))
							.Select(a => a.Value)
							.FirstOrDefault();
						if (!genDiscs.Any() && romBytes == null)
						{
							return false;
						}
						result.NextEmulator = new GPGX(nextComm, result.Game, romBytes, genDiscs, GetCoreSettings<GPGX>(), GetCoreSyncSettings<GPGX>());
						break;
					case "Game Gear":
						var leftBytesGG = xmlGame.Assets.First().Value;
						var rightBytesGG = xmlGame.Assets.Skip(1).First().Value;

						var leftGG = Database.GetGameInfo(leftBytesGG, "left.gg");
						var rightGG = Database.GetGameInfo(rightBytesGG, "right.gg");

						result.NextEmulator = new GGHawkLink(
							nextComm,
							leftGG,
							leftBytesGG,
							rightGG,
							rightBytesGG,
							GetCoreSettings<GGHawkLink>(),
							GetCoreSyncSettings<GGHawkLink>());
						break;
					default:
						return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				try
				{
					//HACK need to get rid of this at some point
					result.Rom = new RomGame(file);
					result.Game = result.Rom.GameInfo;
					result.Game.System = "SNES";
					result.NextEmulator = new LibsnesCore(
						result.Game,
						null,
						result.Rom.FileData,
						Path.GetDirectoryName(path.SubstringBefore('|')), // since we are just getting the directory path, it's safe to remove the archive sub-file (everything after '|')
						nextComm,
						GetCoreSettings<LibsnesCore>(),
						GetCoreSyncSettings<LibsnesCore>()
					);
					return true;
				}
				catch
				{
					DoLoadErrorCallback(ex.ToString(), "DGB", LoadErrorType.Xml);
					return false;
				}
			}
		}

		public bool LoadRom(string path, CoreComm nextComm, string launchLibretroCore, bool forceAccurateCore = false, int recursiveCount = 0)
		{
			if (recursiveCount > 1) // hack to stop recursive calls from endlessly rerunning if we can't load it
			{
				DoLoadErrorCallback("Failed multiple attempts to load ROM.", "");
				return false;
			}

			if (path == null) return false;

			using var file = new HawkFile(); // I'm almost certain that we'll see NREs unless the below call to Open happens, so I deprecated this ctor as a nag --yoshi
			if (!string.IsNullOrEmpty(path))
			{
				// only try mounting a file if a filename was given
				if (OpenAdvanced is OpenAdvanced_MAME) file.NonArchiveExtensions = new[] { ".zip", ".7z" }; // MAME uses these extensions for arcade ROMs, but also accepts all sorts of variations of archives, folders, and files. if we let archive loader handle this, it won't know where to stop, since it'd require MAME's ROM database (which contains ROM names and blob hashes) to look things up, and even then it might be confused by archive/folder structure. so assume the user provides the proper ROM directly, and handle possible errors later
				file.Open(path);
				if (!file.Exists) return false; // if the provided file doesn't even exist, give up!
			}
			else
			{
				Debug.WriteLine("CanonicalFullPath getter is about to be called on an uninitialised HawkFile");
			}

			CanonicalFullPath = file.CanonicalFullPath;

			(RomGame Rom, IEmulator NextEmulator, GameInfo Game) result = (null, null, null);
			try
			{
				if (OpenAdvanced is OpenAdvanced_Libretro)
				{
					// kind of dirty.. we need to stash this, and then we can unstash it in a moment, in case the core doesn't fail
					var oldGame = Global.Game;

					// must be done before LoadNoGame (which triggers retro_init and the paths to be consumed by the core)
					// game name == name of core
					Global.Game = result.Game = new GameInfo
					{
						Name = Path.GetFileNameWithoutExtension(launchLibretroCore),
						System = "Libretro"
					};
					var retro = new LibretroCore(nextComm, result.Game, launchLibretroCore);
					result.NextEmulator = retro;

					if (retro.Description.SupportsNoGame && string.IsNullOrEmpty(path))
					{
						// if we are allowed to run NoGame and we don't have a game, boot up the core that way
						var ret = retro.LoadNoGame();

						Global.Game = oldGame;
						if (!ret)
						{
							DoLoadErrorCallback("LibretroNoGame failed to load. This is weird", "Libretro");
							retro.Dispose();
							return false;
						}
					}
					else
					{
						bool ret;

						if (retro.Description.NeedsArchives)
						{
							// if the core requires an archive file, then try passing the filename of the archive
							// (but do we ever need to actually load the contents of the archive file into ram?)
							if (file.IsArchiveMember) throw new InvalidOperationException("Should not have bound file member for libretro block_extract core");
							ret = retro.LoadPath(file.FullPathWithoutMember);
						}
						else
						{
							// otherwise load the data or pass the filename, as requested. but..
							if (retro.Description.NeedsRomAsPath)
							{
								if (file.IsArchiveMember) throw new InvalidOperationException("Cannot pass archive member to libretro needs_fullpath core");
								ret = retro.LoadPath(file.FullPathWithoutMember);
							}
							else
							{
								ret = HandleArchiveBinding(file) && retro.LoadData(file.ReadAllBytes(), file.Name);
							}
						}

						Global.Game = oldGame;
						if (!ret)
						{
							DoLoadErrorCallback("Libretro failed to load the given file. This is probably due to a core/content mismatch. Moreover, the process is now likely to be hosed. We suggest you restart the program.", "Libretro");
							retro.Dispose();
							return false;
						}
					}
				}
				else
				{
					// not libretro => do extension checking
					var fileExt = file.Extension.ToLowerInvariant();

					// at this point, file is either assigned to the ROM path, if it exists, or is empty and CoreComm is not a libretro core
					// so, we still need to check path here before continuing
					if (string.IsNullOrEmpty(path))
					{
						Console.WriteLine("No ROM to Load");
						return false;
					}

					// do the archive binding we had to skip
					if (!HandleArchiveBinding(file)) return false;

					var cancel = false;
#if true
					bool success;
					if (fileExt == ".m3u") success = TryLoadFromM3UFormatRom(ref result, path, nextComm, file);
					else if (Disc.IsValidExtension(fileExt)) success = TryLoadFromDiscFormatRom(ref result, path, nextComm, file, fileExt);
					else success = fileExt switch
						{
							".xml" => TryLoadFromXMLFormatRom(ref result, path, nextComm, file),
							".psf" => TryLoadFromPSFFormatRom(ref result, path, nextComm, file),
							".minipsf" => TryLoadFromPSFFormatRom(ref result, path, nextComm, file),
							_ => TryLoadFromMiscFormatRom(ref result, ref cancel, path, nextComm, forceAccurateCore, file, fileExt)
						};
#else //TODO by changing the execution order, you can make the above look much nicer --yoshi
					var success = fileExt switch
					{
						".m3u" => TryLoadFromM3UFormatRom(ref result, path, nextComm, file),
						".minipsf" => TryLoadFromPSFFormatRom(ref result, path, nextComm, file),
						".psf" => TryLoadFromPSFFormatRom(ref result, path, nextComm, file),
						".xml" => TryLoadFromXMLFormatRom(ref result, path, nextComm, file),
						_ => Disc.IsValidExtension(fileExt)
							? TryLoadFromDiscFormatRom(ref result, path, nextComm, file, fileExt)
							: TryLoadFromMiscFormatRom(ref result, ref cancel, path, nextComm, forceAccurateCore, file, fileExt)
					};
#endif
					if (!success) return false;
					if (result.NextEmulator == null)
					{
						if (!cancel) DoLoadErrorCallback("No core could load the rom.", null);
						return false;
					}
				}

				Rom = result.Rom;
				LoadedEmulator = result.NextEmulator;
				Game = result.Game;
				return true;
			}
			catch (Exception ex)
			{
				while (ex.InnerException != null) ex = ex.InnerException; // all of the specific exceptions we're trying to catch here aren't expected to have inner exceptions, so drill down in case we got a TargetInvocationException or something like that
				var system = result.Game?.System; //TODO We wouldn't need to pass the `result` tuple around if this could be passed via the exceptions' ctors (difficult since this var is used in the default case below). Passing the tuple around isn't the worst thing, but it prevents the TryLoadFrom*FormatRom helpers from being pure (without side-effects). --yoshi
				switch (ex)
				{
					case UnsupportedGameException _:
						// Specific hack here, as we get more cores of the same system, this isn't scalable
						if (system == "NES") DoMessageCallback("Unable to use quicknes, using NESHawk instead");
						return LoadRom(path, nextComm, launchLibretroCore, true, recursiveCount + 1);
					case MissingFirmwareException mfe:
						DoLoadErrorCallback(mfe.Message, system, path, Deterministic, LoadErrorType.MissingFirmware);
						break;
					case CGBNotSupportedException _:
						// failed to load SGB bios or game does not support SGB mode. To avoid catch-22, disable SGB mode
						Global.Config.GbAsSgb = false;
						DoMessageCallback("Failed to load a GB rom in SGB mode.  Disabling SGB Mode.");
						return LoadRom(path, nextComm, launchLibretroCore, false, recursiveCount + 1);
					case NoAvailableCoreException nace:
						// handle exceptions thrown by the new detected systems that BizHawk does not have cores for
						DoLoadErrorCallback($"{nace.Message}\n\n{nace}", system);
						break;
					default:
						DoLoadErrorCallback($"A core accepted the rom, but threw an exception while loading it:\n\n{ex}", system);
						break;
				}
				return false;
			}
		}
	}
}
