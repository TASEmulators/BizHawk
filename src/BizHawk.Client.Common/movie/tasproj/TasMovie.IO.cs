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

		protected override void AddLumps(ZipStateSaver bs, bool isBackup = false)
		{
			AddBk2Lumps(bs);
			AddTasProjLumps(bs, isBackup);
		}

		private void AddTasProjLumps(ZipStateSaver bs, bool isBackup = false)
		{
			var settings = JsonConvert.SerializeObject(TasStateManager.Settings);
			bs.PutLump(BinaryStateLump.StateHistorySettings, tw => tw.WriteLine(settings));
			bs.PutLump(BinaryStateLump.LagLog, tw => LagLog.Save(tw));
			bs.PutLump(BinaryStateLump.Markers, tw => tw.WriteLine(Markers.ToString()));

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

			bs.PutLump(BinaryStateLump.Session, tw => tw.WriteLine(JsonConvert.SerializeObject(TasSession)));

			if (!isBackup)
			{
				bs.PutLump(BinaryStateLump.StateHistory, bw => TasStateManager.SaveStateHistory(bw));
			}
		}

		protected override void ClearBeforeLoad()
		{
			ClearBk2Fields();
			ClearTasprojExtras();
		}

		private void ClearTasprojExtras()
		{
			LagLog.Clear();
			TasStateManager?.Clear();
			Markers.Clear();
			ChangeLog.Clear();
		}
		
		protected override void LoadFields(ZipStateLoader bl, bool preload)
		{
			LoadBk2Fields(bl, preload);

			if (!preload)
			{
				if (MovieService.IsCurrentTasVersion(Header[HeaderKeys.MovieVersion]))
				{
					LoadTasprojExtras(bl);
				}
				else
				{
					Session.PopupMessage("The current .tasproj is not compatible with this version of BizHawk! .tasproj features failed to load.");
				}
			}
		}
		
		private void LoadTasprojExtras(ZipStateLoader bl)
		{
			bl.GetLump(BinaryStateLump.LagLog, false, delegate(TextReader tr)
			{
				LagLog.Load(tr);
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
					TasSession = JsonConvert.DeserializeObject<TasSession>(json);
				}
				catch
				{
					// Do nothing, and use default settings instead
				}
			});

			ZwinderStateManagerSettings settings = new ZwinderStateManagerSettings();
			bl.GetLump(BinaryStateLump.StateHistorySettings, false, delegate(TextReader tr)
			{
				var json = tr.ReadToEnd();
				try
				{
					settings = JsonConvert.DeserializeObject<ZwinderStateManagerSettings>(json);
				}
				catch
				{
					// Do nothing, and use default settings instead
				}
			});

			bl.GetLump(BinaryStateLump.StateHistory, false, delegate(BinaryReader br, long length)
			{
				TasStateManager?.Dispose();
				TasStateManager = ZwinderStateManager.Create(br, settings, IsReserved);
			});
		}
	}
}
