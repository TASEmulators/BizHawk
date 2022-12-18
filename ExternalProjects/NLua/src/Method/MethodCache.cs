using System;
using System.Reflection;

namespace NLua.Method
{
	internal class MethodCache
	{
		public MethodCache()
		{
			args = Array.Empty<object>();
			argTypes = Array.Empty<MethodArgs>();
			outList = Array.Empty<int>();
		}
		private MethodBase _cachedMethod;

		public MethodBase cachedMethod {
			get
			{
				return _cachedMethod;
			}
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
		// List or arguments
		public object[] args;
		// Positions of out parameters
		public int[] outList;
		// Types of parameters
		public MethodArgs[] argTypes;
	}
}
