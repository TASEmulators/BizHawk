using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
		public static IntPtr SafeResolve(this IImportResolver dll, string entryPoint)
		{
			var ret = dll.Resolve(entryPoint);
			if (ret == IntPtr.Zero)
				throw new NullReferenceException(string.Format("Couldn't resolve entry point \"{0}\"", entryPoint));
			return ret;
		}
	}
}
