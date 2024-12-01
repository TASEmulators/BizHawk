//TODO - object initialization syntax cleanup

using System.Text.RegularExpressions;
using System.IO;

using BizHawk.Common.StringExtensions;

//http://digitalx.org/cue-sheet/index.html "all cue sheet information is a straight 1:1 copy from the cdrwin helpfile"
//http://www.gnu.org/software/libcdio/libcdio.html#Sectors
//this is actually a great reference. they use LSN instead of LBA.. maybe a good idea for us

namespace BizHawk.Emulation.DiscSystem.CUE
{
	/// <summary>
	/// Performs minimum parse processing on a cue file
	/// </summary>
	internal class ParseCueJob : DiscJob
	{
		private readonly string IN_CueString;

		private readonly bool IN_Strict;

		/// <param name="cueString">the cue string to parse</param>
		/// <param name="strict">Indicates whether parsing will be strict or lenient</param>
		public ParseCueJob(string cueString, bool strict = false)
		{
			IN_CueString = cueString;
			IN_Strict = strict;
		}

		/// <summary>
		/// output: the resulting minimally-processed cue file
		/// </summary>
		public CUE_File OUT_CueFile { get; private set; }

		private class CueLineParser
		{
			private int index;
			private readonly string str;
			public bool EOF;
			public CueLineParser(string line)
			{
				str = line;
			}

			public string ReadPath() { return ReadToken(Mode.Quotable); }
			public string ReadToken() { return ReadToken(Mode.Normal); }
			public string ReadLine()
			{
				var len = str.Length;
				var ret = str.Substring(index, len - index);
				index = len;
				EOF = true;
				return ret;
			}

			private enum Mode
			{
				Normal, Quotable
			}

			private string ReadToken(Mode mode)
			{
				if (EOF) return null;

				var isPath = mode == Mode.Quotable;

				var startIndex = index;
				var inToken = false;
				var inQuote = false;
				for (; ; )
				{
					var done = false;
					var c = str[index];
					var isWhiteSpace = c is ' ' or '\t';

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
						var startedQuote = false;
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

				var ret = str.Substring(startIndex, index - startIndex);

				if (mode == Mode.Quotable)
					ret = ret.Trim('"');

				return ret;
			}
		}

		private void LoadFromString()
		{
			TextReader tr = new StringReader(IN_CueString);

			while (true)
			{
				CurrentLine++;
				var line = tr.ReadLine()?.Trim();
				if (line is null) break;
				if (line == string.Empty) continue;
				var clp = new CueLineParser(line);

				var key = clp.ReadToken().ToUpperInvariant();
				
				//remove nonsense at beginning
				if (!IN_Strict)
				{
					while (key.Length > 0)
					{
						var c = key[0];
						if(c == ';') break;
						if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) break;
						key = key.Substring(1);
					}
				}

				if (key.StartsWith(';'))
				{
					clp.EOF = true;
					OUT_CueFile.Commands.Add(new CUE_File.Command.COMMENT(line));
				}
				else switch (key)
				{
					default:
						Warn($"Unknown command: {key}");
						break;

					case "CATALOG":
						if (OUT_CueFile.GlobalDiscInfo.Catalog != null)
							Warn("Multiple CATALOG commands detected. Subsequent ones are ignored.");
						else if (clp.EOF)
							Warn("Ignoring empty CATALOG command");
						else OUT_CueFile.Commands.Add(OUT_CueFile.GlobalDiscInfo.Catalog = new CUE_File.Command.CATALOG(clp.ReadToken()));
						break;

					case "CDTEXTFILE":
						if (OUT_CueFile.GlobalDiscInfo.CDTextFile != null)
							Warn("Multiple CDTEXTFILE commands detected. Subsequent ones are ignored.");
						else if (clp.EOF)
							Warn("Ignoring empty CDTEXTFILE command");
						else OUT_CueFile.Commands.Add(OUT_CueFile.GlobalDiscInfo.CDTextFile = new CUE_File.Command.CDTEXTFILE(clp.ReadPath()));
						break;

					case "FILE":
						{
							var path = clp.ReadPath();
							CueFileType ft;
							if (clp.EOF)
							{
								Error("FILE command is missing file type.");
								ft = CueFileType.Unspecified;
							}
							else
							{
								var strType = clp.ReadToken().ToUpperInvariant();
								switch (strType)
								{
									default:
										Error($"Unknown FILE type: {strType}");
										ft = CueFileType.Unspecified;
										break;
									case "BINARY": ft = CueFileType.BINARY; break;
									case "MOTOROLA": ft = CueFileType.MOTOROLA; break;
									case "BINARAIFF": ft = CueFileType.AIFF; break;
									case "WAVE": ft = CueFileType.WAVE; break;
									case "MP3": ft = CueFileType.MP3; break;
								}
							}
							OUT_CueFile.Commands.Add(new CUE_File.Command.FILE(path, ft));
						}
						break;

					case "FLAGS":
						{
							CueTrackFlags flags = default;
							while (!clp.EOF)
							{
								var flag = clp.ReadToken().ToUpperInvariant();
								switch (flag)
								{
									case "DATA":
									default:
										Warn($"Unknown FLAG: {flag}");
										break;
									case "DCP": flags |= CueTrackFlags.DCP; break;
									case "4CH": flags |= CueTrackFlags._4CH; break;
									case "PRE": flags |= CueTrackFlags.PRE; break;
									case "SCMS": flags |= CueTrackFlags.SCMS; break;
								}
							}
							if (flags == CueTrackFlags.None)
								Warn("Empty FLAG command");
							OUT_CueFile.Commands.Add(new CUE_File.Command.FLAGS(flags));
						}
						break;

					case "INDEX":
						{
							if (clp.EOF)
							{
								Error("Incomplete INDEX command");
								break;
							}
							var strindexnum = clp.ReadToken();
							if (!int.TryParse(strindexnum, out var indexnum) || indexnum < 0 || indexnum > 99)
							{
								Error($"Invalid INDEX number: {strindexnum}");
								break;
							}
							var str_timestamp = clp.ReadToken();
							var ts = new Timestamp(str_timestamp);
							if (!ts.Valid && !IN_Strict)
							{
								//try cleaning it up
								str_timestamp = Regex.Replace(str_timestamp, "[^0-9:]", "");
								ts = new Timestamp(str_timestamp);
							}
							if (!ts.Valid)
							{
								if (IN_Strict)
									Error($"Invalid INDEX timestamp: {str_timestamp}");
								break;
							}
							OUT_CueFile.Commands.Add(new CUE_File.Command.INDEX(indexnum, ts));
						}
						break;

					case "ISRC":
						if (OUT_CueFile.GlobalDiscInfo.ISRC != null)
							Warn("Multiple ISRC commands detected. Subsequent ones are ignored.");
						else if (clp.EOF)
							Warn("Ignoring empty ISRC command");
						else
						{
							var isrc = clp.ReadToken();
							if (isrc.Length != 12)
								Warn($"Invalid ISRC code ignored: {isrc}");
							else
							{
								OUT_CueFile.Commands.Add(OUT_CueFile.GlobalDiscInfo.ISRC = new CUE_File.Command.ISRC(isrc));
							}
						}
						break;

					case "PERFORMER":
						OUT_CueFile.Commands.Add(new CUE_File.Command.PERFORMER(clp.ReadPath() ?? ""));
						break;

					case "POSTGAP":
					case "PREGAP":
						{
							var str_msf = clp.ReadToken();
							var msf = new Timestamp(str_msf);
							if (!msf.Valid)
								Error($"Ignoring {{0}} with invalid length MSF: {str_msf}", key);
							else
							{
								if (key == "POSTGAP")
									OUT_CueFile.Commands.Add(new CUE_File.Command.POSTGAP(msf));
								else
									OUT_CueFile.Commands.Add(new CUE_File.Command.PREGAP(msf));
							}
						}
						break;

					case "REM":
						{
							var comment = clp.ReadLine();
							// cues don't support multiple sessions themselves, but it is common for rips to put SESSION # in REM fields
							// so, if we have such a REM, we'll check if the comment starts with SESSION, and interpret that as a session "command"
							var trimmed = comment.Trim();
							if (trimmed.StartsWith("SESSION ", StringComparison.OrdinalIgnoreCase) && int.TryParse(trimmed.Substring(8), out var number) && number > 0)
							{
								OUT_CueFile.Commands.Add(new CUE_File.Command.SESSION(number));
								break;
							}
							
							OUT_CueFile.Commands.Add(new CUE_File.Command.REM(comment));
							break;
						}


					case "SONGWRITER":
						OUT_CueFile.Commands.Add(new CUE_File.Command.SONGWRITER(clp.ReadPath() ?? ""));
						break;

					case "TITLE":
						OUT_CueFile.Commands.Add(new CUE_File.Command.TITLE(clp.ReadPath() ?? ""));
						break;

					case "TRACK":
						{
							if (clp.EOF)
							{
								Error("Incomplete TRACK command");
								break;
							}

							var str_tracknum = clp.ReadToken();
							if (!int.TryParse(str_tracknum, out var tracknum) || tracknum is < 1 or > 99)
							{
								Error($"Invalid TRACK number: {str_tracknum}");
								break;
							}

							// TODO - check sequentiality? maybe as a warning

							CueTrackType tt;
							var str_trackType = clp.ReadToken();
							switch (str_trackType.ToUpperInvariant())
							{
								default:
									Error($"Unknown TRACK type: {str_trackType}");
									tt = CueTrackType.Unknown;
									break;
								case "AUDIO": tt = CueTrackType.Audio; break;
								case "CDG": tt = CueTrackType.CDG; break;
								case "MODE1/2048": tt = CueTrackType.Mode1_2048; break;
								case "MODE1/2352": tt = CueTrackType.Mode1_2352; break;
								case "MODE2/2336": tt = CueTrackType.Mode2_2336; break;
								case "MODE2/2352": tt = CueTrackType.Mode2_2352; break;
								case "CDI/2336": tt = CueTrackType.CDI_2336; break;
								case "CDI/2352": tt = CueTrackType.CDI_2352; break;
							}

							OUT_CueFile.Commands.Add(new CUE_File.Command.TRACK(tracknum, tt));
						}
						break;
				}

				if (!clp.EOF)
				{
					var remainder = clp.ReadLine();
					if (remainder.TrimStart().StartsWith(';'))
					{
						//add a comment
						OUT_CueFile.Commands.Add(new CUE_File.Command.COMMENT(remainder));
					}
					else Warn($"Unknown text at end of line after processing command: {key}");
				}

			} //end cue parsing loop

			FinishLog();
		}

		public override void Run()
		{
			OUT_CueFile = new();
			LoadFromString();
		}
	}
}
