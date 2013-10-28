using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	public partial class EmuLuaLibrary
	{
		public string movie_filename()
		{
			return Global.MovieSession.Movie.Filename;
		}

		public LuaTable movie_getinput(object frame)
		{
			LuaTable input = _lua.NewTable();

			string s = Global.MovieSession.Movie.GetInput(LuaInt(frame));
			MovieControllerAdapter m = new MovieControllerAdapter { Type = Global.MovieSession.MovieControllerAdapter.Type };
			m.SetControllersAsMnemonic(s);
			foreach (string button in m.Type.BoolButtons)
				input[button] = m[button];

			return input;
		}

		public bool movie_getreadonly()
		{
			return GlobalWinF.MainForm.ReadOnly;
		}

		public bool movie_getrerecordcounting()
		{
			return Global.MovieSession.Movie.IsCountingRerecords;
		}

		public bool movie_isloaded()
		{
			if (Global.MovieSession.Movie.IsActive)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public int movie_length()
		{
			if (Global.MovieSession.Movie.Frames.HasValue)
			{
				return Global.MovieSession.Movie.Frames.Value;
			}
			else
			{
				return -1;
			}
		}

		public string movie_mode()
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

		public string movie_rerecordcount()
		{
			return Global.MovieSession.Movie.Rerecords.ToString();
		}

		public void movie_setreadonly(object lua_input)
		{
			if (lua_input.ToString().ToUpper() == "TRUE" || lua_input.ToString() == "1")
				GlobalWinF.MainForm.SetReadOnly(true);
			else
				GlobalWinF.MainForm.SetReadOnly(false);
		}

		public void movie_setrerecordcounting(object lua_input)
		{
			if (lua_input.ToString().ToUpper() == "TRUE" || lua_input.ToString() == "1")
				Global.MovieSession.Movie.IsCountingRerecords = true;
			else
				Global.MovieSession.Movie.IsCountingRerecords = false;
		}

		public void movie_stop()
		{
			Global.MovieSession.Movie.Stop();
		}
	}
}
