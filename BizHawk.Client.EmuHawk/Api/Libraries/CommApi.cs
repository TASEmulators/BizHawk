using System;
using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Client.ApiHawk;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows.Forms;


namespace BizHawk.Client.EmuHawk
{
	public sealed class CommApi : IComm
	{
		[RequiredService]
		private IEmulator Emulator { get; set; }

		[RequiredService]
		private IVideoProvider VideoProvider { get; set; }

		public CommApi() : base()
		{ }

		public string SocketServerScreenShot()
		{
			return GlobalWin.socketServer.SendScreenshot();
		}
		public string SocketServerScreenShotResponse()
		{
			return GlobalWin.socketServer.SendScreenshot(1000).ToString();
		}

		public string SocketServerSend(string SendString)
		{
			return $"Sent : {GlobalWin.socketServer.SendString(SendString)} bytes";
		}
		public string SocketServerResponse()
		{
			return GlobalWin.socketServer.ReceiveMessage();
		}

		public bool SocketServerSuccessful()
		{
			return GlobalWin.socketServer.Successful();
		}
		public void SocketServerSetTimeout(int timeout)
		{
			GlobalWin.socketServer.SetTimeout(timeout);
		}

		public void SocketServerSetIp(string ip)
		{
			GlobalWin.socketServer.Ip = ip;
		}

		public void SetSocketServerPort(int port)
		{
			GlobalWin.socketServer.Port = port;
		}

		public string SocketServerGetIp()
		{
			return GlobalWin.socketServer.Ip;
		}

		public int SocketServerGetPort()
		{
			return GlobalWin.socketServer.Port;
		}

		public string SocketServerGetInfo()
		{
			return GlobalWin.socketServer.GetInfo();
		}

		// All MemoryMappedFile related methods
		public void MmfSetFilename(string filename)
		{
			GlobalWin.memoryMappedFiles.Filename = filename;
		}

		public string MmfGetFilename()
		{
			return GlobalWin.memoryMappedFiles.Filename;
		}

		public int MmfScreenshot()
		{
			return GlobalWin.memoryMappedFiles.ScreenShotToFile();
		}

		public int MmfWrite(string mmf_filename, string outputString)
		{
			return GlobalWin.memoryMappedFiles.WriteToFile(mmf_filename, Encoding.ASCII.GetBytes(outputString));
		}

		public string MmfRead(string mmf_filename, int expectedSize)
		{
			return GlobalWin.memoryMappedFiles.ReadFromFile(mmf_filename, expectedSize);
		}


		// All HTTP related methods
		public string HttpTest()
		{
			var list = new StringBuilder();
			list.AppendLine(GlobalWin.httpCommunication.TestGet());
			list.AppendLine(GlobalWin.httpCommunication.SendScreenshot());
			list.AppendLine("done testing");
			return list.ToString();
		}
		public string HttpTestGet()
		{
			return GlobalWin.httpCommunication.TestGet();
		}
		public string HttpGet(string url)
		{
			return GlobalWin.httpCommunication.ExecGet(url);
		}

		public string HttpPost(string url, string payload)
		{
			return GlobalWin.httpCommunication.ExecPost(url, payload);
		}
		public string HttpPostScreenshot()
		{
			return GlobalWin.httpCommunication.SendScreenshot();
		}
		public void HttpSetTimeout(int timeout)
		{
			GlobalWin.httpCommunication.SetTimeout(timeout);
		}
		public void HttpSetPostUrl(string url)
		{
			GlobalWin.httpCommunication.PostUrl = url;
		}
		public void HttpSetGetUrl(string url)
		{
			GlobalWin.httpCommunication.GetUrl = url;
		}
		public string HttpGetPostUrl()
		{
			return GlobalWin.httpCommunication.PostUrl;
		}
		public string HttpGetGetUrl()
		{
			return GlobalWin.httpCommunication.GetUrl;
		}
	}
}
