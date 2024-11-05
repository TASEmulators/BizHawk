using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private string Username, ApiToken;
		private bool LoggedIn => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken);

		private event Action LoginStatusChanged;

		private sealed class LoginRequest : RCheevoHttpRequest
		{
			private readonly LibRCheevos.rc_api_login_request_t _apiParams;
			public string Username { get; private set; }
			public string ApiToken { get; private set; }

			public override bool ShouldRetry => false;

			public LoginRequest(string username, string apiToken = null, string password = null)
			{
				_apiParams = new(username, apiToken, password);
			}

			public override void DoRequest()
			{
				var apiParamsResult = _lib.rc_api_init_login_request(out var api_req, in _apiParams);
				InternalDoRequest(apiParamsResult, ref api_req);
			}

			protected override void ResponseCallback(byte[] serv_resp)
			{
				Username = null;
				ApiToken = null;

				if (_lib.rc_api_process_login_response(out var resp, serv_resp) == LibRCheevos.rc_error_t.RC_OK)
				{
					Username = resp.Username;
					ApiToken = resp.ApiToken;
				}

				_lib.rc_api_destroy_login_response(ref resp);
			}
		}

		private bool DoLogin(string username, string apiToken = null, string password = null)
		{
			var loginRequest = new LoginRequest(username, apiToken, password);
			PushRequest(loginRequest);
			loginRequest.Wait();

			Username = loginRequest.Username;
			ApiToken = loginRequest.ApiToken;

			return LoggedIn;
		}

		private void Login()
		{
			var config = _getConfig();
			Username = config.RAUsername;
			ApiToken = SecretStrings.DecryptString(config.RAToken);

			if (LoggedIn)
			{
				// OK, Username and ApiToken are probably valid, let's ensure they are now
				if (DoLogin(Username, apiToken: ApiToken))
				{
					config.RAUsername = Username;
					config.RAToken = SecretStrings.EncryptString(ApiToken);
					PlaySound(_loginSound);
					return;
				}
			}

			using var loginForm = new RCheevosLoginForm((username, password) => DoLogin(username, password: password));
			loginForm.ShowDialog();

			config.RAUsername = Username;
			config.RAToken = SecretStrings.EncryptString(ApiToken);

			if (LoggedIn)
			{
				PlaySound(_loginSound);
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