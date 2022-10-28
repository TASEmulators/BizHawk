using System;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private bool RichPresenceActive { get; set; }

		private string CurrentRichPresence { get; set; }

		private static async Task<bool> StartGameSessionAsync(string username, string api_token, int id)
		{
			var api_params = new LibRCheevos.rc_api_start_session_request_t(username, api_token, id);
			var res = LibRCheevos.rc_error_t.RC_INVALID_STATE;
			if (_lib.rc_api_init_start_session_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
				res = _lib.rc_api_process_start_session_response(out var resp, serv_req);
				_lib.rc_api_destroy_start_session_response(ref resp);
			}

			_lib.rc_api_destroy_request(ref api_req);
			return res == LibRCheevos.rc_error_t.RC_OK;
		}

		// todo: warn on failure?
		private static async void StartGameSession(string username, string api_token, int id)
			=> await StartGameSessionAsync(username, api_token, id).ConfigureAwait(false);

		private static async Task SendPingAsync(string username, string api_token, int id, string rich_presence)
		{
			var api_params = new LibRCheevos.rc_api_ping_request_t(username, api_token, id, rich_presence);
			if (_lib.rc_api_init_ping_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
				_lib.rc_api_process_ping_response(out var resp, serv_req);
				_lib.rc_api_destroy_ping_response(ref resp);
			}

			_lib.rc_api_destroy_request(ref api_req);
		}

		private static async void SendPing(string username, string api_token, int id, string rich_presence)
			=> await SendPingAsync(username, api_token, id, rich_presence).ConfigureAwait(false);

		private readonly byte[] _richPresenceBuffer = new byte[1024];

		private DateTime _lastPingTime = DateTime.Now;
		private static readonly TimeSpan _pingCooldown = new(10000000 * 120); // 2 minutes

		private void CheckPing()
		{
			if (RichPresenceActive)
			{
				var len = _lib.rc_runtime_get_richpresence(ref _runtime, _richPresenceBuffer, _richPresenceBuffer.Length, PeekCallback, IntPtr.Zero, IntPtr.Zero);
				CurrentRichPresence = Encoding.UTF8.GetString(_richPresenceBuffer, 0, len);
			}
			else
			{
				CurrentRichPresence = null;
			}

			var now = DateTime.Now;
			if ((now - _lastPingTime) >= _pingCooldown)
			{
				SendPing(Username, ApiToken, _gameData.GameID, CurrentRichPresence);
				_lastPingTime = now;
			}
		}
	}
}