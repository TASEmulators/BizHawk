#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
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
		public static void ParseArguments(out ParsedCLIFlags parsed, string[] args, Func<byte[]> takeScreenshotCallback)
		{
			string? cmdLoadSlot = null;
			string? cmdLoadState = null;
			string? cmdConfigPath = null;
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
			int? socketPort = null;
			string? socketIP = null;
			string? mmfFilename = null;
			string? urlGet = null;
			string? urlPost = null;
			bool? audiosync = null;
			string? openExtToolDll = null;
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

				var argDowncased = arg.ToLower();
				if (argDowncased.StartsWith("--load-slot="))
				{
					cmdLoadSlot = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--load-state="))
				{
					cmdLoadState = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--config="))
				{
					cmdConfigFile = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--movie="))
				{
					cmdMovie = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--dump-type="))
				{
					cmdDumpType = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--dump-frames="))
				{
					string list = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
					string[] items = list.Split(',');
					currAviWriterFrameList = new HashSet<int>();
					foreach (string item in items)
					{
						currAviWriterFrameList.Add(int.Parse(item));
					}

					// automatically set dump length to maximum frame
					autoDumpLength = currAviWriterFrameList.OrderBy(x => x).Last();
				}
				else if (argDowncased.StartsWith("--version"))
				{
					printVersion = true;
				}
				else if (argDowncased.StartsWith("--dump-name="))
				{
					cmdDumpName = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--dump-length="))
				{
					int.TryParse(argDowncased.Substring(argDowncased.IndexOf('=') + 1), out var len);
					autoDumpLength = len;
				}
				else if (argDowncased.StartsWith("--dump-close"))
				{
					autoCloseOnDump = true;
				}
				else if (argDowncased.StartsWith("--chromeless"))
				{
					// chrome is never shown, even in windowed mode
					chromeless = true;
				}
				else if (argDowncased.StartsWith("--fullscreen"))
				{
					startFullscreen = true;
				}
				else if (argDowncased.StartsWith("--lua="))
				{
					luaScript = arg.Substring(arg.IndexOf('=') + 1);
					luaConsole = true;
				}
				else if (argDowncased.StartsWith("--luaconsole"))
				{
					luaConsole = true;
				}
				else if (argDowncased.StartsWith("--socket_port="))
				{
					int.TryParse(argDowncased.Substring(argDowncased.IndexOf('=') + 1), out var port);
					if (port > 0) socketPort = port;
				}
				else if (argDowncased.StartsWith("--socket_ip="))
				{
					socketIP = argDowncased.Substring(argDowncased.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--mmf="))
				{
					mmfFilename = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--url_get="))
				{
					urlGet = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--url_post="))
				{
					urlPost = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (argDowncased.StartsWith("--audiosync="))
				{
					audiosync = argDowncased.Substring(argDowncased.IndexOf('=') + 1) == "true";
				}
				else if (argDowncased.StartsWith("--open-ext-tool-dll="))
				{
					// the first ext. tool from ExternalToolManager.ToolStripMenu which satisfies both of these will be opened:
					// - available (no load errors, correct system/rom, etc.)
					// - dll path matches given string; or dll filename matches given string with or without `.dll`
					openExtToolDll = arg.Substring(20);
				}
				else
				{
					cmdRom = arg;
				}
			}

			var httpCommunication = urlGet == null && urlPost == null
				? null // don't bother
				: new HttpCommunication(takeScreenshotCallback, urlGet, urlPost);
			var memoryMappedFiles = mmfFilename == null
				? null // don't bother
				: new MemoryMappedFiles(takeScreenshotCallback, mmfFilename);
			SocketServer? socketServer;
			if (socketIP == null && socketPort == null)
			{
				socketServer = null; // don't bother
			}
			else if (socketIP == null || socketPort == null)
			{
				throw new ArgParserException("Socket server needs both --socket_ip and --socket_port. Socket server was not started");
			}
			else
			{
				socketServer = new SocketServer(takeScreenshotCallback, socketIP, socketPort.Value);
			}

			parsed = new ParsedCLIFlags(
				cmdLoadSlot: cmdLoadSlot,
				cmdLoadState: cmdLoadState,
				cmdConfigPath: cmdConfigPath,
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
				socketServer: socketServer,
				memoryMappedFiles: memoryMappedFiles,
				httpCommunication: httpCommunication,
				audiosync: audiosync,
				openExtToolDll: openExtToolDll,
				cmdRom: cmdRom
			);
		}

		public static string? GetCmdConfigFile(string[] args)
		{
			return args.FirstOrDefault(arg => arg.StartsWith("--config=", StringComparison.InvariantCultureIgnoreCase))?.Substring(9);
		}
	}

	public sealed class ArgParserException : Exception
	{
		public ArgParserException(string message) : base(message) {}
	}

	public /*readonly*/ struct ParsedCLIFlags
	{
		public readonly string? cmdLoadSlot;

		public readonly string? cmdLoadState;

		public readonly string? cmdConfigPath;

		public readonly string? cmdConfigFile;

		public readonly string? cmdMovie;

		public readonly string? cmdDumpType;

		public readonly HashSet<int>? _currAviWriterFrameList;

		public /*readonly*/ int _autoDumpLength;

		public readonly bool printVersion;

		public readonly string? cmdDumpName;

		public readonly bool _autoCloseOnDump;

		public readonly bool _chromeless;

		public readonly bool startFullscreen;

		public readonly string? luaScript;

		public readonly bool luaConsole;

		public readonly SocketServer? socketServer;

		public readonly MemoryMappedFiles? memoryMappedFiles;

		public readonly HttpCommunication? httpCommunication;

		public readonly bool? audiosync;

		public readonly string? openExtToolDll;

		public readonly string? cmdRom;

		public ParsedCLIFlags(string? cmdLoadSlot,
			string? cmdLoadState,
			string? cmdConfigPath,
			string? cmdConfigFile,
			string? cmdMovie,
			string? cmdDumpType,
			HashSet<int>? currAviWriterFrameList,
			int autoDumpLength,
			bool printVersion,
			string? cmdDumpName,
			bool autoCloseOnDump,
			bool chromeless,
			bool startFullscreen,
			string? luaScript,
			bool luaConsole,
			SocketServer? socketServer,
			MemoryMappedFiles? memoryMappedFiles,
			HttpCommunication? httpCommunication,
			bool? audiosync,
			string? openExtToolDll,
			string? cmdRom)
		{
			this.cmdLoadSlot = cmdLoadSlot;
			this.cmdLoadState = cmdLoadState;
			this.cmdConfigPath = cmdConfigPath;
			this.cmdConfigFile = cmdConfigFile;
			this.cmdMovie = cmdMovie;
			this.cmdDumpType = cmdDumpType;
			_currAviWriterFrameList = currAviWriterFrameList;
			_autoDumpLength = autoDumpLength;
			this.printVersion = printVersion;
			this.cmdDumpName = cmdDumpName;
			_autoCloseOnDump = autoCloseOnDump;
			_chromeless = chromeless;
			this.startFullscreen = startFullscreen;
			this.luaScript = luaScript;
			this.luaConsole = luaConsole;
			this.socketServer = socketServer;
			this.memoryMappedFiles = memoryMappedFiles;
			this.httpCommunication = httpCommunication;
			this.audiosync = audiosync;
			this.openExtToolDll = openExtToolDll;
			this.cmdRom = cmdRom;
		}
	}
}
