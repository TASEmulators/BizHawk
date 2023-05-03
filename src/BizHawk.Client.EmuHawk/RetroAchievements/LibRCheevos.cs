using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable UnusedMember.Global
// ReSharper disable EnumUnderlyingTypeIsInt
// not sure about these wrt marshalling, so ignore for now
// ReSharper disable FieldCanBeMadeReadOnly.Global

// TODO: Make these record structs

namespace BizHawk.Client.EmuHawk
{
	public abstract class LibRCheevos
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		public enum rc_error_t : int
		{
			RC_OK = 0,
			RC_INVALID_LUA_OPERAND = -1,
			RC_INVALID_MEMORY_OPERAND = -2,
			RC_INVALID_CONST_OPERAND = -3,
			RC_INVALID_FP_OPERAND = -4,
			RC_INVALID_CONDITION_TYPE = -5,
			RC_INVALID_OPERATOR = -6,
			RC_INVALID_REQUIRED_HITS = -7,
			RC_DUPLICATED_START = -8,
			RC_DUPLICATED_CANCEL = -9,
			RC_DUPLICATED_SUBMIT = -10,
			RC_DUPLICATED_VALUE = -11,
			RC_DUPLICATED_PROGRESS = -12,
			RC_MISSING_START = -13,
			RC_MISSING_CANCEL = -14,
			RC_MISSING_SUBMIT = -15,
			RC_MISSING_VALUE = -16,
			RC_INVALID_LBOARD_FIELD = -17,
			RC_MISSING_DISPLAY_STRING = -18,
			RC_OUT_OF_MEMORY = -19,
			RC_INVALID_VALUE_FLAG = -20,
			RC_MISSING_VALUE_MEASURED = -21,
			RC_MULTIPLE_MEASURED = -22,
			RC_INVALID_MEASURED_TARGET = -23,
			RC_INVALID_COMPARISON = -24,
			RC_INVALID_STATE = -25,
			RC_INVALID_JSON = -26
		}

		public enum rc_runtime_event_type_t : byte
		{
			RC_RUNTIME_EVENT_ACHIEVEMENT_ACTIVATED,
			RC_RUNTIME_EVENT_ACHIEVEMENT_PAUSED,
			RC_RUNTIME_EVENT_ACHIEVEMENT_RESET,
			RC_RUNTIME_EVENT_ACHIEVEMENT_TRIGGERED,
			RC_RUNTIME_EVENT_ACHIEVEMENT_PRIMED,
			RC_RUNTIME_EVENT_LBOARD_STARTED,
			RC_RUNTIME_EVENT_LBOARD_CANCELED,
			RC_RUNTIME_EVENT_LBOARD_UPDATED,
			RC_RUNTIME_EVENT_LBOARD_TRIGGERED,
			RC_RUNTIME_EVENT_ACHIEVEMENT_DISABLED,
			RC_RUNTIME_EVENT_LBOARD_DISABLED,
			RC_RUNTIME_EVENT_ACHIEVEMENT_UNPRIMED
		}

		public enum rc_api_image_type_t : int
		{
			RC_IMAGE_TYPE_GAME = 1,
			RC_IMAGE_TYPE_ACHIEVEMENT,
			RC_IMAGE_TYPE_ACHIEVEMENT_LOCKED,
			RC_IMAGE_TYPE_USER,
		}

		public enum rc_runtime_achievement_category_t : int
		{
			RC_ACHIEVEMENT_CATEGORY_CORE = 3,
			RC_ACHIEVEMENT_CATEGORY_UNOFFICIAL = 5,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_runtime_event_t
		{
			public int id;
			public int value;
			public rc_runtime_event_type_t type;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_buffer_t
		{
			public IntPtr write;
			public IntPtr end;
			public IntPtr next;
			public unsafe fixed byte data[256];
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_request_t
		{
			public IntPtr url;
			public IntPtr post_data;
			public rc_api_buffer_t buffer;

			public string URL => Mershul.PtrToStringUtf8(url);
			public string PostData => Mershul.PtrToStringUtf8(post_data);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_response_t
		{
			public int succeeded;
			public IntPtr error_message;
			public rc_api_buffer_t buffer;

			public string ErrorMessage => Mershul.PtrToStringUtf8(error_message);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_user_unlocks_response_t
		{
			public IntPtr achievement_ids;
			public int num_achievement_ids;
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_login_response_t
		{
			public IntPtr username;
			public IntPtr api_token;
			public int score;
			public int num_unread_messages;
			public IntPtr display_name;
			public rc_api_response_t response;

			public string Username => Mershul.PtrToStringUtf8(username);
			public string ApiToken => Mershul.PtrToStringUtf8(api_token);
			public string DisplayName => Mershul.PtrToStringUtf8(display_name);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_start_session_response_t
		{
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_user_unlocks_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			public int game_id;
			[MarshalAs(UnmanagedType.Bool)]
			public bool hardcore;

			public rc_api_fetch_user_unlocks_request_t(string username, string api_token, int game_id, bool hardcore)
			{
				this.username = username;
				this.api_token = api_token;
				this.game_id = game_id;
				this.hardcore = hardcore;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_login_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string password;

			public rc_api_login_request_t(string username, string api_token, string password)
			{
				this.username = username;
				this.api_token = api_token;
				this.password = password;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_start_session_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			public int game_id;

			public rc_api_start_session_request_t(string username, string api_token, int game_id)
			{
				this.username = username;
				this.api_token = api_token;
				this.game_id = game_id;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_award_achievement_response_t
		{
			public int awarded_achievement_id;
			public int new_player_score;
			public int achievements_remaining;
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_achievement_definition_t
		{
			public int id;
			public int points;
			public rc_runtime_achievement_category_t category;
			public IntPtr title;
			public IntPtr description;
			public IntPtr definition;
			public IntPtr author;
			public IntPtr badge_name;
			public long created; // time_t?
			public long updated; // time_t?

			public string Title => Mershul.PtrToStringUtf8(title);
			public string Description => Mershul.PtrToStringUtf8(description);
			public string Definition => Mershul.PtrToStringUtf8(definition);
			public string Author => Mershul.PtrToStringUtf8(author);
			public string BadgeName => Mershul.PtrToStringUtf8(badge_name);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_leaderboard_definition_t
		{
			public int id;
			public int format;
			public IntPtr title;
			public IntPtr description;
			public IntPtr definition;
			public int lower_is_better;
			public int hidden;

			public string Title => Mershul.PtrToStringUtf8(title);
			public string Description => Mershul.PtrToStringUtf8(description);
			public string Definition => Mershul.PtrToStringUtf8(definition);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_game_data_response_t
		{
			public int id;
			public RetroAchievements.ConsoleID console_id;
			public IntPtr title;
			public IntPtr image_name;
			public IntPtr rich_presence_script;
			public IntPtr achievements;
			public int num_achievements;
			public IntPtr leaderboards;
			public int num_leaderboards;
			public rc_api_response_t response;

			public string Title => Mershul.PtrToStringUtf8(title);
			public string ImageName => Mershul.PtrToStringUtf8(image_name);
			public string RichPresenceScript => Mershul.PtrToStringUtf8(rich_presence_script);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_ping_response_t
		{
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_resolve_hash_response_t
		{
			public int game_id;
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_submit_lboard_entry_response_t
		{
			public int submitted_score;
			public int best_score;
			public int new_rank;
			public int num_entries;
			public IntPtr top_entries;
			public int num_top_entries;
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_award_achievement_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			public int achievement_id;
			[MarshalAs(UnmanagedType.Bool)]
			public bool hardcore;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string game_hash;

			public rc_api_award_achievement_request_t(string username, string api_token, int achievement_id, bool hardcore, string game_hash)
			{
				this.username = username;
				this.api_token = api_token;
				this.achievement_id = achievement_id;
				this.hardcore = hardcore;
				this.game_hash = game_hash;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_game_data_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			public int game_id;

			public rc_api_fetch_game_data_request_t(string username, string api_token, int game_id)
			{
				this.username = username;
				this.api_token = api_token;
				this.game_id = game_id;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_image_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string image_name;
			public rc_api_image_type_t image_type;

			public rc_api_fetch_image_request_t(string image_name, rc_api_image_type_t image_type)
			{
				this.image_name = image_name;
				this.image_type = image_type;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_ping_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			public int game_id;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string rich_presence;

			public rc_api_ping_request_t(string username, string api_token, int game_id, string rich_presence)
			{
				this.username = username;
				this.api_token = api_token;
				this.game_id = game_id;
				this.rich_presence = rich_presence;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_resolve_hash_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username; // note: not actually used
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token; // note: not actually used
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string game_hash;

			public rc_api_resolve_hash_request_t(string username, string api_token, string game_hash)
			{
				this.username = username;
				this.api_token = api_token;
				this.game_hash = game_hash;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_submit_lboard_entry_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			public int leaderboard_id;
			public int score;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string game_hash;

			public rc_api_submit_lboard_entry_request_t(string username, string api_token, int leaderboard_id, int score, string game_hash)
			{
				this.username = username;
				this.api_token = api_token;
				this.leaderboard_id = leaderboard_id;
				this.score = score;
				this.game_hash = game_hash;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_achievement_info_response_t
		{
			public int id;
			public int game_id;
			public int num_awarded;
			public int num_players;
			public IntPtr recently_awarded;
			public int num_recently_awarded;
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_games_list_response_t
		{
			public IntPtr entries;
			public int num_entries;
			public rc_api_response_t response;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_leaderboard_info_response_t
		{
			public int id;
			public int format;
			public int lower_is_better;
			public IntPtr title;
			public IntPtr description;
			public IntPtr definition;
			public int game_id;
			public IntPtr author;
			public long created; // time_t?
			public long updated; // time_t?
			public IntPtr entries;
			public int num_entries;
			public rc_api_response_t response;

			public string Title => Mershul.PtrToStringUtf8(title);
			public string Description => Mershul.PtrToStringUtf8(description);
			public string Definition => Mershul.PtrToStringUtf8(definition);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_achievement_info_request_t
		{
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string api_token;
			public int achievement_id;
			public int first_entry;
			public int count;
			public int friends_only;

			public rc_api_fetch_achievement_info_request_t(string username, string api_token, int achievement_id, int first_entry, int count, int friends_only)
			{
				this.username = username;
				this.api_token = api_token;
				this.achievement_id = achievement_id;
				this.first_entry = first_entry;
				this.count = count;
				this.friends_only = friends_only;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_games_list_request_t
		{
			public int console_id;

			public rc_api_fetch_games_list_request_t(int console_id)
			{
				this.console_id = console_id;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_api_fetch_leaderboard_info_request_t
		{
			public int leaderboard_id;
			public int count;
			public int first_entry;
			[MarshalAs(UnmanagedType.LPUTF8Str)]
			public string username;

			public rc_api_fetch_leaderboard_info_request_t(int leaderboard_id, int count, int first_entry, string username)
			{
				this.leaderboard_id = leaderboard_id;
				this.count = count;
				this.first_entry = first_entry;
				this.username = username;
			}
		}

		[UnmanagedFunctionPointer(cc)]
		public delegate void rc_runtime_event_handler_t(IntPtr runtime_event);

		[UnmanagedFunctionPointer(cc)]
		public delegate int rc_peek_t(int address, int num_bytes, IntPtr ud);

		[UnmanagedFunctionPointer(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool rc_runtime_validate_address_t(int address);

		[UnmanagedFunctionPointer(cc)]
		public delegate void rc_hash_message_callback(string message);

		[BizImport(cc)]
		public abstract IntPtr rc_console_memory_regions(RetroAchievements.ConsoleID id);

		[BizImport(cc)]
		public abstract IntPtr rc_console_name(RetroAchievements.ConsoleID id);

		[BizImport(cc)]
		public abstract IntPtr rc_error_str(rc_error_t error_code);

		[BizImport(cc)]
		public abstract IntPtr rc_runtime_alloc();

		[BizImport(cc)]
		public abstract void rc_runtime_init(IntPtr runtime);

		[BizImport(cc)]
		public abstract void rc_runtime_destroy(IntPtr runtime);

		[BizImport(cc)]
		public abstract void rc_runtime_reset(IntPtr runtime);

		[BizImport(cc)]
		public abstract void rc_runtime_do_frame(IntPtr runtime, rc_runtime_event_handler_t rc_runtime_event_handler_t, rc_peek_t peek, IntPtr ud, IntPtr unused);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_progress_size(IntPtr runtime, IntPtr unused);

		[BizImport(cc)]
		public abstract void rc_runtime_serialize_progress(byte[] buffer, IntPtr runtime, IntPtr unused);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_deserialize_progress(IntPtr runtime, byte[] serialized, IntPtr unused);

		[BizImport(cc)]
		public abstract void rc_runtime_invalidate_address(IntPtr runtime, int address);

		[BizImport(cc)]
		public abstract void rc_runtime_validate_addresses(IntPtr runtime, rc_runtime_event_handler_t event_handler, rc_runtime_validate_address_t validate_handler);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_activate_achievement(IntPtr runtime, int id, string memaddr, IntPtr unused, int unused_idx);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_activate_lboard(IntPtr runtime, int id, string memaddr, IntPtr unused, int unused_idx);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_activate_richpresence(IntPtr runtime, string script, IntPtr unused, int unused_idx);

		[BizImport(cc)]
		public abstract IntPtr rc_runtime_get_achievement(IntPtr runtime, int id);

		[BizImport(cc)]
		public abstract IntPtr rc_runtime_get_lboard(IntPtr runtime, int id);

		[BizImport(cc)]
		public abstract int rc_runtime_get_richpresence(IntPtr runtime, byte[] buffer, int buffersize, rc_peek_t peek, IntPtr ud, IntPtr unused);

		[BizImport(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool rc_runtime_get_achievement_measured(IntPtr runtime, int id, out int measured_value, out int measured_target);

		[BizImport(cc)]
		public abstract int rc_runtime_format_achievement_measured(IntPtr runtime, int id, byte[] buffer, long buffer_size);

		[BizImport(cc)]
		public abstract int rc_runtime_format_lboard_value(byte[] buffer, int size, int value, int format);

		[BizImport(cc)]
		public abstract void rc_runtime_deactivate_achievement(IntPtr runtime, int id);

		[BizImport(cc)]
		public abstract void rc_runtime_deactivate_lboard(IntPtr runtime, int id);

		[BizImport(cc)]
		public abstract void rc_hash_init_error_message_callback(rc_hash_message_callback callback);

		[BizImport(cc)]
		public abstract void rc_hash_init_verbose_message_callback(rc_hash_message_callback callback);

		[BizImport(cc)]
		public abstract void rc_hash_init_custom_cdreader(IntPtr reader);

		[BizImport(cc)]
		public abstract void rc_hash_init_custom_filereader(IntPtr reader);

		[BizImport(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool rc_hash_generate_from_buffer(byte[] hash, RetroAchievements.ConsoleID console_id, byte[] buffer, long buffer_size);

		[BizImport(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool rc_hash_generate_from_file(byte[] hash, RetroAchievements.ConsoleID console_id, string path);

		[BizImport(cc)]
		public abstract void rc_hash_initialize_iterator(IntPtr iterator, string path, byte[] buffer, long buffer_size);

		[BizImport(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool rc_hash_iterate(byte[] hash, IntPtr iterator);

		[BizImport(cc)]
		public abstract void rc_hash_destroy_iterator(IntPtr iterator);

		[BizImport(cc)]
		public abstract void rc_api_set_host(string hostname);

		[BizImport(cc)]
		public abstract void rc_api_set_image_host(string hostname);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_login_request(out rc_api_request_t request, ref rc_api_login_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_start_session_request(out rc_api_request_t request, ref rc_api_start_session_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_resolve_hash_request(out rc_api_request_t request, ref rc_api_resolve_hash_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_game_data_request(out rc_api_request_t request, ref rc_api_fetch_game_data_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_user_unlocks_request(out rc_api_request_t request, ref rc_api_fetch_user_unlocks_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_achievement_info_request(out rc_api_request_t request, ref rc_api_fetch_achievement_info_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_leaderboard_info_request(out rc_api_request_t request, ref rc_api_fetch_leaderboard_info_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_image_request(out rc_api_request_t request, ref rc_api_fetch_image_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_award_achievement_request(out rc_api_request_t request, ref rc_api_award_achievement_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_submit_lboard_entry_request(out rc_api_request_t request, ref rc_api_submit_lboard_entry_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_ping_request(out rc_api_request_t request, ref rc_api_ping_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_games_list_request(out rc_api_request_t request, ref rc_api_fetch_games_list_request_t api_params);

		[BizImport(cc)]
		public abstract void rc_api_destroy_request(ref rc_api_request_t request);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_login_response(out rc_api_login_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_start_session_response(out rc_api_start_session_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_resolve_hash_response(out rc_api_resolve_hash_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_fetch_game_data_response(out rc_api_fetch_game_data_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_fetch_user_unlocks_response(out rc_api_fetch_user_unlocks_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_fetch_achievement_info_response(out rc_api_fetch_achievement_info_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_fetch_leaderboard_info_response(out rc_api_fetch_leaderboard_info_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_award_achievement_response(out rc_api_award_achievement_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_submit_lboard_entry_response(out rc_api_submit_lboard_entry_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_ping_response(out rc_api_ping_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_fetch_games_list_response(out rc_api_fetch_games_list_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_login_response(ref rc_api_login_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_start_session_response(ref rc_api_start_session_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_resolve_hash_response(ref rc_api_resolve_hash_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_fetch_game_data_response(ref rc_api_fetch_game_data_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_fetch_user_unlocks_response(ref rc_api_fetch_user_unlocks_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_fetch_achievement_info_response(ref rc_api_fetch_achievement_info_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_fetch_leaderboard_info_response(ref rc_api_fetch_leaderboard_info_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_award_achievement_response(ref rc_api_award_achievement_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_submit_lboard_entry_response(ref rc_api_submit_lboard_entry_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_ping_response(ref rc_api_ping_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_fetch_games_list_response(ref rc_api_fetch_games_list_response_t response);
	}
}