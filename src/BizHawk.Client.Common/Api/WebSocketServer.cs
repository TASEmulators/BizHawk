#nullable enable

using System;
using System.Threading;

namespace BizHawk.Client.Common
{
	public sealed class WebSocketServer
	{
		public ClientWebSocketWrapper Open(Uri uri, int bufferSize, int maxMessages) => new ClientWebSocketWrapper(uri, bufferSize, maxMessages);
	}
}
