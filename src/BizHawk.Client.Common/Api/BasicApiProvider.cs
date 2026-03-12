#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class BasicApiProvider : IExternalApiProvider
	{
		public IReadOnlyCollection<Type> AvailableApis => Container.Libraries.Keys.ToList();

		public ApiContainer Container { get; }

		public BasicApiProvider(ApiContainer apiContainer) => Container = apiContainer;

		public object? GetApi(Type t)
			=> Container.Libraries.TryGetValue(t, out var api)
				? api
				: t == typeof(ApiContainer)
					? Container
					: null;

		public bool HasApi(Type t) => Container.Libraries.ContainsKey(t);
	}
}
