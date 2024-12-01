using System.Diagnostics;

namespace BizHawk.Common
{
	/// <summary>Create a new instance of this class in a <see langword="using"/> block, and it will measure the time elapsed until the block finishes executing. Provide a label to print to stdout or provide a callback for custom behaviour.</summary>
	public class SimpleTime : IDisposable
	{
		private readonly Action<long> _callback;

		private readonly Stopwatch _stopwatch = new Stopwatch();

		public SimpleTime(Action<long> callback)
		{
			_callback = callback;
			_stopwatch.Start();
		}

		public SimpleTime(string label) : this(l => Console.WriteLine($"Elapsed time for {label}: {l} ms")) {}

		public void Dispose()
		{
			_stopwatch.Stop();
			_callback(_stopwatch.ElapsedMilliseconds);
		}
	}
}
