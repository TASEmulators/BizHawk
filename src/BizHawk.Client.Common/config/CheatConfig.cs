namespace BizHawk.Client.Common
{
	public interface ICheatConfig
	{
		bool DisableOnLoad { get; }
		bool AutoSaveOnClose { get; }
		RecentFiles Recent { get; }
	}

	public class CheatConfig : ICheatConfig
	{
		public bool DisableOnLoad { get; set; }

		public bool LoadFileByGame { get; set; }

		public bool AutoSaveOnClose { get; set; }

		public RecentFiles Recent { get; set; }

		public CheatConfig()
			=> RestoreDefaults(alsoWipeRecents: true);

		public void RestoreDefaults(bool alsoWipeRecents = false)
		{
			AutoSaveOnClose = true;
			DisableOnLoad = true;
			LoadFileByGame = true;
			if (alsoWipeRecents) Recent = new(8);
		}
	}
}
