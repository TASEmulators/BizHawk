using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IJoypadApi : IExternalApi
	{
		IDictionary<string, object> Get(int? controller = null);
		IDictionary<string, object> GetImmediate(int? controller = null);
		void SetFromMnemonicStr(string inputLogEntry);
		void Set(Dictionary<string, bool> buttons, int? controller = null);
		void Set(string button, bool? state = null, int? controller = null);
		void SetAnalog(Dictionary<string, float> controls, object controller = null);
		void SetAnalog(string control, float? value = null, object controller = null);
	}
}
