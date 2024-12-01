#nullable enable

using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BizHawk.Client.Common
{
	public class ClientWebSocketWrapper(Uri uri)
	{
		private ClientWebSocket? _w;

		private readonly Queue<string> _receivedMessages = new();

		private readonly Uri _uri = uri;

		/// <summary>calls <see cref="ClientWebSocket.State"/> getter (unless closed/disposed, then <see cref="WebSocketState.Closed"/> is always returned)</summary>
		public WebSocketState State => _w?.State ?? WebSocketState.Closed;

		/// <summary>calls <see cref="ClientWebSocket.CloseOutputAsync"/></summary>
		/// <remarks>also calls <see cref="ClientWebSocket.Dispose"/> (wrapper property <see cref="State"/> will continue to work, method calls will throw <see cref="ObjectDisposedException"/>)</remarks>
		public Task Close(
			WebSocketCloseStatus closeStatus,
			string statusDescription,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
		{
			if (_w == null) throw new ObjectDisposedException(nameof(_w));
			var task = _w.CloseOutputAsync(closeStatus, statusDescription, cancellationToken);
			_w.Dispose();
			_w = null;
			return task;
		}

		/// <summary>calls <see cref="ClientWebSocket.ReceiveAsync"/></summary>
		public async Task Receive(int bufferSize, int maxMessages)
		{
			var buffer = new ArraySegment<byte>(new byte[bufferSize]);
			while ((_w != null) && (_w.State == WebSocketState.Open))
			{
				WebSocketReceiveResult result;
				result = await _w.ReceiveAsync(buffer, CancellationToken.None);
				if (maxMessages == 0 || _receivedMessages.Count < maxMessages)
				{
					_receivedMessages.Enqueue(Encoding.UTF8.GetString(buffer.Array, 0, result.Count));
				}
			}
		}

		public async Task Connect(int bufferSize, int maxMessages)
		{
			_w ??= new();
			if ((_w != null) && (_w.State != WebSocketState.Open))
			{
				await _w.ConnectAsync(_uri, CancellationToken.None);
				await Receive(bufferSize, maxMessages);
			}
		}

		/// <summary>calls <see cref="ClientWebSocket.SendAsync"/></summary>
		public Task Send(
			ArraySegment<byte> buffer,
			WebSocketMessageType messageType,
			bool endOfMessage,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
				=> _w?.SendAsync(buffer, messageType, endOfMessage, cancellationToken)
					?? throw new ObjectDisposedException(nameof(_w));

		/// <summary>calls <see cref="ClientWebSocket.SendAsync"/></summary>
		public Task Send(
			string message,
			bool endOfMessage,
			CancellationToken cancellationToken = default/* == CancellationToken.None */)
		{
			if (_w == null) throw new ObjectDisposedException(nameof(_w));
			return Send(
				new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
				WebSocketMessageType.Text,
				endOfMessage,
				cancellationToken
			);
		}

		/// <summary>pops the first cached message off the message queue, otherwise returns null</summary>
		public string? PopMessage() => (_receivedMessages.Count > 0) ? _receivedMessages.Dequeue() : null;
	}
}
