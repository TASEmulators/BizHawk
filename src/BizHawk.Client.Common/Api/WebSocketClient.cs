using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace BizHawk.Client.Common
{
	public class WebSocketClient
	{
		private Dictionary<string, ClientWebSocket> activeSockets;

		public WebSocketClient()
		{
			activeSockets = new Dictionary<string, ClientWebSocket>();
		}

		public string OpenSocket(string url)
		{
			var ws = new ClientWebSocket();
			var id = new Guid().ToString();

			ws.ConnectAsync(new Uri(url), CancellationToken.None).Wait();

			activeSockets[id] = ws;

			return id;
		}

		public void SendToSocket(string id, string content, bool endOfMessage)
		{
			var ws = activeSockets[id];
			var msg = new ArraySegment<byte>(Encoding.UTF8.GetBytes(content));

			ws.SendAsync(msg, WebSocketMessageType.Text, endOfMessage, CancellationToken.None);
		}

		public string ReceiveFromSocket(string id, int maxRead)
		{
			var ws = activeSockets[id];

			var rcvBytes = new byte[maxRead];
			var rcvBuffer = new ArraySegment<byte>(rcvBytes);

			var result = ws.ReceiveAsync(rcvBuffer, CancellationToken.None).Result;
			string rcvMsg = Encoding.UTF8.GetString(rcvBuffer.Take(result.Count).ToArray());

			return rcvMsg;
		}

		public int GetSocketStatus(string id)
		{
			var ws = activeSockets[id];

			return (int)ws.State;
		}

		public void CloseSocket(string id, int status, string closeMessage)
		{
			var ws = activeSockets[id];

			ws.CloseAsync((WebSocketCloseStatus)status, closeMessage, CancellationToken.None);
		}
	}
}