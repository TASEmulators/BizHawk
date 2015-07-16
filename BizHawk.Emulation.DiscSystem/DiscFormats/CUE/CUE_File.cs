using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem.CUE
{
	/// <summary>
	/// Represents the contents of a cue file
	/// </summary>
	class CUE_File
	{
		// (here are all the commands we can encounter)
		public static class Command
		{
			//TODO - record line number origin of command? Kind of nice but inessential
			public class CATALOG { public string Value; public override string ToString() { return string.Format("CATALOG: {0}", Value); } }
			public class CDTEXTFILE { public string Path; public override string ToString() { return string.Format("CDTEXTFILE: {0}", Path); } }
			public class FILE { public string Path; public CueFileType Type; public override string ToString() { return string.Format("FILE ({0}): {1}", Type, Path); } }
			public class FLAGS { public CueTrackFlags Flags; public override string ToString() { return string.Format("FLAGS {0}", Flags); } }
			public class INDEX { public int Number; public Timestamp Timestamp; public override string ToString() { return string.Format("INDEX {0,2} {1}", Number, Timestamp); } }
			public class ISRC { public string Value; public override string ToString() { return string.Format("ISRC: {0}", Value); } }
			public class PERFORMER { public string Value; public override string ToString() { return string.Format("PERFORMER: {0}", Value); } }
			public class POSTGAP { public Timestamp Length; public override string ToString() { return string.Format("POSTGAP: {0}", Length); } }
			public class PREGAP { public Timestamp Length; public override string ToString() { return string.Format("PREGAP: {0}", Length); } }
			public class REM { public string Value; public override string ToString() { return string.Format("REM: {0}", Value); } }
			public class COMMENT { public string Value; public override string ToString() { return string.Format("COMMENT: {0}", Value); } }
			public class SONGWRITER { public string Value; public override string ToString() { return string.Format("SONGWRITER: {0}", Value); } }
			public class TITLE { public string Value; public override string ToString() { return string.Format("TITLE: {0}", Value); } }
			public class TRACK { public int Number; public CueTrackType Type; public override string ToString() { return string.Format("TRACK {0,2} ({1})", Number, Type); } }
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