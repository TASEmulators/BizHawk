using System;
using BizHawk.Client.Common;

namespace BizHawk.Client.ApiHawk
{
	public sealed class UserDataApi : IUserData
	{
		public UserDataApi() : base()
		{ }

		public void Set(string name, object value)
		{
			if (value != null)
			{
				var t = value.GetType();
				if (!t.IsPrimitive && t != typeof(string))
				{
					throw new InvalidOperationException("Invalid type for userdata");
				}
			}

			Global.UserBag[name] = value;
		}

		public object Get(string key)
		{
			return Global.UserBag.ContainsKey(key)
				? Global.UserBag[key]
				: null;
		}

		public void Clear()
		{
			Global.UserBag.Clear();
		}

		public bool Remove(string key)
		{
			return Global.UserBag.Remove(key);
		}

		public bool ContainsKey(string key)
		{
			return Global.UserBag.ContainsKey(key);
		}
	}
}
