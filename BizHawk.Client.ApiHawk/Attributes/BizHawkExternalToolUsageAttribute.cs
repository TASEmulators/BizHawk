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
		private CoreSystem _System;
		private string _GameHash;

		#endregion

		#region cTor(s)

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolUsageAttribute"/>
		/// </summary>
		/// <param name="usage"><see cref="BizHawkExternalToolUsage"/> i.e. what your external tool is for</param>
		/// <param name="system"><see cref="CoreSystem"/> that your external tool is used for</param>
		/// <param name="gameHash">The game hash, unique game ID (see in the game database)</param>
		public BizHawkExternalToolUsageAttribute(BizHawkExternalToolUsage usage, CoreSystem system, string gameHash)
		{
			if (usage == BizHawkExternalToolUsage.EmulatorSpecific && system == CoreSystem.Null)
			{
				throw new InvalidOperationException("A system must be set");
			}
			if (usage == BizHawkExternalToolUsage.GameSpecific && gameHash.Trim() == string.Empty)
			{
				throw new InvalidOperationException("A game hash must be set");
			}

			_ToolUsage = usage;			
			_System = system;
			_GameHash = gameHash;
		}

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolUsageAttribute"/>
		/// </summary>
		/// <param name="usage"><see cref="BizHawkExternalToolUsage"/> i.e. what your external tool is for</param>
		/// <param name="system"><see cref="CoreSystem"/> that your external tool is used for</param>		
		public BizHawkExternalToolUsageAttribute(BizHawkExternalToolUsage usage, CoreSystem system)
			:this(usage, system, string.Empty)
		{}

		/// <summary>
		/// Initialize a new instance of <see cref="BizHawkExternalToolUsageAttribute"/>
		/// </summary>
		public BizHawkExternalToolUsageAttribute()
			:this(BizHawkExternalToolUsage.Global, CoreSystem.Null, string.Empty)
		{ }


		#endregion

		#region Properties

		/// <summary>
		/// Gets the specific system used by the exetrnal tool
		/// </summary>
		public CoreSystem System
		{
			get
			{
				return _System;
			}
		}

		/// <summary>
		/// Gets the specific game (hash) used by the exetrnal tool
		/// </summary>
		public string GameHash
		{
			get
			{
				return _GameHash;
			}
		}

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

		#endregion
	}
}
