using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private static readonly HttpClient _http = new() { DefaultRequestHeaders = { ConnectionClose = true } };

		private static async Task<byte[]> HttpGet(string url)
		{
			var response = await _http.GetAsync(url).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			}
			return new byte[1];
		}

		private static async Task<byte[]> HttpPost(string url, string post)
		{
			try
			{
				using var content = new StringContent(post, Encoding.UTF8, "application/x-www-form-urlencoded");
				using var response = await _http.PostAsync(url, content).ConfigureAwait(false);
				if (!response.IsSuccessStatusCode)
				{
					return new byte[1];
				}
				return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return new byte[1];
			}
		}

		private static Task SendAPIRequest(in LibRCheevos.rc_api_request_t api_req, Action<byte[]> callback)
		{
			var isPost = api_req.post_data != IntPtr.Zero;
			var url = api_req.URL;
			var postData = isPost ? api_req.PostData : null;
			return Task.Factory.StartNew(() =>
			{
				var apiRequestTask = isPost ? HttpPost(url, postData) : HttpGet(url);
				callback(apiRequestTask.Result);
			}, TaskCreationOptions.RunContinuationsAsynchronously);
		}

		private static Task SendAPIRequestIfOK(LibRCheevos.rc_error_t res, ref LibRCheevos.rc_api_request_t api_req, Action<byte[]> callback)
		{
			var ret = res == LibRCheevos.rc_error_t.RC_OK
				? SendAPIRequest(in api_req, callback)
				: Task.CompletedTask;
			_lib.rc_api_destroy_request(ref api_req);
			// TODO: report failures when res is not RC_OK (can be done in this function, as it's the main thread)
			return ret;
		}
	}
}