using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
			
			public ManualResetEvent SoftcoreInitUnlocksReady { get; }
			public ManualResetEvent HardcoreInitUnlocksReady { get; }

			public async Task InitUnlocks(string username, string api_token, bool hardcore)
			{
				var api_params = new LibRCheevos.rc_api_fetch_user_unlocks_request_t(username, api_token, GameID, hardcore);
				if (_lib.rc_api_init_fetch_user_unlocks_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
				{
					var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
					if (_lib.rc_api_process_fetch_user_unlocks_response(out var resp, serv_req) == LibRCheevos.rc_error_t.RC_OK)
					{
						unsafe
						{
							var unlocks = (int*)resp.achievement_ids;
							for (int i = 0; i < resp.num_achievement_ids; i++)
							{
								if (_cheevos.TryGetValue(unlocks[i], out var cheevo))
								{
									cheevo.SetUnlocked(hardcore, true);
								}
							}
						}
					}

					_lib.rc_api_destroy_fetch_user_unlocks_response(ref resp);
				}

				_lib.rc_api_destroy_request(ref api_req);

				if (hardcore)
				{
					HardcoreInitUnlocksReady?.Set();
				}
				else
				{
					SoftcoreInitUnlocksReady?.Set();
				}
			}

			public async Task LoadImages()
			{
				GameBadge = await GetImage(ImageName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_GAME).ConfigureAwait(false);

				if (_cheevos is null) return;

				foreach (var cheevo in _cheevos.Values)
				{
					cheevo.LoadImages();
				}
			}

			public int TotalCheevoPoints(bool hardcore)
				=> _cheevos?.Values.Sum(c => (c.IsEnabled && !c.Invalid && c.IsUnlocked(hardcore)) ? c.Points : 0) ?? 0;

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
				for (int i = 0; i < resp.num_achievements; i++)
				{
					cheevos.Add(cptr[i].id, new(in cptr[i], allowUnofficialCheevos));
				}

				_cheevos = cheevos;

				var lboards = new Dictionary<int, LBoard>();
				var lptr = (LibRCheevos.rc_api_leaderboard_definition_t*)resp.leaderboards;
				for (int i = 0; i < resp.num_leaderboards; i++)
				{
					lboards.Add(lptr[i].id, new(in lptr[i]));
				}

				_lboards = lboards;

				SoftcoreInitUnlocksReady = new(false);
				HardcoreInitUnlocksReady = new(false);
			}

			public GameData(GameData gameData, Func<bool> allowUnofficialCheevos)
			{
				GameID = gameData.GameID;
				ConsoleID = gameData.ConsoleID;
				Title = gameData.Title;
				ImageName = gameData.ImageName;
				GameBadge = null;
				RichPresenseScript = gameData.RichPresenseScript;

				var cheevos = new Dictionary<int, Cheevo>();
				foreach (var cheevo in gameData.CheevoEnumerable)
				{
					cheevos.Add(cheevo.ID, new(in cheevo, allowUnofficialCheevos));
				}

				_cheevos = cheevos;

				var lboards = new Dictionary<int, LBoard>();
				foreach (var lboard in gameData.LBoardEnumerable)
				{
					lboards.Add(lboard.ID, new(in lboard));
				}

				_lboards = lboards;

				SoftcoreInitUnlocksReady = new(false);
				HardcoreInitUnlocksReady = new(false);
			}

			public GameData()
			{
				GameID = 0;
			}
		}

		private async Task<int> SendHashAsync(string hash)
		{
			var api_params = new LibRCheevos.rc_api_resolve_hash_request_t(null, null, hash);
			var ret = 0;
			if (_lib.rc_api_init_resolve_hash_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
				if (_lib.rc_api_process_resolve_hash_response(out var resp, serv_req) == LibRCheevos.rc_error_t.RC_OK)
				{
					ret = resp.game_id;
				}

				_lib.rc_api_destroy_resolve_hash_response(ref resp);
			}

			_lib.rc_api_destroy_request(ref api_req);
			return ret;
		}

		protected override int IdentifyHash(string hash)
		{
			_gameHash ??= hash;

			if (_cachedGameIds.ContainsKey(hash))
			{
				return _cachedGameIds[hash];
			}

			var ret = SendHashAsync(hash).ConfigureAwait(false).GetAwaiter().GetResult();
			_cachedGameIds.Add(hash, ret);
			return ret;
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

		private static async Task InitGameDataAsync(GameData gameData, string username, string api_token, bool hardcore)
		{
			await gameData.InitUnlocks(username, api_token, hardcore).ConfigureAwait(false);
			await gameData.InitUnlocks(username, api_token, !hardcore).ConfigureAwait(false);
			await gameData.LoadImages().ConfigureAwait(false);
		}

		private static async void InitGameData(GameData gameData, string username, string api_token, bool hardcore)
			=> await InitGameDataAsync(gameData, username, api_token, hardcore);

		private static GameData GetGameData(string username, string api_token, int id, Func<bool> allowUnofficialCheevos)
		{
			var api_params = new LibRCheevos.rc_api_fetch_game_data_request_t(username, api_token, id);
			var ret = new GameData();
			if (_lib.rc_api_init_fetch_game_data_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = SendAPIRequest(api_req).ConfigureAwait(false).GetAwaiter().GetResult();
				if (_lib.rc_api_process_fetch_game_data_response(out var resp, serv_req) == LibRCheevos.rc_error_t.RC_OK)
				{
					ret = new(in resp, allowUnofficialCheevos);
				}

				_lib.rc_api_destroy_fetch_game_data_response(ref resp);
			}

			_lib.rc_api_destroy_request(ref api_req);
			return ret;
		}

		private static async Task<Bitmap> GetImage(string image_name, LibRCheevos.rc_api_image_type_t image_type)
		{
			if (image_name is null) return null;

			var api_params = new LibRCheevos.rc_api_fetch_image_request_t(image_name, image_type);
			Bitmap ret = null;
			if (_lib.rc_api_init_fetch_image_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				try
				{
					var serv_resp = await SendAPIRequest(api_req).ConfigureAwait(false);
					ret = new Bitmap(new MemoryStream(serv_resp));
				}
				catch
				{
					ret = null;
				}
			}

			_lib.rc_api_destroy_request(ref api_req);
			return ret;
		}
	}
}