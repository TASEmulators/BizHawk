using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BizHawk.Client.Common
{
	public sealed class SocketServer
	{
		private IPEndPoint _remoteEp;

		private Socket _soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		private readonly Func<byte[]> _takeScreenshotCallback;

		private (string HostIP, int Port) _targetAddr;

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

		public int Port
		{
			get => _targetAddr.Port;
			set
			{
				_targetAddr.Port = value;
				Connect();
			}
		}

		public (string HostIP, int Port) TargetAddress
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

		public SocketServer(Func<byte[]> takeScreenshotCallback, string ip, int port)
		{
			_takeScreenshotCallback = takeScreenshotCallback;
			TargetAddress = (ip, port);
		}

		private void Connect()
		{
			_remoteEp = new IPEndPoint(IPAddress.Parse(_targetAddr.HostIP), _targetAddr.Port);
			_soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_soc.Connect(_remoteEp);
			Connected = true;
			_soc.ReceiveTimeout = 5;
		}

		public string GetInfo() => $"{_targetAddr.HostIP}:{_targetAddr.Port}";

		public string ReceiveMessage(Encoding encoding = null)
		{
			if (!Connected)
			{
				Connect();
			}

			var encoding1 = encoding ?? Encoding.UTF8;
			var resp = "";
			var receivedBytes = new byte[256];
			var receivedLength = 1;
			while (receivedLength > 0)
			{
				try
				{
					receivedLength = _soc.Receive(receivedBytes, receivedBytes.Length, 0);
					resp += encoding1.GetString(receivedBytes);
				}
				catch
				{
					receivedLength = 0;
				}
			}
			return resp;
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
			var bmpBytes = _takeScreenshotCallback();
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
			var resp = ReceiveMessage();
			return resp == "" ? "Failed to get a response" : resp;
		}

		public int SendString(string sendString, Encoding encoding = null)
		{
			var sentBytes = SendBytes((encoding ?? Encoding.UTF8).GetBytes(sendString));
			Successful = sentBytes > 0;
			return sentBytes;
		}

		public void SetTimeout(int timeout) => _soc.ReceiveTimeout = timeout;

		public void SocketConnected() => Connected = !_soc.Poll(1000, SelectMode.SelectRead) || _soc.Available != 0;
	}
}
