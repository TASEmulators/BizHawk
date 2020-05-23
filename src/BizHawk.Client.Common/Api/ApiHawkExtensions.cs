#nullable enable

namespace BizHawk.Client.Common
{
	public static class ApiHawkExtensions
	{
		/// <returns>an instance of the <see cref="IExternalApi"/> <typeparamref name="T"/> iff available else <see langword="null"/></returns>
		public static T? GetApi<T>(this IExternalApiProvider apiProvider)
			where T : class, IExternalApi
			=> apiProvider.GetApi(typeof(T)) as T;

		/// <returns><see langword="true"/> iff an instance of the <see cref="IExternalApi"/> <typeparamref name="T"/> is available</returns>
		public static bool HasApi<T>(this IExternalApiProvider apiProvider)
			where T : class, IExternalApi
			=> apiProvider.HasApi(typeof(T));
	}
}
