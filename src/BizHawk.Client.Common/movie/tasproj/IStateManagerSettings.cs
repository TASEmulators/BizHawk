namespace BizHawk.Client.Common
{
	public interface IStateManagerSettings
	{
		IStateManager CreateManager(Func<int, bool> reserveCallback);

		IStateManagerSettings Clone();
	}
}
