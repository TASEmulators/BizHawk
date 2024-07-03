using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.Common
{
	public sealed class SocketServer
	{
		public static readonly byte[] LENGTH_PREFIX_SEPARATOR = { (byte) ' ' };

		public static byte[] PrefixWithLength(byte[] payload)
			=> Encoding.ASCII.GetBytes(payload.Length.ToString()).Concat(LENGTH_PREFIX_SEPARATOR).ToArray()
				.ConcatArray(payload);

		private readonly ProtocolType _protocol;

		private IPEndPoint _remoteEp;

		private Socket _soc;

		private readonly Func<byte[]> _takeScreenshotCallback;

		private (string HostIP, ushort Port) _targetAddr;

		public bool Connected { get; private set; }

		public string IP
		{
			get => _targetAddr.HostIP;
			set
			{
				_targetAddr.HostIP = value;
				Connect();
			}
		}

		public ushort Port
		{
			get => _targetAddr.Port;
			set
			{
				_targetAddr.Port = value;
				Connect();
			}
		}

		public (string HostIP, ushort Port) TargetAddress
		{
			get => _targetAddr;
			set
			{
				_targetAddr = value;
				Connect();
			}
		}

#if true
		private const int Retries = 10;
#else
		public int Retries { get; set; } = 10;
#endif

		public bool Successful { get; private set; }

		public SocketServer(Func<byte[]> takeScreenshotCallback, ProtocolType protocol, string ip, ushort port)
		{
			_protocol = protocol;
			ReinitSocket(out _soc);
			_takeScreenshotCallback = takeScreenshotCallback;
			TargetAddress = (ip, port);
		}

		private void ReinitSocket(out Socket socket)
			=> socket = new(AddressFamily.InterNetwork, SocketType.Stream, _protocol);

		private void Connect()
		{
			_remoteEp = new IPEndPoint(IPAddress.Parse(_targetAddr.HostIP), _targetAddr.Port);
			ReinitSocket(out _soc);
			_soc.Connect(_remoteEp);
			Connected = true;
		}

		public string GetInfo() => $"{_targetAddr.HostIP}:{_targetAddr.Port}";

		/// <remarks>
		/// Since BizHawk 2.6.2, all responses must be of the form <c>$"{msg.Length:D} {msg}"</c> i.e. prefixed with the length in base-10 and a space.
		/// </remarks>
		public string ReceiveString(Encoding encoding = null)
		{
			if (!Connected)
			{
				Connect();
			}

			var myencoding = encoding ?? Encoding.UTF8;

			try
			{
				//build length of string into a string
				byte[] oneByte = new byte[1];
				StringBuilder sb = new StringBuilder();
				for (; ; )
				{
					int recvd = _soc.Receive(oneByte, 1, 0);
					if (oneByte[0] == (byte)' ')
						break;
					sb.Append((char)oneByte[0]);
				}

				//receive string of indicated length
				int lenStringBytes = int.Parse(sb.ToString());
				byte[] buf = new byte[lenStringBytes];
				int todo = lenStringBytes;
				int at = 0;
				for (; ; )
				{
					int recvd = _soc.Receive(buf, at, todo, SocketFlags.None);
					if (recvd == 0)
						throw new InvalidOperationException("ReceiveString terminated early");
					todo -= recvd;
					at += recvd;
					if (todo == 0)
						break;
				}
				return myencoding.GetString(buf, 0, lenStringBytes);
			}
			catch
			{
				//not sure I like this, but that's how it was
				return "";
			}
		}

		public int SendBytes(byte[] sendBytes)
		{
			try
			{
				return _soc.Send(sendBytes);
			}
			catch
			{
				return -1;
			}
		}

		public string SendScreenshot(int waitingTime = 0)
		{
			var bmpBytes = PrefixWithLength(_takeScreenshotCallback());
			var sentBytes = 0;
			var tries = 0;
			while (sentBytes <= 0 && tries < Retries)
			{
				try
				{
					tries++;
					sentBytes = SendBytes(bmpBytes);
				}
				catch (SocketException)
				{
					Connect();
					sentBytes = 0;
				}
				if (sentBytes == -1)
				{
					Connect();
				}
			}

			Successful = tries < Retries;
			if (waitingTime == 0)
			{
				return Successful ? "Screenshot was sent" : "Screenshot could not be sent";
			}
			var resp = ReceiveString();
			return resp == "" ? "Failed to get a response" : resp;
		}

		public int SendString(string sendString, Encoding encoding = null)
		{
			var sentBytes = SendBytes(PrefixWithLength((encoding ?? Encoding.UTF8).GetBytes(sendString)));
			Successful = sentBytes > 0;
			return sentBytes;
		}

		public void SetTimeout(int timeout) => _soc.ReceiveTimeout = timeout;

		public void SocketConnected() => Connected = !_soc.Poll(1000, SelectMode.SelectRead) || _soc.Available != 0;
	}
}
