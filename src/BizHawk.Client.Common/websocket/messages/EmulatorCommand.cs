namespace BizHawk.Client.Common.Websocket.Messages
{
	/// <summary>
	/// Perform an emulator action, like speed up.
	/// </summary>
	/// <seealso cref="EmulatorCommandResponseMessage"/>
	public struct EmulatorCommandRequestMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		public StepSpeedRequestMessage? StepSpeed { get; set; }

		public RebootCoreRequestMessage? RebootCore { get; set; }

		public EmulatorCommandRequestMessage(string requestId, StepSpeedRequestMessage message) {
			RequestId = requestId;
			StepSpeed = message;
		}

		public EmulatorCommandRequestMessage(string requestId, RebootCoreRequestMessage message) {
			RequestId = requestId;
			RebootCore = message;
		}

		public EmulatorCommandRequestMessage() { }
	}

	/// <summary>
	/// Change the speed by a preconfigured step
	/// </summary>
	public struct StepSpeedRequestMessage
	{
		/// <summary>
		/// Number of times to change the speed, e.g. -2 means to slow down by two steps, and
		/// +3 means to speed up by 3 steps. The magnitude of the speed change is determined
		/// by the preconfigured settings of the emulator, such as what you see when you
		/// go to Config > Speed/Skip.
		/// </summary>
		public int Steps { get; set; }
		
		public StepSpeedRequestMessage(int steps) {
			Steps = steps;
		}

		public StepSpeedRequestMessage() {}
	}

	/// <summary>
	/// Rebort the emulator core
	/// </summary>
	public struct RebootCoreRequestMessage
	{
		public RebootCoreRequestMessage() {}
	}

	/// <summary>
	/// Result message after sending an request to the <see cref="Topic.EmulatorCommand"/> topic.
	/// </summary>
	/// <seealso cref="EmulatorCommandRequestMessage"/>
	public struct EmulatorCommandResponseMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		/// <summary>
		/// Whether the input was successfully applied.
		/// </summary>
		public bool Success { get; set; }

		public EmulatorCommandResponseMessage() { }

		public EmulatorCommandResponseMessage(string requestId, bool success)
		{
			RequestId = requestId;
			Success = success;
		}
	}
}