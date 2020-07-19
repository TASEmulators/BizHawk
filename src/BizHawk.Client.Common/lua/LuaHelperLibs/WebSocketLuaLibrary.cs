using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using NLua;


namespace BizHawk.Client.Common.lua.LuaHelperLibs
{
	[Description("A library exposing standard .NET string methods")]
	public sealed class WebSocketLuaLibrary : LuaLibraryBase
	{
		private Dictionary<string, ClientWebSocket> activeSockets;

		public WebSocketLuaLibrary(Lua lua) : base(lua)
		{
			activeSockets = new Dictionary<string, ClientWebSocket>();
		}

		public WebSocketLuaLibrary(Lua lua, Action<string> logOutputCallback) : base(lua, logOutputCallback)
		{
			activeSockets = new Dictionary<string, ClientWebSocket>();
		}

		public override string Name => "bizsocket";

		[LuaMethodExample("local ws = bizsocket.open('wss://echo.websocket.org');")]
		[LuaMethod("open", "Opens a websocket and returns the id so that it can be retrieved later.")]
		public string OpenSocket(string url)
		{
			var ws = new ClientWebSocket();
			var id = new Guid().ToString();

			ws.ConnectAsync(new Uri(url), CancellationToken.None).Wait();

			activeSockets[id] = ws;

			return id.ToString();
		}

		[LuaMethodExample("local ws = bizsocket.send(ws_id, 'some message', true);")]
		[LuaMethod("send", "Send a message to a certain websocket id (boolean flag endOfMessage)")]
		public void SendToSocket(string id, string content, bool endOfMessage)
		{
			var ws = activeSockets[id];
			var msg = new ArraySegment<byte>(Encoding.ASCII.GetBytes(content));

			ws.SendAsync(msg, WebSocketMessageType.Text, endOfMessage, CancellationToken.None);
		}

		[LuaMethodExample("local ws = bizsocket.receive(ws_id, max_read);")]
		[LuaMethod("receive", "Receive a message from a certain websocket id and a maximum number of bytes to read.")]
		public string ReceiveFromSocket(string id, int maxRead)
		{
			var ws = activeSockets[id];

			var rcvBytes = new byte[maxRead];
			var rcvBuffer = new ArraySegment<byte>(rcvBytes);

			var result = ws.ReceiveAsync(rcvBuffer, CancellationToken.None).Result;
			byte[] msgBytes = rcvBuffer.Take(result.Count).ToArray();
			string rcvMsg = Encoding.UTF8.GetString(msgBytes);

			return rcvMsg;
		}

		[LuaMethodExample("local ws_status = bizsocket.getstatus(ws_id);")]
		[LuaMethod("getStatus", "Get a websocket's status")]
		public int GetSocketStatus(string id)
		{
			var ws = activeSockets[id];

			return (int)ws.State;
		}

		[LuaMethodExample("local ws_status = bizsocket.close(ws_id, close_status);")]
		[LuaMethod("close", "Close a websocket connection with a close status, defined in section 11.7 of the web sockets protocol spec.")]
		public void CloseSocket(string id, int status, string closeMessage)
		{
			var ws = activeSockets[id];

			ws.CloseAsync((WebSocketCloseStatus) status, closeMessage, CancellationToken.None);
		}
	}
}
