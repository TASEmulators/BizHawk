namespace BizHawk.Client.Common
{
	public interface ISqlApi : IExternalApi
	{
		string CreateDatabase(string name);
		string OpenDatabase(string name);
		string WriteCommand(string query = "");
		object ReadCommand(string query = "");
	}
}
