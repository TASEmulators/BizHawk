using System;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class hold logic interraction for the BizHawkExternalToolUsageAttribute
	/// This attribute helps ApiHawk to know how a tool can be enabled or not
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class BizHawkExternalToolUsageAttribute : Attribute
	{
		#region Fields

		private BizHawkExternalToolUsage _ToolUsage;
		private string _Parameter;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolUsageAttribute"/>
		/// </summary>
		/// <param name="usage"><see cref="BizHawkExternalToolUsage"/> i.e. what your external tool is for</param>
		/// <param name="parameter">The parameter; either emulator type or game hash depending of what you want to do</param>
		public BizHawkExternalToolUsageAttribute(BizHawkExternalToolUsage usage, string parameter)
		{
			_ToolUsage = usage;
			if(usage != BizHawkExternalToolUsage.Global && parameter.Trim() == string.Empty)
			{
				throw new InvalidOperationException("You must specify the parameter. Either emulator type or game hash depending of what you want to do");
			}
			_Parameter = parameter;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolUsageAttribute"/>
		/// </summary>
		public BizHawkExternalToolUsageAttribute()
			:this(BizHawkExternalToolUsage.Global, string.Empty)
		{ }


		#endregion

		#region Properties

		/// <summary>
		/// Gets the tool usage
		/// </summary>
		public BizHawkExternalToolUsage ToolUsage
		{
			get
			{
				return _ToolUsage;
			}
		}

		/// <summary>
		/// Gets the parameter (Emulator or Game hash)
		/// </summary>
		public string Parameter
		{
			get
			{
				return _Parameter;
			}
		}

		#endregion
	}
}
