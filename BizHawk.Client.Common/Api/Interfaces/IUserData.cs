namespace BizHawk.Client.Common
{
	public interface IUserData : IExternalApi
	{
		object this[string key] { get; set; }

		void Clear();

		bool ContainsKey(string key);

		bool Remove(string key);
	}
}
