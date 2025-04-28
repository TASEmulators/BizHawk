using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem.CUE
{
	/// <summary>
	/// Represents the contents of a cue file
	/// </summary>
	internal class CUE_File
	{
		/// <remarks>
		/// (here are all the commands we can encounter)
		/// TODO record line number origin of command? Kind of nice but unessential
		/// </remarks>
		public interface Command
		{
			public readonly struct CATALOG : Command
			{
				public readonly string Value;

				public CATALOG(string value) => Value = value;

				public readonly override string ToString() => $"CATALOG: {Value}";
			}

			public readonly struct CDTEXTFILE : Command
			{
				public readonly string Path;

				public CDTEXTFILE(string path) => Path = path;

				public override string ToString() => $"CDTEXTFILE: {Path}";
			}

			public readonly struct FILE : Command
			{
				public readonly string Path;

				public readonly CueFileType Type;

				public FILE(string path, CueFileType type)
				{
					Path = path;
					Type = type;
				}

				public override string ToString() => $"FILE ({Type}): {Path}";
			}

			public readonly struct FLAGS : Command
			{
				public readonly CueTrackFlags Flags;

				public FLAGS(CueTrackFlags flags) => Flags = flags;

				public override string ToString() => $"FLAGS {Flags}";
			}

			public readonly struct INDEX : Command
			{
				public readonly int Number;

				public readonly Timestamp Timestamp;

				public INDEX(int number, Timestamp timestamp)
				{
					Number = number;
					Timestamp = timestamp;
				}

				public override string ToString() => $"INDEX {Number,2} {Timestamp}";
			}

			public readonly struct ISRC : Command
			{
				public readonly string Value;

				public ISRC(string value) => Value = value;

				public override string ToString() => $"ISRC: {Value}";
			}

			public readonly struct PERFORMER : Command
			{
				public readonly string Value;

				public PERFORMER(string value) => Value = value;

				public override string ToString() => $"PERFORMER: {Value}";
			}

			public readonly struct POSTGAP : Command
			{
				public readonly Timestamp Length;

				public POSTGAP(Timestamp length) => Length = length;

				public override string ToString() => $"POSTGAP: {Length}";
			}

			public readonly struct PREGAP : Command
			{
				public readonly Timestamp Length;

				public PREGAP(Timestamp length) => Length = length;

				public override string ToString() => $"PREGAP: {Length}";
			}

			public readonly struct REM : Command
			{
				public readonly string Value;

				public REM(string value) => Value = value;

				public override string ToString() => $"REM: {Value}";
			}

			public readonly struct COMMENT : Command
			{
				public readonly string Value;

				public COMMENT(string value) => Value = value;

				public override string ToString() => $"COMMENT: {Value}";
			}

			public readonly struct SONGWRITER : Command
			{
				public readonly string Value;

				public SONGWRITER(string value) => Value = value;

				public override string ToString() => $"SONGWRITER: {Value}";
			}

			public readonly struct TITLE : Command
			{
				public readonly string Value;

				public TITLE(string value) => Value = value;

				public override string ToString() => $"TITLE: {Value}";
			}

			public readonly struct TRACK : Command
			{
				public readonly int Number;

				public readonly CueTrackType Type;

				public TRACK(int number, CueTrackType type)
				{
					Number = number;
					Type = type;
				}

				public override string ToString() => $"TRACK {Number,2} ({Type})";
			}

			// This doesn't exist officially, rather it is derived from special REM comments
			// Consider this an "extension" perhaps?
			public readonly struct SESSION : Command
			{
				public readonly int Number;

				public SESSION(int number) => Number = number;

				public override string ToString() => $"SESSION {Number}";
			}
		}

		/// <summary>
		/// Stuff other than the commands, global for the whole disc
		/// </summary>
		public class DiscInfo
		{
			public Command.CATALOG? Catalog;
			public Command.ISRC? ISRC;
			public Command.CDTEXTFILE? CDTextFile;
		}

		/// <summary>
		/// The sequential list of commands parsed out of the cue file
		/// </summary>
		public readonly List<Command> Commands = new();

		/// <summary>
		/// Stuff other than the commands, global for the whole disc
		/// </summary>
		public readonly DiscInfo GlobalDiscInfo = new();
	}
}