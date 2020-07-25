#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class BasicApiProvider : IExternalApiProvider
	{
		private readonly IReadOnlyDictionary<Type, IExternalApi> _libs;

		public IReadOnlyCollection<Type> AvailableApis => _libs.Keys.ToList();

		public BasicApiProvider(ApiContainer apiContainer) => _libs = apiContainer.Libraries;

		public object? GetApi(Type t) => _libs.TryGetValue(t, out var api) ? api : null;

		public bool HasApi(Type t) => _libs.ContainsKey(t);
	}
}
