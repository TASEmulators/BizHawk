using System.Collections.Generic;

namespace BizHawk.API.ApiHawk
{
	/// <remarks>
	/// Changes from 2.4.2:
	/// <list type="bullet">
	/// <item><description>return type of method <c>IDictionary&lt;string, object> Get(int? controller = null)</c> changed to <c>IReadOnlyDictionary&lt;string, object></c> (read-only)</description></item>
	/// <item><description>return type of method <c>IDictionary&lt;string, object> GetImmediate(int? controller = null)</c> changed to <c>IReadOnlyDictionary&lt;string, object></c> (read-only)</description></item>
	/// <item><description>type of first parameter of method <c>void Set(IDictionary&lt;string, bool> buttons, int? controller = null)</c> changed to <c>IReadOnlyDictionary&lt;string, bool></c> (read-only)</description></item>
	/// <item><description>types of parameters of method <c>void SetAnalog(IDictionary&lt;string, float> controls, object? controller = null)</c> changed to <c>IReadOnlyDictionary&lt;string, int>, int?</c> (dict: read-only, and float->int; controller: object?->int?)</description></item>
	/// <item><description>types of parameters of method <c>void SetAnalog(string control, float? value = null, object? controller = null)</c> changed to <c>(string, int?, int?)</c></description></item>
	/// <item><description>method <c>void Set(string button, bool? state = null, int? controller = null)</c> unchanged</description></item>
	/// <item><description>method <c>void SetFromMnemonicStr(string inputLogEntry)</c> unchanged</description></item>
	/// </list>
	/// </remarks>
	public interface IVirtualInputLib
	{
		IReadOnlyDictionary<string, object> Get(int? controller = null);

		IReadOnlyDictionary<string, object> GetImmediate(int? controller = null);

		void Set(IReadOnlyDictionary<string, bool> buttons, int? controller = null);

		void Set(string button, bool? state = null, int? controller = null);

		void SetAnalog(IReadOnlyDictionary<string, int> controls, int? controller = null);

		void SetAnalog(string control, int? value = null, int? controller = null);

		void SetFromMnemonicStr(string inputLogEntry);
	}
}
