using System;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class holds logic interaction for the ExternalToolAttribute
	/// This attribute helps BizHawk to handle ExternalTools
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class BizHawkExternalToolAttribute : Attribute
	{
		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolAttribute"/>
		/// </summary>
		/// <param name="name">Tool's name</param>
		/// <param name="description">Small description about the tool itself</param>
		/// <param name="iconResourceName">Icon embedded resource name</param>
		public BizHawkExternalToolAttribute(string name, string description, string iconResourceName)
		{
			Name = name;
			Description = description;
			IconResourceName = iconResourceName;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolAttribute"/>
		/// </summary>
		/// <param name="name">Tool's name</param>
		/// <param name="description">Small description about the tool itself</param>
		public BizHawkExternalToolAttribute(string name, string description)
			: this(name, description, "")
		{ }

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolAttribute"/>
		/// </summary>
		/// <param name="name">Tool's name</param>
		public BizHawkExternalToolAttribute(string name)
			:this(name, "", "")
		{}

		#endregion

		#region Properties

		/// <summary>
		/// Gets tool's friendly name
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets tool's description
		/// </summary>
		public string Description { get; }
		

		/// <summary>
		/// Get the name of the embedded resource icon
		/// </summary>
		/// <remarks>Don't forget to set compile => Embedded resource to the icon file in your project</remarks>
		public string IconResourceName { get; }

		#endregion
	}
}
