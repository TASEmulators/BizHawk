#nullable enable

using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class ClientWebSocketWrapper
	{
		private ClientWebSocket? _w;
		
		private List<string> _receivedMessages;

		Uri _uri;

		/// <summary>calls <see cref="ClientWebSocket.State"/> getter (unless closed/disposed, then <see cref="WebSocketState.Closed"/> is always returned)</summary>
		public WebSocketState State => _w?.State ?? WebSocketState.Closed;

		public ClientWebSocketWrapper(Uri uri, int bufferSize, int maxMessages)
		{
			_uri = uri;
			_w = new ClientWebSocket();
			_receivedMessages = new List<string>();
			try{
				Connect(bufferSize, maxMessages).Wait();
			}
			catch(Exception ex){}
		}

		/// <summary>calls <see cref="ClientWebSocket.CloseAsync"/></summary>
		/// <remarks>also calls <see cref="ClientWebSocket.Dispose"/> (wrapper property <see cref="State"/> will continue to work, method calls will throw <see cref="ObjectDisposedException"/>)</remarks>
		public Task Close(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken? cancellationToken = null)
		{
			if (_w == null) throw new ObjectDisposedException(nameof(_w));
			var task = _w.CloseOutputAsync(closeStatus, statusDescription, cancellationToken ?? CancellationToken.None);
			_w.Dispose();
			_w = null;
			return task;
		}

		/// <summary>calls <see cref="ClientWebSocket.ReceiveAsync"/></summary>
		public async Task Receive(int bufferSize, int maxMessages){
			var buffer = new ArraySegment<byte>(new byte[bufferSize]);
			while (_w != null && _w.State == WebSocketState.Open)
			{
				WebSocketReceiveResult result;
				result = await _w.ReceiveAsync(buffer, CancellationToken.None);
				if (maxMessages == 0 || _receivedMessages.length < maxMessages)
					_receivedMessages.Add(Encoding.UTF8.GetString(buffer.Array,0,result.Count));
			}
		}

		public async Task Connect(int bufferSize, int maxMessages){
			if (_w == null){
				_w = new ClientWebSocket();
			}
			Console.WriteLine(_w.State);
			if(_w != null && _w.State != WebSocketState.Open){
				_w.ConnectAsync(_uri, CancellationToken.None).Wait();
				Receive(bufferSize, maxMessages);
			}
		}

		/// <summary>calls <see cref="ClientWebSocket.SendAsync"/></summary>
		public Task Send(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken? cancellationToken = null)
			=> _w?.SendAsync(buffer, messageType, endOfMessage, cancellationToken ?? CancellationToken.None)
				?? throw new ObjectDisposedException(nameof(_w));

		/// <summary>calls <see cref="ClientWebSocket.SendAsync"/></summary>
		public Task Send(string message, bool endOfMessage, CancellationToken? cancellationToken = null)
		{
			if (_w == null) throw new ObjectDisposedException(nameof(_w));
			return Send(
				new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
				WebSocketMessageType.Text,
				endOfMessage,
				cancellationToken ?? CancellationToken.None
			);
		}

		public string GetMessage()
		{
			if (_receivedMessages == null || _receivedMessages.Count == 0) return "";
			string returnThis = _receivedMessages[0];
			_receivedMessages.RemoveAt(0);
			return returnThis;
		}
	}
}
