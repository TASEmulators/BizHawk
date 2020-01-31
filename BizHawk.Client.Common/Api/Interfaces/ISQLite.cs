namespace BizHawk.Client.Common
{
	public interface ISQLite : IExternalApi
	{
		string CreateDatabase(string name);

		string ExecCommand(string query = null);

		dynamic ExecCommandWithResult(string query = null);

		string OpenDatabase(string name);
	}
}
