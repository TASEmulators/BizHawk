using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// A generic implementation of IExternalApi provider that provides
	/// this functionality to any core.
	/// The provider will scan an IExternal and register all IExternalApis
	/// that the core object itself implements.  In addition it provides
	/// a Register() method to allow the core to pass in any additional Apis
	/// </summary>
	/// <seealso cref="IExternalApiProvider"/> 
	public class BasicApiProvider : IExternalApiProvider
	{
		private readonly Dictionary<Type, IExternalApi> _apis = new Dictionary<Type, IExternalApi>();

		public BasicApiProvider(IApiContainer container)
		{
			// simplified logic here doesn't scan for possible Apis; just adds what it knows is implemented by the PluginApi
			// this removes the possibility of automagically picking up a Api in a nested class, (find the type, then
			// find the field), but we're going to keep such logic out of the basic provider.  Anything the passed
			// container doesn't implement directly needs to be added with Register()
			// this also fully allows apis that are not IExternalApi
			var libs = container.Libraries;

			_apis = libs;
		}

		/// <summary>
		/// the client can call this to register an additional Api
		/// </summary>
		/// <typeparam name="T">The <seealso cref="IExternalApi"/> to register</typeparam>
		public void Register<T>(T api)
			where T : IExternalApi
		{
			if (api == null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			_apis[typeof(T)] = api;
		}

		public T GetApi<T>()
			where T : IExternalApi
		{
			return (T)GetApi(typeof(T));
		}

		public object GetApi(Type t)
		{
			KeyValuePair<Type, IExternalApi>[] k = _apis.Where(kvp => t.IsAssignableFrom(kvp.Key)).ToArray();
			if (k.Length > 0)
			{
				return k[0].Value;
			}

			return null;
		}

		public bool HasApi<T>()
			where T : IExternalApi
		{
			return HasApi(typeof(T));
		}

		public bool HasApi(Type t)
		{
			return _apis.ContainsKey(t);
		}

		public IEnumerable<Type> AvailableApis => _apis.Select(d => d.Key);
	}
}
