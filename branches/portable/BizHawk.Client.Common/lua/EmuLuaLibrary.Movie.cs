using System;
using LuaInterface;

namespace BizHawk.Client.Common
{
	public sealed class MovieLuaLibrary : LuaLibraryBase
	{
		public MovieLuaLibrary(Lua lua)
			: base(lua) { }

		public MovieLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }


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
			var input = Lua.NewTable();
			var adapter = Global.MovieSession.Movie.GetInputState(frame);

			foreach (var button in adapter.Type.BoolButtons)
			{
				input[button] = adapter[button];
			}

			foreach (var button in adapter.Type.FloatControls)
			{
				input[button] = adapter[button];
			}

			return input;
		}

		[LuaMethodAttributes(
			"getinputasmnemonic",
			"Returns the input of a given frame of the loaded movie in a raw inputlog string"
		)]
		public string GetInputAsMnemonic(int frame)
		{
			if (Global.MovieSession.Movie.IsActive && frame < Global.MovieSession.Movie.InputLogLength)
			{
				var lg = Global.MovieSession.LogGeneratorInstance();
				lg.SetSource(Global.MovieSession.Movie.GetInputState(frame));
				return lg.GenerateLogEntry();
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
			"getrerecordcount",
			"Gets the rerecord count of the current movie."
		)]
		public static ulong GetRerecordCount()
		{
			return Global.MovieSession.Movie.Rerecords;
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
			"[Deprecated] Alias of getrerecordcount"
		)]
		public static string RerecordCount()
		{
			return GetRerecordCount().ToString();
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
			"setrerecordcount",
			"Sets the rerecord count of the current movie."
		)]
		public static void SetRerecordCount(double count)
		{
			// Lua numbers are always double, integer precision holds up
			// to 53 bits, so throw an error if it's bigger than that.
			const double precisionLimit = 9007199254740992d;

			if (count > precisionLimit)
				throw new Exception("Rerecord count exceeds Lua integer precision.");

			Global.MovieSession.Movie.Rerecords = (ulong)count;
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
				var movie = Global.MovieSession.Movie;
				var system = movie.HeaderEntries[HeaderKeys.PLATFORM];
				var pal = movie.HeaderEntries.ContainsKey(HeaderKeys.PAL) &&
						movie.HeaderEntries[HeaderKeys.PAL] == "1";

				return new PlatformFrameRates()[system, pal];
			}

			return 0.0;
		}
	}
}
