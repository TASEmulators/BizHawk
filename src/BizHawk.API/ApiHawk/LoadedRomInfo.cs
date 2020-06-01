using System.Collections.Generic;

namespace BizHawk.API.ApiHawk
{
	public readonly struct LoadedRomInfo
	{
		public readonly string Hash;

		public readonly bool IsInDatabase;

		public readonly string? MapperName;

		public readonly string Name;

		public readonly IReadOnlyDictionary<string, string> Options;

		public readonly RomStatus Status;

		public LoadedRomInfo(
			string hash,
			bool isInDatabase,
			string? mapperName,
			string name,
			IReadOnlyDictionary<string, string> options,
			RomStatus status
		)
		{
			Hash = hash;
			IsInDatabase = isInDatabase;
			MapperName = mapperName;
			Name = name;
			Options = options;
			Status = status;
		}
	}
}
