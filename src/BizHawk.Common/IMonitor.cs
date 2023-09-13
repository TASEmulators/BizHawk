namespace BizHawk.Common
{
	public interface IMonitor
	{
		void Enter();

		void Exit();
	}

	public static class MonitorExtensions
	{
		public static EnterExitWrapper EnterExit(this IMonitor m)
			=> new(m);

		public readonly ref struct EnterExitWrapper
		{
			// yes, this can be null
			private readonly IMonitor? _m;

			// disallow public construction outside of EnterExit extension
			internal EnterExitWrapper(IMonitor? m)
			{
				_m = m;
				_m?.Enter();
			}

			public void Dispose()
				=> _m?.Exit();
		}
	}
}
