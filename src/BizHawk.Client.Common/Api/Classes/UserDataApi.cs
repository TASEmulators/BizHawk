using System.Collections.Generic;
using System.Linq;

#if NET5_0_OR_GREATER
using KeyCollectionType = System.Collections.Generic.IReadOnlySet<string>;
#else
using KeyCollectionType = System.Collections.Generic.IReadOnlyCollection<string>;
#endif

namespace BizHawk.Client.Common
{
	public sealed class UserDataApi : IUserDataApi
	{
		private readonly IMovieSession _movieSession;

		public KeyCollectionType Keys
		{
			get
			{
				ICollection<string> keys = _movieSession.UserBag.Keys;
				return (keys as KeyCollectionType) ?? keys.ToList();
			}
		}

		public UserDataApi(IMovieSession movieSession) => _movieSession = movieSession;

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
