using System;
using System.ComponentModel;
using NLua;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	[Description("A library for communicating with other programs")]
	public sealed class CommunicationLuaLibrary : LuaLibraryBase
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IVideoProvider VideoProvider { get; set; }

		public CommunicationLuaLibrary(Lua lua)
			: base(lua) { }

		public CommunicationLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "comm";

		//TO DO: not fully working yet!
		[LuaMethod("getluafunctionslist", "returns a list of implemented functions")]
		public static string GetLuaFunctionsList()
		{
			var list = new StringBuilder();
			foreach (var function in typeof(CommunicationLuaLibrary).GetMethods())
			{
				list.AppendLine(function.ToString());
			}
			return list.ToString();
		}

		[LuaMethod("socketServerIsConnected", "socketServerIsConnected")]
		public bool SocketServerIsConnected() => GlobalWin.socketServer.Connected;

		[LuaMethod("socketServerScreenShot", "sends a screenshot to the Socket server")]
		public string SocketServerScreenShot()
		{
			CheckSocketServer();
			return GlobalWin.socketServer?.SendScreenshot();
		}

		[LuaMethod("socketServerScreenShotResponse", "sends a screenshot to the Socket server and retrieves the response")]
		public string SocketServerScreenShotResponse()
		{
			CheckSocketServer();
			return GlobalWin.socketServer?.SendScreenshot(1000).ToString();
		}

		[LuaMethod("socketServerSend", "sends a string to the Socket server")]
		public int SocketServerSend(string SendString)
		{
			if (!CheckSocketServer())
			{
				return -1;
			}
			return GlobalWin.socketServer.SendString(SendString);
		}

		[LuaMethod("socketServerResponse", "receives a message from the Socket server")]
		public string SocketServerResponse()
		{
			CheckSocketServer();
			return GlobalWin.socketServer?.ReceiveMessage();
		}

		[LuaMethod("socketServerSuccessful", "returns the status of the last Socket server action")]
		public bool SocketServerSuccessful()
		{
			if (!CheckSocketServer())
			{
				return false;
			}
			return GlobalWin.socketServer.Successful();
		}

		[LuaMethod("socketServerSetTimeout", "sets the timeout in milliseconds for receiving messages")]
		public void SocketServerSetTimeout(int timeout)
		{
			CheckSocketServer();
			GlobalWin.socketServer?.SetTimeout(timeout);
		}

		[LuaMethod("socketServerSetIp", "sets the IP address of the Lua socket server")]
		public void SocketServerSetIp(string ip)
		{
			CheckSocketServer();
			GlobalWin.socketServer.Ip = ip;
		}

		[LuaMethod("socketServerSetPort", "sets the port of the Lua socket server")]
		public void SocketServerSetPort(int port)
		{
			CheckSocketServer();
			GlobalWin.socketServer.Port = port;
		}

		[LuaMethod("socketServerGetIp", "returns the IP address of the Lua socket server")]
		public string SocketServerGetIp()
		{
			return GlobalWin.socketServer?.Ip;
		}

		[LuaMethod("socketServerGetPort", "returns the port of the Lua socket server")]
		public int? SocketServerGetPort()
		{
			return GlobalWin.socketServer?.Port;
		}

		[LuaMethod("socketServerGetInfo", "returns the IP and port of the Lua socket server")]
		public string SocketServerGetInfo()
		{
			if (!CheckSocketServer())
			{
				return "";
			}
			return GlobalWin.socketServer.GetInfo();
		}

		private bool CheckSocketServer()
		{
			if (GlobalWin.socketServer == null)
			{
				Log("Socket server was not initialized, please initialize it via the command line");
				return false;
			}
			return true;
		}

		// All MemoryMappedFile related methods
		[LuaMethod("mmfSetFilename", "Sets the filename for the screenshots")]
		public void MmfSetFilename(string filename)
		{
			CheckMmf();
			GlobalWin.memoryMappedFiles.Filename = filename;
		}

		[LuaMethod("mmfGetFilename", "Gets the filename for the screenshots")]
		public string MmfGetFilename()
		{
			CheckMmf();
			return GlobalWin.memoryMappedFiles?.Filename;
		}

		[LuaMethod("mmfScreenshot", "Saves screenshot to memory mapped file")]
		public int MmfScreenshot()
		{
			CheckMmf();
			return GlobalWin.memoryMappedFiles.ScreenShotToFile();
		}

		[LuaMethod("mmfWrite", "Writes a string to a memory mapped file")]
		public int MmfWrite(string mmf_filename, string outputString)
		{
			CheckMmf();
			return GlobalWin.memoryMappedFiles.WriteToFile(mmf_filename, Encoding.ASCII.GetBytes(outputString));
		}
		[LuaMethod("mmfRead", "Reads a string from a memory mapped file")]
		public string MmfRead(string mmf_filename, int expectedSize)
		{
			CheckMmf();
			return GlobalWin.memoryMappedFiles?.ReadFromFile(mmf_filename, expectedSize).ToString();
		}

		private void CheckMmf()
		{
			if (GlobalWin.memoryMappedFiles == null)
			{
				Log("Memory mapped file was not initialized, please initialize it via the command line");
			}
		}

		// All HTTP related methods
		[LuaMethod("httpTest", "tests HTTP connections")]
		public string HttpTest()
		{
			var list = new StringBuilder();
			list.AppendLine(GlobalWin.httpCommunication.TestGet());
			list.AppendLine(GlobalWin.httpCommunication.SendScreenshot());
			list.AppendLine("done testing");
			return list.ToString();
		}

		[LuaMethod("httpTestGet", "tests the HTTP GET connection")]
		public string HttpTestGet()
		{
			CheckHttp();
			return GlobalWin.httpCommunication?.TestGet();
		}

		[LuaMethod("httpGet", "makes a HTTP GET request")]
		public string HttpGet(string url)
		{
			CheckHttp();
			return GlobalWin.httpCommunication?.ExecGet(url);
		}

		[LuaMethod("httpPost", "makes a HTTP POST request")]
		public string HttpPost(string url, string payload)
		{
			CheckHttp();
			return GlobalWin.httpCommunication?.ExecPost(url, payload);
		}

		[LuaMethod("httpPostScreenshot", "HTTP POST screenshot")]
		public string HttpPostScreenshot()
		{
			CheckHttp();
			return GlobalWin.httpCommunication?.SendScreenshot();
		}

		[LuaMethod("httpSetTimeout", "Sets HTTP timeout in milliseconds")]
		public void HttpSetTimeout(int timeout)
		{
			CheckHttp();
			GlobalWin.httpCommunication?.SetTimeout(timeout);
		}

		[LuaMethod("httpSetPostUrl", "Sets HTTP POST URL")]
		public void HttpSetPostUrl(string url)
		{
			CheckHttp();
			GlobalWin.httpCommunication.PostUrl = url;
		}

		[LuaMethod("httpSetGetUrl", "Sets HTTP GET URL")]
		public void HttpSetGetUrl(string url)
		{
			CheckHttp();
			GlobalWin.httpCommunication.GetUrl = url;
		}

		[LuaMethod("httpGetPostUrl", "Gets HTTP POST URL")]
		public string HttpGetPostUrl()
		{
			CheckHttp();
			return GlobalWin.httpCommunication?.PostUrl;
		}

		[LuaMethod("httpGetGetUrl", "Gets HTTP GET URL")]
		public string HttpGetGetUrl()
		{
			CheckHttp();
			return GlobalWin.httpCommunication?.GetUrl;
		}

		private void CheckHttp()
		{
			if (GlobalWin.httpCommunication == null)
			{
				Log("HTTP was not initialized, please initialize it via the command line");
			}
		}
	}
}