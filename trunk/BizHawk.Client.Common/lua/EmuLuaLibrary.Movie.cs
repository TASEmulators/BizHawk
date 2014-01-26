using LuaInterface;

namespace BizHawk.Client.Common
{
	public class MovieLuaLibrary : LuaLibraryBase
	{
		public MovieLuaLibrary(Lua lua)
		{
			_lua = lua;
		}

		public override string Name { get { return "movie"; } }

		private readonly Lua _lua;

		[LuaMethodAttributes(
			"filename",
			"TODO"
		)]
		public static string Filename()
		{
			return Global.MovieSession.Movie.Filename;
		}

		[LuaMethodAttributes(
			"getinput",
			"TODO"
		)]
		public LuaTable GetInput(object frame)
		{
			var input = _lua.NewTable();

			var m = new MovieControllerAdapter { Type = Global.MovieSession.MovieControllerAdapter.Type };
			m.SetControllersAsMnemonic(
				Global.MovieSession.Movie.GetInput(LuaInt(frame))
			);

			foreach (var button in m.Type.BoolButtons)
			{
				input[button] = m[button];
			}

			return input;
		}

		[LuaMethodAttributes(
			"getreadonly",
			"TODO"
		)]
		public static bool GetReadOnly()
		{
			return Global.MovieSession.ReadOnly;
		}

		[LuaMethodAttributes(
			"getrerecordcounting",
			"TODO"
		)]
		public static bool GetRerecordCounting()
		{
			return Global.MovieSession.Movie.IsCountingRerecords;
		}

		[LuaMethodAttributes(
			"isloaded",
			"TODO"
		)]
		public static bool IsLoaded()
		{
			return Global.MovieSession.Movie.IsActive;
		}

		[LuaMethodAttributes(
			"length",
			"TODO"
		)]
		public static double Length()
		{
			return Global.MovieSession.Movie.FrameCount;
		}

		[LuaMethodAttributes(
			"mode",
			"TODO"
		)]
		public static string Mode()
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

		[LuaMethodAttributes(
			"rerecordcount",
			"TODO"
		)]
		public static string RerecordCount()
		{
			return Global.MovieSession.Movie.Header.Rerecords.ToString();
		}

		[LuaMethodAttributes(
			"setreadonly",
			"TODO"
		)]
		public static void SetReadOnly(object readonlyVal)
		{
			Global.MovieSession.ReadOnly = readonlyVal.ToString().ToUpper() == "TRUE" || readonlyVal.ToString() == "1";
		}

		[LuaMethodAttributes(
			"setrerecordcounting",
			"TODO"
		)]
		public static void SetRerecordCounting(object countVal)
		{
			Global.MovieSession.Movie.IsCountingRerecords = countVal.ToString().ToUpper() == "TRUE" || countVal.ToString() == "1";
		}

		[LuaMethodAttributes(
			"stop",
			"TODO"
		)]
		public static void Stop()
		{
			Global.MovieSession.Movie.Stop();
		}
	}
}
