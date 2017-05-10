using System;

namespace BizHawk.Client.ApiHawk
{
	/// <summary>
	/// This class hold logic interraction for the ExternalToolAttribute
	/// This attribute helps BizHawk to handle ExternalTools
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class BizHawkExternalToolAttribute : Attribute
	{
		#region Fields

		private string _Name;
		private string _Description;
		private string _IconResourceName;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolAttribute"/>
		/// </summary>
		/// <param name="name">Tool's name</param>
		/// <param name="description">Small description about the tool itself</param>
		/// <param name="iconResourceName">Icon embedded resource name</param>
		public BizHawkExternalToolAttribute(string name, string description, string iconResourceName)
		{
			_Name = name;
			_Description = description;
			_IconResourceName = iconResourceName;
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
		public string Name
		{
			get
			{
				return _Name;
			}
		}

		/// <summary>
		/// Gets tool's descriptino
		/// </summary>
		public string Description
		{
			get
			{
				return _Description;
			}
		}

		/// <summary>
		/// Get the name of the embedded resource icon
		/// </summary>
		/// <remarks>Don't forget to set compile => Embedded reource to the icon file in your project</remarks>
		public string IconResourceName
		{
			get
			{
				return _IconResourceName;
			}
		}

		#endregion
	}
}
