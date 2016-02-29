namespace BizHawk.Emulation.Common
{
	public interface IInputPollable : IEmulatorService
	{
		/// <summary>
		/// The lag count.
		/// </summary>
		int LagCount { get; set; }

		/// <summary>
		/// If the current frame is a lag frame.
		/// All cores should define it the same, a lag frame is a frame in which input was not polled.
		/// </summary>
		bool IsLagFrame { get; set; }

		IInputCallbackSystem InputCallbacks { get; }
	}
}
