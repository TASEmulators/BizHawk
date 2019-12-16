using System;

namespace BizHawk.Client.Common
{
	public sealed class UserDataApi : IUserData
	{
		public void Set(string name, object value)
		{
			if (value != null)
			{
				var t = value.GetType();
				if (!t.IsPrimitive && t != typeof(string)) throw new InvalidOperationException("Invalid type for userdata");
			}
			Global.UserBag[name] = value;
		}

		public object Get(string key) => Global.UserBag.TryGetValue(key, out var value) ? value : null;

		public void Clear() => Global.UserBag.Clear();

		public bool Remove(string key) => Global.UserBag.Remove(key);

		public bool ContainsKey(string key) => Global.UserBag.ContainsKey(key);
	}
}
