#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IExternalApiProvider
	{
		/// <returns>a list of all currently registered <see cref="IExternalApi">APIs</see> that are available</returns>
		IReadOnlyCollection<Type> AvailableApis { get; }

		ApiContainer Container { get; }

		/// <returns>an instance of the <see cref="IExternalApi"/> <paramref name="t"/> iff available else <see langword="null"/></returns>
		object? GetApi(Type t);

		/// <returns><see langword="true"/> iff an instance of the <see cref="IExternalApi"/> <paramref name="t"/> is available</returns>
		bool HasApi(Type t);
	}
}
