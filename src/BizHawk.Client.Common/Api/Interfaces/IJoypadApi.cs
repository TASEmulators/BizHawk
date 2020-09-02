using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IJoypadApi : IExternalApi
	{
		IDictionary<string, object> Get(int? controller = null);
		IDictionary<string, object> GetImmediate(int? controller = null);
		void SetFromMnemonicStr(string inputLogEntry);
		void Set(IDictionary<string, bool> buttons, int? controller = null);
		void Set(string button, bool? state = null, int? controller = null);
		void SetAnalog(IDictionary<string, int?> controls, object controller = null);
		void SetAnalog(string control, int? value = null, object controller = null);
	}
}
