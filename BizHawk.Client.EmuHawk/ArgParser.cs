using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BizHawk.Client.EmuHawk
{
	// parses command line arguments and adds the values to a class attribute
	// default values are null for strings and false for boolean
	// the last value will overwrite previously set values
	// unrecognized parameters are simply ignored or in the worst case assumed to be a ROM name [cmdRom]
	public class ArgParser
	{
		public string CmdRom { get; set; }
		public string CmdLoadSlot { get; set; }
		public string CmdLoadState { get; set; }
		public string CmdConfigFile { get; set; }
		public string CmdMovie { get; set; }
		public string CmdDumpType { get; set; }
		public string CmdDumpName { get; set; }
		public HashSet<int> CurrAviWriterFrameList { get; set; } = new HashSet<int>();
		public int AutoDumpLength { get; set; }
		public bool AutoCloseOnDump { get; set; }

		// chrome is never shown, even in windowed mode
		public bool Chromeless { get; set; }
		public bool StartFullscreen { get; set; }
		public string LuaScript { get; set; }
		public bool LuaConsole { get; set; }
		public bool PrintVersion { get; set; }
		public int SocketPort { get; set; }
		public string SocketIp { get; set; }
		public string MmfFilename { get; set; }
		public string UrlGet { get; set; }
		public string UrlPost { get; set; }
		public bool? AudioSync { get; set; }

		/// <exception cref="ArgParserException"><c>--socket_ip</c> passed without specifying <c>--socket_port</c> or vice-versa</exception>
		public void ParseArguments(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				// For some reason sometimes visual studio will pass this to us on the commandline. it makes no sense.
				if (args[i] == ">")
				{
					i++;
					var stdout = args[i];
					Console.SetOut(new StreamWriter(stdout));
					continue;
				}

				var arg = args[i].ToLower();
				if (arg.StartsWith("--load-slot="))
				{
					CmdLoadSlot = arg.Substring(arg.IndexOf('=') + 1);
				}

				if (arg.StartsWith("--load-state="))
				{
					CmdLoadState = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				if (arg.StartsWith("--config="))
				{
					CmdConfigFile = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--movie="))
				{
					CmdMovie = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-type="))
				{
					CmdDumpType = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-frames="))
				{
					string list = arg.Substring(arg.IndexOf('=') + 1);
					string[] items = list.Split(',');
					CurrAviWriterFrameList = new HashSet<int>();
					foreach (string item in items)
					{
						CurrAviWriterFrameList.Add(int.Parse(item));
					}

					// automatically set dump length to maximum frame
					AutoDumpLength = CurrAviWriterFrameList.OrderBy(x => x).Last();
				}
				else if (arg.StartsWith("--version"))
				{
					PrintVersion = true;
				}
				else if (arg.StartsWith("--dump-name="))
				{
					CmdDumpName = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-length="))
				{
					if (int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out int autoDumpLength))
					{
						AutoDumpLength = autoDumpLength;
					}
				}
				else if (arg.StartsWith("--dump-close"))
				{
					AutoCloseOnDump = true;
				}
				else if (arg.StartsWith("--chromeless"))
				{
					Chromeless = true;
				}
				else if (arg.StartsWith("--fullscreen"))
				{
					StartFullscreen = true;
				}
				else if (arg.StartsWith("--lua="))
				{
					LuaScript = args[i].Substring(args[i].IndexOf('=') + 1);
					LuaConsole = true;
				}
				else if (arg.StartsWith("--luaconsole"))
				{
					LuaConsole = true;
				}
				else if (arg.StartsWith("--socket_port="))
				{
					if (int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out int socketPort))
					{
						SocketPort = socketPort;
					}
				}
				else if (arg.StartsWith("--socket_ip="))
				{
					SocketIp = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--mmf="))
				{
					MmfFilename = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--url_get="))
				{
					UrlGet = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--url_post="))
				{
					UrlPost = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--audiosync="))
				{
					AudioSync = arg.Substring(arg.IndexOf('=') + 1) == "true";
				}
				else
				{
					CmdRom = args[i];
				}
			}

			// initialize HTTP communication
			if (UrlGet != null || UrlPost != null)
			{
				GlobalWin.httpCommunication = new Communication.HttpCommunication();
				if (UrlGet != null)
				{
					GlobalWin.httpCommunication.GetUrl = UrlGet;
				}
				if (UrlPost != null)
				{
					GlobalWin.httpCommunication.PostUrl = UrlPost;
				}
			}

			// initialize socket server
			if (SocketIp != null && SocketPort > 0)
			{
				GlobalWin.socketServer = new Communication.SocketServer();
				GlobalWin.socketServer.SetIp(SocketIp, SocketPort);
			}
			else if (SocketIp == null ^ SocketPort == 0)
			{
				throw new ArgParserException("Socket server needs both --socket_ip and --socket_port. Socket server was not started");
			}

			// initialize mapped memory files
			if (MmfFilename != null)
			{
				GlobalWin.memoryMappedFiles = new Communication.MemoryMappedFiles
				{
					Filename = MmfFilename
				};
			}
		}

		public static string GetCmdConfigFile(string[] args)
		{
			return args.FirstOrDefault(arg => arg.StartsWith("--config=", StringComparison.InvariantCultureIgnoreCase))?.Substring(9);
		}
	}

	public class ArgParserException : Exception
	{
		public ArgParserException(string message) : base(message)
		{
		}
	}
}
