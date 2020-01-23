#nullable disable

using System;
using System.Diagnostics;

namespace BizHawk.Common
{
	public class SimpleTime : IDisposable
	{
		private readonly Stopwatch _w;
		private readonly Action<int> _f;

		public SimpleTime(string s)
			: this(t => Console.WriteLine("Elapsed time for {0}: {1}ms", s, t))
		{
		}

		public SimpleTime(Action<int> f)
		{
			_f = f;
			_w = new Stopwatch();
			_w.Start();
		}

		public void Dispose()
		{
			_w.Stop();
			_f((int)_w.ElapsedMilliseconds);
		}
	}
}
