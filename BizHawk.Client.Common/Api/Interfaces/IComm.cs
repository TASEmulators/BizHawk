namespace BizHawk.Client.Common
{
	public interface IComm : IExternalApi
	{
		#region Sockets
		string SocketServerScreenShot();
		string SocketServerScreenShotResponse();
		string SocketServerSend(string SendString);
		string SocketServerResponse();
		bool SocketServerSuccessful();
		void SocketServerSetTimeout(int timeout);
		#endregion

		#region MemoryMappedFiles
		void MmfSetFilename(string filename);
		string MmfGetFilename();
		int MmfScreenshot();
		int MmfWrite(string mmf_filename, string outputString);
		string MmfRead(string mmf_filename, int expectedSize);
		#endregion

		#region HTTP
		string HttpTest();
		string HttpTestGet();
		string HttpGet(string url);
		string HttpPost(string url, string payload);
		string HttpPostScreenshot();
		void HttpSetTimeout(int timeout);
		void HttpSetPostUrl(string url);
		void HttpSetGetUrl(string url);
		string HttpGetPostUrl();
		string HttpGetGetUrl();
		#endregion
	}
}
