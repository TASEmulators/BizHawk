using System;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private string Username, ApiToken;
		private bool LoggedIn => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken);

		private event Action LoginStatusChanged;

		private bool DoLogin(string username, string apiToken = null, string password = null)
		{
			Username = null;
			ApiToken = null;

			var api_params = new LibRCheevos.rc_api_login_request_t(username, apiToken, password);
			var res = _lib.rc_api_init_login_request(out var api_req, ref api_params);
			SendAPIRequestIfOK(res, ref api_req, serv_resp =>
			{
				if (_lib.rc_api_process_login_response(out var resp, serv_resp) == LibRCheevos.rc_error_t.RC_OK)
				{
					Username = resp.Username;
					ApiToken = resp.ApiToken;
				}

				_lib.rc_api_destroy_login_response(ref resp);
			}).Wait(); // currently, this is done synchronously

			return LoggedIn;
		}

		private void Login()
		{
			var config = _getConfig();
			Username = config.RAUsername;
			ApiToken = config.RAToken;

			if (LoggedIn)
			{
				// OK, Username and ApiToken are probably valid, let's ensure they are now
				if (DoLogin(Username, apiToken: ApiToken))
				{
					config.RAUsername = Username;
					config.RAToken = ApiToken;
					if (EnableSoundEffects) _loginSound.PlayNoExceptions();
					return;
				}
			}

			using var loginForm = new RCheevosLoginForm((username, password) => DoLogin(username, password: password));
			loginForm.ShowDialog();

			config.RAUsername = Username;
			config.RAToken = ApiToken;

			if (LoggedIn && EnableSoundEffects)
			{
				_loginSound.PlayNoExceptions();
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