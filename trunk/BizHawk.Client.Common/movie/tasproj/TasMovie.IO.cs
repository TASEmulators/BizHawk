using System;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Common.IOExtensions;

namespace BizHawk.Client.Common
{
	public partial class TasMovie
	{
		protected override void Write(string fn)
		{
			var file = new FileInfo(fn);
			if (!file.Directory.Exists)
			{
				Directory.CreateDirectory(file.Directory.ToString());
			}

			using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write))
			using (var bs = new BinaryStateSaver(fs, false))
			{
				bs.PutLump(BinaryStateLump.Movieheader, tw => tw.WriteLine(Header.ToString()));
				bs.PutLump(BinaryStateLump.Comments, tw => tw.WriteLine(CommentsString()));
				bs.PutLump(BinaryStateLump.Subtitles, tw => tw.WriteLine(Subtitles.ToString()));
				bs.PutLump(BinaryStateLump.SyncSettings, tw => tw.WriteLine(SyncSettingsJson));

				bs.PutLump(BinaryStateLump.Input, tw => tw.WriteLine(RawInputLog()));

				
				bs.PutLump(BinaryStateLump.GreenzoneSettings, tw => tw.WriteLine(StateManager.Settings.ToString()));
				if (StateManager.Settings.SaveGreenzone)
				{
					bs.PutLump(BinaryStateLump.Greenzone, (BinaryWriter bw) => bw.Write(StateManager.ToArray()));
				}

				bs.PutLump(BinaryStateLump.LagLog, (BinaryWriter bw) => bw.Write(LagLog.ToByteArray()));
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
				ClearTasprojExtrasBeforeLoad();

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

				bl.GetLump(BinaryStateLump.Input, true, delegate(TextReader tr)
				{
					var errorMessage = string.Empty;
					ExtractInputLog(tr, out errorMessage);
				});

				if (StartsFromSavestate)
				{
					bl.GetCoreState(
						delegate(BinaryReader br)
						{
							BinarySavestate = br.ReadBytes((int)br.BaseStream.Length);
						},
						delegate(TextReader tr)
						{
							TextSavestate = tr.ReadToEnd();
						});
				}

				// TasMovie enhanced information
				bl.GetLump(BinaryStateLump.LagLog, false, delegate(BinaryReader br)
				{
					var bytes = br.BaseStream.ReadAllBytes();
					LagLog = bytes.ToBools().ToList();
				});

				bl.GetLump(BinaryStateLump.GreenzoneSettings, false, delegate(TextReader tr)
				{
					StateManager.Settings.PopulateFromString(tr.ReadToEnd());
				});

				if (StateManager.Settings.SaveGreenzone)
				{
					bl.GetLump(BinaryStateLump.Greenzone, false, delegate(BinaryReader br)
					{
						StateManager.FromArray(br.BaseStream.ReadAllBytes());
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
			}

			Changes = false;
			return true;
		}

		private void ClearTasprojExtrasBeforeLoad()
		{
			LagLog.Clear();
			StateManager.Clear();
		}

		private void RestoreLagLog(byte[] buffer)
		{

		}
	}
}
