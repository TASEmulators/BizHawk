using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Client.Common;

namespace BizHawk.Client.ApiHawk
{
	public sealed class MovieApi : IMovie
	{
		private static class MoviePluginStatic
		{
			public static string Filename()
			{
				return Global.MovieSession.Movie.Filename;
			}

			public static bool GetReadOnly()
			{
				return Global.MovieSession.ReadOnly;
			}

			public static ulong GetRerecordCount()
			{
				return Global.MovieSession.Movie.Rerecords;
			}

			public static bool GetRerecordCounting()
			{
				return Global.MovieSession.Movie.IsCountingRerecords;
			}

			public static bool IsLoaded()
			{
				return Global.MovieSession.Movie.IsActive;
			}

			public static double Length()
			{
				return Global.MovieSession.Movie.FrameCount;
			}

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

			public static void SetReadOnly(bool readOnly)
			{
				Global.MovieSession.ReadOnly = readOnly;
			}

			public static void SetRerecordCount(double count)
			{
				// Lua numbers are always double, integer precision holds up
				// to 53 bits, so throw an error if it's bigger than that.
				const double PrecisionLimit = 9007199254740992d;

				if (count > PrecisionLimit)
				{
					throw new Exception("Rerecord count exceeds Lua integer precision.");
				}

				Global.MovieSession.Movie.Rerecords = (ulong)count;
			}

			public static void SetRerecordCounting(bool counting)
			{
				Global.MovieSession.Movie.IsCountingRerecords = counting;
			}

			public static void Stop()
			{
				Global.MovieSession.Movie.Stop();
			}

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
		public MovieApi()
		{ }

		public bool StartsFromSavestate()
		{
			return Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.StartsFromSavestate;
		}

		public bool StartsFromSaveram()
		{
			return Global.MovieSession.Movie.IsActive && Global.MovieSession.Movie.StartsFromSaveRam;
		}

		public Dictionary<string, dynamic> GetInput(int frame)
		{
			if (!Global.MovieSession.Movie.IsActive)
			{
				Console.WriteLine("No movie loaded");
				return null;
			}

			var input = new Dictionary<string, dynamic>();
			var adapter = Global.MovieSession.Movie.GetInputState(frame);

			if (adapter == null)
			{
				Console.WriteLine("Can't get input of the last frame of the movie. Use the previous frame");
				return null;
			}

			foreach (var button in adapter.Definition.BoolButtons)
			{
				input[button] = adapter.IsPressed(button);
			}

			foreach (var button in adapter.Definition.FloatControls)
			{
				input[button] = adapter.GetFloat(button);
			}

			return input;
		}

		public string GetInputAsMnemonic(int frame)
		{
			if (Global.MovieSession.Movie.IsActive && frame < Global.MovieSession.Movie.InputLogLength)
			{
				var lg = Global.MovieSession.LogGeneratorInstance();
				lg.SetSource(Global.MovieSession.Movie.GetInputState(frame));
				return lg.GenerateLogEntry();
			}

			return "";
		}

		public void Save(string filename = "")
		{
			if (!Global.MovieSession.Movie.IsActive)
			{
				return;
			}

			if (!string.IsNullOrEmpty(filename))
			{
				filename += $".{Global.MovieSession.Movie.PreferredExtension}";
				var test = new FileInfo(filename);
				if (test.Exists)
				{
					Console.WriteLine($"File {filename} already exists, will not overwrite");
					return;
				}

				Global.MovieSession.Movie.Filename = filename;
			}

			Global.MovieSession.Movie.Save();
		}

		public Dictionary<string,string> GetHeader()
		{
			var table = new Dictionary<string,string>();
			if (Global.MovieSession.Movie.IsActive)
			{
				foreach (var kvp in Global.MovieSession.Movie.HeaderEntries)
				{
					table[kvp.Key] = kvp.Value;
				}
			}

			return table;
		}

		public List<string> GetComments()
		{
			var list = new List<string>(Global.MovieSession.Movie.Comments.Count);
			if (Global.MovieSession.Movie.IsActive)
			{
				for (int i = 0; i < Global.MovieSession.Movie.Comments.Count; i++)
				{
					list[i] = Global.MovieSession.Movie.Comments[i];
				}
			}

			return list;
		}

		public List<string> GetSubtitles()
		{
			var list = new List<string>(Global.MovieSession.Movie.Subtitles.Count);
			if (Global.MovieSession.Movie.IsActive)
			{
				for (int i = 0; i < Global.MovieSession.Movie.Subtitles.Count; i++)
				{
					list[i] = Global.MovieSession.Movie.Subtitles[i].ToString();
				}
			}

			return list;
		}

		public string Filename()
		{
			return MoviePluginStatic.Filename();
		}

		public bool GetReadOnly()
		{
			return MoviePluginStatic.GetReadOnly();
		}

		public ulong GetRerecordCount()
		{
			return MoviePluginStatic.GetRerecordCount();
		}

		public bool GetRerecordCounting()
		{
			return MoviePluginStatic.GetRerecordCounting();
		}

		public bool IsLoaded()
		{
			return MoviePluginStatic.IsLoaded();
		}

		public double Length()
		{
			return MoviePluginStatic.Length();
		}

		public string Mode()
		{
			return MoviePluginStatic.Mode();
		}

		public void SetReadOnly(bool readOnly)
		{
			MoviePluginStatic.SetReadOnly(readOnly);
		}

		public void SetRerecordCount(double count)
		{
			MoviePluginStatic.SetRerecordCount(count);
		}

		public void SetRerecordCounting(bool counting)
		{
			MoviePluginStatic.SetRerecordCounting(counting);
		}

		public void Stop()
		{
			MoviePluginStatic.Stop();
		}

		public double GetFps()
		{
			return MoviePluginStatic.GetFps();
		}
	}
}
