#nullable enable

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BizHawk.Client.Common.Websocket.Messages
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum Topic : ushort
	{
		/// <summary>
		/// Topic for general errors. Some errors may be client-specific, such as when
		/// a request was made that could not be assigned to a topic.
		/// </summary>
		Error = 0,

		/// <summary>
		/// Topic for registration requests.
		/// </summary>
		/// <see cref="RegistrationRequestMessage"/>
		/// <see cref="RegistrationResponseMessage"/>
		Registration = 1,

		/// <summary>
		/// This is a client-specific topic, i.e. only the client who sent the message
		/// will receive the echoed response.
		/// </summary>
		/// <see cref="EchoRequestMessage"/>
		/// <see cref="EchoResponseMessage"/>
		Echo = 2,

		GetInputOptions = 3,
	}
}