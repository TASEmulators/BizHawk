namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// This service specifies the interaction of the client and the core in terms of the state of input polling
	/// A lag frame is a frame in which input was not polled
	/// Input callbacks fire whenever input is polled
	/// This service is used for the lag counter on the front end, if available.  In addition,
	/// LUA script makes use of input callbacks as well as reporting lag status
	/// Setters for both the count and lag flag are used by tools who offer custom notions of lag
	/// Additionally, movie support could in theory make use of input callbacks
	/// </summary>
	public interface IInputPollable : IEmulatorService
	{
		/// <summary>
		/// Gets or sets the current lag count.
		/// </summary>
		int LagCount { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not current frame is a lag frame.
		/// All cores should define it the same, a lag frame is a frame in which input was not polled.
		/// </summary>
		bool IsLagFrame { get; set; }

		IInputCallbackSystem InputCallbacks { get; }
	}
}
