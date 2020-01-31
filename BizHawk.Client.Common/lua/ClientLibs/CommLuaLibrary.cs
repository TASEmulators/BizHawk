using System;
using System.ComponentModel;
using System.Text;

using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library for communicating with other programs")]
	public sealed class CommLuaLibrary : DelegatingLuaLibraryEmu
	{
		public CommLuaLibrary(Lua lua)
			: base(lua) { }

		public CommLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "comm";

		//TO DO: not fully working yet!
		[LuaMethod("getluafunctionslist", "returns a list of implemented functions")]
		public static string GetLuaFunctionsList()
		{
			var list = new StringBuilder();
			foreach (var function in typeof(CommLuaLibrary).GetMethods())
			{
				list.AppendLine(function.ToString());
			}
			return list.ToString();
		}

		[LuaMethod("socketServerIsConnected", "socketServerIsConnected")]
		public bool SocketServerIsConnected() => APIs.Comm.SocketServer.Connected;

		[LuaMethod("socketServerScreenShot", "sends a screenshot to the Socket server")]
		public string SocketServerScreenShot()
		{
			CheckSocketServer();
			return APIs.Comm.SocketServer?.SendScreenshot();
		}

		[LuaMethod("socketServerScreenShotResponse", "sends a screenshot to the Socket server and retrieves the response")]
		public string SocketServerScreenShotResponse()
		{
			CheckSocketServer();
			return APIs.Comm.SocketServer?.SendScreenshot(1000).ToString();
		}

		[LuaMethod("socketServerSend", "sends a string to the Socket server")]
		public int SocketServerSend(string SendString)
		{
			if (!CheckSocketServer())
			{
				return -1;
			}
			return APIs.Comm.SocketServer.SendString(SendString);
		}

		[LuaMethod("socketServerResponse", "receives a message from the Socket server")]
		public string SocketServerResponse()
		{
			CheckSocketServer();
			return APIs.Comm.SocketServer?.ReceiveMessage();
		}

		[LuaMethod("socketServerSuccessful", "returns the status of the last Socket server action")]
		public bool SocketServerSuccessful()
		{
			if (!CheckSocketServer())
			{
				return false;
			}
			return APIs.Comm.SocketServer.Successful();
		}

		[LuaMethod("socketServerSetTimeout", "sets the timeout in milliseconds for receiving messages")]
		public void SocketServerSetTimeout(int timeout)
		{
			CheckSocketServer();
			APIs.Comm.SocketServer?.SetTimeout(timeout);
		}

		[LuaMethod("socketServerSetIp", "sets the IP address of the Lua socket server")]
		public void SocketServerSetIp(string ip)
		{
			CheckSocketServer();
			APIs.Comm.SocketServer.Ip = ip;
		}

		[LuaMethod("socketServerSetPort", "sets the port of the Lua socket server")]
		public void SocketServerSetPort(int port)
		{
			CheckSocketServer();
			APIs.Comm.SocketServer.Port = port;
		}

		[LuaMethod("socketServerGetIp", "returns the IP address of the Lua socket server")]
		public string SocketServerGetIp()
		{
			return APIs.Comm.SocketServer?.Ip;
		}

		[LuaMethod("socketServerGetPort", "returns the port of the Lua socket server")]
		public int? SocketServerGetPort()
		{
			return APIs.Comm.SocketServer?.Port;
		}

		[LuaMethod("socketServerGetInfo", "returns the IP and port of the Lua socket server")]
		public string SocketServerGetInfo()
		{
			if (!CheckSocketServer())
			{
				return "";
			}
			return APIs.Comm.SocketServer.GetInfo();
		}

		private bool CheckSocketServer()
		{
			if (APIs.Comm.SocketServer == null)
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
			APIs.Comm.MemoryMappedFiles.Filename = filename;
		}

		[LuaMethod("mmfGetFilename", "Gets the filename for the screenshots")]
		public string MmfGetFilename()
		{
			CheckMmf();
			return APIs.Comm.MemoryMappedFiles?.Filename;
		}

		[LuaMethod("mmfScreenshot", "Saves screenshot to memory mapped file")]
		public int MmfScreenshot()
		{
			CheckMmf();
			return APIs.Comm.MemoryMappedFiles.ScreenShotToFile();
		}

		[LuaMethod("mmfWrite", "Writes a string to a memory mapped file")]
		public int MmfWrite(string mmf_filename, string outputString)
		{
			CheckMmf();
			return APIs.Comm.MemoryMappedFiles.WriteToFile(mmf_filename, Encoding.ASCII.GetBytes(outputString));
		}
		[LuaMethod("mmfRead", "Reads a string from a memory mapped file")]
		public string MmfRead(string mmf_filename, int expectedSize)
		{
			CheckMmf();
			return APIs.Comm.MemoryMappedFiles?.ReadFromFile(mmf_filename, expectedSize).ToString();
		}

		private void CheckMmf()
		{
			if (APIs.Comm.MemoryMappedFiles == null)
			{
				Log("Memory mapped file was not initialized, please initialize it via the command line");
			}
		}

		// All HTTP related methods
		[LuaMethod("httpTest", "tests HTTP connections")]
		public string HttpTest()
		{
			var list = new StringBuilder();
			list.AppendLine(APIs.Comm.HTTP.TestGet());
			list.AppendLine(APIs.Comm.HTTP.SendScreenshot());
			list.AppendLine("done testing");
			return list.ToString();
		}

		[LuaMethod("httpTestGet", "tests the HTTP GET connection")]
		public string HttpTestGet()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.TestGet();
		}

		[LuaMethod("httpGet", "makes a HTTP GET request")]
		public string HttpGet(string url)
		{
			CheckHttp();
			return APIs.Comm.HTTP?.ExecGet(url);
		}

		[LuaMethod("httpPost", "makes a HTTP POST request")]
		public string HttpPost(string url, string payload)
		{
			CheckHttp();
			return APIs.Comm.HTTP?.ExecPost(url, payload);
		}

		[LuaMethod("httpPostScreenshot", "HTTP POST screenshot")]
		public string HttpPostScreenshot()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.SendScreenshot();
		}

		[LuaMethod("httpSetTimeout", "Sets HTTP timeout in milliseconds")]
		public void HttpSetTimeout(int timeout)
		{
			CheckHttp();
			APIs.Comm.HTTP?.SetTimeout(timeout);
		}

		[LuaMethod("httpSetPostUrl", "Sets HTTP POST URL")]
		public void HttpSetPostUrl(string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.PostUrl = url;
		}

		[LuaMethod("httpSetGetUrl", "Sets HTTP GET URL")]
		public void HttpSetGetUrl(string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.GetUrl = url;
		}

		[LuaMethod("httpGetPostUrl", "Gets HTTP POST URL")]
		public string HttpGetPostUrl()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.PostUrl;
		}

		[LuaMethod("httpGetGetUrl", "Gets HTTP GET URL")]
		public string HttpGetGetUrl()
		{
			CheckHttp();
			return APIs.Comm.HTTP?.GetUrl;
		}

		private void CheckHttp()
		{
			if (APIs.Comm.HTTP == null)
			{
				Log("HTTP was not initialized, please initialize it via the command line");
			}
		}
	}
}