using System;
using System.Reflection;

namespace NLua.Method
{
	internal class MethodCache
	{
		private MethodBase _cachedMethod;

		public MethodBase CachedMethod
		{
			get => _cachedMethod;
			set
			{
				_cachedMethod = value;
				var mi = value as MethodInfo;

				if (mi != null)
				{
					IsReturnVoid = mi.ReturnType == typeof(void);
				}
			}
		}

		public bool IsReturnVoid;
		public object[] args = Array.Empty<object>(); // List or arguments
		public int[] outList = Array.Empty<int>(); // Positions of out parameters
		public MethodArgs[] argTypes = Array.Empty<MethodArgs>(); // Types of parameters
	}
}
