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
		public bool LoadFileByGame { get; set; } = true;
		public bool AutoSaveOnClose { get; set; } = true;
		public RecentFiles Recent { get; set; } = new RecentFiles(8);
	}
}
