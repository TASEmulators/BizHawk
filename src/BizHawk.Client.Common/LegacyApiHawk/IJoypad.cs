#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	[LegacyApiHawk]
	public interface IJoypad : IExternalApi
	{
		[LegacyApiHawk]
		IDictionary<string, object> Get(int? controller = null);

		[LegacyApiHawk]
		IDictionary<string, object> GetImmediate(int? controller = null);

		[LegacyApiHawk]
		void Set(Dictionary<string, bool> buttons, int? controller = null);

		[LegacyApiHawk]
		void Set(string button, bool? state = null, int? controller = null);

		[LegacyApiHawk]
		void SetAnalog(Dictionary<string, float> controls, object? controller = null);

		[LegacyApiHawk]
		void SetAnalog(string control, float? value = null, object? controller = null);

		[LegacyApiHawk]
		void SetFromMnemonicStr(string inputLogEntry);
	}
}
