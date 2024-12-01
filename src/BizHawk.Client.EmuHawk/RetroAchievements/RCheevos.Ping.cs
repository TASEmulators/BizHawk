using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private bool RichPresenceActive { get; set; }
		private string CurrentRichPresence { get; set; }

		private sealed class StartGameSessionRequest : RCheevoHttpRequest
		{
			private readonly LibRCheevos.rc_api_start_session_request_t _apiParams;

			public StartGameSessionRequest(string username, string apiToken, uint gameId, string gameHash, bool hardcore)
			{
				_apiParams = new(username, apiToken, gameId, gameHash, hardcore);
			}

			public override void DoRequest()
			{
				var apiParamsResult = _lib.rc_api_init_start_session_request(out var api_req, in _apiParams);
				InternalDoRequest(apiParamsResult, ref api_req);
			}

			protected override void ResponseCallback(byte[] serv_resp)
			{
				var res = _lib.rc_api_process_start_session_response(out var resp, serv_resp);
				_lib.rc_api_destroy_start_session_response(ref resp);
				if (res != LibRCheevos.rc_error_t.RC_OK)
				{
					Console.WriteLine($"StartGameSessionRequest failed in ResponseCallback with {res}");
				}
			}
		}

		private void StartGameSession()
			=> PushRequest(new StartGameSessionRequest(Username, ApiToken, _gameData.GameID, _gameHash, HardcoreMode));

		private sealed class PingRequest : RCheevoHttpRequest
		{
			private readonly LibRCheevos.rc_api_ping_request_t _apiParams;

			public PingRequest(string username, string apiToken, uint gameId, string richPresence, string gameHash, bool hardcore)
			{
				_apiParams = new(username, apiToken, gameId, richPresence, gameHash, hardcore);
			}

			public override void DoRequest()
			{
				var apiParamsResult = _lib.rc_api_init_ping_request(out var api_req, in _apiParams);
				InternalDoRequest(apiParamsResult, ref api_req);
			}

			protected override void ResponseCallback(byte[] serv_resp)
			{
				var res = _lib.rc_api_process_ping_response(out var resp, serv_resp);
				_lib.rc_api_destroy_ping_response(ref resp);
				if (res != LibRCheevos.rc_error_t.RC_OK)
				{
					Console.WriteLine($"PingRequest failed in ResponseCallback with {res}");
				}
			}
		}

		private void SendPing()
			=> PushRequest(new PingRequest(Username, ApiToken, _gameData.GameID, CurrentRichPresence, _gameHash, HardcoreMode));

		private readonly byte[] _richPresenceBuffer = new byte[1024];

		private DateTime _lastPingTime = DateTime.Now;
		private static readonly TimeSpan _pingCooldown = new(10000000 * 120); // 2 minutes

		private void CheckPing()
		{
			if (RichPresenceActive)
			{
				var len = _lib.rc_runtime_get_richpresence(_runtime, _richPresenceBuffer, (uint)_richPresenceBuffer.Length, _peekcb, IntPtr.Zero, IntPtr.Zero);
				CurrentRichPresence = Encoding.UTF8.GetString(_richPresenceBuffer, 0, len);
			}
			else
			{
				CurrentRichPresence = null;
			}

			var now = DateTime.Now;
			if (now - _lastPingTime < _pingCooldown) return;
			SendPing();
			_lastPingTime = now;
		}
	}
}