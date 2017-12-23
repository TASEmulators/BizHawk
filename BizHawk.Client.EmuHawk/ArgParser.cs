using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BizHawk.Client.EmuHawk
{
	class ArgParser
		//parses command line arguments and adds the values to a class attribute
		//default values are null for strings and false for boolean
		//the last value will overwrite previously set values
		//unrecognized parameters are simply ignored or in the worst case assumed to be a ROM name [cmdRom]
	{
		public string cmdRom = null;
		public string cmdLoadSlot = null;
		public string cmdLoadState = null;
		public string cmdMovie = null;
		public string cmdDumpType = null;
		public string cmdDumpName = null;
		public HashSet<int> _currAviWriterFrameList;
		public int _autoDumpLength;
		public bool _autoCloseOnDump = false;
		// chrome is never shown, even in windowed mode
		public bool _chromeless = false;
		public bool startFullscreen = false;
		public string luaScript = null;
		public bool luaConsole = false;
		public int socket_port = 9999;
		public string socket_ip = null;
		public string mmf_filename = null;
		public string URL_get = null;
		public string URL_post = null;

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
					cmdLoadSlot = arg.Substring(arg.IndexOf('=') + 1);
				}

				if (arg.StartsWith("--load-state="))
				{
					cmdLoadState = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--movie="))
				{
					cmdMovie = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-type="))
				{
					cmdDumpType = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-frames="))
				{
					string list = arg.Substring(arg.IndexOf('=') + 1);
					string[] items = list.Split(',');
					_currAviWriterFrameList = new HashSet<int>();
					foreach (string item in items)
					{
						_currAviWriterFrameList.Add(int.Parse(item));
					}

					// automatically set dump length to maximum frame
					_autoDumpLength = _currAviWriterFrameList.OrderBy(x => x).Last();
				}
				else if (arg.StartsWith("--dump-name="))
				{
					cmdDumpName = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--dump-length="))
				{
					int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out _autoDumpLength);
				}
				else if (arg.StartsWith("--dump-close"))
				{
					_autoCloseOnDump = true;
				}
				else if (arg.StartsWith("--chromeless"))
				{
					_chromeless = true;
				}
				else if (arg.StartsWith("--fullscreen"))
				{
					startFullscreen = true;
				}
				else if (arg.StartsWith("--lua="))
				{
					luaScript = args[i].Substring(args[i].IndexOf('=') + 1);
					luaConsole = true;
				}
				else if (arg.StartsWith("--luaconsole"))
				{
					luaConsole = true;
				}
				else if (arg.StartsWith("--socket_port="))
				{
					int.TryParse(arg.Substring(arg.IndexOf('=') + 1), out socket_port);
				}
				else if (arg.StartsWith("--socket_ip="))
				{
					socket_ip = arg.Substring(arg.IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--mmf="))
				{
					mmf_filename = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--url_get="))
				{
					URL_get = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else if (arg.StartsWith("--url_post="))
				{
					URL_post = args[i].Substring(args[i].IndexOf('=') + 1);
				}
				else
				{
					cmdRom = args[i];
				}
			}
			////initialize HTTP communication
			if (URL_get != null || URL_post != null)
			{
				if (URL_get != null)
				{
					GlobalWin.httpCommunication.initialized = true;
					GlobalWin.httpCommunication.SetGetUrl(URL_get);
				}
				if (URL_post != null)
				{
					GlobalWin.httpCommunication.initialized = true;
					GlobalWin.httpCommunication.SetPostUrl(URL_post);
				}
			}
			//inititalize socket server
			if (socket_ip != null && socket_port > -1)
			{
				GlobalWin.socketServer.initialized = true;
				GlobalWin.socketServer.SetIp(socket_ip, socket_port);
			}
			else if (socket_ip != null)
			{
				GlobalWin.socketServer.initialized = true;
				GlobalWin.socketServer.SetIp(socket_ip);
			}
			
			//initialize mapped memory files
			if (mmf_filename != null)
			{
				GlobalWin.memoryMappedFiles.initialized = true;
				GlobalWin.memoryMappedFiles.SetFilename(mmf_filename);
			}
		}
	}
}
