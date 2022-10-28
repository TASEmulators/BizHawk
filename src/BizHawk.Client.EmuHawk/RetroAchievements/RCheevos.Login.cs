using System;
using System.Threading;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private string Username, ApiToken;
		private bool LoggedIn => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken);

		private ManualResetEvent InitLoginDone { get; }

		private event Action LoginStatusChanged;

		private async Task<bool> LoginCallback(string username, string password)
		{
			Username = null;
			ApiToken = null;

			var api_params = new LibRCheevos.rc_api_login_request_t(username, null, password);
			if (_lib.rc_api_init_login_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
				if (_lib.rc_api_process_login_response(out var resp, serv_req) == LibRCheevos.rc_error_t.RC_OK)
				{
					Username = resp.Username;
					ApiToken = resp.ApiToken;
				}

				_lib.rc_api_destroy_login_response(ref resp);
			}

			_lib.rc_api_destroy_request(ref api_req);

			return LoggedIn;
		}

		private async void Login()
		{
			var config = _getConfig();
			Username = config.RAUsername;
			ApiToken = config.RAToken;

			if (LoggedIn)
			{
				// OK, Username and ApiToken are probably valid, let's ensure they are now
				var api_params = new LibRCheevos.rc_api_login_request_t(Username, ApiToken, null);

				Username = null;
				ApiToken = null;

				if (_lib.rc_api_init_login_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
				{
					var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
					if (_lib.rc_api_process_login_response(out var resp, serv_req) == LibRCheevos.rc_error_t.RC_OK)
					{
						Username = resp.Username;
						ApiToken = resp.ApiToken;
					}

					_lib.rc_api_destroy_login_response(ref resp);
				}

				_lib.rc_api_destroy_request(ref api_req);
			}

			if (LoggedIn)
			{
				config.RAUsername = Username;
				config.RAToken = ApiToken;
				InitLoginDone.Set();
				if (EnableSoundEffects) _loginSound.Play();
				return;
			}

			using var loginForm = new RCheevosLoginForm(LoginCallback);
			loginForm.ShowDialog();
			
			config.RAUsername = Username;
			config.RAToken = ApiToken;
			InitLoginDone.Set();

			if (LoggedIn && EnableSoundEffects)
			{
				_loginSound.Play();
			}
		}

		private void Logout()
		{
			var config = _getConfig();
			config.RAUsername = Username = string.Empty;
			config.RAToken = ApiToken = string.Empty;
			_cachedGameDatas.Clear(); // no longer valid
			// should be fine to leave other things be, they'll be reinit on login
		}
	}
}