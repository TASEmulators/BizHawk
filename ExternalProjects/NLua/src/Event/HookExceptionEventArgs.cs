using System;

namespace NLua.Event
{
	public class HookExceptionEventArgs : EventArgs
	{
		public Exception Exception { get; }

		public HookExceptionEventArgs(Exception ex)
		{
			Exception = ex;
		}
	}
}
