#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Sockets;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.StringExtensions;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Parses command line flags from a string array into various instance fields.
	/// </summary>
	/// <remarks>
	/// If a flag is given multiple times, the last is taken.<br/>
	/// If a flag that isn't recognised is given, it is parsed as a filename. As noted above, the last filename is taken.
	/// </remarks>
	public static class ArgParser
	{
		/// <exception cref="ArgParserException"><c>--socket_ip</c> passed without specifying <c>--socket_port</c> or vice-versa</exception>
		public static void ParseArguments(out ParsedCLIFlags parsed, string[] args)
		{
			string? cmdLoadSlot = null;
			string? cmdLoadState = null;
			string? cmdConfigFile = null;
			string? cmdMovie = null;
			string? cmdDumpType = null;
			HashSet<int>? currAviWriterFrameList = null;
			int? autoDumpLength = null;
			bool? printVersion = null;
			string? cmdDumpName = null;
			bool? autoCloseOnDump = null;
			bool? chromeless = null;
			bool? startFullscreen = null;
			string? luaScript = null;
			bool? luaConsole = null;
			ushort? socketPort = null;
			string? socketIP = null;
			string? mmfFilename = null;
			string? urlGet = null;
			string? urlPost = null;
			bool? audiosync = null;
			string? openExtToolDll = null;
			var socketProtocol = ProtocolType.Tcp;
			List<(string Key, string Value)>? userdataUnparsedPairs = null;
			string? cmdRom = null;

			for (var i = 0; i < args.Length; i++)
			{
				var arg = args[i];

				if (arg == ">")
				{
					// For some reason sometimes visual studio will pass this to us on the commandline. it makes no sense.
					var stdout = args[++i];
					Console.SetOut(new StreamWriter(stdout));
					continue;
				}

				var argDowncased = arg.ToLowerInvariant();
				if (argDowncased.StartsWithOrdinal("--load-slot="))
				{
					cmdLoadSlot = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--load-state="))
				{
					cmdLoadState = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--config="))
				{
					cmdConfigFile = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--movie="))
				{
					cmdMovie = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--dump-type="))
				{
					// ignored unless `--dump-name` also passed
					cmdDumpType = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--dump-frames="))
				{
					// comma-separated list of integers, indices of frames which should be included in the A/V dump (encoding)
					string list = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
					currAviWriterFrameList = new();
					currAviWriterFrameList.AddRange(list.Split(',').Select(int.Parse));
					// automatically set dump length to maximum frame
					autoDumpLength = currAviWriterFrameList.Max();
				}
				else if (argDowncased.StartsWithOrdinal("--version"))
				{
					printVersion = true;
				}
				else if (argDowncased.StartsWithOrdinal("--dump-name="))
				{
					// ignored unless `--dump-type` also passed
					cmdDumpName = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--dump-length="))
				{
					var len = int.TryParse(argDowncased.Substring(argDowncased.IndexOf('=') + 1), out var i1) ? i1 : default;
					autoDumpLength = len;
				}
				else if (argDowncased.StartsWithOrdinal("--dump-close"))
				{
					autoCloseOnDump = true;
				}
				else if (argDowncased.StartsWithOrdinal("--chromeless"))
				{
					// chrome is never shown, even in windowed mode
					chromeless = true;
				}
				else if (argDowncased.StartsWithOrdinal("--fullscreen"))
				{
					startFullscreen = true;
				}
				else if (argDowncased.StartsWithOrdinal("--lua="))
				{
					luaScript = arg.Substring(arg.IndexOf('=') + 1);
					// implies `--luaconsole`
					luaConsole = true;
				}
				else if (argDowncased.StartsWithOrdinal("--luaconsole"))
				{
					luaConsole = true;
				}
				else if (argDowncased.StartsWithOrdinal("--socket_port="))
				{
					// must be paired with `--socket_ip`
					var port = ushort.TryParse(arg.Substring(14), out var i1) ? i1 : (ushort) 0;
					if (port > 0) socketPort = port;
				}
				else if (argDowncased.StartsWithOrdinal("--socket_ip="))
				{
					// must be paired with `--socket_port`
					socketIP = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--socket_udp"))
				{
					// ignored unless `--socket_ip --socket_port` also passed
					socketProtocol = ProtocolType.Udp;
				}
				else if (argDowncased.StartsWithOrdinal("--mmf="))
				{
					mmfFilename = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--url_get="))
				{
					urlGet = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--url_post="))
				{
					urlPost = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWithOrdinal("--audiosync="))
				{
					// `true` is the only truthy value, all else falsey
					// if not set, uses remembered state from config
					audiosync = argDowncased.Substring(argDowncased.IndexOf('=') + 1) == "true";
				}
				else if (argDowncased.StartsWithOrdinal("--open-ext-tool-dll="))
				{
					// the first ext. tool from ExternalToolManager.ToolStripMenu which satisfies both of these will be opened:
					// - available (no load errors, correct system/rom, etc.)
					// - dll path matches given string; or dll filename matches given string with or without `.dll`
					openExtToolDll = arg.Substring(20);
				}
				else if (argDowncased.StartsWithOrdinal("--userdata="))
				{
					// pairs in the format `k1:v1;k2:v2` (mind your shell escape sequences)
					// if the value is `true`/`false` it's interpreted as a boolean,
					// if it's a valid 32-bit signed integer e.g. `-1234` it's interpreted as such, if it's a valid 32-bit float e.g. `12.34` it's interpreted as such,
					// else it's interpreted as a string
					userdataUnparsedPairs = new();
					foreach (var s in arg.Substring(11).Split(';'))
					{
						var iColon = s.IndexOf(':');
						if (iColon is -1) throw new ArgParserException("malformed userdata (';' without ':')");
						userdataUnparsedPairs.Add((s.Substring(startIndex: 0, length: iColon), s.Substring(iColon + 1)));
					}
				}
				else
				{
					cmdRom = arg;
				}
			}

			var httpAddresses = urlGet == null && urlPost == null
				? ((string?, string?)?) null // don't bother
				: (urlGet, urlPost);
			(string, ushort)? socketAddress;
			if (socketIP == null && socketPort == null)
			{
				socketAddress = null; // don't bother
			}
			else if (socketIP == null || socketPort == null)
			{
				throw new ArgParserException("Socket server needs both --socket_ip and --socket_port. Socket server was not started");
			}
			else
			{
				socketAddress = (socketIP, socketPort.Value);
			}

			parsed = new ParsedCLIFlags(
				cmdLoadSlot: cmdLoadSlot is null ? null : int.Parse(cmdLoadSlot),
				cmdLoadState: cmdLoadState,
				cmdConfigFile: cmdConfigFile,
				cmdMovie: cmdMovie,
				cmdDumpType: cmdDumpType,
				currAviWriterFrameList: currAviWriterFrameList,
				autoDumpLength: autoDumpLength ?? 0,
				printVersion: printVersion ?? false,
				cmdDumpName: cmdDumpName,
				autoCloseOnDump: autoCloseOnDump ?? false,
				chromeless: chromeless ?? false,
				startFullscreen: startFullscreen ?? false,
				luaScript: luaScript,
				luaConsole: luaConsole ?? false,
				socketAddress: socketAddress,
				mmfFilename: mmfFilename,
				httpAddresses: httpAddresses,
				audiosync: audiosync,
				openExtToolDll: openExtToolDll,
				socketProtocol: socketProtocol,
				userdataUnparsedPairs: userdataUnparsedPairs,
				cmdRom: cmdRom
			);
		}

		public sealed class ArgParserException : Exception
		{
			public ArgParserException(string message) : base(message) {}
		}
	}
}
