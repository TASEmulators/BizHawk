using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	internal partial class TasMovie
	{
		public Func<string> ClientSettingsForSave { get; set; }
		public Action<string> GetClientSettingsOnLoad { get; set; }

		protected override void Write(string fn, bool backup = false)
		{
			var file = new FileInfo(fn);
			if (file.Directory != null && !file.Directory.Exists)
			{
				Directory.CreateDirectory(file.Directory.ToString());
			}

			using var bs = new BinaryStateSaver(fn, false);
			bs.PutLump(BinaryStateLump.Movieheader, tw => tw.WriteLine(Header.ToString()));
			bs.PutLump(BinaryStateLump.Comments, tw => tw.WriteLine(CommentsString()));
			bs.PutLump(BinaryStateLump.Subtitles, tw => tw.WriteLine(Subtitles.ToString()));
			bs.PutLump(BinaryStateLump.SyncSettings, tw => tw.WriteLine(SyncSettingsJson));
			bs.PutLump(BinaryStateLump.Input, WriteInputLog);

			// TasProj extras
			var settings = JsonConvert.SerializeObject(TasStateManager.Settings);
			bs.PutLump(BinaryStateLump.StateHistorySettings, tw => tw.WriteLine(settings));

			bs.PutLump(BinaryStateLump.LagLog, tw => LagLog.Save(tw));
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
				bs.PutLump(BinaryStateLump.VerificationLog, tw => tw.WriteLine(VerificationLog.ToInputLog()));
			}

			if (Branches.Any())
			{
				Branches.Save(bs);
			}

			bs.PutLump(BinaryStateLump.Session, tw => tw.WriteLine(JsonConvert.SerializeObject(Session)));

			if (TasStateManager.Settings.SaveStateHistory && !backup)
			{
				bs.PutLump(BinaryStateLump.StateHistory, bw => TasStateManager.Save(bw));
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
					LagLog.Load(tr);
				});

				bl.GetLump(BinaryStateLump.StateHistorySettings, false, delegate(TextReader tr)
				{
					var json = tr.ReadToEnd();
					try
					{
						TasStateManager.Settings = JsonConvert.DeserializeObject<TasStateManagerSettings>(json);
					}
					catch
					{
						// Do nothing, and use default settings instead
					}
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
					var json = tr.ReadToEnd();
					try
					{
						Session = JsonConvert.DeserializeObject<TasSession>(json);
					}
					catch
					{
						// Do nothing, and use default settings instead
					}
				});

				if (!preload)
				{
					if (TasStateManager.Settings.SaveStateHistory)
					{
						bl.GetLump(BinaryStateLump.StateHistory, false, delegate(BinaryReader br, long length)
						{
							TasStateManager.Load(br);
						});
					}
				}
			}

			Changes = false;
			return true;
		}

		private void ClearTasprojExtras()
		{
			LagLog.Clear();
			TasStateManager.Clear();
			Markers.Clear();
			ChangeLog.Clear();
		}
	}
}
