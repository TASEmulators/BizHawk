using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem.CUE
{
	/// <summary>
	/// Represents the contents of a cue file
	/// </summary>
	internal class CUE_File
	{
		// (here are all the commands we can encounter)
		public static class Command
		{
			//TODO - record line number origin of command? Kind of nice but inessential
			public class CATALOG { public string Value; public override string ToString() { return $"CATALOG: {Value}"; } }
			public class CDTEXTFILE { public string Path; public override string ToString() { return $"CDTEXTFILE: {Path}"; } }
			public class FILE { public string Path; public CueFileType Type; public override string ToString() { return $"FILE ({Type}): {Path}"; } }
			public class FLAGS { public CueTrackFlags Flags; public override string ToString() { return $"FLAGS {Flags}"; } }
			public class INDEX { public int Number; public Timestamp Timestamp; public override string ToString() { return $"INDEX {Number,2} {Timestamp}"; } }
			public class ISRC { public string Value; public override string ToString() { return $"ISRC: {Value}"; } }
			public class PERFORMER { public string Value; public override string ToString() { return $"PERFORMER: {Value}"; } }
			public class POSTGAP { public Timestamp Length; public override string ToString() { return $"POSTGAP: {Length}"; } }
			public class PREGAP { public Timestamp Length; public override string ToString() { return $"PREGAP: {Length}"; } }
			public class REM { public string Value; public override string ToString() { return $"REM: {Value}"; } }
			public class COMMENT { public string Value; public override string ToString() { return $"COMMENT: {Value}"; } }
			public class SONGWRITER { public string Value; public override string ToString() { return $"SONGWRITER: {Value}"; } }
			public class TITLE { public string Value; public override string ToString() { return $"TITLE: {Value}"; } }
			public class TRACK { public int Number; public CueTrackType Type; public override string ToString() { return $"TRACK {Number,2} ({Type})"; } }
		}


		/// <summary>
		/// Stuff other than the commands, global for the whole disc
		/// </summary>
		public class DiscInfo
		{
			public Command.CATALOG Catalog;
			public Command.ISRC ISRC;
			public Command.CDTEXTFILE CDTextFile;
		}

		/// <summary>
		/// The sequential list of commands parsed out of the cue file
		/// </summary>
		public List<object> Commands = new List<object>();

		/// <summary>
		/// Stuff other than the commands, global for the whole disc
		/// </summary>
		public DiscInfo GlobalDiscInfo = new DiscInfo();
	}
}