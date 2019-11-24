using System;
using System.IO;
using System.Linq;
using System.Text;

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
				bs.PutLump(BinaryStateLump.StateHistorySettings, tw => tw.WriteLine(_stateManager.Settings.ToString()));

				bs.PutLump(BinaryStateLump.LagLog, tw => _lagLog.Save(tw));
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
				}

				bs.PutLump(BinaryStateLump.Session, tw => tw.WriteLine(Session.ToString()));

				if (_stateManager.Settings.SaveStateHistory && !backup)
				{
					bs.PutLump(BinaryStateLump.StateHistory, (BinaryWriter bw) => _stateManager.Save(bw));
				}
			}

			if (!backup)
			{
				Changes = false;
			}
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
					IsCountingRerecords = false;
					ExtractInputLog(tr, out _);
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
				bl.GetLump(BinaryStateLump.LagLog, false, delegate(TextReader tr)
				{
					_lagLog.Load(tr);
				});

				bl.GetLump(BinaryStateLump.StateHistorySettings, false, delegate(TextReader tr)
				{
					_stateManager.Settings.PopulateFromString(tr.ReadToEnd());
				});

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

				if (GetClientSettingsOnLoad != null)
				{
					string clientSettings = "";
					bl.GetLump(BinaryStateLump.ClientSettings, false, delegate(TextReader tr)
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

					if (!string.IsNullOrWhiteSpace(clientSettings))
					{
						GetClientSettingsOnLoad(clientSettings);
					}
				}

				bl.GetLump(BinaryStateLump.VerificationLog, false, delegate(TextReader tr)
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

				Branches.Load(bl, this);

				bl.GetLump(BinaryStateLump.Session, false, delegate(TextReader tr)
				{
					Session.PopulateFromString(tr.ReadToEnd());
				});

				if (!preload)
				{
					if (_stateManager.Settings.SaveStateHistory)
					{
						bl.GetLump(BinaryStateLump.StateHistory, false, delegate(BinaryReader br, long length)
						{
							_stateManager.Load(br);
						});
					}

					// Movie should always have a state at frame 0.
					if (!StartsFromSavestate && Global.Emulator.Frame == 0)
					{
						_stateManager.Capture();
					}
				}
			}

			Changes = false;
			return true;
		}

		private void ClearTasprojExtras()
		{
			ClearLagLog();
			_stateManager.Clear();
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
