//TODO - object initialization syntax cleanup

using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

//http://digitalx.org/cue-sheet/index.html "all cue sheet information is a straight 1:1 copy from the cdrwin helpfile"
//http://www.gnu.org/software/libcdio/libcdio.html#Sectors
//this is actually a great reference. they use LSN instead of LBA.. maybe a good idea for us

namespace BizHawk.Emulation.DiscSystem
{
	partial class CUE_Context
	{
		public class ParseCueJob : LoggedJob
		{
			/// <summary>
			/// input: the cue string to parse
			/// </summary>
			public string IN_CueString;

			/// <summary>
			/// output: the resulting minimally-processed cue file
			/// </summary>
			public CueFile OUT_CueFile;
		}

		/// <summary>
		/// Represents the contents of a cue file
		/// </summary>
		public class CueFile
		{

			// (here are all the commands we can encounter)
			public static class Command
			{
				//TODO - record line number origin of command? Kind of nice but inessential
				public class CATALOG { public string Value; public override string ToString() { return string.Format("CATALOG: {0}", Value); } }
				public class CDTEXTFILE { public string Path; public override string ToString() { return string.Format("CDTEXTFILE: {0}", Path); } }
				public class FILE { public string Path; public FileType Type; public override string ToString() { return string.Format("FILE ({0}): {1}", Type, Path); } }
				public class FLAGS { public TrackFlags Flags; public override string ToString() { return string.Format("FLAGS {0}", Flags); } }
				public class INDEX { public int Number; public Timestamp Timestamp; public override string ToString() { return string.Format("INDEX {0,2} {1}", Number, Timestamp); } }
				public class ISRC { public string Value; public override string ToString() { return string.Format("ISRC: {0}", Value); } }
				public class PERFORMER { public string Value; public override string ToString() { return string.Format("PERFORMER: {0}", Value); } }
				public class POSTGAP { public Timestamp Length; public override string ToString() { return string.Format("POSTGAP: {0}", Length); } }
				public class PREGAP { public Timestamp Length; public override string ToString() { return string.Format("PREGAP: {0}", Length); } }
				public class REM { public string Value; public override string ToString() { return string.Format("REM: {0}", Value); } }
				public class COMMENT { public string Value; public override string ToString() { return string.Format("COMMENT: {0}", Value); } }
				public class SONGWRITER { public string Value; public override string ToString() { return string.Format("SONGWRITER: {0}", Value); } }
				public class TITLE { public string Value; public override string ToString() { return string.Format("TITLE: {0}", Value); } }
				public class TRACK { public int Number; public TrackType Type; public override string ToString() { return string.Format("TRACK {0,2} ({1})", Number, Type); } }
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

			[Flags]
			public enum TrackFlags
			{
				None = 0,
				PRE = 1, //Pre-emphasis enabled (audio tracks only)
				DCP = 2, //Digital copy permitted
				DATA = 4, //Set automatically by cue-processing equipment, here for completeness
				_4CH = 8, //Four channel audio
				SCMS = 64, //Serial copy management system (not supported by all recorders) (??)
			}

			//All audio files (WAVE, AIFF, and MP3) must be in 44.1KHz 16-bit stereo format.
			//BUT NOTE: MP3 can be VBR and the length can't be known without decoding the whole thing. 
			//But, some ideas: 
			//1. we could operate ffmpeg differently to retrieve the length, which maybe it can do without having to decode the entire thing
			//2. we could retrieve it from an ID3 if present.
			//3. as a last resort, since MP3 is the annoying case usually, we could include my c# mp3 parser and sum the length (test the performance, this might be reasonably fast on par with ECM parsing)
			//NOTE: once deciding the length, we would have to stick with it! samples would have to be discarded or inserted to make the track work out
			//but we COULD effectively achieve stream-loading mp3 discs, with enough work.
			public enum FileType
			{
				Unspecified,
				BINARY, //Intel binary file (least significant byte first)
				MOTOROLA, //Motorola binary file (most significant byte first)
				AIFF, //Audio AIFF file
				WAVE, //Audio WAVE file
				MP3, //Audio MP3 file
			}

			public enum TrackType
			{
				Unknown,
				Audio, //Audio/Music (2352)
				CDG, //Karaoke CD+G (2448)
				Mode1_2048, //CDROM Mode1 Data (cooked)
				Mode1_2352, //CDROM Mode1 Data (raw)
				Mode2_2336, //CDROM-XA Mode2 Data (could contain form 1 or form 2)
				Mode2_2352, //CDROM-XA Mode2 Data (but there's no reason to distinguish this from Mode1_2352 other than to alert us that the entire session should be XA
				CDI_2336, //CDI Mode2 Data
				CDI_2352 //CDI Mode2 Data
			}

			class CueLineParser
			{
				int index;
				string str;
				public bool EOF;
				public CueLineParser(string line)
				{
					str = line;
				}

				public string ReadPath() { return ReadToken(Mode.Quotable); }
				public string ReadToken() { return ReadToken(Mode.Normal); }
				public string ReadLine()
				{
					int len = str.Length;
					string ret = str.Substring(index, len - index);
					index = len;
					EOF = true;
					return ret;
				}

				enum Mode
				{
					Normal, Quotable
				}

				string ReadToken(Mode mode)
				{
					if (EOF) return null;

					bool isPath = mode == Mode.Quotable;

					int startIndex = index;
					bool inToken = false;
					bool inQuote = false;
					for (; ; )
					{
						bool done = false;
						char c = str[index];
						bool isWhiteSpace = (c == ' ' || c == '\t');

						if (isWhiteSpace)
						{
							if (inQuote)
								index++;
							else
							{
								if (inToken)
									done = true;
								else
									index++;
							}
						}
						else
						{
							bool startedQuote = false;
							if (!inToken)
							{
								startIndex = index;
								if (isPath && c == '"')
									startedQuote = inQuote = true;
								inToken = true;
							}
							switch (str[index])
							{
								case '"':
									index++;
									if (inQuote && !startedQuote)
									{
										done = true;
									}
									break;
								case '\\':
									index++;
									break;

								default:
									index++;
									break;
							}
						}
						if (index == str.Length)
						{
							EOF = true;
							done = true;
						}
						if (done) break;
					}

					string ret = str.Substring(startIndex, index - startIndex);

					if (mode == Mode.Quotable)
						ret = ret.Trim('"');

					return ret;
				}
			}

			internal void LoadFromString(ParseCueJob job)
			{
				string cueString = job.IN_CueString;
				TextReader tr = new StringReader(cueString);

				for (; ; )
				{
					job.CurrentLine++;
					string line = tr.ReadLine();
					if (line == null) break;
					line = line.Trim();
					if (line == "") continue;
					var clp = new CueLineParser(line);

					string key = clp.ReadToken().ToUpperInvariant();
					if (key.StartsWith(";"))
					{
						clp.EOF = true;
						Commands.Add(new Command.COMMENT() { Value = line });
					}
					else switch (key)
					{
						default:
							job.Warn("Unknown command: " + key);
							break;

						case "CATALOG":
							if (GlobalDiscInfo.Catalog != null)
								job.Warn("Multiple CATALOG commands detected. Subsequent ones are ignored.");
							else if (clp.EOF)
								job.Warn("Ignoring empty CATALOG command");
							else Commands.Add(GlobalDiscInfo.Catalog = new Command.CATALOG() { Value = clp.ReadToken() });
							break;

						case "CDTEXTFILE":
							if (GlobalDiscInfo.CDTextFile != null)
								job.Warn("Multiple CDTEXTFILE commands detected. Subsequent ones are ignored.");
							else if (clp.EOF)
								job.Warn("Ignoring empty CDTEXTFILE command");
							else Commands.Add(GlobalDiscInfo.CDTextFile = new Command.CDTEXTFILE() { Path = clp.ReadPath() });
							break;

						case "FILE":
							{
								var path = clp.ReadPath();
								FileType ft;
								if (clp.EOF)
								{
									job.Error("FILE command is missing file type.");
									ft = FileType.Unspecified;
								}
								else
								{
									var strType = clp.ReadToken().ToUpperInvariant();
									switch (strType)
									{
										default:
											job.Error("Unknown FILE type: " + strType);
											ft = FileType.Unspecified;
											break;
										case "BINARY": ft = FileType.BINARY; break;
										case "MOTOROLA": ft = FileType.MOTOROLA; break;
										case "BINARAIFF": ft = FileType.AIFF; break;
										case "WAVE": ft = FileType.WAVE; break;
										case "MP3": ft = FileType.MP3; break;
									}
								}
								Commands.Add(new Command.FILE() { Path = path, Type = ft });
							}
							break;

						case "FLAGS":
							{
								var cmd = new Command.FLAGS();
								Commands.Add(cmd);
								while (!clp.EOF)
								{
									var flag = clp.ReadToken().ToUpperInvariant();
									switch (flag)
									{
										case "DATA":
										default:
											job.Warn("Unknown FLAG: " + flag);
											break;
										case "DCP": cmd.Flags |= TrackFlags.DCP; break;
										case "4CH": cmd.Flags |= TrackFlags._4CH; break;
										case "PRE": cmd.Flags |= TrackFlags.PRE; break;
										case "SCMS": cmd.Flags |= TrackFlags.SCMS; break;
									}
								}
								if (cmd.Flags == TrackFlags.None)
									job.Warn("Empty FLAG command");
							}
							break;

						case "INDEX":
							{
								if (clp.EOF)
								{
									job.Error("Incomplete INDEX command");
									break;
								}
								string strindexnum = clp.ReadToken();
								int indexnum;
								if (!int.TryParse(strindexnum, out indexnum) || indexnum < 0 || indexnum > 99)
								{
									job.Error("Invalid INDEX number: " + strindexnum);
									break;
								}
								string str_timestamp = clp.ReadToken();
								var ts = new Timestamp(str_timestamp);
								if (!ts.Valid)
								{
									job.Error("Invalid INDEX timestamp: " + str_timestamp);
									break;
								}
								Commands.Add(new Command.INDEX() { Number = indexnum, Timestamp = ts });
							}
							break;

						case "ISRC":
							if (GlobalDiscInfo.ISRC != null)
								job.Warn("Multiple ISRC commands detected. Subsequent ones are ignored.");
							else if (clp.EOF)
								job.Warn("Ignoring empty ISRC command");
							else
							{
								var isrc = clp.ReadToken();
								if (isrc.Length != 12)
									job.Warn("Invalid ISRC code ignored: " + isrc);
								else
								{
									Commands.Add(new Command.ISRC() { Value = isrc });
								}
							}
							break;

						case "PERFORMER":
							Commands.Add(new Command.PERFORMER() { Value = clp.ReadPath() ?? "" });
							break;

						case "POSTGAP":
						case "PREGAP":
							{
								var str_msf = clp.ReadToken();
								var msf = new Timestamp(str_msf);
								if (!msf.Valid)
									job.Error("Ignoring {0} with invalid length MSF: " + str_msf, key);
								else
								{
									if (key == "POSTGAP")
										Commands.Add(new Command.POSTGAP() { Length = msf });
									else
										Commands.Add(new Command.PREGAP() { Length = msf });
								}
							}
							break;

						case "REM":
							Commands.Add(new Command.REM() { Value = clp.ReadLine() });
							break;

						case "SONGWRITER":
							Commands.Add(new Command.SONGWRITER() { Value = clp.ReadPath() ?? "" });
							break;

						case "TITLE":
							Commands.Add(new Command.TITLE() { Value = clp.ReadPath() ?? "" });
							break;

						case "TRACK":
							{
								if (clp.EOF)
								{
									job.Error("Incomplete TRACK command");
									break;
								}
								string str_tracknum = clp.ReadToken();
								int tracknum;
								if (!int.TryParse(str_tracknum, out tracknum) || tracknum < 1 || tracknum > 99)
								{
									job.Error("Invalid TRACK number: " + str_tracknum);
									break;
								}

								//TODO - check sequentiality? maybe as a warning

								TrackType tt;
								var str_trackType = clp.ReadToken();
								switch (str_trackType.ToUpperInvariant())
								{
									default:
										job.Error("Unknown TRACK type: " + str_trackType);
										tt = TrackType.Unknown;
										break;
									case "AUDIO": tt = TrackType.Audio; break;
									case "CDG": tt = TrackType.CDG; break;
									case "MODE1/2048": tt = TrackType.Mode1_2048; break;
									case "MODE1/2352": tt = TrackType.Mode1_2352; break;
									case "MODE2/2336": tt = TrackType.Mode2_2336; break;
									case "MODE2/2352": tt = TrackType.Mode2_2352; break;
									case "CDI/2336": tt = TrackType.CDI_2336; break;
									case "CDI/2352": tt = TrackType.CDI_2352; break;
								}

								Commands.Add(new Command.TRACK() { Number = tracknum, Type = tt });
							}
							break;
					}

					if (!clp.EOF)
					{
						var remainder = clp.ReadLine();
						if (remainder.TrimStart().StartsWith(";"))
						{
							//add a comment
							Commands.Add(new Command.COMMENT() { Value = remainder });
						}
						else job.Warn("Unknown text at end of line after processing command: " + key);
					}

				} //end cue parsing loop

				job.FinishLog();
			} //LoadFromString
		}

		/// <summary>
		/// Performs minimum parse processing on a cue file
		/// </summary>
		public void ParseCueFile(ParseCueJob job)
		{
			job.OUT_CueFile = new CueFile();
			job.OUT_CueFile.LoadFromString(job);
		}

	} //partial class
} //namespace