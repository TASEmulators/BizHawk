using System;
using System.ComponentModel;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class UserDataPluginLibrary
	{
		public UserDataPluginLibrary()
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
			if (Global.UserBag.ContainsKey(key))
			{
				return Global.UserBag[key];
			}

			return null;
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
