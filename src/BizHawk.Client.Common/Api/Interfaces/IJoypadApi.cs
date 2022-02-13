using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>for querying or modifying virtual input</summary>
	/// <seealso cref="IInputApi"/>
	public interface IJoypadApi : IExternalApi
	{
		IReadOnlyDictionary<string, object> Get(int? controller = null);
		IReadOnlyDictionary<string, object> GetWithMovie(int? controller = null);
		IReadOnlyDictionary<string, object> GetImmediate(int? controller = null);
		void SetFromMnemonicStr(string inputLogEntry);
		void Set(IReadOnlyDictionary<string, bool> buttons, int? controller = null);
		void Set(string button, bool? state = null, int? controller = null);
		void SetAnalog(IReadOnlyDictionary<string, int?> controls, int? controller = null);
		void SetAnalog(string control, int? value = null, int? controller = null);
	}
}
