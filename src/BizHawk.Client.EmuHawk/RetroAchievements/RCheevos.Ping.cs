using System;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private bool RichPresenceActive { get; set; }
		private string CurrentRichPresence { get; set; }
		private bool GameSessionStartSuccessful { get; set; }

		private Task _startGameSessionTask;

		private void StartGameSession()
		{
			GameSessionStartSuccessful = false;
			var api_params = new LibRCheevos.rc_api_start_session_request_t(Username, ApiToken, _gameData.GameID);
			var res = _lib.rc_api_init_start_session_request(out var api_req, ref api_params);
			_startGameSessionTask = SendAPIRequestIfOK(res, ref api_req, serv_resp =>
			{
				GameSessionStartSuccessful = _lib.rc_api_process_start_session_response(out var resp, serv_resp) == LibRCheevos.rc_error_t.RC_OK;
				_lib.rc_api_destroy_start_session_response(ref resp);
			});
		}

		private static void SendPing(string username, string api_token, int id, string rich_presence)
		{
			var api_params = new LibRCheevos.rc_api_ping_request_t(username, api_token, id, rich_presence);
			var res = _lib.rc_api_init_ping_request(out var api_req, ref api_params);
			SendAPIRequestIfOK(res, ref api_req, static serv_resp =>
			{
				_lib.rc_api_process_ping_response(out var resp, serv_resp);
				_lib.rc_api_destroy_ping_response(ref resp);
			});
		}

		private readonly byte[] _richPresenceBuffer = new byte[1024];

		private DateTime _lastPingTime = DateTime.Now;
		private static readonly TimeSpan _pingCooldown = new(10000000 * 120); // 2 minutes

		private void CheckPing()
		{
			if (RichPresenceActive)
			{
				var len = _lib.rc_runtime_get_richpresence(ref _runtime, _richPresenceBuffer, _richPresenceBuffer.Length, _peekcb, IntPtr.Zero, IntPtr.Zero);
				CurrentRichPresence = Encoding.UTF8.GetString(_richPresenceBuffer, 0, len);
			}
			else
			{
				CurrentRichPresence = null;
			}

			var now = DateTime.Now;
			if (now - _lastPingTime < _pingCooldown) return;
			SendPing(Username, ApiToken, _gameData.GameID, CurrentRichPresence);
			_lastPingTime = now;
		}
	}
}