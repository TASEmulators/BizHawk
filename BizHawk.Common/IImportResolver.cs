using System;
using System.Collections.Generic;

namespace BizHawk.Common
{
	/// <summary>
	/// interface for a dynamic link library or similar
	/// </summary>
	public interface IImportResolver
	{
		IntPtr Resolve(string entryPoint);
	}

	public static class ImportResolverExtensions
	{
		/// <summary>
		/// Resolve an entry point and throw an exception if that resolution is NULL
		/// </summary>
		/// <param name="dll"></param>
		/// <param name="entryPoint"></param>
		/// <returns></returns>
		public static IntPtr SafeResolve(this IImportResolver dll, string entryPoint)
		{
			var ret = dll.Resolve(entryPoint);
			if (ret == IntPtr.Zero)
			{
				throw new NullReferenceException($"Couldn't resolve entry point \"{entryPoint}\"");
			}

			return ret;
		}
	}

    /// <summary>
    /// compose multiple ImportResolvers, where subsequent ones takes precedence over earlier ones
    /// </summary>
    public class PatchImportResolver : IImportResolver
    {
        private readonly List<IImportResolver> _resolvers = new List<IImportResolver>();

        public PatchImportResolver(params IImportResolver[] rr)
        {
            Add(rr);
        }
        public void Add(params IImportResolver[] rr)
        {
            _resolvers.AddRange(rr);
        }

        public IntPtr Resolve(string entryPoint)
        {
            for (int i = _resolvers.Count - 1; i >= 0; i--)
            {
                var ret = _resolvers[i].Resolve(entryPoint);
                if (ret != IntPtr.Zero)
                    return ret;
            }
            return IntPtr.Zero;
        }
    }
}
