using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using BizHawk.Common.PathExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Emulation.Common
{
	public static class EmulatorExtensions
	{
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

		// TODO: a better place for these
		public static string CoreName(this Type type)
		{
			if (type == null)
			{
				return "";
			}

			var attr = (CoreAttribute)Attribute.GetCustomAttribute(type, typeof(CoreAttribute));
			return attr?.CoreName ?? "";
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
		/// See ToBoolButtonNameList(). Works the same except with float controls
		/// </summary>
		public static List<string> ToFloatControlNameList(this IController controller, int? controllerNum = null)
		{
			return ToControlNameList(controller.Definition.FloatControls, controllerNum);
		}

		private static List<string> ToControlNameList(List<string> buttonList, int? controllerNum = null)
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

		public static IDictionary<string, dynamic> ToDictionary(this IController controller, int? controllerNum = null)
		{
			var buttons = new Dictionary<string, dynamic>();

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
			foreach (var button in controller.Definition.FloatControls)
			{
				if (controllerNum == null)
				{
					buttons[button] = controller.GetFloat(button);
				}
				else if (button.Length > 2 && button.Substring(0, 2) == $"P{controllerNum}")
				{
					var sub = button.Substring(3);
					buttons[sub] = controller.GetFloat($"P{controllerNum} {sub}");
				}
			}

			return buttons;
		}

		public static string FilesystemSafeName(this GameInfo game)
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
	}
}