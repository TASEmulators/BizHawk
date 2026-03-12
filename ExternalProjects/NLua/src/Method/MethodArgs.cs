using System;

namespace NLua.Method
{
	/// <summary>
	/// Parameter information
	/// </summary>
	internal class MethodArgs
	{
		// Position of parameter
		public int Index;
		public Type ParameterType;

		// Type-conversion function
		public ExtractValue ExtractValue;
		public bool IsParamsArray;

		public bool IsNilAllowed;
	}
}