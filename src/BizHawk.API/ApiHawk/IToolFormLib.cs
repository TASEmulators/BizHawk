namespace BizHawk.API.ApiHawk
{
	/// <remarks>
	/// Changes from 2.4.2:
	/// <list type="bullet">
	/// <item><description>all methods removed</description></item>
	/// <item><description>method <c>bool EnsureToolLoaded(string typeName)</c> added</description></item>
	/// </list>
	/// </remarks>
	public interface IToolFormLib
	{
		bool EnsureToolLoaded(string typeName);
	}
}
