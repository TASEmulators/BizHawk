using System.Collections.Generic;

namespace BizHawk.API.ApiHawk
{
	/// <remarks>
	/// Changes from 2.4.2:
	/// <list type="bullet">
	/// <item><description>return type of method <c>Dictionary&lt;string, bool> Get()</c> changed to <c>IReadOnlyDictionary&lt;string, bool></c></description></item>
	/// <item><description>return type of method <c>Dictionary&lt;string, object> GetMouse()</c> changed to <c>IReadOnlyDictionary&lt;string, object></c></description></item>
	/// </list>
	/// </remarks>
	public interface IHostInputLib
	{
		IReadOnlyDictionary<string, bool> Get();

		IReadOnlyDictionary<string, object> GetMouse();
	}
}
