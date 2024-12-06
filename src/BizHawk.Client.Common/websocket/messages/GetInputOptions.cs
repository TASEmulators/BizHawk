using System.Collections.Generic;

namespace BizHawk.Client.Common.Websocket.Messages
{

	/// <summary>
	/// Requests the available options for controlling the current emulator.
	/// 
	/// Register for <see cref="Topic.GetInputOptions"/>
	/// </summary>
	public struct GetInputOptionsRequestMessage
	{
		public GetInputOptionsRequestMessage() { }
	}

	/// <summary>
	/// Response message after client has sent a <see cref="GetInputOptionsRequestMessage"/>
	/// 
	/// Register for <see cref="Topic.GetInputOptions"/>
	/// </summary>
	public struct GetInputOptionsResponseMessage
	{
		/// <summary>
		/// The set of controls that can be sent in a TBD message to control the current emulator.
		/// </summary>
		public HashSet<string> Inputs { get; set; }

		public GetInputOptionsResponseMessage() { }

		public GetInputOptionsResponseMessage(HashSet<string> inputs)
		{
			Inputs = inputs;
		}
	}
}