using System.IO;

using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	internal partial class TasMovie
	{
		public Func<string> ClientSettingsForSave { get; set; }
		public string LoadedClientSettings { get; private set; }

		protected override void AddLumps(ZipStateSaver bs, bool isBackup = false)
		{
			AddBk2Lumps(bs);
			AddTasProjLumps(bs, isBackup);
		}

		private void AddTasProjLumps(ZipStateSaver bs, bool isBackup = false)
		{
			// at this point, TasStateManager may be null if we're currently importing a .bk2

			var settings = JsonConvert.SerializeObject(
				TasStateManager?.Settings ?? Session.Settings.DefaultTasStateManagerSettings,
				new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Objects }
			);
			bs.PutLump(BinaryStateLump.StateHistorySettings, tw => tw.WriteLine(settings));
			bs.PutLump(BinaryStateLump.LagLog, tw => LagLog.Save(tw), zstdCompress: true);
			bs.PutLump(BinaryStateLump.Markers, tw => tw.WriteLine(Markers.ToString()));

			if (ClientSettingsForSave != null)
			{
				var inputRollSettingsJson = ClientSettingsForSave();
				bs.PutLump(BinaryStateLump.ClientSettings, (TextWriter tw) => tw.Write(inputRollSettingsJson));
			}

			if (VerificationLog.Count is not 0)
			{
				bs.PutLump(BinaryStateLump.VerificationLog, tw => tw.WriteLine(VerificationLog.ToInputLog()));
			}

			if (Branches.Count is not 0) Branches.Save(bs);

			bs.PutLump(BinaryStateLump.Session, tw => tw.WriteLine(JsonConvert.SerializeObject(TasSession)));

			if (!isBackup && TasStateManager is not null)
			{
				bs.PutLump(BinaryStateLump.StateHistory, bw => TasStateManager.SaveStateHistory(bw));
			}
		}

		protected override void ClearBeforeLoad()
		{
			base.ClearBeforeLoad();
			ClearTasprojExtras();
		}

		private void ClearTasprojExtras()
		{
			LagLog.Clear();
			TasStateManager?.Clear();
			Markers.Clear();
			ChangeLog.Clear();
		}

		protected override void LoadFields(ZipStateLoader bl)
		{
			base.LoadFields(bl);

			if (MovieService.IsCurrentTasVersion(Header[HeaderKeys.MovieVersion]))
			{
				LoadTasprojExtras(bl);
			}
			else
			{
				Session.PopupMessage("The current .tasproj is not compatible with this version of BizHawk! .tasproj features failed to load.");
				Markers.Add(0, StartsFromSavestate ? "Savestate" : "Power on");
			}

			ChangeLog.Clear();
			Changes = false;
		}

		private void LoadTasprojExtras(ZipStateLoader bl)
		{
			bl.GetLump(BinaryStateLump.LagLog, abort: false, tr => LagLog.Load(tr));

			bl.GetLump(BinaryStateLump.Markers, abort: false, tr =>
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

			bl.GetLump(BinaryStateLump.ClientSettings, abort: false, tr =>
			{
				string inputRollSettings = tr.ReadToEnd();

				if (!string.IsNullOrEmpty(inputRollSettings))
					LoadedClientSettings = inputRollSettings;
			});

			bl.GetLump(BinaryStateLump.VerificationLog, abort: false, tr =>
			{
				VerificationLog.Clear();
				while (true)
				{
					var line = tr.ReadLine();
					if (string.IsNullOrEmpty(line))
					{
						break;
					}

					if (line.StartsWith('|'))
					{
						VerificationLog.Add(line);
					}
				}
			});

			Branches.Load(bl, this);

			bl.GetLump(BinaryStateLump.Session, abort: false, tr =>
			{
				var json = tr.ReadToEnd();
				try
				{
					TasSession = JsonConvert.DeserializeObject<TasSession>(json);
					Branches.Current = TasSession.CurrentBranch;
				}
				catch
				{
					// Do nothing, and use default settings instead
				}
			});

			IStateManagerSettings settings = Session.Settings.DefaultTasStateManagerSettings;
			bl.GetLump(BinaryStateLump.StateHistorySettings, abort: false, tr =>
			{
				var json = tr.ReadToEnd();
				try
				{
					settings = JsonConvert.DeserializeObject<IStateManagerSettings>(json);
				}
				catch
				{
					// Do nothing, and use default settings instead
				}
			});

			TasStateManager?.Dispose();
			bool badHistory = false;
			var hasHistory = bl.GetLump(BinaryStateLump.StateHistory, abort: false, br =>
			{
				try
				{
					TasStateManager = settings.CreateManager(IsReserved);
					TasStateManager.LoadStateHistory(br);
				}
				catch
				{
					// Continue with a fresh manager. If state history got corrupted, the file is still very much useable
					// and we would want the user to be able to load, and regenerate their state history
					// however, we still have an issue of how state history got corrupted
					badHistory = true;
					Session.PopupMessage("State history was corrupted, clearing and working with a fresh history.");
				}
			});

			if (!hasHistory || badHistory)
			{
				try
				{
					TasStateManager = settings.CreateManager(IsReserved);
				}
				catch
				{
					TasStateManager = Session.Settings.DefaultTasStateManagerSettings.CreateManager(IsReserved);
				}
			}
		}
	}
}
