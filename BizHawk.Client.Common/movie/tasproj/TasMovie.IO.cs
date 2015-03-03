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

		private const double PROGRESS_STEP = 100 / 12; // TODO hardcoded for now, there might be a better way of doing this

		private double _totalProgress = 0;

		private void ReportProgress(double percent)
		{
			if (_progressReportWorker != null)
			{
				_totalProgress += percent;
				_progressReportWorker.ReportProgress((int)_totalProgress);
			}
		}

		protected override void Write(string fn)
		{
			_totalProgress = 0;

			var file = new FileInfo(fn);
			if (!file.Directory.Exists)
			{
				Directory.CreateDirectory(file.Directory.ToString());
			}

			using (var bs = new BinaryStateSaver(fn, false))
			{
				bs.PutLump(BinaryStateLump.Movieheader, tw => tw.WriteLine(Header.ToString()));
				ReportProgress(PROGRESS_STEP);
				bs.PutLump(BinaryStateLump.Comments, tw => tw.WriteLine(CommentsString()));
				ReportProgress(PROGRESS_STEP);
				bs.PutLump(BinaryStateLump.Subtitles, tw => tw.WriteLine(Subtitles.ToString()));
				ReportProgress(PROGRESS_STEP);
				bs.PutLump(BinaryStateLump.SyncSettings, tw => tw.WriteLine(SyncSettingsJson));
				ReportProgress(PROGRESS_STEP);
				bs.PutLump(BinaryStateLump.Input, tw => tw.WriteLine(RawInputLog()));
				ReportProgress(PROGRESS_STEP);

				// TasProj extras
				bs.PutLump(BinaryStateLump.StateHistorySettings, tw => tw.WriteLine(StateManager.Settings.ToString()));
				ReportProgress(PROGRESS_STEP);
				if (StateManager.Settings.SaveStateHistory)
				{
					bs.PutLump(BinaryStateLump.StateHistory, (BinaryWriter bw) => StateManager.Save(bw));
				}
				ReportProgress(PROGRESS_STEP);

				bs.PutLump(BinaryStateLump.LagLog, (BinaryWriter bw) => LagLog.Save(bw));
				ReportProgress(PROGRESS_STEP);
				bs.PutLump(BinaryStateLump.Markers, tw => tw.WriteLine(Markers.ToString()));
				ReportProgress(PROGRESS_STEP);

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
				ReportProgress(PROGRESS_STEP);
				if (ClientSettingsForSave != null)
				{
					var clientSettingsJson = ClientSettingsForSave();
					bs.PutLump(BinaryStateLump.ClientSettings, (TextWriter tw) => tw.Write(clientSettingsJson));
				}
				ReportProgress(PROGRESS_STEP);

				if (VerificationLog.Any())
				{
					bs.PutLump(BinaryStateLump.VerificationLog, tw => tw.WriteLine(InputLogToString(VerificationLog)));
				}
				ReportProgress(PROGRESS_STEP);
			}

			Changes = false;
		}

		public override bool Load()
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

				if (StateManager.Settings.SaveStateHistory)
				{
					bl.GetLump(BinaryStateLump.StateHistory, false, delegate(BinaryReader br, long length)
					{
						StateManager.Load(br);
					});
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
			}

			Changes = false;
			return true;
		}

		private void ClearTasprojExtras()
		{
			LagLog.Clear();
			StateManager.Clear();
			Markers.Clear();
		}

		private static string InputLogToString(List<string> log)
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
