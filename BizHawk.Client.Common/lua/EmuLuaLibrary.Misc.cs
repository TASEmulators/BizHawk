using System;
using System.Net;
using System.Net.Sockets;
using System.ComponentModel;
using System.IO;
using System.Text;

using NLua;

namespace BizHawk.Client.Common
{
	[Description("A library for misc .net methods")]
	public sealed class MiscLuaLibrary : LuaLibraryBase
	{
		public MiscLuaLibrary(Lua lua)
			: base(lua) { }

		public MiscLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		Socket sender;
		IPEndPoint remoteEP;
		TcpClient client;
		string IP;
		int port;

		public override string Name => "misc";

		[LuaMethod("sleep", ".NET sleep")]
		public static void Sleep(int val)
		{
			System.Threading.Thread.Sleep(val);
		}

		[LuaMethod("waitforfilechange", ".NET's WaitForChanged(type, timeout), but takes a path, filter and timeout")]
		public static string Waitforfilechange(string path, string filter, int timeout)
		{
			FileSystemWatcher watcher = new FileSystemWatcher(path, filter);
			WaitForChangedResult result = watcher.WaitForChanged(WatcherChangeTypes.All, timeout);
			return result.Name;
		}

		[LuaMethod("sendtosocket", "Send data to a socket, and block until a response is received. Return the data.")]
		public string Sendtosocket(string data)
		{
			try
			{
				if (!this.client.Connected)
				{
					this.client.Connect(IP, port);
				}
				byte[] bytes = new byte[1024];
				// Encode the data string into a byte array.  
				byte[] msg = Encoding.ASCII.GetBytes(data);
				NetworkStream stream = this.client.GetStream();
				stream.Write(msg, 0, msg.Length);
				StringBuilder myCompleteMessage = new StringBuilder();

				if (stream.CanRead)
				{
					byte[] myReadBuffer = new byte[1024];
					int numberOfBytesRead = 0;
					// Incoming message may be larger than the buffer size.
					do
					{
						numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);

						myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));

					}
					while (stream.DataAvailable);

				}

				return myCompleteMessage.ToString();
			}
			catch (ArgumentNullException ane)
			{
				return "__err__" + ane.ToString();
			}
			catch (SocketException se)
			{
				return "__err__" + se.ToString();
			}
			catch (Exception e)
			{
				return "__err__" + e.ToString();
			}
		}

		[LuaMethod("closesocket", "connect to IP and port")]
		public void Closesocket()
		{
			this.client.Close();
		}

		[LuaMethod("connecttosocketandsend", "connect to IP and port")]
		public static string ConnectToSocketAndSend(string IP, int port, string data)
		{
			try
			{
				

				TcpClient client = new TcpClient(IP, port);
				// Create a TCP/IP  socket.  
				//this.sender = new Socket(AddressFamily.InterNetwork,
				//	SocketType.Stream, ProtocolType.Tcp);
				//this.sender.Connect(remoteEP);
				//
				byte[] bytes = new byte[1024];
				// Encode the data string into a byte array.  
				byte[] msg = Encoding.ASCII.GetBytes(data);
				NetworkStream stream = client.GetStream();
				stream.Write(msg, 0, msg.Length);
				StringBuilder myCompleteMessage = new StringBuilder();

				if (stream.CanRead)
				{
					byte[] myReadBuffer = new byte[1024];
					int numberOfBytesRead = 0;
					// Incoming message may be larger than the buffer size.
					do
					{
						numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);

						myCompleteMessage.AppendFormat("{0}", Encoding.ASCII.GetString(myReadBuffer, 0, numberOfBytesRead));

					}
					while (stream.DataAvailable);

				}

				return myCompleteMessage.ToString();
			}
			catch (ArgumentNullException ane)
			{
				return "__err__" + ane.ToString();
			}
			catch (SocketException se)
			{
				return "__err__" + se.ToString();
			}
			catch (Exception e)
			{
				return "__err__" + e.ToString();
			}
		}


		[LuaMethod("connecttosocket", "connect to IP and port")]
		public void ConnectToSocket(string IP, int port)
		{
			try
			{

				this.client = new TcpClient(IP, port);
				// Create a TCP/IP  socket.  
				//this.sender = new Socket(AddressFamily.InterNetwork,
				//	SocketType.Stream, ProtocolType.Tcp);
				//this.sender.Connect(remoteEP);
				//
			}
			catch (ArgumentNullException ane)
			{
				Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
			}
			catch (SocketException se)
			{
				Console.WriteLine("SocketException : {0}", se.ToString());
			}
			catch (Exception e)
			{
				Console.WriteLine("Unexpected exception : {0}", e.ToString());
			}
		}

	}
}
