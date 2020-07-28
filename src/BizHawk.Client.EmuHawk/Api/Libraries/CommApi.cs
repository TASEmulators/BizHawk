#nullable enable

using System;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class CommApi : ICommApi
	{
		private readonly (HttpCommunication HTTP, MemoryMappedFiles MMF, SocketServer Sockets, WebSocketClient WebSocketClient) _networkingHelpers;

		public HttpCommunication? HTTP => _networkingHelpers.HTTP;

		public MemoryMappedFiles? MMF => _networkingHelpers.MMF;

		public SocketServer? Sockets => _networkingHelpers.Sockets;

		public WebSocketClient? WebSocketClient => _networkingHelpers.WebSocketClient;

		public CommApi(Action<string> logCallback, DisplayManager displayManager, InputManager inputManager, IMainFormForApi mainForm)
		{
			_networkingHelpers = mainForm.NetworkingHelpers;
		}

		public string? HttpTest() => HTTP == null ? null : string.Join("\n", HttpTestGet(), HTTP.SendScreenshot(), "done testing");

		public string? HttpTestGet() => HTTP?.Get(HTTP.GetUrl)?.Result;
	}
}
