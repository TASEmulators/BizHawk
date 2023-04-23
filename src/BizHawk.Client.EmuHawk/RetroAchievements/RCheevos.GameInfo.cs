using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos
	{
		private readonly RCheevosGameInfoForm _gameInfoForm = new();

		private ConsoleID _consoleId;

		private string _gameHash;
		private readonly Dictionary<string, int> _cachedGameIds = new(); // keep around IDs per hash to avoid unneeded API calls for a simple RebootCore

		private GameData _gameData;
		private readonly Dictionary<int, GameData> _cachedGameDatas = new(); // keep game data around to avoid unneeded API calls for a simple RebootCore

		private Task _activeModeUnlocksTask, _inactiveModeUnlocksTask;

		public class GameData
		{
			public int GameID { get; }
			public ConsoleID ConsoleID { get; }
			public string Title { get; }
			private string ImageName { get; }
			public Bitmap GameBadge { get; private set; }
			public string RichPresenseScript { get; }

			private readonly IReadOnlyDictionary<int, Cheevo> _cheevos;
			private readonly IReadOnlyDictionary<int, LBoard> _lboards;

			public IEnumerable<Cheevo> CheevoEnumerable => _cheevos.Values;
			public IEnumerable<LBoard> LBoardEnumerable => _lboards.Values;

			public Cheevo GetCheevoById(int i) => _cheevos[i];
			public LBoard GetLboardById(int i) => _lboards[i];

			public Task InitUnlocks(string username, string api_token, bool hardcore, Action callback = null)
			{
				var api_params = new LibRCheevos.rc_api_fetch_user_unlocks_request_t(username, api_token, GameID, hardcore);
				var res = _lib.rc_api_init_fetch_user_unlocks_request(out var api_req, ref api_params);
				return SendAPIRequestIfOK(res, ref api_req, serv_resp =>
				{
					if (_lib.rc_api_process_fetch_user_unlocks_response(out var resp, serv_resp) == LibRCheevos.rc_error_t.RC_OK)
					{
						unsafe
						{
							var unlocks = (int*)resp.achievement_ids;
							for (var i = 0; i < resp.num_achievement_ids; i++)
							{
								if (_cheevos.TryGetValue(unlocks![i], out var cheevo))
								{
									cheevo.SetUnlocked(hardcore, true);
								}
							}
						}

						_lib.rc_api_destroy_fetch_user_unlocks_response(ref resp);
					}

					callback?.Invoke();
				});
			}

			public void LoadImages()
			{
				GetImage(ImageName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_GAME, badge => GameBadge = badge);

				if (_cheevos is null) return;

				foreach (var cheevo in _cheevos.Values)
				{
					cheevo.LoadImages();
				}
			}

			public int TotalCheevoPoints(bool hardcore)
				=> _cheevos?.Values.Sum(c => c.IsEnabled && !c.Invalid && c.IsUnlocked(hardcore) ? c.Points : 0) ?? 0;

			public unsafe GameData(in LibRCheevos.rc_api_fetch_game_data_response_t resp, Func<bool> allowUnofficialCheevos)
			{
				GameID = resp.id;
				ConsoleID = resp.console_id;
				Title = resp.Title;
				ImageName = resp.ImageName;
				GameBadge = null;
				RichPresenseScript = resp.RichPresenceScript;

				var cheevos = new Dictionary<int, Cheevo>();
				var cptr = (LibRCheevos.rc_api_achievement_definition_t*)resp.achievements;
				for (var i = 0; i < resp.num_achievements; i++)
				{
					cheevos.Add(cptr![i].id, new(in cptr[i], allowUnofficialCheevos));
				}

				_cheevos = cheevos;

				var lboards = new Dictionary<int, LBoard>();
				var lptr = (LibRCheevos.rc_api_leaderboard_definition_t*)resp.leaderboards;
				for (var i = 0; i < resp.num_leaderboards; i++)
				{
					lboards.Add(lptr![i].id, new(in lptr[i]));
				}

				_lboards = lboards;
			}

			public GameData(GameData gameData, Func<bool> allowUnofficialCheevos)
			{
				GameID = gameData.GameID;
				ConsoleID = gameData.ConsoleID;
				Title = gameData.Title;
				ImageName = gameData.ImageName;
				GameBadge = null;
				RichPresenseScript = gameData.RichPresenseScript;

				_cheevos = gameData.CheevoEnumerable.ToDictionary<Cheevo, int, Cheevo>(cheevo => cheevo.ID, cheevo => new(in cheevo, allowUnofficialCheevos));
				_lboards = gameData.LBoardEnumerable.ToDictionary<LBoard, int, LBoard>(lboard => lboard.ID, lboard => new(in lboard));
			}

			public GameData()
			{
				GameID = 0;
			}
		}

		private static int SendHash(string hash)
		{
			var api_params = new LibRCheevos.rc_api_resolve_hash_request_t(null, null, hash);
			var ret = 0;
			var res = _lib.rc_api_init_resolve_hash_request(out var api_req, ref api_params);
			SendAPIRequestIfOK(res, ref api_req, serv_resp =>
			{
				if (_lib.rc_api_process_resolve_hash_response(out var resp, serv_resp) == LibRCheevos.rc_error_t.RC_OK)
				{
					ret = resp.game_id;
				}

				_lib.rc_api_destroy_resolve_hash_response(ref resp);
			}).Wait(); // currently, this is done synchronously

			return ret;
		}

		protected override int IdentifyHash(string hash)
		{
			_gameHash ??= hash;
			return _cachedGameIds.GetValueOrPut(hash, SendHash);
		}

		protected override int IdentifyRom(byte[] rom)
		{
			var hash = new byte[33];
			if (_lib.rc_hash_generate_from_buffer(hash, _consoleId, rom, rom.Length))
			{
				return IdentifyHash(Encoding.ASCII.GetString(hash, 0, 32));
			}

			_gameHash ??= "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE";
			return 0;
		}

		private void InitGameData()
		{
			_activeModeUnlocksTask = _gameData.InitUnlocks(Username, ApiToken, HardcoreMode, () =>
			{
				foreach (var cheevo in _gameData.CheevoEnumerable)
				{
					if (cheevo.IsEnabled && !cheevo.IsUnlocked(HardcoreMode))
					{
						_lib.rc_runtime_activate_achievement(ref _runtime, cheevo.ID, cheevo.Definition, IntPtr.Zero, 0);
					}
				}
			});

			_inactiveModeUnlocksTask = _gameData.InitUnlocks(Username, ApiToken, !HardcoreMode);
			_gameData.LoadImages();

			foreach (var lboard in _gameData.LBoardEnumerable)
			{
				_lib.rc_runtime_activate_lboard(ref _runtime, lboard.ID, lboard.Definition, IntPtr.Zero, 0);
			}

			if (_gameData.RichPresenseScript is not null)
			{
				_lib.rc_runtime_activate_richpresence(ref _runtime, _gameData.RichPresenseScript, IntPtr.Zero, 0);
			}
		}

		private static GameData GetGameData(string username, string api_token, int id, Func<bool> allowUnofficialCheevos)
		{
			var api_params = new LibRCheevos.rc_api_fetch_game_data_request_t(username, api_token, id);
			var ret = new GameData();
			var res = _lib.rc_api_init_fetch_game_data_request(out var api_req, ref api_params);
			SendAPIRequestIfOK(res, ref api_req, serv_resp =>
			{
				if (_lib.rc_api_process_fetch_game_data_response(out var resp, serv_resp) == LibRCheevos.rc_error_t.RC_OK)
				{
					ret = new(in resp, allowUnofficialCheevos);
				}

				_lib.rc_api_destroy_fetch_game_data_response(ref resp);
			}).Wait();

			return ret;
		}

		private static void GetImage(string image_name, LibRCheevos.rc_api_image_type_t image_type, Action<Bitmap> callback)
		{
			if (image_name is null)
			{
				callback(null);
				return;
			}

			var api_params = new LibRCheevos.rc_api_fetch_image_request_t(image_name, image_type);
			var res = _lib.rc_api_init_fetch_image_request(out var api_req, ref api_params);
			SendAPIRequestIfOK(res, ref api_req, serv_resp =>
			{
				Bitmap image;
				try
				{
					image = new(new MemoryStream(serv_resp));
				}
				catch
				{
					image = null;
				}

				callback(image);
			});
		}
	}
}