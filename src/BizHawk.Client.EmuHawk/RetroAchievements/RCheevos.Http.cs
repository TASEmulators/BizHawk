using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private static readonly HttpClient _http = new();

		private static async Task<byte[]> HttpGet(string url)
		{
			_http.DefaultRequestHeaders.ConnectionClose = false;
			var response = await _http.GetAsync(url).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			}
			return null;
		}

		private static async Task<byte[]> HttpPost(string url, string post)
		{
			_http.DefaultRequestHeaders.ConnectionClose = true;
			HttpResponseMessage response;
			try
			{
				response = await _http.PostAsync(url + "?" + post, null).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				return Encoding.UTF8.GetBytes(e.ToString()); // bleh
			}
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
		}

		private static Task<byte[]> SendAPIRequest(in LibRCheevos.rc_api_request_t api_req)
			=> api_req.post_data != IntPtr.Zero ? HttpPost(api_req.URL, api_req.PostData) : HttpGet(api_req.URL);
	}
}