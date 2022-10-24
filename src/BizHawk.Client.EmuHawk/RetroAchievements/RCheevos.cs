using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.IOExtensions;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class RCheevosExtensions
	{
		public static LibRCheevos.rc_error_t ThrowOnError(this LibRCheevos.rc_error_t error_code)
		{
			if (error_code < LibRCheevos.rc_error_t.RC_OK)
			{
				throw new Exception($"RCHEEVOS ERROR: {Enum.GetName(typeof(LibRCheevos.rc_error_t), error_code)}");
			}

			return error_code;
		}
	}

	public partial class RCheevos : RetroAchievements
	{
		private static readonly LibRCheevos _lib;

		static RCheevos()
		{
			var resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "librcheevos.so" : "librcheevos.dll", hasLimitedLifetime: false);
			_lib = BizInvoker.GetInvoker<LibRCheevos>(resolver, CallingConventionAdapters.Native);
		}

		private LibRCheevos.rc_runtime_t _runtime;

		private readonly Dictionary<int, ReadMemoryFunc> _readMap = new();

		private ConsoleID _consoleId;
		private GameData _gameData;
		private string _gameHash;

		private string Username, ApiToken;
		private bool LoggedIn => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(ApiToken);
		private bool SessionActive { get; set; }
		private bool HardcoreMode { get; set; }

		private ManualResetEvent InitLoginDone { get; }

		private struct Cheevo
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
			public bool SetUnlocked(bool hardcore, bool unlocked)
				=> hardcore ? IsHardcoreUnlocked = unlocked : IsSoftcoreUnlocked = unlocked;
			public bool IsPrimed { get; set; }
			public bool AllowUnofficialCheevos { get; set; }
			public bool Invalid { get; set; }
			public bool IsEnabled => !Invalid && (AllowUnofficialCheevos || IsOfficial);
			public bool IsOfficial => Category is LibRCheevos.rc_runtime_achievement_category_t.RC_ACHIEVEMENT_CATEGORY_CORE;

			public void SetUnofficialCheevosPolicy(bool allow)
			{
				AllowUnofficialCheevos = allow;
			}

			public async void LoadImages()
			{
				BadgeUnlocked = await GetImage(BadgeName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_ACHIEVEMENT).ConfigureAwait(false);
				BadgeLocked = await GetImage(BadgeName, LibRCheevos.rc_api_image_type_t.RC_IMAGE_TYPE_ACHIEVEMENT_LOCKED).ConfigureAwait(false);
			}

			public Cheevo(in LibRCheevos.rc_api_achievement_definition_t cheevo)
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
				AllowUnofficialCheevos = false;
				Invalid = false;
			}
		}

		private struct LBoard
		{
			public LBoard(in LibRCheevos.rc_api_leaderboard_definition_t cheevo)
			{

			}
		}

		private struct GameData
		{
			public int GameID { get; }
			public ConsoleID ConsoleID { get; }
			public string Title { get; }
			private string ImageName { get; }
			public Bitmap GameBadge { get; private set; }
			public string RichPresenseScript { get; }

			private readonly IReadOnlyDictionary<int, Cheevo> _cheevos;
			private readonly IReadOnlyDictionary<int, LBoard> _lboards;

			public IEnumerable<Cheevo> CheevoEnumerable => _cheevos?.Values;
			public IEnumerable<LBoard> LBoardEnumerable => _lboards?.Values;

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

			public unsafe GameData(in LibRCheevos.rc_api_fetch_game_data_response_t resp)
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
					cheevos.Add(cptr[i].id, new(in cptr[i]));
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
		}

		private event Action LoginStatusChanged;

		private void BuildMenu()
		{
			var tsmiddi = _raDropDownItems;
			tsmiddi.Clear();

			var shutDownRAItem = new ToolStripMenuItem("Shutdown RetroAchievements");
			shutDownRAItem.Click += (_, _) => _shutdownRACallback();
			tsmiddi.Add(shutDownRAItem);

			var loginItem = new ToolStripMenuItem("Login");
			loginItem.Click += (_, _) =>
			{
				Login();
				Restart();
				LoginStatusChanged();
			};
			loginItem.Visible = !LoggedIn;
			tsmiddi.Add(loginItem);

			var logoutItem = new ToolStripMenuItem("Logout");
			logoutItem.Click += (_, _) =>
			{
				Logout();
				LoginStatusChanged();
			};
			logoutItem.Visible = LoggedIn;
			tsmiddi.Add(logoutItem);

			LoginStatusChanged += () => loginItem.Visible = !LoggedIn;
			LoginStatusChanged += () => logoutItem.Visible = LoggedIn;

			var tss = new ToolStripSeparator();
			tsmiddi.Add(tss);


		}

		private static readonly HttpClient _http = new();

		private static async Task<byte[]> HttpGet(string url)
		{
			_http.DefaultRequestHeaders.ConnectionClose = false;
			var response = await _http.GetAsync(url).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsByteArrayAsync();
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
				return;
			}

			using var loginForm = new RCheevosLoginForm(LoginCallback);
			loginForm.ShowDialog();
			
			config.RAUsername = Username;
			config.RAToken = ApiToken;
			InitLoginDone.Set();
		}

		private void Logout()
		{
			var config = _getConfig();
			config.RAUsername = Username = string.Empty;
			config.RAToken = ApiToken = string.Empty;
			// should be fine to leave other things be, they'll be reinit on login
		}

		private async void InitGame(int id)
		{
			SessionActive = await StartGameSession(id).ConfigureAwait(false);
			_gameData = await GetGameData(id).ConfigureAwait(false);
			await _gameData.InitUnlocks(Username, ApiToken, HardcoreMode).ConfigureAwait(false);
			await _gameData.InitUnlocks(Username, ApiToken, !HardcoreMode).ConfigureAwait(false);
			await _gameData.LoadImages().ConfigureAwait(false);
		}

		private async Task<bool> StartGameSession(int id)
		{
			var api_params = new LibRCheevos.rc_api_start_session_request_t(Username, ApiToken, id);
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

		private async Task<GameData> GetGameData(int id)
		{
			var api_params = new LibRCheevos.rc_api_fetch_game_data_request_t(Username, ApiToken, id);
			var ret = default(GameData);
			if (_lib.rc_api_init_fetch_game_data_request(out var api_req, ref api_params) == LibRCheevos.rc_error_t.RC_OK)
			{
				var serv_req = await SendAPIRequest(api_req).ConfigureAwait(false);
				if (_lib.rc_api_process_fetch_game_data_response(out var resp, serv_req) == LibRCheevos.rc_error_t.RC_OK)
				{
					ret = new(in resp);
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
			return SendHashAsync(hash).Result; // bad!
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

		public RCheevos(IMainFormForRetroAchievements mainForm, InputManager inputManager, ToolManager tools,
			Func<Config> getConfig, ToolStripItemCollection raDropDownItems, Action shutdownRACallback)
			: base(mainForm, inputManager, tools, getConfig, raDropDownItems, shutdownRACallback)
		{
			_runtime = default;
			_lib.rc_runtime_init(ref _runtime);
			InitLoginDone = new(false);
			Login();
		}

		private bool _disposed = false;

		public override void Dispose()
		{
			if (_disposed) return;
			_lib.rc_runtime_destroy(ref _runtime);
			_disposed = true;
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

		public override void Stop()
		{
		}

		public override void Restart()
		{
			InitLoginDone.WaitOne();

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
							_readMap.Add(addr + i, memFunctions.ReadFunc);
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

				InitGame(ids.Count > 0 ? ids[0] : 0);

				var waitInit = HardcoreMode ? _gameData.HardcoreInitUnlocksReady : _gameData.SoftcoreInitUnlocksReady;
				if (waitInit is not null)
				{
					waitInit.WaitOne();

					foreach (var cheevo in _gameData.CheevoEnumerable)
					{
						if (cheevo.IsEnabled && !cheevo.IsUnlocked(HardcoreMode))
						{
							_lib.rc_runtime_activate_achievement(ref _runtime, cheevo.ID, cheevo.Definition, IntPtr.Zero, 0);
						}
					}
				}
			}
			else
			{
				InitGame(0);
			}

			bool cb(int address)
			{
				return _readMap.ContainsKey(address);
			}

			// validate address now that we have cheevos init
			_lib.rc_runtime_validate_addresses(ref _runtime, EventHandlerCallback, cb);

			Update();
			BuildMenu();

			// note: this can only catch quicksaves (probably only case of accidential use from hotkeys)
			_mainForm.EmuClient.BeforeQuickLoad += (_, e) =>
			{
				if (HardcoreMode)
				{
					e.Handled = _mainForm.ShowMessageBox2(null, "Loading a quicksave is not allowed in hardcode mode. Abort loading state?", "Warning", EMsgBoxIcon.Warning);
				}
			};
		}

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
		}

		private async Task SendUnlockAchievementAsync(int id, bool hardcore, string hash)
		{
			var api_params = new LibRCheevos.rc_api_award_achievement_request_t(Username, ApiToken, id, hardcore, hash);
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
				// todo: warn user
			}
		}

		private async void SendUnlockAchievement(int id, bool hardcore, string hash)
			=> await SendUnlockAchievementAsync(id, hardcore, hash).ConfigureAwait(false);

		private unsafe void EventHandlerCallback(IntPtr runtime_event)
		{
			var evt = (LibRCheevos.rc_runtime_event_t*)runtime_event;
			switch (evt->type)
			{
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_TRIGGERED:
					{
						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							_lib.rc_runtime_deactivate_achievement(ref _runtime, evt->id);

							cheevo.SetUnlocked(HardcoreMode, true);

							if (cheevo.IsOfficial)
							{
								SendUnlockAchievement(evt->id, HardcoreMode, _gameHash);
							}
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_PRIMED:
					{
						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							cheevo.IsPrimed = true;
						}

						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_STARTED:
					{
						// todo
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_CANCELED:
					{
						// todo
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_UPDATED:
					{
						// todo
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_LBOARD_TRIGGERED:
					{
						// todo
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
						// todo
						break;
					}
				case LibRCheevos.rc_runtime_event_type_t.RC_RUNTIME_EVENT_ACHIEVEMENT_UNPRIMED:
					{
						var cheevo = _gameData.GetCheevoById(evt->id);
						if (cheevo.IsEnabled)
						{
							cheevo.IsPrimed = false;
						}

						break;
					}
			}
		}

		private int PeekCallback(int address, int num_bytes, IntPtr ud)
		{
			byte Peek(int addr)
				=> _readMap.TryGetValue(addr, out var func) ? func(addr) : (byte)0;

			return num_bytes switch
			{
				1 => Peek(address),
				2 => Peek(address) | (Peek(address + 1) << 8),
				4 => Peek(address) | (Peek(address + 1) << 8) | (Peek(address + 2) << 16) | (Peek(address + 3) << 24),
				_ => throw new InvalidOperationException($"Requested {num_bytes} in {nameof(PeekCallback)}"),
			};
		}

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
		}
	}
}