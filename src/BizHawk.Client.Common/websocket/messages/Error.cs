namespace BizHawk.Client.Common.Websocket.Messages
{

	/// <summary>
	/// Error message sent from the server for <see cref="Topic.Error"/>
	/// </summary>
	public struct ErrorMessage
	{
		public ErrorType Type { get; set; }

		public string? Message { get; set; }

		public ErrorMessage() { }

		public ErrorMessage(ErrorType type)
		{
			Type = type;
		}

		public ErrorMessage(ErrorType type, string message)
		{
			Type = type;
			Message = message;
		}
	}

	public enum ErrorType : ushort
	{
		UnknownRequest = 0
	}
}