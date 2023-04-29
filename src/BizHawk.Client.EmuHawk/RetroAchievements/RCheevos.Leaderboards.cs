using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
#if false
		private readonly RCheevosLeaderboardListForm _lboardListForm = new();
#endif

		private class LboardTriggerTask
		{
			private LibRCheevos.rc_api_submit_lboard_entry_request_t _apiParams;
			public Task Task { get; private set; }
			public bool Success { get; private set; }

			private void LboardTriggerTaskCallback(byte[] serv_resp)
			{
				var res = _lib.rc_api_process_submit_lboard_entry_response(out var resp, serv_resp);
				_lib.rc_api_destroy_submit_lboard_entry_response(ref resp);
				Success = res == LibRCheevos.rc_error_t.RC_OK;
			}

			public void DoRequest()
			{
				var res = _lib.rc_api_init_submit_lboard_entry_request(out var api_req, ref _apiParams);
				Task = SendAPIRequestIfOK(res, ref api_req, LboardTriggerTaskCallback);
			}

			public LboardTriggerTask(string username, string api_token, int id, int value, string hash)
			{
				_apiParams = new(username, api_token, id, value, hash);
				DoRequest();
			}
		}

		// keep a list of all cheevo unlock trigger tasks that have been queued
		// on Dispose(), we wait for all these to complete
		// on Update(), we clear out successfully completed tasks, any not completed will be resent
		private readonly List<LboardTriggerTask> _queuedLboardTriggerTasks = new();

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
	}
}