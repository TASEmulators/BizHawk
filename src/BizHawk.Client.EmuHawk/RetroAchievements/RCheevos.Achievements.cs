using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private readonly RCheevosAchievementListForm _cheevoListForm = new();

		private sealed class CheevoUnlockRequest : RCheevoHttpRequest
		{
			private readonly LibRCheevos.rc_api_award_achievement_request_t _apiParams;
			private readonly DateTime _unlockTime;

			protected override void ResponseCallback(byte[] serv_resp)
			{
				var res = _lib.rc_api_process_award_achievement_response(out var resp, serv_resp);
				_lib.rc_api_destroy_award_achievement_response(ref resp);
				if (res != LibRCheevos.rc_error_t.RC_OK)
				{
					Console.WriteLine($"CheevoUnlockRequest failed in ResponseCallback with {res}");
				}
			}

			public override void DoRequest()
			{
				var secondsSinceUnlock = (DateTime.UtcNow - _unlockTime).TotalSeconds;
				var apiParams = new LibRCheevos.rc_api_award_achievement_request_t(_apiParams.username, _apiParams.api_token,
					_apiParams.achievement_id, _apiParams.hardcore, _apiParams.game_hash, (uint)secondsSinceUnlock);
				var apiParamsResult = _lib.rc_api_init_award_achievement_request(out var api_req, in apiParams);
				InternalDoRequest(apiParamsResult, ref api_req);
			}

			public CheevoUnlockRequest(string username, string api_token, uint achievement_id, bool hardcore, string game_hash)
			{
				_apiParams = new(username, api_token, achievement_id, hardcore, game_hash, seconds_since_unlock: 0);
				_unlockTime = DateTime.UtcNow;
			}
		}

		private bool CheevosActive { get; set; }
		private bool AllowUnofficialCheevos { get; set; }

		public class Cheevo
		{
			public uint ID { get; }
			public uint Points { get; }
			public LibRCheevos.rc_runtime_achievement_category_t Category { get; }
			public string Title { get; }
			public string Description { get; }
			public string Definition { get; }
			public string Author { get; }
			private string BadgeName { get; }
			public Bitmap BadgeUnlocked => _badgeUnlockedRequest?.Image;
			public Bitmap BadgeLocked => _badgeLockedRequest?.Image;

			private ImageRequest _badgeUnlockedRequest, _badgeLockedRequest;

			public DateTime Created { get; }
			public DateTime Updated { get; }
			public LibRCheevos.rc_runtime_achievement_type_t Type { get; }
			public float Rarity { get; }
			public float RarityHardcore { get; }

			public bool IsSoftcoreUnlocked { get; set; }
			public bool IsHardcoreUnlocked { get; set; }

			public bool IsUnlocked(bool hardcore)
				=> hardcore ? IsHardcoreUnlocked : IsSoftcoreUnlocked;

			public void SetUnlocked(bool hardcore, bool unlocked)
			{
				if (hardcore)
				{
					IsHardcoreUnlocked = unlocked;
				}
				else
				{
					IsSoftcoreUnlocked = unlocked;
				}
			}

			public bool IsPrimed { get; set; }
			private Func<bool> AllowUnofficialCheevos { get; }
			public bool Invalid { get; set; }
			public bool IsEnabled => !Invalid && (IsOfficial || AllowUnofficialCheevos());
			public bool IsOfficial => Category is LibRCheevos.rc_runtime_achievement_category_t.RC_ACHIEVEMENT_CATEGORY_CORE;

			public void LoadImages(IList<RCheevoHttpRequest> requests)
			{
				_badgeUnlockedRequest = new(BadgeName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_ACHIEVEMENT);
				_badgeLockedRequest = new(BadgeName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_ACHIEVEMENT_LOCKED);
				requests.Add(_badgeUnlockedRequest); 
				requests.Add(_badgeLockedRequest); 
			}

			public Cheevo(in LibRCheevos.rc_api_achievement_definition_t cheevo, Func<bool> allowUnofficialCheevos)
			{
				ID = cheevo.id;
				Points = cheevo.points;
				Category = cheevo.category;
				Title = cheevo.Title;
				Description = cheevo.Description;
				Definition = cheevo.Definition;
				Author = cheevo.Author;
				BadgeName = cheevo.BadgeName;
				Created = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(cheevo.created).ToLocalTime();
				Updated = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(cheevo.updated).ToLocalTime();
				Type = cheevo.type;
				Rarity = cheevo.rarity;
				RarityHardcore = cheevo.rarity_hardcore;
				IsSoftcoreUnlocked = false;
				IsHardcoreUnlocked = false;
				IsPrimed = false;
				AllowUnofficialCheevos = allowUnofficialCheevos;
				Invalid = false;
			}

			public Cheevo(in Cheevo cheevo, Func<bool> allowUnofficialCheevos)
			{
				ID = cheevo.ID;
				Points = cheevo.Points;
				Category = cheevo.Category;
				Title = cheevo.Title;
				Description = cheevo.Description;
				Definition = cheevo.Definition;
				Author = cheevo.Author;
				BadgeName = cheevo.BadgeName;
				Created = cheevo.Created;
				Updated = cheevo.Updated;
				Type = cheevo.Type;
				Rarity = cheevo.Rarity;
				RarityHardcore = cheevo.RarityHardcore;
				IsSoftcoreUnlocked = false;
				IsHardcoreUnlocked = false;
				IsPrimed = false;
				AllowUnofficialCheevos = allowUnofficialCheevos;
				Invalid = false;
			}
		}

		private readonly byte[] _cheevoFormatBuffer = new byte[1024];

		private string GetCheevoProgress(uint id)
		{
			var len = _lib.rc_runtime_format_achievement_measured(_runtime, id, _cheevoFormatBuffer, (uint)_cheevoFormatBuffer.Length);
			return Encoding.ASCII.GetString(_cheevoFormatBuffer, 0, len);
		}

		private void ToggleUnofficialCheevos()
		{
			if (_gameData.GameID == 0)
			{
				AllowUnofficialCheevos = !AllowUnofficialCheevos;
				return;
			}

			_activeModeUnlocksRequest.Wait();

			DeactivateCheevos(HardcoreMode);
			AllowUnofficialCheevos = !AllowUnofficialCheevos;
			ActivateCheevos(HardcoreMode);
		}

		private void ToSoftcoreMode()
		{
			if (_gameData == null || _gameData.GameID == 0) return;
			
			// don't worry if the meanings of _active and _inactive are wrong
			// if they are, then they're both already finished

			// first deactivate any hardcore cheevos
			// if _activeModeUnlocksRequest is still active, it's hardcore mode
			_activeModeUnlocksRequest.Wait();
			DeactivateCheevos(true);

			// now activate the softcore cheevos
			// if _inactiveModeUnlocksRequest is still active, it's softcore mode
			_inactiveModeUnlocksRequest.Wait();
			ActivateCheevos(false);

			Update();
		}

		private void DeactivateCheevos(bool hardcore)
		{
			foreach (var cheevo in _gameData.CheevoEnumerable)
			{
				if (cheevo.IsEnabled && !cheevo.IsUnlocked(hardcore))
				{
					_lib.rc_runtime_deactivate_achievement(_runtime, cheevo.ID);
				}
			}
		}

		private bool _activeModeCheevosOnceActivated;

		private void ActivateCheevos(bool hardcore)
		{
			foreach (var cheevo in _gameData.CheevoEnumerable)
			{
				if (cheevo.IsEnabled && !cheevo.IsUnlocked(hardcore))
				{
					_lib.rc_runtime_activate_achievement(_runtime, cheevo.ID, cheevo.Definition, IntPtr.Zero, 0);
				}
			}

			_activeModeCheevosOnceActivated = true;
		}

		private void OneShotActivateActiveModeCheevos()
		{
			if (_activeModeCheevosOnceActivated || _gameData.GameID == 0) return;
			_activeModeUnlocksRequest.Wait();
			ActivateCheevos(HardcoreMode);
		}
	}
}