using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
#if false
		private readonly RCheevosLeaderboardListForm _lboardListForm = new();
#endif

		private sealed class LboardTriggerRequest : RCheevoHttpRequest
		{
			private readonly LibRCheevos.rc_api_submit_lboard_entry_request_t _apiParams;
			private readonly DateTime _completionTime;

			protected override void ResponseCallback(byte[] serv_resp)
			{
				var res = _lib.rc_api_process_submit_lboard_entry_response(out var resp, serv_resp);
				_lib.rc_api_destroy_submit_lboard_entry_response(ref resp);
				if (res != LibRCheevos.rc_error_t.RC_OK)
				{
					Console.WriteLine($"LboardTriggerRequest failed in ResponseCallback with {res}");
				}
			}

			public override void DoRequest()
			{
				var secondsSinceCompletion = (DateTime.UtcNow - _completionTime).TotalSeconds;
				var apiParams = new LibRCheevos.rc_api_submit_lboard_entry_request_t(_apiParams.username, _apiParams.api_token,
					_apiParams.leaderboard_id, _apiParams.score, _apiParams.game_hash, (uint)secondsSinceCompletion);
				var apiParamsResult = _lib.rc_api_init_submit_lboard_entry_request(out var api_req, in apiParams);
				InternalDoRequest(apiParamsResult, ref api_req);
			}

			public LboardTriggerRequest(string username, string api_token, uint id, int value, string hash)
			{
				_apiParams = new(username, api_token, id, value, hash, seconds_since_completion: 0);
				_completionTime = DateTime.UtcNow;
			}
		}

		private bool LBoardsActive { get; set; }
		private LBoard CurrentLboard { get; set; }

		public class LBoard
		{
			public uint ID { get; }
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
	}
}