using System;
using System.Collections.Generic;

namespace BizHawk.API.Base
{
	public abstract class APIEnvironment
	{
		/// <remarks>how this environment will be exposed to future environments</remarks>
		private readonly HistoricAPIEnvironment _keep;

		public readonly HistoricAPIEnvironment Last;

		public readonly Action<string> LogCallback;

		public readonly IDictionary<Guid, byte[]> MemorySnapshots;

		protected APIEnvironment(Action<string> logCallback, HistoricAPIEnvironment last, out HistoricAPIEnvironment keep)
		{
			Last = last;
			LogCallback = logCallback;
			var memorySnapshots = new Dictionary<Guid, byte[]>();
			MemorySnapshots = memorySnapshots;

			keep = _keep = new HistoricAPIEnvironment(last, memorySnapshots);
		}
	}
}
