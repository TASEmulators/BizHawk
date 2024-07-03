using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private static readonly HttpClient _http = new() { DefaultRequestHeaders = { ConnectionClose = true } };

		/// <summary>
		/// A concurrent stack containing all pending HTTP requests
		/// The main thread will push new requests onto this stack
		/// The HTTP thread will pop requests and start them
		/// </summary>
		private readonly ConcurrentStack<RCheevoHttpRequest> _inactiveHttpRequests = new();

		/// <summary>
		/// A list containing all currently active HTTP requests
		/// Completed requests might be restarted if ShouldRetry is true
		/// Otherwise, the completed request is disposed and removed
		/// Only the HTTP thread is allowed to use this list, no other thread may use it
		/// </summary>
		private readonly List<RCheevoHttpRequest> _activeHttpRequests = new();

		private volatile bool _isActive;
		private readonly Thread _httpThread;
		private readonly AutoResetEvent _threadThrottle = new(false);

		/// <summary>
		/// Base class for all HTTP requests to rcheevos servers
		/// </summary>
		public abstract class RCheevoHttpRequest : IDisposable
		{
			private readonly object _syncObject = new();
			private readonly ManualResetEventSlim _completionEvent = new();
			private bool _isDisposed;

			public virtual bool ShouldRetry { get; protected set; }

			public bool IsCompleted
			{
				get
				{
					lock (_syncObject)
					{
						return _isDisposed || _completionEvent.IsSet;
					}
				}
			}

			public abstract void DoRequest();
			protected abstract void ResponseCallback(byte[] serv_resp);

			public void Wait()
			{
				lock (_syncObject)
				{
					if (_isDisposed) return;
					_completionEvent.Wait();
				}
			}

			public virtual void Dispose()
			{
				if (_isDisposed) return;

				lock (_syncObject)
				{
					_completionEvent.Wait();
					_completionEvent.Dispose();
					_isDisposed = true;
				}
			}

			/// <summary>
			/// Don't use, for FailedRCheevosRequest use only
			/// </summary>
			protected void DisposeWithoutWait()
			{
#pragma warning disable BHI1101 // yeah, complain I guess, but this is a hack so meh
				if (GetType() != typeof(FailedRCheevosRequest)) throw new InvalidOperationException();
#pragma warning restore BHI1101
				_completionEvent.Dispose();
				_isDisposed = true;
			}

			public void Reset()
			{
				ShouldRetry = false;
				_completionEvent.Reset();
			}

			protected void InternalDoRequest(LibRCheevos.rc_error_t apiParamsResult, ref LibRCheevos.rc_api_request_t request)
			{
				if (apiParamsResult != LibRCheevos.rc_error_t.RC_OK)
				{
					// api params were bad, so we can't send a request
					// therefore any retry will fail
					ShouldRetry = false;
					_completionEvent.Set();
					_lib.rc_api_destroy_request(ref request);
					return;
				}

				var apiTask = request.post_data != IntPtr.Zero
					? HttpPost(request.URL, request.PostData, request.ContentType)
					: HttpGet(request.URL);

				_ = apiTask.ContinueWith(async t =>
				{
					var result = await t;
					if (result is null) // likely a timeout
					{
						ShouldRetry = true;
						_completionEvent.Set();
					}
					else
					{
						ResponseCallback(result);
						ShouldRetry = false; // this is a bit naive, but if the response callback "fails," retrying will just result in the same thing
						_completionEvent.Set();
					}
				}, default, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);

				_lib.rc_api_destroy_request(ref request);
			}
		}

		/// <summary>
		/// Represents a generic failed rcheevos request
		/// </summary>
		public sealed class FailedRCheevosRequest : RCheevoHttpRequest
		{
			public static readonly FailedRCheevosRequest Singleton = new();

			public override bool ShouldRetry => false;

			protected override void ResponseCallback(byte[] serv_resp)
			{
			}

			public override void DoRequest()
			{
			}

			private FailedRCheevosRequest()
			{
				DisposeWithoutWait();
			}
		}

		private void PushRequest(RCheevoHttpRequest request)
		{
			_inactiveHttpRequests.Push(request);
			_threadThrottle.Set();
		}

		private void PushRequests(IEnumerable<RCheevoHttpRequest> requests)
		{
			if (requests is RCheevoHttpRequest[] requestsArray)
			{
				_inactiveHttpRequests.PushRange(requestsArray);
			}
			else
			{
				foreach (var request in requests)
				{
					_inactiveHttpRequests.Push(request);
				}
			}

			_threadThrottle.Set();
		}

		private void HttpRequestThreadProc()
		{
			while (_isActive)
			{
				while (_inactiveHttpRequests.TryPop(out var request))
				{
					request.DoRequest();
					_activeHttpRequests.Add(request);
				}

				foreach (var activeRequest in _activeHttpRequests.Where(activeRequest => activeRequest.IsCompleted && activeRequest.ShouldRetry).ToArray())
				{
					activeRequest.Reset();
					activeRequest.DoRequest();
				}

				_activeHttpRequests.RemoveAll(activeRequest =>
				{
					var shouldRemove = activeRequest.IsCompleted && !activeRequest.ShouldRetry;
					if (shouldRemove)
					{
						activeRequest.Dispose();
					}

					return shouldRemove;
				});

				_threadThrottle.WaitOne(100000); // the default HTTP client timeout is 10 seconds
			}

			// typically I'd rather do this Dispose()
			// but the Wait() semantics mean we can't do that on the UI thread
			// so this thread is responsible for disposing

			// add any remaining requests, we don't want a user to miss out on an achievement due to closing the emulator too soon...
			while (_inactiveHttpRequests.TryPop(out var request))
			{
				request.DoRequest();
				_activeHttpRequests.Add(request);
			}

			foreach (var request in _activeHttpRequests)
			{
				if (request is ImageRequest) continue; // THIS IS BAD, I KNOW (but don't want the user to wait for useless ImageRequests to finish)
				// (probably wouldn't be so many ImageRequests anyways if we cache them on disk)
				request.Dispose(); // implicitly waits for the request to finish or timeout (hope it doesn't timeout)
			}
		}

		private static async Task<byte[]> HttpGet(string url)
		{
			try
			{
				var response = await _http.GetAsync(url).ConfigureAwait(false);
				return response.IsSuccessStatusCode
					? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)
					: null;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return null;
			}
		}

		private static async Task<byte[]> HttpPost(string url, string post, string type)
		{
			try
			{
				using var content = new StringContent(post, Encoding.UTF8, type ?? "application/x-www-form-urlencoded");
				using var response = await _http.PostAsync(url, content).ConfigureAwait(false);
				return response.IsSuccessStatusCode
					? await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false)
					: null;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return null;
			}
		}
	}
}