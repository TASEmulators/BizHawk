using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public sealed class HttpCommunication
	{
		private readonly HttpClient _client = new HttpClient();

		private readonly Func<byte[]> _takeScreenshotCallback;

		public int DefaultTimeout { get; set; } = 500;

		public string GetUrl;

		public string PostUrl;

		public int Timeout { get; set; }

		public HttpCommunication(Func<byte[]> takeScreenshotCallback, string getURL, string postURL)
		{
			_takeScreenshotCallback = takeScreenshotCallback;
			GetUrl = getURL;
			PostUrl = postURL;
			_client.DefaultRequestHeaders.UserAgent.ParseAdd(VersionInfo.UserAgentEscaped);
		}

		public string ExecGet(string url = null) => Get(url ?? GetUrl).Result;

		public string ExecPost(string url = null, string payload = "") => Post(
			url ?? PostUrl,
			new FormUrlEncodedContent(new Dictionary<string, string> { ["payload"] = payload })
		).Result;

		public async Task<string> Get(string url)
		{
			_client.DefaultRequestHeaders.ConnectionClose = false;
			var response = await _client.GetAsync(url).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsStringAsync();
			}
			return null;
		}

		public async Task<string> Post(string url, FormUrlEncodedContent content)
		{
			_client.DefaultRequestHeaders.ConnectionClose = true;
			HttpResponseMessage response;
			try
			{
				response = await _client.PostAsync(url, content).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				return e.ToString();

			}
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			return await response.Content.ReadAsStringAsync();
		}

		public string SendScreenshot(string url = null, string parameter = "screenshot")
		{
			var url1 = url ?? PostUrl;
			var content = new FormUrlEncodedContent(new Dictionary<string, string> { [parameter] = Convert.ToBase64String(_takeScreenshotCallback()) });
			Task<string> postResponse = null;
			var trials = 5;
			while (postResponse == null && trials > 0)
			{
				postResponse = Post(url1, content);
				trials -= 1;
			}
			return postResponse?.Result;
		}

		public void SetTimeout(int timeout)
		{
			if (Timeout == 0 && timeout == 0)
			{
				Timeout = DefaultTimeout;
			}

			if (timeout != 0)
			{
				_client.Timeout = new TimeSpan(0, 0, 0, timeout / 1000, timeout % 1000);
				Timeout = timeout;
			}
		}
	}
}
