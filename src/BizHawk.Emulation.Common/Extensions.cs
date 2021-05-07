using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	public static class EmulatorExtensions
	{
		public static readonly IReadOnlyDictionary<string, string> SystemIDDisplayNames = new Dictionary<string, string>
		{
			["A26"] = "Atari 2600",
			["A78"] = "Atari 7800",
			["AmstradCPC"] = "Amstrad CPC",
			["AppleII"] = "Apple II",
			["C64"] = "Commodore 64",
			["ChannelF"] = "Channel F",
			["Coleco"] = "ColecoVision",
			["DGB"] = "Game Boy Link",
			["GB"] = "GB",
			["GB3x"] = "Game Boy Link 3x",
			["GB4x"] = "Game Boy Link 4x",
			["GBA"] = "Gameboy Advance",
			["GBC"] = "Gameboy Color",
			["GEN"] = "Genesis",
			["GG"] = "Game Gear",
			["INTV"] = "Intellivision",
			["Libretro"] = "Libretro",
			["Lynx"] = "Lynx",
			["MAME"] = "MAME",
			["N64"] = "Nintendo 64",
			["NDS"] = "NDS",
			["NES"] = "NES",
			["NGP"] = "Neo-Geo Pocket",
			["O2"] = "Odyssey2",
			["PCE"] = "TurboGrafx-16",
			["PCECD"] = "TurboGrafx - 16(CD)",
			["PCFX"] = "PCFX",
			["PSX"] = "PlayStation",
			["SAT"] = "Saturn",
			["SG"] = "SG-1000",
			["SGX"] = "SuperGrafx",
			["SMS"] = "Sega Master System",
			["SNES"] = "SNES",
			["TI83"] = "TI - 83",
			["UZE"] = "Uzebox",
			["VB"] = "Virtual Boy",
			["VEC"] = "Vectrex",
			["WSWAN"] = "WonderSwan",
			["ZXSpectrum"] = "ZX Spectrum",
		};

		public static CoreAttribute Attributes(this IEmulator core)
		{
			return (CoreAttribute)Attribute.GetCustomAttribute(core.GetType(), typeof(CoreAttribute));
		}

		// todo: most of the special cases involving the NullEmulator should probably go away
		public static bool IsNull(this IEmulator core)
		{
			return core == null || core is NullEmulator;
		}

		public static bool HasVideoProvider(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IVideoProvider>();
		}

		public static IVideoProvider AsVideoProvider(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IVideoProvider>();
		}

		/// <summary>
		/// Returns the core's VideoProvider, or a suitable dummy provider
		/// </summary>
		public static IVideoProvider AsVideoProviderOrDefault(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IVideoProvider>()
				?? NullVideo.Instance;
		}

		public static bool HasSoundProvider(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<ISoundProvider>();
		}

		public static ISoundProvider AsSoundProvider(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ISoundProvider>();
		}

		private static readonly ConditionalWeakTable<IEmulator, ISoundProvider> CachedNullSoundProviders = new ConditionalWeakTable<IEmulator, ISoundProvider>();

		/// <summary>
		/// returns the core's SoundProvider, or a suitable dummy provider
		/// </summary>
		public static ISoundProvider AsSoundProviderOrDefault(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ISoundProvider>()
				?? CachedNullSoundProviders.GetValue(core, e => new NullSound(core.VsyncNumerator(), core.VsyncDenominator()));
		}

		public static bool HasMemoryDomains(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IMemoryDomains>();
		}

		public static IMemoryDomains AsMemoryDomains(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IMemoryDomains>();
		}

		public static bool HasSaveRam(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<ISaveRam>();
		}

		public static ISaveRam AsSaveRam(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ISaveRam>();
		}

		public static bool HasSavestates(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IStatable>();
		}

		public static IStatable AsStatable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IStatable>();
		}

		public static bool CanPollInput(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IInputPollable>();
		}

		public static IInputPollable AsInputPollable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IInputPollable>();
		}

		public static bool InputCallbacksAvailable(this IEmulator core)
		{
			// TODO: this is a pretty ugly way to handle this
			var pollable = core?.ServiceProvider.GetService<IInputPollable>();
			if (pollable != null)
			{
				try
				{
					var callbacks = pollable.InputCallbacks;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}

			return false;
		}

		public static bool HasDriveLight(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IDriveLight>();
		}

		public static IDriveLight AsDriveLight(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IDriveLight>();
		}

		public static bool CanDebug(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IDebuggable>();
		}

		public static IDebuggable AsDebuggable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IDebuggable>();
		}

		public static bool CpuTraceAvailable(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<ITraceable>();
		}

		public static ITraceable AsTracer(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ITraceable>();
		}

		public static bool MemoryCallbacksAvailable(this IEmulator core)
		{
			// TODO: this is a pretty ugly way to handle this
			var debuggable = core?.ServiceProvider.GetService<IDebuggable>();
			if (debuggable != null)
			{
				try
				{
					var callbacks = debuggable.MemoryCallbacks;
					return true;
				}
				catch (NotImplementedException)
				{
					return false;
				}
			}

			return false;
		}

		public static bool MemoryCallbacksAvailable(this IDebuggable core)
		{
			if (core == null)
			{
				return false;
			}

			try
			{
				var callbacks = core.MemoryCallbacks;
				return true;
			}
			catch (NotImplementedException)
			{
				return false;
			}
		}

		public static bool CanDisassemble(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IDisassemblable>();
		}

		public static IDisassemblable AsDisassembler(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IDisassemblable>();
		}

		public static bool HasRegions(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IRegionable>();
		}

		public static IRegionable AsRegionable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IRegionable>();
		}

		public static bool CanCDLog(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<ICodeDataLogger>();
		}

		public static ICodeDataLogger AsCodeDataLogger(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ICodeDataLogger>();
		}

		public static ILinkable AsLinkable(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ILinkable>();
		}

		public static bool UsesLinkCable(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<ILinkable>();
		}

		public static bool CanGenerateGameDBEntries(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<ICreateGameDBEntries>();
		}

		public static ICreateGameDBEntries AsGameDBEntryGenerator(this IEmulator core)
		{
			return core.ServiceProvider.GetService<ICreateGameDBEntries>();
		}

		public static bool HasBoardInfo(this IEmulator core)
		{
			return core != null && core.ServiceProvider.HasService<IBoardInfo>();
		}

		public static IBoardInfo AsBoardInfo(this IEmulator core)
		{
			return core.ServiceProvider.GetService<IBoardInfo>();
		}

		public static (int X, int Y) ScreenLogicalOffsets(this IEmulator core)
		{
			if (core != null && core.ServiceProvider.HasService<IVideoLogicalOffsets>())
			{
				var offsets = core.ServiceProvider.GetService<IVideoLogicalOffsets>();
				return (offsets.ScreenX, offsets.ScreenY);
			}

			return (0, 0);
		}

		public static string RomDetails(this IEmulator core)
		{
			if (core != null && core.ServiceProvider.HasService<IRomInfo>())
			{
				return core.ServiceProvider.GetService<IRomInfo>().RomDetails;
			}

			return "";
		}

		public static int VsyncNumerator(this IEmulator core)
		{
			if (core != null && core.HasVideoProvider())
			{
				return core.AsVideoProvider().VsyncNumerator;
			}

			return 60;
		}

		public static int VsyncDenominator(this IEmulator core)
		{
			if (core != null && core.HasVideoProvider())
			{
				return core.AsVideoProvider().VsyncDenominator;
			}

			return 1;
		}

		public static double VsyncRate(this IEmulator core)
		{
			return core.VsyncNumerator() / (double)core.VsyncDenominator();
		}

		public static bool IsImplemented(this MethodInfo info)
		{
			return !info.GetCustomAttributes(false).Any(a => a is FeatureNotImplementedAttribute);
		}

		/// <summary>
		/// Gets a list of boolean button names. If a controller number is specified, only returns button names
		/// (without the "P" prefix) that match that controller number. If a controller number is NOT specified,
		/// then all button names are returned.
		///
		/// For example, consider example "P1 A", "P1 B", "P2 A", "P2 B". See below for sample outputs:
		///   - ToBoolButtonNameList(controller, 1) -> [A, B]
		///   - ToBoolButtonNameList(controller, 2) -> [A, B]
		///   - ToBoolButtonNameList(controller, null) -> [P1 A, P1 B, P2 A, P2 B]
		/// </summary>
		public static List<string> ToBoolButtonNameList(this IController controller, int? controllerNum = null)
		{
			return ToControlNameList(controller.Definition.BoolButtons, controllerNum);
		}

		/// <summary>
		/// See ToBoolButtonNameList(). Works the same except with axes
		/// </summary>
		public static List<string> ToAxisControlNameList(this IController controller, int? controllerNum = null)
		{
			return ToControlNameList(controller.Definition.Axes.Keys, controllerNum);
		}

		private static List<string> ToControlNameList(IEnumerable<string> buttonList, int? controllerNum = null)
		{
			var buttons = new List<string>();
			foreach (var button in buttonList)
			{
				if (controllerNum != null && button.Length > 2 && button.Substring(0, 2) == $"P{controllerNum}")
				{
					var sub = button.Substring(3);
					buttons.Add(sub);
				}
				else if (controllerNum == null)
				{
					buttons.Add(button);
				}
			}
			return buttons;
		}

		public static IDictionary<string, object> ToDictionary(this IController controller, int? controllerNum = null)
		{
			var buttons = new Dictionary<string, object>();

			foreach (var button in controller.Definition.BoolButtons)
			{
				if (controllerNum == null)
				{
					buttons[button] = controller.IsPressed(button);
				}
				else if (button.Length > 2 && button.Substring(0, 2) == $"P{controllerNum}")
				{
					var sub = button.Substring(3);
					buttons[sub] = controller.IsPressed($"P{controllerNum} {sub}");
				}
			}
			foreach (var button in controller.Definition.Axes.Keys)
			{
				if (controllerNum == null)
				{
					buttons[button] = controller.AxisValue(button);
				}
				else if (button.Length > 2 && button.Substring(0, 2) == $"P{controllerNum}")
				{
					var sub = button.Substring(3);
					buttons[sub] = controller.AxisValue($"P{controllerNum} {sub}");
				}
			}

			return buttons;
		}

		public static string FilesystemSafeName(this IGameInfo game)
		{
			var pass1 = game.Name
				.Replace('/', '+') // '/' is the path dir separator, obviously (methods in Path will treat it as such, even on Windows)
				.Replace('|', '+') // '|' is the filename-member separator for archives in HawkFile
				.Replace(":", " -") // ':' is the path separator in lists (Path.GetFileName will drop all but the last entry in such a list)
				.Replace("\"", ""); // '"' is just annoying as it needs escaping on the command-line
			var filesystemDir = Path.GetDirectoryName(pass1);
			var pass2 = Path.GetFileName(pass1).RemoveInvalidFileSystemChars();
			return Path.Combine(filesystemDir, pass2.RemoveSuffix('.')); // trailing '.' would be duplicated when file extension is added
		}

		/// <summary>
		/// Adds an axis to the receiver <see cref="ControllerDefinition"/>, and returns it.
		/// The new axis will appear after any that were previously defined.
		/// </summary>
		/// <param name="constraint">pass only for one axis in a pair, by convention the X axis</param>
		/// <returns>identical reference to <paramref name="def"/>; the object is mutated</returns>
		public static ControllerDefinition AddAxis(this ControllerDefinition def, string name, Range<int> range, int neutral, bool isReversed = false, AxisConstraint constraint = null)
		{
			def.Axes.Add(name, new AxisSpec(range, neutral, isReversed, constraint));
			return def;
		}

		/// <summary>
		/// Adds an X/Y pair of axes to the receiver <see cref="ControllerDefinition"/>, and returns it.
		/// The new axes will appear after any that were previously defined.
		/// </summary>
		/// <param name="nameFormat">format string e.g. <c>"P1 Left {0}"</c> (will be used to interpolate <c>"X"</c> and <c>"Y"</c>)</param>
		/// <returns>identical reference to <paramref name="def"/>; the object is mutated</returns>
		public static ControllerDefinition AddXYPair(this ControllerDefinition def, string nameFormat, AxisPairOrientation pDir, Range<int> rangeX, int neutralX, Range<int> rangeY, int neutralY, AxisConstraint constraint = null)
		{
			var yAxisName = string.Format(nameFormat, "Y");
			var finalConstraint = constraint ?? new NoOpAxisConstraint(yAxisName);
			return def.AddAxis(string.Format(nameFormat, "X"), rangeX, neutralX, ((byte) pDir & 2) != 0, finalConstraint)
				.AddAxis(yAxisName, rangeY, neutralY, ((byte) pDir & 1) != 0);
		}

		/// <summary>
		/// Adds an X/Y pair of axes to the receiver <see cref="ControllerDefinition"/>, and returns it.
		/// The new axes will appear after any that were previously defined.
		/// </summary>
		/// <param name="nameFormat">format string e.g. <c>"P1 Left {0}"</c> (will be used to interpolate <c>"X"</c> and <c>"Y"</c>)</param>
		/// <returns>identical reference to <paramref name="def"/>; the object is mutated</returns>
		public static ControllerDefinition AddXYPair(this ControllerDefinition def, string nameFormat, AxisPairOrientation pDir, Range<int> rangeBoth, int neutralBoth, AxisConstraint constraint = null)
			=> def.AddXYPair(nameFormat, pDir, rangeBoth, neutralBoth, rangeBoth, neutralBoth, constraint);

		/// <summary>
		/// Adds an X/Y/Z triple of axes to the receiver <see cref="ControllerDefinition"/>, and returns it.
		/// The new axes will appear after any that were previously defined.
		/// </summary>
		/// <param name="nameFormat">format string e.g. <c>"P1 Tilt {0}"</c> (will be used to interpolate <c>"X"</c>, <c>"Y"</c>, and <c>"Z"</c>)</param>
		/// <returns>identical reference to <paramref name="def"/>; the object is mutated</returns>
		public static ControllerDefinition AddXYZTriple(this ControllerDefinition def, string nameFormat, Range<int> rangeAll, int neutralAll)
			=> def.AddAxis(string.Format(nameFormat, "X"), rangeAll, neutralAll)
				.AddAxis(string.Format(nameFormat, "Y"), rangeAll, neutralAll)
				.AddAxis(string.Format(nameFormat, "Z"), rangeAll, neutralAll);

		public static AxisSpec With(this in AxisSpec spec, Range<int> range, int neutral) => new AxisSpec(range, neutral, spec.IsReversed, spec.Constraint);

		/// <summary>Get a firmware as a byte array</summary>
		/// <param name="sysId">the core systemID</param>
		/// <param name="firmwareId">the firmware id</param>
		/// <param name="required">if true, result is guaranteed to be valid; else null is possible if not found</param>
		/// <param name="msg">message to show if fail to get</param>
		/// <remarks>TODO inline (only change is wrapping strings in <see cref="FirmwareID"/> ctor, these IDs should probably be consts in each core's class)</remarks>
		public static byte[] GetFirmware(this ICoreFileProvider cfp, string sysId, string firmwareId, bool required, string msg = null)
			=> cfp.GetFirmware(new(system: sysId, firmware: firmwareId), required: required, msg: msg);

		/// <remarks>TODO inline (only change is wrapping strings in <see cref="FirmwareID"/> ctor, these IDs should probably be consts in each core's class)</remarks>
		public static byte[] GetFirmwareWithGameInfo(this ICoreFileProvider cfp, string sysId, string firmwareId, bool required, out GameInfo gi, string msg = null)
			=> cfp.GetFirmwareWithGameInfo(new(system: sysId, firmware: firmwareId), required: required, out gi, msg: msg);

		public static string SystemIDToDisplayName(string sysID)
			=> SystemIDDisplayNames.TryGetValue(sysID, out var dispName) ? dispName : string.Empty;
	}
}
