#nullable enable

using System.Net;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

namespace BizHawk.Client.Common
{
	public sealed class WebSocketServer
	{
		private readonly HttpListener httpListener;

		/// <param name="host">
		/// 	host address to register for listening to connections, defaults to <see cref="IPAddress.Loopback"/>>
		/// </param>
		/// <param name="port">port to register for listening to connections</param>
		public WebSocketServer(IPAddress? host = null, int port = 3333)
		{
			httpListener = new();
			httpListener.Prefixes.Add($"http://{host}:{port}/");
		}

		/// <summary>
		/// Starts the websocket server at the configured address and registers clients.
		/// </summary>
		/// <param name="cancellationToken">optional cancellation token to stop the server</param>
		/// <returns>async task for the server loop</returns>
		public async Task Start(CancellationToken cancellationToken = default)
		{
			Console.WriteLine("Starting the connection listener");
			httpListener.Start();
			Console.WriteLine("Started the connection listener");
			while (!cancellationToken.IsCancellationRequested)
			{
				Console.WriteLine("Waiting for a request");
				var context = await httpListener.GetContextAsync();
				if (context is null) return;

				Console.WriteLine("Got a request");
				if (!context.Request.IsWebSocketRequest)
				{
					Console.WriteLine("Request not for a websocket");
					context.Response.Abort();
					return;
				}

				Console.WriteLine("Got a websocket request, waiting for the handshake");
				var webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
				if (webSocketContext is null) return;

				string clientId = Guid.NewGuid().ToString();
				Console.WriteLine($"Assigning client ID {clientId}");
				var webSocket = webSocketContext.WebSocket;
				_ = Task.Run(async () =>
				{
					Console.WriteLine($"Starting message send loop for {clientId}");
					while ((webSocket.State == WebSocketState.Open) && !cancellationToken.IsCancellationRequested)
					{
						// TODO replace
						await Task.Delay(1000, cancellationToken);
						ArraySegment<byte> messageBuffer = new(Encoding.UTF8.GetBytes($"Hello {clientId}\r\n"));
						await webSocket.SendAsync(
							messageBuffer,
							WebSocketMessageType.Text,
							endOfMessage: true,
							cancellationToken
						);
					}
					Console.WriteLine($"Done with message send loop for {clientId}");
				}, cancellationToken);

				_ = Task.Run(async () =>
				{
					byte[] buffer = new byte[1024];
					var stringBuilder = new StringBuilder(2048);
					Console.WriteLine($"Starting message receive loop for {clientId}");
					while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
					{
						ArraySegment<byte> messageBuffer = new(buffer);
						var receiveResult = await webSocket.ReceiveAsync(messageBuffer, cancellationToken);
						if (receiveResult.Count == 0)
							return;

						// TODO replace
						stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, receiveResult.Count));
						if (receiveResult.EndOfMessage)
						{
							Console.WriteLine($"{clientId}: {stringBuilder}");
							stringBuilder = new StringBuilder();
						}
					}
					Console.WriteLine($"Done with message receive loop for {clientId}");
				}, cancellationToken);
			}
		}
	}
}
