using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class holds a separator for RamWatch
	/// Use the static property Instance to get it
	/// </summary>
	public sealed class SeparatorWatch : Watch
	{
		/// <summary>
		/// Initialize a new separator instance
		/// </summary>
		internal SeparatorWatch()
			:base(null, 0, WatchSize.Separator, DisplayType.Separator, true, string.Empty)
		{ }

		/// <summary>
		/// Gets the separator instance
		/// </summary>
		public static SeparatorWatch Instance
		{
			get { return new SeparatorWatch(); }
		}

		/// <summary>
		/// Get the appropriate DisplayType
		/// </summary>
		/// <returns>DisplayType.Separator nothing else</returns>
		public override IEnumerable<DisplayType> AvailableTypes()
		{
			yield return DisplayType.Separator;
		}

		#region Stuff to ignore

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override int Value
		{
			get { return 0; }
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override int ValueNoFreeze
		{
			get { return 0; }
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override int Previous
		{
			get { return 0; }
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override string ValueString
		{
			get { return string.Empty; }
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override string PreviousStr
		{
			get { return string.Empty; }
		}

		/// <summary>
		/// TTransform the current instance into a displayable (short representation) string
		/// It's used by the "Display on screen" option in the RamWatch window
		/// </summary>
		/// <returns>A well formatted string representation</returns>
		public override string ToDisplayString()
		{
			return "----";
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override bool Poke(string value)
		{
			return false;
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override void ResetPrevious()
		{
			return;
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override string Diff { get { return string.Empty; } }

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override uint MaxValue
		{
			get { return 0; }
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override void Update() { return; }

		#endregion
	}
}
