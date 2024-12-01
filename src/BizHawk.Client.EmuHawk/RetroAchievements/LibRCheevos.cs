using System.IO;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;

// time_t = long?

#pragma warning disable IDE1006 // Naming Styles

// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable StructCanBeMadeReadOnly
// ReSharper disable UnusedMember.Global

namespace BizHawk.Client.EmuHawk
{
	public abstract class LibRCheevos
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		private const UnmanagedType STR_MARSHAL_HINT
#if NETSTANDARD2_1_OR_GREATER || NET47_OR_GREATER || NETCOREAPP1_1_OR_GREATER
			= UnmanagedType.LPUTF8Str;
#else
			= UnmanagedType.LPStr; // presumably this will produce mojibake for non-ASCII text but is otherwise safe? CPP confirmed it was the right one so I'm trusting him --yoshi
#endif

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
			RC_INVALID_JSON = -26,
			RC_API_FAILURE = -27,
			RC_LOGIN_REQUIRED = -28,
			RC_NO_GAME_LOADED = -29,
			RC_HARDCORE_DISABLED = -30,
			RC_ABORTED = -31,
			RC_NO_RESPONSE = -32,
			RC_ACCESS_DENIED = -33,
			RC_INVALID_CREDENTIALS = -34,
			RC_EXPIRED_TOKEN = -35,
			RC_INSUFFICIENT_BUFFER = -36,
			RC_INVALID_VARIABLE_NAME = -37,
			RC_UNKNOWN_VARIABLE_NAME = -38
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
			RC_RUNTIME_EVENT_ACHIEVEMENT_UNPRIMED,
			RC_RUNTIME_EVENT_ACHIEVEMENT_PROGRESS_UPDATED
		}

		public enum rc_api_image_type_t : uint
		{
			RC_IMAGE_TYPE_GAME = 1,
			RC_IMAGE_TYPE_ACHIEVEMENT,
			RC_IMAGE_TYPE_ACHIEVEMENT_LOCKED,
			RC_IMAGE_TYPE_USER,
		}

		public enum rc_runtime_achievement_category_t : uint
		{
			RC_ACHIEVEMENT_CATEGORY_CORE = 3,
			RC_ACHIEVEMENT_CATEGORY_UNOFFICIAL = 5,
		}

		public enum rc_runtime_achievement_type_t : uint
		{
			RC_ACHIEVEMENT_TYPE_STANDARD = 0,
			RC_ACHIEVEMENT_TYPE_MISSABLE = 1,
			RC_ACHIEVEMENT_TYPE_PROGRESSION = 2,
			RC_ACHIEVEMENT_TYPE_WIN = 3,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_runtime_event_t
		{
			public uint id;
			public int value;
			public rc_runtime_event_type_t type;
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_buffer_chunk_t(IntPtr write, IntPtr end, IntPtr start, IntPtr next);

		[StructLayout(LayoutKind.Sequential)]
		public struct rc_buffer_t
		{
			public rc_buffer_chunk_t chunk;
			public unsafe fixed byte data[256];
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_request_t(IntPtr url, IntPtr post_data, IntPtr content_type, rc_buffer_t buffer)
		{
			public readonly string URL => Mershul.PtrToStringUtf8(url);
			public readonly string PostData => Mershul.PtrToStringUtf8(post_data);
			public readonly string ContentType => Mershul.PtrToStringUtf8(content_type);
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_response_t(int succeeded, IntPtr error_message, IntPtr error_code, rc_buffer_t buffer)
		{
			public readonly string ErrorMessage => Mershul.PtrToStringUtf8(error_message);
			public readonly string ErrorCode => Mershul.PtrToStringUtf8(error_code);
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_fetch_user_unlocks_response_t(IntPtr achievement_ids, uint num_achievement_ids, rc_api_response_t response);

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_login_response_t(
			IntPtr username, IntPtr api_token, uint score, uint score_softcore, uint num_unread_messages, IntPtr display_name, rc_api_response_t response)
		{
			public readonly string Username => Mershul.PtrToStringUtf8(username);
			public readonly string ApiToken => Mershul.PtrToStringUtf8(api_token);
			public readonly string DisplayName => Mershul.PtrToStringUtf8(display_name);
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_start_session_response_t(
			IntPtr hardcore_unlocks, IntPtr unlocks, uint num_hardcore_unlocks, uint num_unlocks, long server_now, rc_api_response_t response);

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_fetch_user_unlocks_request_t(string username, string api_token, uint game_id, bool hardcore)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token;
			public readonly uint game_id = game_id;
			[MarshalAs(UnmanagedType.Bool)]
			public readonly bool hardcore = hardcore;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_login_request_t(string username, string api_token, string password)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string password = password;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_start_session_request_t(string username, string api_token, uint game_id, string game_hash, bool hardcore)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token;
			public readonly uint game_id = game_id;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string game_hash = game_hash;
			[MarshalAs(UnmanagedType.Bool)]
			public readonly bool hardcore = hardcore;
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_award_achievement_response_t(
			uint awarded_achievement_id,
			uint new_player_score,
			uint new_player_score_softcore,
			uint achievements_remaining,
			rc_api_response_t response);

		[StructLayout(LayoutKind.Sequential)]
		public readonly record struct rc_api_achievement_definition_t(uint id, uint points, rc_runtime_achievement_category_t category,
			IntPtr title, IntPtr description, IntPtr definition, IntPtr author, IntPtr badge_name, long created, long updated,
			rc_runtime_achievement_type_t type, float rarity, float rarity_hardcore)
		{
			public string Title => Mershul.PtrToStringUtf8(title);
			public string Description => Mershul.PtrToStringUtf8(description);
			public string Definition => Mershul.PtrToStringUtf8(definition);
			public string Author => Mershul.PtrToStringUtf8(author);
			public string BadgeName => Mershul.PtrToStringUtf8(badge_name);
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly record struct rc_api_leaderboard_definition_t(
			uint id, int format, IntPtr title, IntPtr description, IntPtr definition, byte lower_is_better, byte hidden)
		{
			public string Title => Mershul.PtrToStringUtf8(title);
			public string Description => Mershul.PtrToStringUtf8(description);
			public string Definition => Mershul.PtrToStringUtf8(definition);
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_fetch_game_data_response_t(uint id, RetroAchievements.ConsoleID console_id, IntPtr title, IntPtr image_name,
			IntPtr rich_presence_script, IntPtr achievements, uint num_achievements, IntPtr leaderboards, uint num_leaderboards, rc_api_response_t response)
		{
			public readonly string Title => Mershul.PtrToStringUtf8(title);
			public readonly string ImageName => Mershul.PtrToStringUtf8(image_name);
			public readonly string RichPresenceScript => Mershul.PtrToStringUtf8(rich_presence_script);
		}

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_ping_response_t(rc_api_response_t response);

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_resolve_hash_response_t(uint game_id, rc_api_response_t response);

		[StructLayout(LayoutKind.Sequential)]
		public record struct rc_api_submit_lboard_entry_response_t(
			int submitted_score, int best_score, uint new_rank, uint num_entries, IntPtr top_entries, uint num_top_entries, rc_api_response_t response);

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_award_achievement_request_t(string username, string api_token, uint achievement_id, bool hardcore, string game_hash, uint seconds_since_unlock)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token;
			public readonly uint achievement_id = achievement_id;
			[MarshalAs(UnmanagedType.Bool)]
			public readonly bool hardcore = hardcore;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string game_hash = game_hash;
			public readonly uint seconds_since_unlock = seconds_since_unlock;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_fetch_game_data_request_t(string username, string api_token, uint game_id)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token;
			public readonly uint game_id = game_id;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_fetch_image_request_t(string image_name, rc_api_image_type_t image_type)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string image_name = image_name;
			public readonly rc_api_image_type_t image_type = image_type;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_ping_request_t(string username, string api_token, uint game_id, string rich_presence, string game_hash, bool hardcore)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token;
			public readonly uint game_id = game_id;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string rich_presence = rich_presence;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string game_hash = game_hash;
			[MarshalAs(UnmanagedType.Bool)]
			public readonly bool hardcore = hardcore;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_resolve_hash_request_t(string username, string api_token, string game_hash)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username; // note: not actually used
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token; // note: not actually used
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string game_hash = game_hash;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly struct rc_api_submit_lboard_entry_request_t(string username, string api_token, uint leaderboard_id, int score, string game_hash, uint seconds_since_completion)
		{
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string username = username;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string api_token = api_token;
			public readonly uint leaderboard_id = leaderboard_id;
			public readonly int score = score;
			[MarshalAs(STR_MARSHAL_HINT)]
			public readonly string game_hash = game_hash;
			public readonly uint seconds_since_completion = seconds_since_completion;
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly record struct rc_hash_filereader(
			rc_hash_filereader.rc_hash_filereader_open_file_handler open,
			rc_hash_filereader.rc_hash_filereader_seek_handler seek,
			rc_hash_filereader.rc_hash_filereader_tell_handler tell,
			rc_hash_filereader.rc_hash_filereader_read_handler read,
			rc_hash_filereader.rc_hash_filereader_close_file_handler close)
		{
			[UnmanagedFunctionPointer(cc)]
			public delegate IntPtr rc_hash_filereader_open_file_handler([MarshalAs(STR_MARSHAL_HINT)] string path_utf8);

			[UnmanagedFunctionPointer(cc)]
			public delegate void rc_hash_filereader_seek_handler(IntPtr file_handle, long offset, SeekOrigin origin);

			[UnmanagedFunctionPointer(cc)]
			public delegate long rc_hash_filereader_tell_handler(IntPtr file_handle);

			[UnmanagedFunctionPointer(cc)]
			public delegate nuint rc_hash_filereader_read_handler(IntPtr file_handle, IntPtr buffer, nuint requested_bytes);

			[UnmanagedFunctionPointer(cc)]
			public delegate void rc_hash_filereader_close_file_handler(IntPtr file_handle);
		}

		[StructLayout(LayoutKind.Sequential)]
		public readonly record struct rc_hash_cdreader(
			rc_hash_cdreader.rc_hash_cdreader_open_track_handler open_track,
			rc_hash_cdreader.rc_hash_cdreader_read_sector_handler read_sector,
			rc_hash_cdreader.rc_hash_cdreader_close_track_handler close_track,
			rc_hash_cdreader.rc_hash_cdreader_first_track_sector_handler first_track_sector)
		{
			[UnmanagedFunctionPointer(cc)]
			public delegate IntPtr rc_hash_cdreader_open_track_handler([MarshalAs(STR_MARSHAL_HINT)] string path, int track);

			[UnmanagedFunctionPointer(cc)]
			public delegate nuint rc_hash_cdreader_read_sector_handler(IntPtr track_handle, uint sector, IntPtr buffer, nuint requested_bytes);

			[UnmanagedFunctionPointer(cc)]
			public delegate void rc_hash_cdreader_close_track_handler(IntPtr track_handle);

			[UnmanagedFunctionPointer(cc)]
			public delegate uint rc_hash_cdreader_first_track_sector_handler(IntPtr track_handle);
		}

		[UnmanagedFunctionPointer(cc)]
		public delegate void rc_runtime_event_handler_t(IntPtr runtime_event);

		[UnmanagedFunctionPointer(cc)]
		public delegate uint rc_runtime_peek_t(uint address, uint num_bytes, IntPtr ud);

		[UnmanagedFunctionPointer(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool rc_runtime_validate_address_t(uint address);

		[UnmanagedFunctionPointer(cc)]
		public delegate void rc_hash_message_callback([MarshalAs(STR_MARSHAL_HINT)] string message);

		[UnmanagedFunctionPointer(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool rc_hash_3ds_get_cia_normal_key_func(byte common_key_index, IntPtr out_normal_key);

		[UnmanagedFunctionPointer(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public delegate bool rc_hash_3ds_get_ncch_normal_keys_func(IntPtr primary_key_y, byte secondary_key_x_slot, IntPtr optional_program_id, IntPtr out_primary_key, IntPtr out_secondary_key);

		[BizImport(cc)]
		public abstract IntPtr rc_version_string();

		[BizImport(cc)]
		public abstract IntPtr rc_error_str(rc_error_t error_code);

		[BizImport(cc)]
		public abstract IntPtr rc_runtime_alloc();

		[BizImport(cc)]
		public abstract void rc_runtime_destroy(IntPtr runtime);

		[BizImport(cc)]
		public abstract void rc_runtime_reset(IntPtr runtime);

		[BizImport(cc)]
		public abstract void rc_runtime_do_frame(IntPtr runtime, rc_runtime_event_handler_t rc_runtime_event_handler_t, rc_runtime_peek_t peek, IntPtr ud, IntPtr unused);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_progress_size(IntPtr runtime, IntPtr unused);

		[BizImport(cc)]
		public abstract void rc_runtime_serialize_progress_sized(byte[] buffer, uint buffer_size, IntPtr runtime, IntPtr unused);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_deserialize_progress_sized(IntPtr runtime, byte[] serialized, uint serialized_size, IntPtr unused);

		[BizImport(cc)]
		public abstract void rc_runtime_validate_addresses(IntPtr runtime, rc_runtime_event_handler_t event_handler, rc_runtime_validate_address_t validate_handler);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_activate_achievement(IntPtr runtime, uint id, string memaddr, IntPtr unused, int unused_idx);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_activate_lboard(IntPtr runtime, uint id, string memaddr, IntPtr unused, int unused_idx);

		[BizImport(cc)]
		public abstract rc_error_t rc_runtime_activate_richpresence(IntPtr runtime, string script, IntPtr unused, int unused_idx);

		[BizImport(cc)]
		public abstract int rc_runtime_get_richpresence(IntPtr runtime, byte[] buffer, nuint buffersize, rc_runtime_peek_t peek, IntPtr ud, IntPtr unused);

		[BizImport(cc)]
		public abstract int rc_runtime_format_achievement_measured(IntPtr runtime, uint id, byte[] buffer, nuint buffer_size);

		[BizImport(cc)]
		public abstract int rc_runtime_format_lboard_value(byte[] buffer, int size, int value, int format);

		[BizImport(cc)]
		public abstract void rc_runtime_deactivate_achievement(IntPtr runtime, uint id);

		[BizImport(cc)]
		public abstract void rc_hash_init_error_message_callback(rc_hash_message_callback callback);

		[BizImport(cc)]
		public abstract void rc_hash_init_verbose_message_callback(rc_hash_message_callback callback);

		[BizImport(cc, Compatibility = true)]
		public abstract void rc_hash_init_custom_cdreader(in rc_hash_cdreader reader);

		[BizImport(cc, Compatibility = true)]
		public abstract void rc_hash_init_custom_filereader(in rc_hash_filereader reader);

		[BizImport(cc)]
		public abstract void rc_hash_init_3ds_get_cia_normal_key_func(rc_hash_3ds_get_cia_normal_key_func func);

		[BizImport(cc)]
		public abstract void rc_hash_init_3ds_get_ncch_normal_keys_func(rc_hash_3ds_get_ncch_normal_keys_func func);

		[BizImport(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool rc_hash_generate_from_buffer(byte[] hash, RetroAchievements.ConsoleID console_id, byte[] buffer, nuint buffer_size);

		[BizImport(cc)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public abstract bool rc_hash_generate_from_file(byte[] hash, RetroAchievements.ConsoleID console_id, string path);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_login_request(out rc_api_request_t request, in rc_api_login_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_start_session_request(out rc_api_request_t request, in rc_api_start_session_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_resolve_hash_request(out rc_api_request_t request, in rc_api_resolve_hash_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_game_data_request(out rc_api_request_t request, in rc_api_fetch_game_data_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_user_unlocks_request(out rc_api_request_t request, in rc_api_fetch_user_unlocks_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_fetch_image_request(out rc_api_request_t request, in rc_api_fetch_image_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_award_achievement_request(out rc_api_request_t request, in rc_api_award_achievement_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_submit_lboard_entry_request(out rc_api_request_t request, in rc_api_submit_lboard_entry_request_t api_params);

		[BizImport(cc, Compatibility = true)]
		public abstract rc_error_t rc_api_init_ping_request(out rc_api_request_t request, in rc_api_ping_request_t api_params);

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
		public abstract rc_error_t rc_api_process_award_achievement_response(out rc_api_award_achievement_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_submit_lboard_entry_response(out rc_api_submit_lboard_entry_response_t response, byte[] server_response);

		[BizImport(cc)]
		public abstract rc_error_t rc_api_process_ping_response(out rc_api_ping_response_t response, byte[] server_response);

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
		public abstract void rc_api_destroy_award_achievement_response(ref rc_api_award_achievement_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_submit_lboard_entry_response(ref rc_api_submit_lboard_entry_response_t response);

		[BizImport(cc)]
		public abstract void rc_api_destroy_ping_response(ref rc_api_ping_response_t response);
	}
}