using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common.RamSearchEngine
{
	public class SearchEngineSettings
	{
		public SearchEngineSettings(IMemoryDomains memoryDomains)
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
		}

		/*Require restart*/
		public enum SearchMode
		{
			Fast, Detailed
		}

		public SearchMode Mode { get; set; }
		public MemoryDomain Domain { get; set; }
		public WatchSize Size { get; set; }
		public bool CheckMisAligned { get; set; }

		/*Can be changed mid-search*/
		public DisplayType Type { get; set; }
		public bool BigEndian { get; set; }
		public PreviousType PreviousType { get; set; }
	}
}
