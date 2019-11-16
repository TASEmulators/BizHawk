namespace BizHawk.Client.Common
{
	public interface IUserData : IExternalApi
	{
		void Set(string name, object value);
		object Get(string key);
		void Clear();
		bool Remove(string key);
		bool ContainsKey(string key);
	}
}
