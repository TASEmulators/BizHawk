using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private readonly RCheevosLeaderboardListForm _lboardListForm = new();

		private bool LBoardsActive { get; set; }

		private LBoard CurrentLboard { get; set; }

		public class LBoard
		{
			public int ID { get; }
			public int Format { get; }
			public string Title { get; }
			public string Description { get; }
			public string Definition { get; }
			public bool LowerIsBetter { get; }
			public bool Hidden { get; }
			public bool Invalid { get; set; }
			public string Score { get; private set; }

			private readonly byte[] _scoreFormatBuffer = new byte[1024];

			public void SetScore(int val)
			{
				var len = _lib.rc_runtime_format_lboard_value(_scoreFormatBuffer, _scoreFormatBuffer.Length, val, Format);
				Score = Encoding.ASCII.GetString(_scoreFormatBuffer, 0, len);
			}

			public LBoard(in LibRCheevos.rc_api_leaderboard_definition_t lboard)
			{
				ID = lboard.id;
				Format = lboard.format;
				Title = lboard.Title;
				Description = lboard.Description;
				Definition = lboard.Definition;
				LowerIsBetter = lboard.lower_is_better != 0;
				Hidden = lboard.hidden != 0;
				Invalid = false;
				SetScore(0);
			}

			public LBoard(in LBoard lboard)
			{
				ID = lboard.ID;
				Format = lboard.Format;
				Title = lboard.Title;
				Description = lboard.Description;
				Definition = lboard.Definition;
				LowerIsBetter = lboard.LowerIsBetter;
				Hidden = lboard.Hidden;
				Invalid = false;
				SetScore(0);
			}
		}

		private static async Task SendTriggerLeaderboardAsync(string username, string api_token, int id, int value, string hash)
		{
			var api_params = new LibRCheevos.rc_api_submit_lboard_entry_request_t(username, api_token, id, value, hash);
			var res = LibRCheevos.rc_error_t.RC_INVALID_STATE;
			if (_lib.rc_api_init_submit_lboard_entry_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
				res = _lib.rc_api_process_submit_lboard_entry_response(out var resp, serv_req);
				_lib.rc_api_destroy_submit_lboard_entry_response(ref resp);
			}

			_lib.rc_api_destroy_request(ref api_req);

			if (res != LibRCheevos.rc_error_t.RC_OK)
			{
				// todo: warn user?
			}
		}

		private static async void SendTriggerLeaderboard(string username, string api_token, int id, int value, string hash)
			=> await SendTriggerLeaderboardAsync(username, api_token, id, value, hash).ConfigureAwait(false);
	}
}