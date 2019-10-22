using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.EmuHawk
{
	public class FileFilterEntry
	{
		public string Description { get; }
		public string[] Filters { get; }
		public string[] DeveloperFilters { get; }

		public FileFilterEntry(string description, string filters, string developerFilters = null)
		{
			Description = description;
			Filters = filters?.Split(';') ?? Array.Empty<string>();
			DeveloperFilters = developerFilters?.Split(';') ?? Array.Empty<string>();
		}

		public IEnumerable<string> EffectiveFilters
		{
			get
			{
				IEnumerable<string> effectiveFilters = Filters;
				if (VersionInfo.DeveloperBuild)
				{
					effectiveFilters = effectiveFilters.Concat(DeveloperFilters);
				}
				return effectiveFilters;
			}
		}
	}
}
