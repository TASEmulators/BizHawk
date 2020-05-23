using System;
using System.Collections.Generic;

namespace BizHawk.API.Base
{
	public sealed class HistoricAPIEnvironment
	{
		public readonly HistoricAPIEnvironment Last;

		public readonly IReadOnlyDictionary<Guid, byte[]> MemorySnapshots;

		public HistoricAPIEnvironment(
			HistoricAPIEnvironment last,
			IReadOnlyDictionary<Guid, byte[]> memorySnapshots
		)
		{
			Last = last;
			MemorySnapshots = memorySnapshots;
		}
	}
}
