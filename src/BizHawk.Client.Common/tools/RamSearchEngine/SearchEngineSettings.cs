using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.RamSearchEngine
{
	public class SearchEngineSettings
	{
		public SearchEngineSettings(IMemoryDomains memoryDomains, bool useUndoHistory)
		{
			BigEndian = memoryDomains.MainMemory.EndianType == MemoryDomain.Endian.Big;
			Size = (WatchSize)memoryDomains.MainMemory.WordSize;
			Type = DisplayType.Unsigned;
			Mode = memoryDomains.MainMemory.Size > 1024 * 1024
				? SearchMode.Fast
				: SearchMode.Detailed;

			Domain = memoryDomains.MainMemory;
			CheckMisAligned = false;
			PreviousType = PreviousType.LastSearch;
			UseUndoHistory = useUndoHistory;
		}

		/*Require restart*/
		public SearchMode Mode { get; set; }
		public MemoryDomain Domain { get; set; }
		public WatchSize Size { get; set; }
		public bool CheckMisAligned { get; set; }

		/*Can be changed mid-search*/
		public DisplayType Type { get; set; }
		public bool BigEndian { get; set; }
		public PreviousType PreviousType { get; set; }
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
