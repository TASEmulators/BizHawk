using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace BizHawk.Common
{
	public class SimpleTime : IDisposable
	{
		private Stopwatch w;
		private Action<int> f;

		public SimpleTime(string s)
			:this(t => Console.WriteLine("Elapsed time for {0}: {1}ms", s, t))
		{
		}

		public SimpleTime(Action<int> f)
		{
			this.f = f;
			w = new Stopwatch();
			w.Start();
		}

		public void Dispose()
		{
			w.Stop();
			f((int)w.ElapsedMilliseconds);
		}
	}
}
