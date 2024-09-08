using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.RamSearchEngine
{
	public class SearchEngineSettings
	{
		public SearchEngineSettings(IMemoryDomains memoryDomains, bool useUndoHistory)
		{
			BigEndian = memoryDomains.MainMemory.EndianType == MemoryDomain.Endian.Big;
			Size = (WatchSize)memoryDomains.MainMemory.WordSize;
			Mode = memoryDomains.MainMemory.Size > 1024 * 1024
				? SearchMode.Fast
				: SearchMode.Detailed;

			Domain = memoryDomains.MainMemory;
			UseUndoHistory = useUndoHistory;
		}

		/*Require restart*/
		public SearchMode Mode { get; set; }
		public MemoryDomain Domain { get; set; }
		public WatchSize Size { get; set; }
		public bool CheckMisAligned { get; set; }

		/*Can be changed mid-search*/
		public WatchDisplayType Type { get; set; } = WatchDisplayType.Unsigned;
		public bool BigEndian { get; set; }
		public PreviousType PreviousType { get; set; } = PreviousType.LastSearch;
		public bool UseUndoHistory { get; set; }
	}

	public static class SearchEngineSettingsExtensions
	{
		public static bool IsFastMode(this SearchEngineSettings settings)
			=> settings.Mode == SearchMode.Fast;

		public static bool IsDetailed(this SearchEngineSettings settings)
			=> settings.Mode == SearchMode.Detailed;
	}
}
