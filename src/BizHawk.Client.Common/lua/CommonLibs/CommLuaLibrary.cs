using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library for communicating with other programs")]
	public sealed class CommLuaLibrary : LuaLibraryBase
	{
		private readonly IDictionary<Guid, ClientWebSocketWrapper> _websockets = new Dictionary<Guid, ClientWebSocketWrapper>();

		public CommLuaLibrary(IPlatformLuaLibEnv luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "comm";

		//TO DO: not fully working yet!
		[LuaMethod("getluafunctionslist", "returns a list of implemented functions")]
		[return: LuaArbitraryStringParam]
		public static string GetLuaFunctionsList()
		{
			var list = new StringBuilder();
			foreach (var function in typeof(CommLuaLibrary).GetMethods())
			{
				list.AppendLine(function.ToString());
			}
			return UnFixString(list.ToString());
		}

		[LuaMethod("socketServerIsConnected", "socketServerIsConnected")]
		public bool SocketServerIsConnected()
			=> APIs.Comm.Sockets.Connected;

		[LuaMethod("socketServerScreenShot", "sends a screenshot to the Socket server")]
		[return: LuaArbitraryStringParam]
		public string SocketServerScreenShot()
		{
			CheckSocketServer();
			return UnFixString(APIs.Comm.Sockets?.SendScreenshot());
		}

		[LuaMethod("socketServerScreenShotResponse", "sends a screenshot to the Socket server and retrieves the response")]
		[return: LuaArbitraryStringParam]
		public string SocketServerScreenShotResponse()
		{
			CheckSocketServer();
			return UnFixString(APIs.Comm.Sockets?.SendScreenshot(1000));
		}

		[LuaMethod("socketServerSend", "sends a string to the Socket server")]
		public int SocketServerSend([LuaArbitraryStringParam] string SendString)
		{
			if (!CheckSocketServer())
			{
				return -1;
			}
			return APIs.Comm.Sockets.SendString(FixString(SendString));
		}

		[LuaMethod("socketServerSendBytes", "sends bytes to the Socket server")]
		public int SocketServerSendBytes(LuaTable byteArray)
		{
			if (!CheckSocketServer()) return -1;
			return APIs.Comm.Sockets.SendBytes(_th.EnumerateValues<double>(byteArray).Select(d => (byte) d).ToArray());
		}

		[LuaMethod("socketServerResponse", "receives a message from the Socket server")]
		[return: LuaArbitraryStringParam]
		public string SocketServerResponse()
		{
			CheckSocketServer();
			return UnFixString(APIs.Comm.Sockets?.ReceiveString());
		}

		[LuaMethod("socketServerSuccessful", "returns the status of the last Socket server action")]
		public bool SocketServerSuccessful()
		{
			return CheckSocketServer() && APIs.Comm.Sockets.Successful;
		}

		[LuaMethod("socketServerSetTimeout", "sets the timeout in milliseconds for receiving messages")]
		public void SocketServerSetTimeout(int timeout)
		{
			CheckSocketServer();
			APIs.Comm.Sockets?.SetTimeout(timeout);
		}

		[LuaMethod("socketServerSetIp", "sets the IP address of the Lua socket server")]
		public void SocketServerSetIp([LuaASCIIStringParam] string ip)
		{
			CheckSocketServer();
			APIs.Comm.Sockets.IP = ip;
		}

		[LuaMethod("socketServerSetPort", "sets the port of the Lua socket server")]
		public void SocketServerSetPort(int port)
		{
			CheckSocketServer();
			APIs.Comm.Sockets.Port = port;
		}

		[LuaMethod("socketServerGetIp", "returns the IP address of the Lua socket server")]
		[return: LuaASCIIStringParam]
		public string SocketServerGetIp()
		{
			return APIs.Comm.Sockets?.IP;
		}

		[LuaMethod("socketServerGetPort", "returns the port of the Lua socket server")]
		public int? SocketServerGetPort()
		{
			return APIs.Comm.Sockets?.Port;
		}

		[LuaMethod("socketServerGetInfo", "returns the IP and port of the Lua socket server")]
		[return: LuaASCIIStringParam]
		public string SocketServerGetInfo()
		{
			return CheckSocketServer()
				? APIs.Comm.Sockets.GetInfo()
				: "";
		}

		private bool CheckSocketServer()
		{
			if (APIs.Comm.Sockets == null)
			{
				Log("Socket server was not initialized, please initialize it via the command line");
				return false;
			}

			return true;
		}

		// All MemoryMappedFile related methods
		[LuaMethod("mmfSetFilename", "Sets the filename for the screenshots")]
		public void MmfSetFilename([LuaArbitraryStringParam] string filename)
			=> APIs.Comm.MMF.Filename = FixString(filename);

		[LuaMethod("mmfGetFilename", "Gets the filename for the screenshots")]
		[return: LuaArbitraryStringParam]
		public string MmfGetFilename()
		{
			return UnFixString(APIs.Comm.MMF.Filename);
		}

		[LuaMethod("mmfScreenshot", "Saves screenshot to memory mapped file")]
		public int MmfScreenshot()
		{
			return APIs.Comm.MMF.ScreenShotToFile();
		}

		[LuaMethod("mmfWrite", "Writes a string to a memory mapped file")]
		public int MmfWrite([LuaArbitraryStringParam] string mmf_filename, [LuaArbitraryStringParam] string outputString)
			=> APIs.Comm.MMF.WriteToFile(FixString(mmf_filename), FixString(outputString));

		[LuaMethod("mmfWriteBytes", "Write bytes to a memory mapped file")]
		public int MmfWriteBytes([LuaArbitraryStringParam] string mmf_filename, LuaTable byteArray)
			=> APIs.Comm.MMF.WriteToFile(FixString(mmf_filename), _th.EnumerateValues<double>(byteArray).Select(d => (byte)d).ToArray());

		[LuaMethod("mmfCopyFromMemory", "Copy a section of the memory to a memory mapped file")]
		public int MmfCopyFromMemory(
			[LuaArbitraryStringParam] string mmf_filename,
			long addr,
			int length,
			[LuaASCIIStringParam] string domain)
				=> APIs.Comm.MMF.WriteToFile(FixString(mmf_filename), APIs.Memory.ReadByteRange(addr, length, domain).ToArray());

		[LuaMethod("mmfCopyToMemory", "Copy a memory mapped file to a section of the memory")]
		public void MmfCopyToMemory(
			[LuaArbitraryStringParam] string mmf_filename,
			long addr,
			int length,
			[LuaASCIIStringParam] string domain)
				=> APIs.Memory.WriteByteRange(addr, new List<byte>(APIs.Comm.MMF.ReadBytesFromFile(FixString(mmf_filename), length)), domain);

		[LuaMethod("mmfRead", "Reads a string from a memory mapped file")]
		[return: LuaArbitraryStringParam]
		public string MmfRead([LuaArbitraryStringParam] string mmf_filename, int expectedSize)
			=> UnFixString(APIs.Comm.MMF.ReadFromFile(FixString(mmf_filename), expectedSize));

		[LuaMethod("mmfReadBytes", "Reads bytes from a memory mapped file")]
		public LuaTable MmfReadBytes([LuaArbitraryStringParam] string mmf_filename, int expectedSize)
			=> _th.ListToTable(APIs.Comm.MMF.ReadBytesFromFile(FixString(mmf_filename), expectedSize), indexFrom: 0);

		// All HTTP related methods
		[LuaMethod("httpTest", "tests HTTP connections")]
		[return: LuaArbitraryStringParam]
		public string HttpTest()
		{
			if (APIs.Comm.HTTP == null) throw new NullReferenceException(); // to match previous behaviour
			return UnFixString(APIs.Comm.HttpTest());
		}

		[LuaMethod("httpTestGet", "tests the HTTP GET connection")]
		[return: LuaArbitraryStringParam]
		public string HttpTestGet()
		{
			CheckHttp();
			return UnFixString(APIs.Comm.HttpTestGet());
		}

		[LuaMethod("httpGet", "makes a HTTP GET request")]
		[return: LuaArbitraryStringParam]
		public string HttpGet([LuaArbitraryStringParam] string url)
		{
			CheckHttp();
			return UnFixString(APIs.Comm.HTTP?.ExecGet(FixString(url)));
		}

		[LuaMethod("httpPost", "makes a HTTP POST request")]
		[return: LuaArbitraryStringParam]
		public string HttpPost([LuaArbitraryStringParam] string url, [LuaArbitraryStringParam] string payload)
		{
			CheckHttp();
			return UnFixString(APIs.Comm.HTTP?.ExecPost(FixString(url), FixString(payload)));
		}

		[LuaMethod("httpPostScreenshot", "HTTP POST screenshot")]
		[return: LuaArbitraryStringParam]
		public string HttpPostScreenshot()
		{
			CheckHttp();
			return UnFixString(APIs.Comm.HTTP?.SendScreenshot());
		}

		[LuaMethod("httpSetTimeout", "Sets HTTP timeout in milliseconds")]
		public void HttpSetTimeout(int timeout)
		{
			CheckHttp();
			APIs.Comm.HTTP?.SetTimeout(timeout);
		}

		[LuaMethod("httpSetPostUrl", "Sets HTTP POST URL")]
		public void HttpSetPostUrl([LuaArbitraryStringParam] string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.PostUrl = FixString(url);
		}

		[LuaMethod("httpSetGetUrl", "Sets HTTP GET URL")]
		public void HttpSetGetUrl([LuaArbitraryStringParam] string url)
		{
			CheckHttp();
			APIs.Comm.HTTP.GetUrl = FixString(url);
		}

		[LuaMethod("httpGetPostUrl", "Gets HTTP POST URL")]
		[return: LuaArbitraryStringParam]
		public string HttpGetPostUrl()
		{
			CheckHttp();
			return UnFixString(APIs.Comm.HTTP?.PostUrl);
		}

		[LuaMethod("httpGetGetUrl", "Gets HTTP GET URL")]
		[return: LuaArbitraryStringParam]
		public string HttpGetGetUrl()
		{
			CheckHttp();
			return UnFixString(APIs.Comm.HTTP?.GetUrl);
		}

		private void CheckHttp()
		{
			if (APIs.Comm.HTTP == null)
			{
				Log("HTTP was not initialized, please initialize it via the command line");
			}
		}

#if ENABLE_WEBSOCKETS
		[LuaMethod("ws_open", "Opens a websocket and returns the id so that it can be retrieved later.")]
		[LuaMethodExample("local ws_id = comm.ws_open(\"wss://echo.websocket.org\");")]
		[return: LuaASCIIStringParam]
		public string WebSocketOpen([LuaArbitraryStringParam] string uri)
		{
			var wsServer = APIs.Comm.WebSockets;
			if (wsServer == null)
			{
				Log("WebSocket server is somehow not available");
				return null;
			}
			var guid = new Guid();
			_websockets[guid] = wsServer.Open(new Uri(FixString(uri)));
			return guid.ToString();
		}

		[LuaMethod("ws_send", "Send a message to a certain websocket id (boolean flag endOfMessage)")]
		[LuaMethodExample("local ws = comm.ws_send(ws_id, \"some message\", true);")]
		public void WebSocketSend(
			[LuaASCIIStringParam] string guid,
			[LuaArbitraryStringParam] string content,
			bool endOfMessage)
		{
			if (_websockets.TryGetValue(Guid.Parse(guid), out var wrapper)) wrapper.Send(FixString(content), endOfMessage);
		}

		[LuaMethod("ws_receive", "Receive a message from a certain websocket id and a maximum number of bytes to read")]
		[LuaMethodExample("local ws = comm.ws_receive(ws_id, str_len);")]
		[return: LuaArbitraryStringParam]
		public string WebSocketReceive([LuaASCIIStringParam] string guid, int bufferCap)
			=> _websockets.TryGetValue(Guid.Parse(guid), out var wrapper)
				? UnFixString(wrapper.Receive(bufferCap))
				: null;

		[LuaMethod("ws_get_status", "Get a websocket's status")]
		[LuaMethodExample("local ws_status = comm.ws_get_status(ws_id);")]
		public int? WebSocketGetStatus([LuaASCIIStringParam] string guid)
			=> _websockets.TryGetValue(Guid.Parse(guid), out var wrapper)
				? (int) wrapper.State
				: (int?) null;

		[LuaMethod("ws_close", "Close a websocket connection with a close status")]
		[LuaMethodExample("local ws_status = comm.ws_close(ws_id, close_status);")]
		public void WebSocketClose(
			[LuaASCIIStringParam] string guid,
			WebSocketCloseStatus status,
			[LuaArbitraryStringParam] string closeMessage)
		{
			if (_websockets.TryGetValue(Guid.Parse(guid), out var wrapper)) wrapper.Close(status, FixString(closeMessage));
		}
#endif
	}
}