using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.PathExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class RCheevos : RetroAchievements
	{
		private static readonly LibRCheevos _lib;

		static RCheevos()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "librcheevos.so" : "librcheevos.dll", hasLimitedLifetime: false);
			_lib = BizInvoker.GetInvoker<LibRCheevos>(resolver, CallingConventionAdapters.Native);
		}

		private readonly RCheevosGameInfoForm _gameInfoForm = new();
		private readonly RCheevosAchievementListForm _cheevoListForm = new();
		private readonly RCheevosLeaderboardListForm _lboardListForm = new();

		// NOTE: these are net framework only...
		// this logic should probably be the main sound class
		// this shouldn't be a blocker to moving to net core anyways
		private static readonly SoundPlayer _loginSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/login.wav"));
		private static readonly SoundPlayer _unlockSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/unlock.wav"));
		private static readonly SoundPlayer _lboardStartSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/lb.wav"));
		private static readonly SoundPlayer _lboardFailedSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/lbcancel.wav"));
		private static readonly SoundPlayer _infoSound = new(Path.Combine(PathUtils.ExeDirectoryPath, "overlay/info.wav"));

		private LibRCheevos.rc_runtime_t _runtime;

		private readonly Dictionary<int, (ReadMemoryFunc Func, int Start)> _readMap = new();

		private ConsoleID _consoleId;

		private string _gameHash;
		private readonly Dictionary<string, int> _cachedGameIds = new(); // keep around IDs per hash to avoid unneeded API calls for a simple RebootCore

		private GameData _gameData;
		private readonly Dictionary<int, GameData> _cachedGameDatas = new(); // keep game data around to avoid unneeded API calls for a simple RebootCore

		private string CurrentRichPresence { get; set; }

		private string Username, ApiToken;
		private bool LoggedIn => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken);

		private ToolStripMenuItem _hardcoreModeMenuItem;
		private bool _hardcoreMode;

		private bool CheevosActive { get; set; }
		private bool LBoardsActive { get; set; }
		private bool RichPresenceActive { get; set; }
		private bool HardcoreMode
		{
			get => _hardcoreMode;
			set => _hardcoreModeMenuItem.Checked = value;
		}

		private bool EnableSoundEffects { get; set; }
		private bool AllowUnofficialCheevos { get; set; }

		private ManualResetEvent InitLoginDone { get; }

		public class Cheevo
		{
			public int ID { get; }
			public int Points { get; }
			public LibRCheevos.rc_runtime_achievement_category_t Category { get; }
			public string Title { get; }
			public string Description { get; }
			public string Definition { get; }
			public string Author { get; }
			private string BadgeName { get; }
			public Bitmap BadgeUnlocked { get; private set; }
			public Bitmap BadgeLocked { get; private set; }
			public DateTime Created { get; }
			public DateTime Updated { get; }

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

			public async void LoadImages()
			{
				BadgeUnlocked = await GetImage(BadgeName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_ACHIEVEMENT).ConfigureAwait(false);
				BadgeLocked = await GetImage(BadgeName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_ACHIEVEMENT_LOCKED).ConfigureAwait(false);
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
				BadgeUnlocked = null;
				BadgeLocked = null;
				Created = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(cheevo.created).ToLocalTime();
				Updated = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(cheevo.updated).ToLocalTime();
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
				BadgeUnlocked = null;
				BadgeLocked = null;
				Created = cheevo.Created;
				Updated = cheevo.Updated;
				IsSoftcoreUnlocked = false;
				IsHardcoreUnlocked = false;
				IsPrimed = false;
				AllowUnofficialCheevos = allowUnofficialCheevos;
				Invalid = false;
			}
		}

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

		private readonly byte[] _cheevoFormatBuffer = new byte[1024];

		public string GetCheevoProgress(int id)
		{
			var len = _lib.rc_runtime_format_achievement_measured(ref _runtime, id, _cheevoFormatBuffer, _cheevoFormatBuffer.Length);
			return Encoding.ASCII.GetString(_cheevoFormatBuffer, 0, len);
		}

		private event Action LoginStatusChanged;

		private void BuildMenu(ToolStripItemCollection raDropDownItems)
		{
			raDropDownItems.Clear();

			var shutDownRAItem = new ToolStripMenuItem("Shutdown RetroAchievements");
			shutDownRAItem.Click += (_, _) => _shutdownRACallback();
			raDropDownItems.Add(shutDownRAItem);

			var autoStartRAItem = new ToolStripMenuItem("Autostart RetroAchievements")
			{
				Checked = _getConfig().RAAutostart,
				CheckOnClick = true,
			};
			autoStartRAItem.CheckedChanged += (_, _) => _getConfig().RAAutostart ^= true;
			raDropDownItems.Add(autoStartRAItem);

			var loginItem = new ToolStripMenuItem("Login")
			{
				Visible = !LoggedIn
			};
			loginItem.Click += (_, _) =>
			{
				Login();
				firstRestart = true; // kinda
				Restart();
				LoginStatusChanged();
			};
			raDropDownItems.Add(loginItem);

			var logoutItem = new ToolStripMenuItem("Logout")
			{
				Visible = LoggedIn
			};
			logoutItem.Click += (_, _) =>
			{
				Logout();
				LoginStatusChanged();
			};
			raDropDownItems.Add(logoutItem);

			LoginStatusChanged += () => loginItem.Visible = !LoggedIn;
			LoginStatusChanged += () => logoutItem.Visible = LoggedIn;

			var tss = new ToolStripSeparator();
			raDropDownItems.Add(tss);

			var enableCheevosItem = new ToolStripMenuItem("Enable Achievements")
			{
				Checked = CheevosActive,
				CheckOnClick = true
			};
			enableCheevosItem.CheckedChanged += (_, _) => CheevosActive ^= true;
			raDropDownItems.Add(enableCheevosItem);

			var enableLboardsItem = new ToolStripMenuItem("Enable Leaderboards")
			{
				Checked = LBoardsActive,
				CheckOnClick = true,
				Enabled = HardcoreMode
			};
			enableLboardsItem.CheckedChanged += (_, _) => LBoardsActive ^= true;
			raDropDownItems.Add(enableLboardsItem);

			var enableRichPresenceItem = new ToolStripMenuItem("Enable Rich Presence")
			{
				Checked = RichPresenceActive,
				CheckOnClick = true
			};
			enableRichPresenceItem.CheckedChanged += (_, _) => RichPresenceActive ^= true;
			raDropDownItems.Add(enableRichPresenceItem);

			var enableHardcoreItem = new ToolStripMenuItem("Enable Hardcore Mode")
			{
				Checked = HardcoreMode,
				CheckOnClick = true
			};
			enableHardcoreItem.CheckedChanged += (_, _) =>
			{
				_hardcoreMode ^= true;
				enableLboardsItem.Enabled = HardcoreMode;

				if (HardcoreMode)
				{
					_hardcoreMode = _mainForm.RebootCore(); // unset hardcore mode if we fail to reboot core somehow
				}
				else
				{
					ToSoftcoreMode();
				}
			};
			raDropDownItems.Add(enableHardcoreItem);

			_hardcoreModeMenuItem = enableHardcoreItem;

			var enableSoundEffectsItem = new ToolStripMenuItem("Enable Sound Effects")
			{
				Checked = EnableSoundEffects,
				CheckOnClick = true
			};
			enableSoundEffectsItem.CheckedChanged += (_, _) => EnableSoundEffects ^= true;
			raDropDownItems.Add(enableSoundEffectsItem);

			var enableUnofficialCheevosItem = new ToolStripMenuItem("Test Unofficial Achievements")
			{
				Checked = AllowUnofficialCheevos,
				CheckOnClick = true
			};
			enableUnofficialCheevosItem.CheckedChanged += (_, _) => ToggleUnofficialCheevos();
			raDropDownItems.Add(enableUnofficialCheevosItem);

			tss = new ToolStripSeparator();
			raDropDownItems.Add(tss);

			var viewGameInfoItem = new ToolStripMenuItem("View Game Info");
			viewGameInfoItem.Click += (_, _) =>
			{
				_gameInfoForm.OnFrameAdvance(_gameData.GameBadge, _gameData.TotalCheevoPoints(HardcoreMode),
					CurrentLboard is null ? "N/A" : $"{CurrentLboard.Description} ({CurrentLboard.Score})");

				_gameInfoForm.Show();
			};
			raDropDownItems.Add(viewGameInfoItem);

			var viewCheevoListItem = new ToolStripMenuItem("View Achievement List");
			viewCheevoListItem.Click += (_, _) =>
			{
				_cheevoListForm.OnFrameAdvance(HardcoreMode, true);
				_cheevoListForm.Show();
			};
			raDropDownItems.Add(viewCheevoListItem);

			var viewLboardListItem = new ToolStripMenuItem("View Leaderboard List");
			viewLboardListItem.Click += (_, _) =>
			{
				_lboardListForm.OnFrameAdvance(true);
				_lboardListForm.Show();
			};
			raDropDownItems.Add(viewLboardListItem);
		}

		private static readonly HttpClient _http = new();

		private static async Task<byte[]> HttpGet(string url)
		{
			_http.DefaultRequestHeaders.ConnectionClose = false;
			var response = await _http.GetAsync(url).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			}
			return null;
		}

		private static async Task<byte[]> HttpPost(string url, string post)
		{
			_http.DefaultRequestHeaders.ConnectionClose = true;
			HttpResponseMessage response;
			try
			{
				response = await _http.PostAsync(url + "?" + post, null).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				return Encoding.UTF8.GetBytes(e.ToString());
			}
			if (!response.IsSuccessStatusCode)
			{
				return null;
			}
			return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
		}

		private static Task<byte[]> SendAPIRequestAsync(LibRCheevos.rc_api_request_t api_req)
		{
			var ret = api_req.post_data != IntPtr.Zero
				? HttpPost(api_req.URL, api_req.PostData)
				: HttpGet(api_req.URL);

			return ret;
		}

		private static async Task<string> SendAPIRequest(LibRCheevos.rc_api_request_t api_req)
		{
			var bytes = await SendAPIRequestReturnRaw(api_req).ConfigureAwait(false);
			return Encoding.UTF8.GetString(bytes);
		}

		private static Task<byte[]> SendAPIRequestReturnRaw(LibRCheevos.rc_api_request_t api_req)
			=> api_req.post_data != IntPtr.Zero ? HttpPost(api_req.URL, api_req.PostData) : HttpGet(api_req.URL);

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

		private static async Task InitGameGataAsync(GameData gameData, string username, string api_token, bool hardcore)
		{
			await gameData.InitUnlocks(username, api_token, hardcore).ConfigureAwait(false);
			await gameData.InitUnlocks(username, api_token, !hardcore).ConfigureAwait(false);
			await gameData.LoadImages().ConfigureAwait(false);
		}

		private static async void InitGameGata(GameData gameData, string username, string api_token, bool hardcore)
			=> await InitGameGataAsync(gameData, username, api_token, hardcore);

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
					var serv_resp = await SendAPIRequestReturnRaw(api_req).ConfigureAwait(false);
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

		protected override void HandleHardcoreModeDisable(string reason)
		{
			_mainForm.ShowMessageBox(null, $"{reason} Disabling hardcore mode.", "Warning", EMsgBoxIcon.Warning);
			HardcoreMode = false;
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

		public RCheevos(IMainFormForRetroAchievements mainForm, InputManager inputManager, ToolManager tools,
			Func<Config> getConfig, ToolStripItemCollection raDropDownItems, Action shutdownRACallback)
			: base(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback)
		{
			_runtime = default;
			_lib.rc_runtime_init(ref _runtime);
			InitLoginDone = new(false);
			Login();
			InitLoginDone.WaitOne();

			var config = _getConfig();
			CheevosActive = config.RACheevosActive;
			LBoardsActive = config.RALBoardsActive;
			RichPresenceActive = config.RARichPresenceActive;
			_hardcoreMode = config.RAHardcoreMode;
			EnableSoundEffects = config.RASoundEffects;
			AllowUnofficialCheevos = config.RAAllowUnofficialCheevos;

			BuildMenu(raDropDownItems);
		}

		public override void Dispose()
		{
			_lib.rc_runtime_destroy(ref _runtime);
			Stop();
			_gameInfoForm.Dispose();
			_cheevoListForm.Dispose();
			_lboardListForm.Dispose();
		}

		public override void OnSaveState(string path)
		{
			if (!LoggedIn)
			{
				return;
			}

			var size = _lib.rc_runtime_progress_size(ref _runtime, IntPtr.Zero);
			if (size > 0)
			{
				var buffer = new byte[(int)size];
				_lib.rc_runtime_serialize_progress(buffer, ref _runtime, IntPtr.Zero);
				using var file = File.OpenWrite(path + ".rap");
				file.Write(buffer, 0, buffer.Length);
			}
		}

		public override void OnLoadState(string path)
		{
			if (!LoggedIn)
			{
				return;
			}

			if (HardcoreMode)
			{
				HandleHardcoreModeDisable("Loading savestates is not allowed in hardcore mode.");
			}

			_lib.rc_runtime_reset(ref _runtime);

			if (File.Exists(path + ".rap"))
			{
				using var file = File.OpenRead(path + ".rap");
				var buffer = file.ReadAllBytes();
				_lib.rc_runtime_deserialize_progress(ref _runtime, buffer, IntPtr.Zero);
			}
		}

		// not sure if we really need to do anything here...
		// nice way to ensure config is written back every so often (and on close)
		public override void Stop()
		{
			var config = _getConfig();
			config.RACheevosActive = CheevosActive;
			config.RALBoardsActive = LBoardsActive;
			config.RARichPresenceActive = RichPresenceActive;
			config.RAHardcoreMode = HardcoreMode;
			config.RASoundEffects = EnableSoundEffects;
			config.RAAllowUnofficialCheevos = AllowUnofficialCheevos;
		}

		private void ToggleUnofficialCheevos()
		{
			if (_gameData.GameID == 0)
			{
				AllowUnofficialCheevos ^= true;
				return;
			}

			var initReady = HardcoreMode ? _gameData.HardcoreInitUnlocksReady : _gameData.SoftcoreInitUnlocksReady;
			initReady.WaitOne();

			foreach (var cheevo in _gameData.CheevoEnumerable)
			{
				if (cheevo.IsEnabled && !cheevo.IsUnlocked(HardcoreMode))
				{
					_lib.rc_runtime_deactivate_achievement(ref _runtime, cheevo.ID);
				}
			}

			AllowUnofficialCheevos ^= true;

			foreach (var cheevo in _gameData.CheevoEnumerable)
			{
				if (cheevo.IsEnabled && !cheevo.IsUnlocked(HardcoreMode))
				{
					_lib.rc_runtime_activate_achievement(ref _runtime, cheevo.ID, cheevo.Definition, IntPtr.Zero, 0);
				}
			}
		}

		private void ToSoftcoreMode()
		{
			if (_gameData == null || _gameData.GameID == 0) return;

			_gameData.SoftcoreInitUnlocksReady.WaitOne();

			foreach (var cheevo in _gameData.CheevoEnumerable)
			{
				if (cheevo.IsEnabled && !cheevo.IsUnlocked(false))
				{
					_lib.rc_runtime_deactivate_achievement(ref _runtime, cheevo.ID);
				}
			}

			_gameData.HardcoreInitUnlocksReady.WaitOne();

			foreach (var cheevo in _gameData.CheevoEnumerable)
			{
				if (cheevo.IsEnabled && !cheevo.IsUnlocked(true))
				{
					_lib.rc_runtime_activate_achievement(ref _runtime, cheevo.ID, cheevo.Definition, IntPtr.Zero, 0);
				}
			}

			Update();
		}

		private bool firstRestart = true;

		public override void Restart()
		{
			if (firstRestart)
			{
				firstRestart = false;
				if (HardcoreMode)
				{
					HardcoreMode = _mainForm.RebootCore(); // unset hardcore mode if we fail to reboot core somehow
					if (HardcoreMode && _mainForm.CurrentlyOpenRomArgs is not null)
					{
						// if we aren't hardcore anymore, we failed to reboot the core (and didn't call Restart probably)
						// if CurrentlyOpenRomArgs is null, then Restart won't be called (as RebootCore returns true immediately), so
						return;
					}
				}
			}

			if (!LoggedIn)
			{
				return;
			}

			// reinit the runtime
			_lib.rc_runtime_destroy(ref _runtime);
			_runtime = default;
			_lib.rc_runtime_init(ref _runtime);

			// get console id
			_consoleId = SystemIdToConsoleId();

			// init the read map
			_readMap.Clear();

			if (Emu.HasMemoryDomains())
			{
				_memFunctions = CreateMemoryBanks(_consoleId, Domains, Emu.CanDebug() ? Emu.AsDebuggable() : null);

				var addr = 0;
				foreach (var memFunctions in _memFunctions)
				{
					if (memFunctions.ReadFunc is not null)
					{
						for (int i = 0; i < memFunctions.BankSize; i++)
						{
							_readMap.Add(addr + i, (memFunctions.ReadFunc, addr));
						}
					}

					addr += memFunctions.BankSize;
				}
			}

			// verify and init whatever is loaded
			AllGamesVerified = true;
			_gameHash = null; // will be set by first IdentifyHash

			if (_mainForm.CurrentlyOpenRomArgs is not null)
			{
				var ids = GetRAGameIds(_mainForm.CurrentlyOpenRomArgs.OpenAdvanced, _consoleId);

				AllGamesVerified = !ids.Contains(0);

				var gameId = ids.Count > 0 ? ids[0] : 0;

				if (gameId != 0)
				{
					if (_cachedGameDatas.TryGetValue(gameId, out var cachedGameData))
					{
						_gameData = new GameData(cachedGameData, () => AllowUnofficialCheevos);
					}
					else
					{
						_gameData = GetGameData(Username, ApiToken, gameId, () => AllowUnofficialCheevos);
					}

					StartGameSession(Username, ApiToken, gameId);

					_cachedGameDatas.Remove(gameId);
					_cachedGameDatas.Add(gameId, _gameData);

					InitGameGata(_gameData, Username, ApiToken, HardcoreMode);

					foreach (var lboard in _gameData.LBoardEnumerable)
					{
						_lib.rc_runtime_activate_lboard(ref _runtime, lboard.ID, lboard.Definition, IntPtr.Zero, 0);
					}

					if (_gameData.RichPresenseScript is not null)
					{
						_lib.rc_runtime_activate_richpresence(ref _runtime, _gameData.RichPresenseScript, IntPtr.Zero, 0);
					}

					var waitInit = HardcoreMode ? _gameData.HardcoreInitUnlocksReady : _gameData.SoftcoreInitUnlocksReady;
					// hopefully not too long, given we spent some time doing other work
					waitInit.WaitOne();

					foreach (var cheevo in _gameData.CheevoEnumerable)
					{
						if (cheevo.IsEnabled && !cheevo.IsUnlocked(HardcoreMode))
						{
							_lib.rc_runtime_activate_achievement(ref _runtime, cheevo.ID, cheevo.Definition, IntPtr.Zero, 0);
						}
					}
				}
				else
				{
					_gameData = new GameData();
				}
			}
			else
			{
				_gameData = new GameData();
			}

			// validate addresses now that we have cheevos init
			_lib.rc_runtime_validate_addresses(ref _runtime, EventHandlerCallback, address => _readMap.ContainsKey(address));

			_gameInfoForm.Restart(_gameData.Title, _gameHash, _gameData.TotalCheevoPoints(HardcoreMode));
			_cheevoListForm.Restart(_gameData.GameID == 0 ? Array.Empty<Cheevo>() : _gameData.CheevoEnumerable, GetCheevoProgress);
			_lboardListForm.Restart(_gameData.GameID == 0 ? Array.Empty<LBoard>() : _gameData.LBoardEnumerable);

			Update();

			// note: this can only catch quicksaves (probably only case of accidential use from hotkeys)
			_mainForm.EmuClient.BeforeQuickLoad += (_, e) =>
			{
				if (HardcoreMode)
				{
					e.Handled = _mainForm.ShowMessageBox2(null, "Loading a quicksave is not allowed in hardcode mode. Abort loading state?", "Warning", EMsgBoxIcon.Warning);
				}
			};
		}

		private readonly byte[] _richPresenceBuffer = new byte[1024];
		private DateTime _lastPingTime = DateTime.Now;
		private static readonly TimeSpan _pingCooldown = new(10000000 * 120); // 2 minutes

		public override void Update()
		{
			if (!LoggedIn)
			{
				return;
			}

			if (HardcoreMode)
			{
				CheckHardcoreModeConditions();
			}

			if (_gameData.GameID != 0)
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

		private static async Task SendUnlockAchievementAsync(string username, string api_token, int id, bool hardcore, string hash)
		{
			var api_params = new LibRCheevos.rc_api_award_achievement_request_t(username, api_token, id, hardcore, hash);
			var res = LibRCheevos.rc_error_t.RC_INVALID_STATE;
			if (_lib.rc_api_init_award_achievement_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
				res = _lib.rc_api_process_award_achievement_response(out var resp, serv_req);
				_lib.rc_api_destroy_award_achievement_response(ref resp);
			}

			_lib.rc_api_destroy_request(ref api_req);

			if (res != LibRCheevos.rc_error_t.RC_OK)
			{
				// todo: warn user? correct local version of cheevos?
			}
		}

		private static async void SendUnlockAchievement(string username, string api_token, int id, bool hardcore, string hash)
			=> await SendUnlockAchievementAsync(username, api_token, id, hardcore, hash).ConfigureAwait(false);

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

		private unsafe void EventHandlerCallback(IntPtr runtime_event)
		{
			var evt = (LibRCheevos.rc_runtime_event_t*)runtime_event;
			switch (evt->type)
			{
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_TRIGGERED:
					{
						if (!CheevosActive) return;

						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							_lib.rc_runtime_deactivate_achievement(ref _runtime, evt->id);

							cheevo.SetUnlocked(HardcoreMode, true);
							var prefix = HardcoreMode ? "[HARDCORE] " : "";
							_mainForm.AddOnScreenMessage($"{prefix}Achievement Unlocked!");
							_mainForm.AddOnScreenMessage(cheevo.Description);
							if (EnableSoundEffects) _unlockSound.Play();

							if (cheevo.IsOfficial)
							{
								SendUnlockAchievement(Username, ApiToken, evt->id, HardcoreMode, _gameHash);
							}
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_PRIMED:
					{
						if (!CheevosActive) return;

						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							cheevo.IsPrimed = true;
							var prefix = HardcoreMode ? "[HARDCORE] " : "";
							_mainForm.AddOnScreenMessage($"{prefix}Achievement Primed!");
							_mainForm.AddOnScreenMessage(cheevo.Description);
							if (EnableSoundEffects) _infoSound.Play();
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_STARTED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							lboard.SetScore(evt->value);

							if (!lboard.Hidden)
							{
								CurrentLboard = lboard;
								_mainForm.AddOnScreenMessage($"Leaderboard Attempt Started!");
								_mainForm.AddOnScreenMessage(lboard.Description);
								if (EnableSoundEffects) _lboardStartSound.Play();
							}
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_CANCELED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							if (!lboard.Hidden)
							{
								if (lboard == CurrentLboard)
								{
									CurrentLboard = null;
								}

								_mainForm.AddOnScreenMessage($"Leaderboard Attempt Failed! ({lboard.Score})");
								_mainForm.AddOnScreenMessage(lboard.Description);
								if (EnableSoundEffects) _lboardFailedSound.Play();
							}

							lboard.SetScore(0);
						}
						
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_UPDATED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							lboard.SetScore(evt->value);
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_TRIGGERED:
					{
						if (!LBoardsActive || !HardcoreMode) return;

						var lboard = _gameData.GetLboardById(evt->id);
						if (!lboard.Invalid)
						{
							SendTriggerLeaderboard(Username, ApiToken, evt->id, evt->value, _gameHash);

							if (!lboard.Hidden)
							{
								if (lboard == CurrentLboard)
								{
									CurrentLboard = null;
								}

								_mainForm.AddOnScreenMessage($"Leaderboard Attempt Complete! ({lboard.Score})");
								_mainForm.AddOnScreenMessage(lboard.Description);
								if (EnableSoundEffects) _unlockSound.Play();
							}
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_DISABLED:
					{
						var cheevo = _gameData.GetCheevoById(evt->id);
						cheevo.Invalid = true;
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_DISABLED:
					{
						var lboard = _gameData.GetLboardById(evt->id);
						lboard.Invalid = true;
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_UNPRIMED:
					{
						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							cheevo.IsPrimed = false;
							var prefix = HardcoreMode ? "[HARDCORE] " : "";
							_mainForm.AddOnScreenMessage($"{prefix}Achievement Unprimed!");
							_mainForm.AddOnScreenMessage(cheevo.Description);
							if (EnableSoundEffects) _infoSound.Play();
						}

						break;
					}
			}
		}

		private int PeekCallback(int address, int num_bytes, IntPtr ud)
		{
			byte Peek(int addr)
				=> _readMap.TryGetValue(addr, out var reader) ? reader.Func(addr - reader.Start) : (byte)0;

			return num_bytes switch
			{
				1 => Peek(address),
				2 => Peek(address) | (Peek(address + 1) << 8),
				4 => Peek(address) | (Peek(address + 1) << 8) | (Peek(address + 2) << 16) | (Peek(address + 3) << 24),
				_ => throw new InvalidOperationException($"Requested {num_bytes} in {nameof(PeekCallback)}"),
			};
		}

		private LBoard CurrentLboard { get; set; }

		public override void OnFrameAdvance()
		{
			if (!LoggedIn)
			{
				return;
			}

			var input = _inputManager.ControllerOutput;
			foreach (var resetButton in input.Definition.BoolButtons.Where(b => b.Contains("Power") || b.Contains("Reset")))
			{
				if (input.IsPressed(resetButton))
				{
					_lib.rc_runtime_reset(ref _runtime);
					break;
				}
			}

			if (Emu.HasMemoryDomains())
			{
				// we want to EnterExit to prevent wbx host spam when peeks are spammed
				using (Domains.MainMemory.EnterExit())
				{
					_lib.rc_runtime_do_frame(ref _runtime, EventHandlerCallback, PeekCallback, IntPtr.Zero, IntPtr.Zero);
				}
			}
			else
			{
				_lib.rc_runtime_do_frame(ref _runtime, EventHandlerCallback, PeekCallback, IntPtr.Zero, IntPtr.Zero);
			}

			if (_gameInfoForm.IsShown)
			{
				_gameInfoForm.OnFrameAdvance(_gameData.GameBadge, _gameData.TotalCheevoPoints(HardcoreMode),
					CurrentLboard is null ? "N/A" : $"{CurrentLboard.Description} ({CurrentLboard.Score})");
			}

			if (_cheevoListForm.IsShown)
			{
				_cheevoListForm.OnFrameAdvance(HardcoreMode);
			}

			if (_lboardListForm.IsShown)
			{
				_lboardListForm.OnFrameAdvance();
			}
		}
	}
}