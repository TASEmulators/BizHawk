namespace BizHawk.Client.Common.Websocket.Messages
{
	/// <summary>
	/// Send an input to the current emulator. For valid inputs, <see cref="GetInputOptionsRequestMessage"/>
	/// </summary>
	/// <seealso cref="InputResponseMessage"/>
	public struct InputRequestMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		public ClickInputRequestMessage? Click { get; set; }

		public ToggleInputRequestMessage? Toggle { get; set; }

		public InputRequestMessage(string requestId, ClickInputRequestMessage click)
		{
			RequestId = requestId;
			Click = click;
		}

		public InputRequestMessage(string requestId, ToggleInputRequestMessage toggle)
		{
			RequestId = requestId;
			Toggle = toggle;
		}

		public InputRequestMessage() { }
	}

	/// <summary>
	/// Request to click an input for a single frame.
	/// </summary>
	public struct ClickInputRequestMessage 
	{
		/// <summary>
		/// Name of the input, e.g. one of the inputs returned in <see cref="GetInputOptionsResponseMessage"/>
		/// </summary>
		public string Name { get; set; }
	}

	/// <summary>
	/// Request to switch an input between on/pressed to off/unpressed.
	/// </summary>
	public struct ToggleInputRequestMessage 
	{
		/// <summary>
		/// Name of the input, e.g. one of the inputs returned in <see cref="GetInputOptionsResponseMessage"/>
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Optional value to set the toggle to, rather than flipping the state.
		/// </summary>
		public bool? Value { get; set; }
	}

	/// <summary>
	/// Result message after sending an input to the <see cref="Topic.Input"/> topic.
	/// </summary>
	/// <seealso cref="InputRequestMessage"/>
	public struct InputResponseMessage
	{
		/// <summary>
		/// Request ID, which will be sent in the response to identify the round-trip exchange.
		/// </summary>
		public string RequestId { get; set; }

		/// <summary>
		/// Whether the input was successfully applied.
		/// </summary>
		public bool Success { get; set; }

		public InputResponseMessage() { }

		public InputResponseMessage(string requestId, bool success)
		{
			RequestId = requestId;
			Success = success;
		}
	}
}