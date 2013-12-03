using LuaInterface;

namespace BizHawk.Client.Common
{
	public class MovieLuaLibrary : LuaLibraryBase
	{
		public MovieLuaLibrary(Lua lua)
			: base()
		{
			_lua = lua;
		}

		public override string Name { get { return "movie"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"filename",
					"getinput",
					"getreadonly",
					"getrerecordcounting",
					"isloaded",
					"length",
					"mode",
					"rerecordcount",
					"setreadonly",
					"setrerecordcounting",
					"stop",
				};
			}
		}

		private Lua _lua;

		public static string movie_filename()
		{
			return Global.MovieSession.Movie.Filename;
		}

		public LuaTable movie_getinput(object frame)
		{
			LuaTable input = _lua.NewTable();

			MovieControllerAdapter m = new MovieControllerAdapter { Type = Global.MovieSession.MovieControllerAdapter.Type };
			m.SetControllersAsMnemonic(
				Global.MovieSession.Movie.GetInput(LuaInt(frame))
			);

			foreach (string button in m.Type.BoolButtons)
			{
				input[button] = m[button];
			}

			return input;
		}

		public static bool movie_getreadonly()
		{
			return Global.MovieSession.ReadOnly;
		}

		public static bool movie_getrerecordcounting()
		{
			return Global.MovieSession.Movie.IsCountingRerecords;
		}

		public static bool movie_isloaded()
		{
			return Global.MovieSession.Movie.IsActive;
		}

		public static double movie_length()
		{
			return Global.MovieSession.Movie.FrameCount;
		}

		public static string movie_mode()
		{
			if (Global.MovieSession.Movie.IsFinished)
			{
				return "FINISHED";
			}
			else if (Global.MovieSession.Movie.IsPlaying)
			{
				return "PLAY";
			}
			else if (Global.MovieSession.Movie.IsRecording)
			{
				return "RECORD";
			}
			else
			{
				return "INACTIVE";
			}
		}

		public static string movie_rerecordcount()
		{
			return Global.MovieSession.Movie.Header.Rerecords.ToString();
		}

		public static void movie_setreadonly(object lua_input)
		{
			Global.MovieSession.ReadOnly = (lua_input.ToString().ToUpper() == "TRUE"
				|| lua_input.ToString() == "1");
		}

		public static void movie_setrerecordcounting(object lua_input)
		{
			Global.MovieSession.Movie.IsCountingRerecords
				= (lua_input.ToString().ToUpper() == "TRUE" || lua_input.ToString() == "1");
		}

		public static void movie_stop()
		{
			Global.MovieSession.Movie.Stop();
		}
	}
}
