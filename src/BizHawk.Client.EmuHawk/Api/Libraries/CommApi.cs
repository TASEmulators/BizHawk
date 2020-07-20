using System.Text;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class CommApi : ICommApi
	{
		public string SocketServerScreenShot() => GlobalWin.socketServer.SendScreenshot();

		public string SocketServerScreenShotResponse() => GlobalWin.socketServer.SendScreenshot(1000);

		public string SocketServerSend(string SendString) => $"Sent : {GlobalWin.socketServer.SendString(SendString)} bytes";

		public string SocketServerResponse() => GlobalWin.socketServer.ReceiveMessage();

		public bool SocketServerSuccessful() => GlobalWin.socketServer.Successful;

		public void SocketServerSetTimeout(int timeout) => GlobalWin.socketServer.SetTimeout(timeout);

		public void SocketServerSetIp(string ip) => GlobalWin.socketServer.IP = ip;

		public void SetSocketServerPort(int port) => GlobalWin.socketServer.Port = port;

		public string SocketServerGetIp() => GlobalWin.socketServer.IP;

		public int SocketServerGetPort() => GlobalWin.socketServer.Port;

		public string SocketServerGetInfo() => GlobalWin.socketServer.GetInfo();

		public void MmfSetFilename(string filename) => GlobalWin.memoryMappedFiles.Filename = filename;

		public string MmfGetFilename() => GlobalWin.memoryMappedFiles.Filename;

		public int MmfScreenshot() => GlobalWin.memoryMappedFiles.ScreenShotToFile();

		public int MmfWrite(string mmf_filename, string outputString) => GlobalWin.memoryMappedFiles.WriteToFile(mmf_filename, Encoding.ASCII.GetBytes(outputString));

		public string MmfRead(string mmf_filename, int expectedSize) => GlobalWin.memoryMappedFiles.ReadFromFile(mmf_filename, expectedSize);

		public string HttpTest() => string.Join("\n", GlobalWin.httpCommunication.TestGet(), GlobalWin.httpCommunication.SendScreenshot(), "done testing");

		public string HttpTestGet() => GlobalWin.httpCommunication.TestGet();

		public string HttpGet(string url) => GlobalWin.httpCommunication.ExecGet(url);

		public string HttpPost(string url, string payload) => GlobalWin.httpCommunication.ExecPost(url, payload);

		public string HttpPostScreenshot() => GlobalWin.httpCommunication.SendScreenshot();

		public void HttpSetTimeout(int timeout) => GlobalWin.httpCommunication.SetTimeout(timeout);

		public void HttpSetPostUrl(string url) => GlobalWin.httpCommunication.PostUrl = url;

		public void HttpSetGetUrl(string url) => GlobalWin.httpCommunication.GetUrl = url;

		public string HttpGetPostUrl() => GlobalWin.httpCommunication.PostUrl;

		public string HttpGetGetUrl() => GlobalWin.httpCommunication.GetUrl;
	}
}
