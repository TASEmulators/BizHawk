using System;

namespace BizHawk.Client.Common
{
	public sealed class UserDataApi : IUserData
	{
		/// <exception cref="InvalidOperationException">(from setter) type of <paramref name="value"/> cannot be used in userdata</exception>
		public object this[string key]
		{
			get => Global.UserBag.TryGetValue(key, out var value) ? value : null;
			set
			{
				if (value != null)
				{
					var t = value.GetType();
					if (!t.IsPrimitive && t != typeof(string)) throw new InvalidOperationException("Invalid type for userdata");
				}
				Global.UserBag[key] = value;
			}
		}

		public void Clear() => Global.UserBag.Clear();

		public bool ContainsKey(string key) => Global.UserBag.ContainsKey(key);

		public bool Remove(string key) => Global.UserBag.Remove(key);
	}
}
