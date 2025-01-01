namespace BizHawk.Client.Common.Websocket.Messages
{
	/// <summary>
	/// Request message for custom subtopics coming from e.g. lua scripts.
	/// 
	/// Register for <see cref="Topic.Custom"/>
	/// </summary>
	/// <seealso cref="CustomResponseMessage"/>
	public struct CustomRequestMessage
	{

		/// <summary>
		/// Sub-topic defined by the custom handler.
		/// </summary>
		public string SubTopic { get; set; }
		
		/// <summary>
		/// Message body. Should be a valid string representation of whatever data are needed by
		/// the custom handler, such as a JSON-encoded object (which would be double-encoded when serialized).
		/// </summary>
		public string Message { get; set; }

		public CustomRequestMessage() { }

		public CustomRequestMessage(string subTopic, string message)
		{
			SubTopic = subTopic;
			Message = message;
		}
	}

	/// <summary>
	/// Response message for custom subtopics coming from e.g. lua scripts.
	/// 
	/// Register for <see cref="Topic.Custom"/>
	/// </summary>
	/// <seealso cref="CustomRequestMessage"/>
	public struct CustomResponseMessage
	{
		/// <summary>
		/// Sub-topic defined by the custom handler.
		/// </summary>
		public string SubTopic { get; set; }
		
		/// <summary>
		/// Message body. Should be a valid string representation of whatever data are needed by
		/// the client, such as a JSON-encoded object (which would be double-encoded when serialized).
		/// </summary>
		public string Message { get; set; }

		public CustomResponseMessage() { }

		public CustomResponseMessage(string subTopic, string message)
		{
			SubTopic = subTopic;
			Message = message;
		}
	}
}