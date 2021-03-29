#nullable enable

using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public struct FeedbackBind
	{
		public string? Channel;

		/// <remarks>"X# "/"J# " (with the trailing space)</remarks>
		public string? GamepadPrefix;

		[JsonIgnore]
		public bool IsZeroed => GamepadPrefix == null;

		public float Prescale;

		public FeedbackBind(string prefix, string channel, float prescale)
		{
			GamepadPrefix = prefix;
			Channel = channel;
			Prescale = prescale;
		}
	}
}
