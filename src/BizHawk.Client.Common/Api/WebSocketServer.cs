#nullable enable

using System;
using System.Threading;

namespace BizHawk.Client.Common
{
	public sealed class WebSocketServer
	{
		public ClientWebSocketWrapper Open(Uri uri, CancellationToken? cancellationToken = null) => new ClientWebSocketWrapper(uri, cancellationToken);
	}
}
