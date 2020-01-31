using System;

using NLua;

// ReSharper disable UnusedMember.Global
namespace BizHawk.Client.Common
{
	public sealed class MovieLuaLibrary : DelegatingLuaLibrary
	{
		public MovieLuaLibrary(Lua lua)
			: base(lua) { }

		public MovieLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "movie";

		[LuaMethodExample("if ( movie.startsfromsavestate( ) ) then\r\n\tconsole.log( \"Returns whether or not the movie is a savestate-anchored movie\" );\r\nend;")]
		[LuaMethod("startsfromsavestate", "Returns whether or not the movie is a savestate-anchored movie")]
		public bool StartsFromSavestate() => APIs.InputMovie.StartsFromSavestate;

		[LuaMethodExample("if ( movie.startsfromsaveram( ) ) then\r\n\tconsole.log( \"Returns whether or not the movie is a saveram-anchored movie\" );\r\nend;")]
		[LuaMethod("startsfromsaveram", "Returns whether or not the movie is a saveram-anchored movie")]
		public bool StartsFromSaveram() => APIs.InputMovie.StartsFromSaveram;

		[LuaMethodExample("local stmovfil = movie.filename( );")]
		[LuaMethod("filename", "Returns the file name including path of the currently loaded movie")]
		public string Filename() => APIs.InputMovie.Filename;

		[LuaMethodExample("local nlmovget = movie.getinput( 500 );")]
		[LuaMethod("getinput", "Returns a table of buttons pressed on a given frame of the loaded movie")]
		public LuaTable GetInput(int frame, int? controller = null) => APIs.InputMovie.GetInput(frame, controller).ToLuaTable(Lua);

		[LuaMethodExample("local stmovget = movie.getinputasmnemonic( 500 );")]
		[LuaMethod("getinputasmnemonic", "Returns the input of a given frame of the loaded movie in a raw inputlog string")]
		public string GetInputAsMnemonic(int frame) => APIs.InputMovie.GetInputAsMnemonic(frame);

		[LuaMethodExample("if ( movie.getreadonly( ) ) then\r\n\tconsole.log( \"Returns true if the movie is in read-only mode, false if in read+write\" );\r\nend;")]
		[LuaMethod("getreadonly", "Returns true if the movie is in read-only mode, false if in read+write")]
		public bool GetReadOnly() => APIs.InputMovie.IsReadOnly;

		[LuaMethodExample("local ulmovget = movie.getrerecordcount();")]
		[LuaMethod("getrerecordcount", "Gets the rerecord count of the current movie.")]
		public ulong GetRerecordCount() => APIs.InputMovie.RerecordCount;

		[LuaMethodExample("if ( movie.getrerecordcounting( ) ) then\r\n\tconsole.log( \"Returns whether or not the current movie is incrementing rerecords on loadstate\" );\r\nend;")]
		[LuaMethod("getrerecordcounting", "Returns whether or not the current movie is incrementing rerecords on loadstate")]
		public bool GetRerecordCounting() => APIs.InputMovie.IsRerecordCounting;

		[LuaMethodExample("if ( movie.isloaded( ) ) then\r\n\tconsole.log( \"Returns true if a movie is loaded in memory ( play, record, or finished modes ), false if not ( inactive mode )\" );\r\nend;")]
		[LuaMethod("isloaded", "Returns true if a movie is loaded in memory (play, record, or finished modes), false if not (inactive mode)")]
		public bool IsLoaded() => APIs.InputMovie.IsLoaded;

		[LuaMethodExample("local domovlen = movie.length( );")]
		[LuaMethod("length", "Returns the total number of frames of the loaded movie")]
		public double Length() => APIs.InputMovie.Length;

		[LuaMethodExample("local stmovmod = movie.mode( );")]
		[LuaMethod("mode", "Returns the mode of the current movie. Possible modes: \"PLAY\", \"RECORD\", \"FINISHED\", \"INACTIVE\"")]
		public string Mode() => APIs.InputMovie.Mode;

		[LuaMethodExample("movie.save( \"C:\\moviename.ext\" );")]
		[LuaMethod("save", "Saves the current movie to the disc. If the filename is provided (no extension or path needed), the movie is saved under the specified name to the current movie directory. The filename may contain a subdirectory, it will be created if it doesn't exist. Existing files won't get overwritten.")]
		public void Save(string filename = "") => APIs.InputMovie.Save(filename);

		[LuaMethodExample("movie.setreadonly( false );")]
		[LuaMethod("setreadonly", "Sets the read-only state to the given value. true for read only, false for read+write")]
		public void SetReadOnly(bool readOnly) => APIs.InputMovie.IsReadOnly = readOnly;

		[LuaMethodExample("movie.setrerecordcount( 20.0 );")]
		[LuaMethod("setrerecordcount", "Sets the rerecord count of the current movie.")]
		public void SetRerecordCount(double count) => APIs.InputMovie.RerecordCount = (ulong) count.AsInteger();

		[LuaMethodExample("movie.setrerecordcounting( true );")]
		[LuaMethod("setrerecordcounting", "Sets whether or not the current movie will increment the rerecord counter on loadstate")]
		public void SetRerecordCounting(bool counting) => APIs.InputMovie.IsRerecordCounting = counting;

		[LuaMethodExample("movie.stop( );")]
		[LuaMethod("stop", "Stops the current movie")]
		public void Stop() => APIs.InputMovie.Stop();

		[LuaMethodExample("local domovget = movie.getfps( );")]
		[LuaMethod("getfps", "If a movie is loaded, gets the frames per second used by the movie to determine the movie length time")]
		public double GetFps() => APIs.InputMovie.FramesPerSecond;

		[LuaMethodExample("local nlmovget = movie.getheader( );")]
		[LuaMethod("getheader", "If a movie is active, will return the movie header as a lua table")]
		public LuaTable GetHeader() => APIs.InputMovie.Header.ToLuaTable(Lua);

		[LuaMethodExample("local nlmovget = movie.getcomments( );")]
		[LuaMethod("getcomments", "If a movie is active, will return the movie comments as a lua table")]
		public LuaTable GetComments() => APIs.InputMovie.Comments.ToLuaTable(Lua);

		[LuaMethodExample("local nlmovget = movie.getsubtitles( );")]
		[LuaMethod("getsubtitles", "If a movie is active, will return the movie subtitles as a lua table")]
		public LuaTable GetSubtitles() => APIs.InputMovie.Subtitles.ToLuaTable(Lua);
	}
}
