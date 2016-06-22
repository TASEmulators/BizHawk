using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.IOExtensions;
using System.Diagnostics;
using System.ComponentModel;

namespace BizHawk.Client.Common
{
	public partial class TasMovie
	{
		public Func<string> ClientSettingsForSave { get; set; }
		public Action<string> GetClientSettingsOnLoad { get; set; }

		protected override void Write(string fn, bool backup = false)
		{
			var file = new FileInfo(fn);
			if (!file.Directory.Exists)
			{
				Directory.CreateDirectory(file.Directory.ToString());
			}

			using (var bs = new BinaryStateSaver(fn, false))
			{
				bs.PutLump(BinaryStateLump.Movieheader, tw => tw.WriteLine(Header.ToString()));
				bs.PutLump(BinaryStateLump.Comments, tw => tw.WriteLine(CommentsString()));
				bs.PutLump(BinaryStateLump.Subtitles, tw => tw.WriteLine(Subtitles.ToString()));
				bs.PutLump(BinaryStateLump.SyncSettings, tw => tw.WriteLine(SyncSettingsJson));
				bs.PutLump(BinaryStateLump.Input, tw => WriteInputLog(tw));

				// TasProj extras
				bs.PutLump(BinaryStateLump.StateHistorySettings, tw => tw.WriteLine(StateManager.Settings.ToString()));

				if (StateManager.Settings.SaveStateHistory)
				{
					bs.PutLump(BinaryStateLump.StateHistory, (BinaryWriter bw) => StateManager.Save(bw));
				}

				bs.PutLump(BinaryStateLump.LagLog, (BinaryWriter bw) => LagLog.Save(bw));
				bs.PutLump(BinaryStateLump.Markers, tw => tw.WriteLine(Markers.ToString()));

				if (StartsFromSavestate)
				{
					if (TextSavestate != null)
					{
						bs.PutLump(BinaryStateLump.CorestateText, (TextWriter tw) => tw.Write(TextSavestate));
					}
					else
					{
						bs.PutLump(BinaryStateLump.Corestate, (BinaryWriter bw) => bw.Write(BinarySavestate));
					}
				}
				else if (StartsFromSaveRam)
				{
					bs.PutLump(BinaryStateLump.MovieSaveRam, (BinaryWriter bw) => bw.Write(SaveRam));
				}

				if (ClientSettingsForSave != null)
				{
					var clientSettingsJson = ClientSettingsForSave();
					bs.PutLump(BinaryStateLump.ClientSettings, (TextWriter tw) => tw.Write(clientSettingsJson));
				}

				if (VerificationLog.Any())
				{
					bs.PutLump(BinaryStateLump.VerificationLog, tw => tw.WriteLine(InputLogToString(VerificationLog)));
				}

				if (Branches.Any())
				{
					Branches.Save(bs);
					if (StateManager.Settings.BranchStatesInTasproj)
					{
						bs.PutLump(BinaryStateLump.BranchStateHistory, (BinaryWriter bw) => StateManager.SaveBranchStates(bw));
					}
				}

				bs.PutLump(BinaryStateLump.Session, tw => tw.WriteLine(Session.ToString()));
			}

			if (!backup)
				Changes = false;
		}

		public override bool Load(bool preload)
		{
			var file = new FileInfo(Filename);
			if (!file.Exists)
			{
				return false;
			}

			using (var bl = BinaryStateLoader.LoadAndDetect(Filename, true))
			{
				if (bl == null)
				{
					return false;
				}

				ClearBeforeLoad();
				ClearTasprojExtras();

				bl.GetLump(BinaryStateLump.Movieheader, true, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							var pair = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

							if (pair.Length > 1)
							{
								Header.Add(pair[0], pair[1]);
							}
						}
					}
				});

				bl.GetLump(BinaryStateLump.Comments, true, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							Comments.Add(line);
						}
					}
				});

				bl.GetLump(BinaryStateLump.Subtitles, true, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							Subtitles.AddFromString(line);
						}
					}
				});

				bl.GetLump(BinaryStateLump.SyncSettings, true, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							SyncSettingsJson = line;
						}
					}
				});

				bl.GetLump(BinaryStateLump.Input, true, delegate(TextReader tr) // Note: ExtractInputLog will clear Lag and State data potentially, this must come before loading those
				{
					var errorMessage = string.Empty;
					IsCountingRerecords = false;
					ExtractInputLog(tr, out errorMessage);
					IsCountingRerecords = true;
				});

				if (StartsFromSavestate)
				{
					bl.GetCoreState(
						delegate(BinaryReader br, long length)
						{
							BinarySavestate = br.ReadBytes((int)length);
						},
						delegate(TextReader tr)
						{
							TextSavestate = tr.ReadToEnd();
						});
				}
				else if (StartsFromSaveRam)
				{
					bl.GetLump(BinaryStateLump.MovieSaveRam, false,
						delegate(BinaryReader br, long length)
						{
							SaveRam = br.ReadBytes((int)length);
						});
				}

				// TasMovie enhanced information
				if (bl.HasLump(BinaryStateLump.LagLog))
				{
					bl.GetLump(BinaryStateLump.LagLog, false, delegate(BinaryReader br, long length)
					{
						LagLog.Load(br);
					});
				}

				bl.GetLump(BinaryStateLump.StateHistorySettings, false, delegate(TextReader tr)
				{
					StateManager.Settings.PopulateFromString(tr.ReadToEnd());
				});

				if(!preload)
				{
					if (StateManager.Settings.SaveStateHistory)
					{
						bl.GetLump(BinaryStateLump.StateHistory, false, delegate(BinaryReader br, long length)
						{
							StateManager.Load(br);
						});
					}

					// Movie should always have a state at frame 0.
					if (!this.StartsFromSavestate && Global.Emulator.Frame == 0)
						StateManager.Capture();
				}

				bl.GetLump(BinaryStateLump.Markers, false, delegate(TextReader tr)
				{
					string line;
					while ((line = tr.ReadLine()) != null)
					{
						if (!string.IsNullOrWhiteSpace(line))
						{
							Markers.Add(new TasMovieMarker(line));
						}
					}
				});

				if (GetClientSettingsOnLoad != null && bl.HasLump(BinaryStateLump.ClientSettings))
				{
					string clientSettings = string.Empty;
					bl.GetLump(BinaryStateLump.ClientSettings, true, delegate(TextReader tr)
					{
						string line;
						while ((line = tr.ReadLine()) != null)
						{
							if (!string.IsNullOrWhiteSpace(line))
							{
								clientSettings = line;
							}
						}
					});

					GetClientSettingsOnLoad(clientSettings);
				}

				if (bl.HasLump(BinaryStateLump.VerificationLog))
				{
					bl.GetLump(BinaryStateLump.VerificationLog, true, delegate(TextReader tr)
					{
						VerificationLog.Clear();
						while (true)
						{
							var line = tr.ReadLine();
							if (string.IsNullOrEmpty(line))
							{
								break;
							}

							if (line.StartsWith("|"))
							{
								VerificationLog.Add(line);
							}
						}
					});
				}

				Branches.Load(bl, this);
				if (StateManager.Settings.BranchStatesInTasproj)
				{
					bl.GetLump(BinaryStateLump.BranchStateHistory, false, delegate(BinaryReader br, long length)
					{
						StateManager.LoadBranchStates(br);
					});
				}

				bl.GetLump(BinaryStateLump.Session, false, delegate(TextReader tr)
				{
					Session.PopulateFromString(tr.ReadToEnd());
				});
			}

			Changes = false;
			return true;
		}

		private void ClearTasprojExtras()
		{
			LagLog.Clear();
			StateManager.Clear();
			Markers.Clear();
			ChangeLog.ClearLog();
		}

		private static string InputLogToString(IStringLog log)
		{
			var sb = new StringBuilder();
			foreach (var record in log)
			{
				sb.AppendLine(record);
			}

			return sb.ToString();
		}
	}
}
