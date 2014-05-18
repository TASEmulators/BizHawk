using LuaInterface;

namespace BizHawk.Client.Common
{
	public class MovieLuaLibrary : LuaLibraryBase
	{
		private readonly Lua _lua;

		public MovieLuaLibrary(Lua lua)
		{
			_lua = lua;
		}

		public override string Name { get { return "movie"; } }

		[LuaMethodAttributes(
			"filename",
			"Returns the file name including path of the currently loaded movie"
		)]
		public static string Filename()
		{
			return Global.MovieSession.Movie.Filename;
		}

		[LuaMethodAttributes(
			"getinput",
			"Returns a table of buttons pressed on a given frame of the loaded movie"
		)]
		public LuaTable GetInput(int frame)
		{
			var input = _lua.NewTable();

			var m = new MovieControllerAdapter { Type = Global.MovieSession.MovieControllerAdapter.Type };
			m.SetControllersAsMnemonic(
				Global.MovieSession.Movie.GetInput(frame));

			foreach (var button in m.Type.BoolButtons)
			{
				input[button] = m[button];
			}

			return input;
		}

		[LuaMethodAttributes(
			"getinputasmnemonic",
			"Returns the input of a given frame of the loaded movie in a raw inputlog string"
		)]
		public string GetInputAsMnemonic(int frame)
		{
			if (frame < Global.MovieSession.Movie.InputLogLength)
			{
				return Global.MovieSession.Movie.GetInput(frame);
			}

			return string.Empty;
		}

		[LuaMethodAttributes(
			"getreadonly",
			"Returns true if the movie is in read-only mode, false if in read+write"
		)]
		public static bool GetReadOnly()
		{
			return Global.MovieSession.ReadOnly;
		}

		[LuaMethodAttributes(
			"getrerecordcounting",
			"Returns whether or not the current movie is incrementing rerecords on loadstate"
		)]
		public static bool GetRerecordCounting()
		{
			return Global.MovieSession.Movie.IsCountingRerecords;
		}

		[LuaMethodAttributes(
			"isloaded",
			"Returns true if a movie is loaded in memory (play, record, or finished modes), false if not (inactive mode)"
		)]
		public static bool IsLoaded()
		{
			return Global.MovieSession.Movie.IsActive;
		}

		[LuaMethodAttributes(
			"length",
			"Returns the total number of frames of the loaded movie"
		)]
		public static double Length()
		{
			return Global.MovieSession.Movie.FrameCount;
		}

		[LuaMethodAttributes(
			"mode",
			"Returns the mode of the current movie. Possible modes: PLAY, RECORD, FINISHED, INACTIVE"
		)]
		public static string Mode()
		{
			if (Global.MovieSession.Movie.IsFinished)
			{
				return "FINISHED";
			}
			
			if (Global.MovieSession.Movie.IsPlaying)
			{
				return "PLAY";
			}
			
			if (Global.MovieSession.Movie.IsRecording)
			{
				return "RECORD";
			}
			
			return "INACTIVE";
		}

		[LuaMethodAttributes(
			"rerecordcount",
			"Returns the current rerecord count for the loaded movie"
		)]
		public static string RerecordCount()
		{
			return Global.MovieSession.Movie.Header.Rerecords.ToString();
		}

		[LuaMethodAttributes(
			"setreadonly",
			"Sets the read-only state to the given value. true for read only, false for read+write"
		)]
		public static void SetReadOnly(bool readOnly)
		{
			Global.MovieSession.ReadOnly = readOnly;
		}

		[LuaMethodAttributes(
			"setrerecordcounting",
			"Sets whether or not the current movie will increment the rerecord counter on loadstate"
		)]
		public static void SetRerecordCounting(bool counting)
		{
			Global.MovieSession.Movie.IsCountingRerecords = counting;
		}

		[LuaMethodAttributes(
			"stop",
			"Stops the current movie"
		)]
		public static void Stop()
		{
			Global.MovieSession.Movie.Stop();
		}

		[LuaMethodAttributes(
			"getfps",
			"If a movie is loaded, gets the frames per second used by the movie to determine the movie length time"
		)]
		public static double GetFps()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				return Global.MovieSession.Movie.Fps;
			}

			return 0.0;
		}
	}
}
