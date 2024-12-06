namespace BizHawk.Client.Common.Websocket.Messages
{
	/// <summary>
	/// Send an input to the current emulator. For valid inputs, <see cref="GetInputOptionsRequestMessage"/>
	/// </summary>
	/// <seealso cref="InputResponseMessage"/>
	public struct InputRequestMessage
	{
		/// <summary>
		/// Name of the input, e.g. one of the inputs returned in <see cref="GetInputOptionsResponseMessage"/>
		/// </summary>
		public string Name { get; set; }

		public InputRequestMessage() { }

		public InputRequestMessage(string name)
		{
			Name = name;
		}
	}

	/// <summary>
	/// Result message after sending an input to the <see cref="Topic.Input"/> topic.
	/// </summary>
	/// <seealso cref="InputRequestMessage"/>
	public struct InputResponseMessage
	{
		/// <summary>
		/// Whether the input was successfully applied.
		/// </summary>
		public bool Success { get; set; }

		public InputResponseMessage() { }

		public InputResponseMessage(bool success)
		{
			Success = success;
		}
	}
}