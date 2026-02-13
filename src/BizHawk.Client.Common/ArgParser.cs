#nullable enable

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net.Sockets;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	/// <summary>Parses command-line flags into a <see cref="ParsedCLIFlags"/> struct.</summary>
	public static class ArgParser
	{
		private static readonly Argument<string?> ArgumentRomFilePath = new("rom")
		{
			DefaultValueFactory = _ => null,
			Description = "path; if specified, the file will be loaded the same way as it would be from `File` > `Open...`; this argument can and should be given LAST despite what it says at the top of --help",
		};

		private static readonly Option<string?> OptionAVDumpAudioSync = new("--audiosync")
		{
			Description = "bool; `true` is the only truthy value, all else falsey; if not set, uses remembered state from config",
		};

		private static readonly Option<int?> OptionAVDumpEndAtFrame = new("--dump-length")
		{
			Description = "int; frame index at which to stop A/V dumping (encoding)",
		};

		private static readonly Option<string?> OptionAVDumpFrameList = new("--dump-frames"); // desc added in static ctor

		private static readonly Option<string?> OptionAVDumpName = new("--dump-name"); // desc added in static ctor

		private static readonly Option<bool> OptionAVDumpQuitWhenDone = new("--dump-close")
		{
			Description = "pass to quit completely after A/V dumping (encoding) finishes",
		};

		private static readonly Option<string?> OptionAVDumpType = new("--dump-type"); // desc added in static ctor

		private static readonly Option<string?> OptionConfigFilePath = new("--config")
		{
			Description = "path of config file to use",
		};

		private static readonly Option<bool> OptionGDIPlus = new("--gdi")
		{
			Description = "use the GDI+ display method rather than whatever preference is set in the config file",
		};

		private static readonly Option<string?> OptionHTTPClientURIGET = new("--url-get", "--url_get")
		{
			Description = "string; URI to use for HTTP 'GET' IPC (Lua `comm.http*Get*`)",
		};

		private static readonly Option<string?> OptionHTTPClientURIPOST = new("--url-post", "--url_post")
		{
			Description = "string; URI to use for HTTP 'POST' IPC (Lua `comm.http*Post*`)",
		};

		private static readonly Option<bool> OptionLaunchChromeless = new("--chromeless")
		{
			Description = "never show the GUI (a.k.a. 'chrome'), not even in windowed mode",
		};

		private static readonly Option<bool> OptionLaunchFullscreen = new("--fullscreen")
		{
			Description = "launch in fullscreen",
		};

		private static readonly Option<int?> OptionLoadQuicksaveSlot = new("--load-slot")
		{
			Description = "int; quicksave slot which should be loaded on launch",
		};

		private static readonly Option<string?> OptionLoadSavestateFilePath = new("--load-state"); // desc added in static ctor

		private static readonly Option<string?> OptionLuaFilePath = new("--lua"); // desc added in static ctor

		private static readonly Option<string?> OptionMMFPath = new("--mmf")
		{
			Description = "path of file to use for 'memory-mapped file' IPC (Lua `comm.mmf*`)",
		};

		private static readonly Option<string?> OptionMovieFilePath = new("--movie")
		{
			Description = "path; input movie which should be loaded on launch",
		};

		private static readonly Option<string?> OptionOpenExternalTool = new("--open-ext-tool-dll")
		{
			Description = "the first ext. tool from ExternalToolManager.ToolStripMenu which satisfies both of these will be opened: 1) available (no load errors, correct system/rom, etc.) and 2) dll path matches given string; or dll filename matches given string with or without `.dll`",
		};

		private static readonly Option<bool> OptionOpenLuaConsole = new("--luaconsole")
		{
			Description = "open the Lua Console, even if not loading a script",
		};

		private static readonly Option<bool> OptionQueryAppVersion = new("--version")
		{
			Description = "print version information and immediately exit",
		};

		private static readonly Option<string?> OptionSocketServerIP = new("--socket-ip", "--socket_ip"); // desc added in static ctor

		private static readonly Option<ushort?> OptionSocketServerPort = new("--socket-port", "--socket_port"); // desc added in static ctor

		private static readonly Option<bool> OptionSocketServerUseUDP = new("--socket-udp", "--socket_udp"); // desc added in static ctor

		private static readonly Option<string?> OptionUserdataUnparsedPairs = new("--userdata")
		{
			Description = "pairs in the format `k1:v1;k2:v2` (mind your shell escape sequences); if the value is `true`/`false` it's interpreted as a boolean, if it's a valid 32-bit signed integer e.g. `-1234` it's interpreted as such, if it's a valid 32-bit float e.g. `12.34` it's interpreted as such, else it's interpreted as a string",
		};

		static ArgParser()
		{
			OptionAVDumpFrameList.Description = $"comma-separated list of integers, indices of frames which should be included in the A/V dump (encoding); implies `{OptionAVDumpEndAtFrame.Name}=<end>` where `<end>` is the highest frame listed";
			OptionAVDumpName.Description = $"ignored unless `{OptionAVDumpType.Name}` also passed";
			OptionAVDumpType.Description = $"ignored unless `{OptionAVDumpName.Name}` also passed";
			OptionLoadSavestateFilePath.Description = $"path; savestate which should be loaded on launch; this takes precedence over `{OptionLoadQuicksaveSlot.Name}`";
			OptionLuaFilePath.Description = $"path; Lua script or Console session to load; implies `{OptionOpenLuaConsole.Name}`";
			OptionSocketServerIP.Description = $"string; IP address for Unix socket IPC (Lua `comm.socket*`); must be paired with `{OptionSocketServerPort.Name}`";
			OptionSocketServerPort.Description = $"int; port for Unix socket IPC (Lua `comm.socket*`); must be paired with `{OptionSocketServerIP.Name}`";
			OptionSocketServerUseUDP.Description = $"pass to use UDP instead of TCP for Unix socket IPC (Lua `comm.socket*`); ignored unless `{OptionSocketServerIP.Name} {OptionSocketServerPort.Name}` also passed";
		}

		private static RootCommand GetRootCommand()
		{
			RootCommand root = new($"{
				(string.IsNullOrEmpty(VersionInfo.CustomBuildString) ? "EmuHawk" : VersionInfo.CustomBuildString)
			}, a multi-system emulator frontend\n{VersionInfo.GetEmuVersion()}");
			root.Add(ArgumentRomFilePath);
			root.Options.RemoveAll(option => option is VersionOption); // we have our own version command

			// `--help` uses this order, so keep alphabetised by flag
			root.Add(/* --audiosync */ OptionAVDumpAudioSync);
			root.Add(/* --chromeless */ OptionLaunchChromeless);
			root.Add(/* --config */ OptionConfigFilePath);
			root.Add(/* --dump-close */ OptionAVDumpQuitWhenDone);
			root.Add(/* --dump-frames */ OptionAVDumpFrameList);
			root.Add(/* --dump-length */ OptionAVDumpEndAtFrame);
			root.Add(/* --dump-name */ OptionAVDumpName);
			root.Add(/* --dump-type */ OptionAVDumpType);
			root.Add(/* --fullscreen */ OptionLaunchFullscreen);
			root.Add(/* --gdi */ OptionGDIPlus);
			root.Add(/* --load-slot */ OptionLoadQuicksaveSlot);
			root.Add(/* --load-state */ OptionLoadSavestateFilePath);
			root.Add(/* --lua */ OptionLuaFilePath);
			root.Add(/* --luaconsole */ OptionOpenLuaConsole);
			root.Add(/* --mmf */ OptionMMFPath);
			root.Add(/* --movie */ OptionMovieFilePath);
			root.Add(/* --open-ext-tool-dll */ OptionOpenExternalTool);
			root.Add(/* --socket-ip */ OptionSocketServerIP);
			root.Add(/* --socket-port */ OptionSocketServerPort);
			root.Add(/* --socket-udp */ OptionSocketServerUseUDP);
			root.Add(/* --url-get */ OptionHTTPClientURIGET);
			root.Add(/* --url-post */ OptionHTTPClientURIPOST);
			root.Add(/* --userdata */ OptionUserdataUnparsedPairs);
			root.Add(/* --version */ OptionQueryAppVersion);

			return root;
		}

		private static void EnsureConsole()
		{
			if (!OSTailoredCode.IsUnixHost)
			{
				// the behavior of this kinda sucks, but it's better than nothing I think
				Win32Imports.AttachConsole(Win32Imports.ATTACH_PARENT_PROCESS);
			}
		}

		/// <return>exit code, or <see langword="null"/> if should not exit</return>
		/// <exception cref="ArgParserException">parsing failure, or invariant broken</exception>
		public static int? ParseArguments(out ParsedCLIFlags parsed, string[] args, bool fromUnitTest = false)
		{
			parsed = default;
			if (!fromUnitTest && args.Length is not 0) Console.Error.WriteLine($"parsing command-line flags: {string.Join(" ", args)}");
			var rootCommand = GetRootCommand();
			var result = CommandLineParser.Parse(rootCommand, args);
			if (result.Errors.Count is not 0)
			{
				// generate useful commandline error output
				EnsureConsole();
				if (!fromUnitTest) result.Invoke();
				// show first error in modal dialog (done in `catch` block in `Program`)
				throw new ArgParserException($"failed to parse command-line arguments: {result.Errors[0].Message}");
			}
			if (result.Action is not null)
			{
				// means e.g. `./EmuHawkMono.sh --help` was passed, run whatever behaviour it normally has
				EnsureConsole();
				return fromUnitTest ? 0 : result.Invoke();
			}
			if (result.GetValue(OptionQueryAppVersion))
			{
				// means e.g. `./EmuHawkMono.sh --version` was passed, so print that and exit immediately
				EnsureConsole();
				if (!fromUnitTest) Console.WriteLine(VersionInfo.GetEmuVersion());
				return 0;
			}

			var autoDumpLength = result.GetValue(OptionAVDumpEndAtFrame);
			HashSet<int>? currAviWriterFrameList = null;
			if (result.GetValue(OptionAVDumpFrameList) is string list)
			{
				currAviWriterFrameList = new();
				currAviWriterFrameList.AddRange(list.Split(',').Select(int.Parse));
				// automatically set dump length to maximum frame
				autoDumpLength ??= currAviWriterFrameList.Max();
			}

			var luaScript = result.GetValue(OptionLuaFilePath);
			var luaConsole = luaScript is not null || result.GetValue(OptionOpenLuaConsole);

			var socketIP = result.GetValue(OptionSocketServerIP);
			var socketPort = result.GetValue(OptionSocketServerPort);
			var socketAddress = socketIP is null && socketPort is null
				? ((string, ushort)?) null // don't bother
				: socketIP is not null && socketPort is not null
					? (socketIP, socketPort.Value)
					: throw new ArgParserException("Socket server needs both --socket_ip and --socket_port. Socket server was not started");

			var httpClientURIGET = result.GetValue(OptionHTTPClientURIGET);
			var httpClientURIPOST = result.GetValue(OptionHTTPClientURIPOST);
			var httpAddresses = httpClientURIGET is null && httpClientURIPOST is null
					? ((string?, string?)?) null // don't bother
					: (httpClientURIGET, httpClientURIPOST);

			var audiosync = result.GetValue(OptionAVDumpAudioSync)?.EqualsIgnoreCase("true");

			List<(string Key, string Value)>? userdataUnparsedPairs = null;
			if (result.GetValue(OptionUserdataUnparsedPairs) is string list1)
			{
				userdataUnparsedPairs = new();
				foreach (var s in list1.Split(';'))
				{
					var iColon = s.IndexOf(':');
					if (iColon is -1) throw new ArgParserException("malformed userdata (';' without ':')");
					userdataUnparsedPairs.Add((s.Substring(startIndex: 0, length: iColon), s.Substring(iColon + 1)));
				}
			}

			parsed = new(
				cmdLoadSlot: result.GetValue(OptionLoadQuicksaveSlot),
				cmdLoadState: result.GetValue(OptionLoadSavestateFilePath),
				cmdConfigFile: result.GetValue(OptionConfigFilePath),
				cmdMovie: result.GetValue(OptionMovieFilePath),
				cmdDumpType: result.GetValue(OptionAVDumpType),
				currAviWriterFrameList: currAviWriterFrameList,
				autoDumpLength: autoDumpLength ?? 0,
				cmdDumpName: result.GetValue(OptionAVDumpName),
				autoCloseOnDump: result.GetValue(OptionAVDumpQuitWhenDone),
				chromeless: result.GetValue(OptionLaunchChromeless),
				startFullscreen: result.GetValue(OptionLaunchFullscreen),
				gdiPlusRequested: result.GetValue(OptionGDIPlus),
				luaScript: luaScript,
				luaConsole: luaConsole,
				socketAddress: socketAddress,
				mmfFilename: result.GetValue(OptionMMFPath),
				httpAddresses: httpAddresses,
				audiosync: audiosync,
				openExtToolDll: result.GetValue(OptionOpenExternalTool),
				socketProtocol: result.GetValue(OptionSocketServerUseUDP) ? ProtocolType.Udp : ProtocolType.Tcp,
				userdataUnparsedPairs: userdataUnparsedPairs,
				cmdRom: result.GetValue(ArgumentRomFilePath)
			);
			return null;
		}

		public sealed class ArgParserException : Exception
		{
			public ArgParserException(string message) : base(message) {}
		}
	}
}
