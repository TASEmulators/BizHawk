using System;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class UserDataApi : IUserDataApi
	{
		private readonly IMovieSession _movieSession;

		public UserDataApi(Action<string> logCallback, IMovieSession movieSession)
		{
			_movieSession = movieSession;
		}

		/// <exception cref="InvalidOperationException">type of <paramref name="value"/> cannot be used in userdata</exception>
		public void Set(string name, object value)
		{
			if (value != null)
			{
				var t = value.GetType();
				if (!t.IsPrimitive && t != typeof(string)) throw new InvalidOperationException("Invalid type for userdata");
			}
			_movieSession.UserBag[name] = value;
		}

		public object Get(string key) => _movieSession.UserBag.TryGetValue(key, out var value) ? value : null;

		public void Clear() => _movieSession.UserBag.Clear();

		public bool Remove(string key) => _movieSession.UserBag.Remove(key);

		public bool ContainsKey(string key) => _movieSession.UserBag.ContainsKey(key);
	}
}
