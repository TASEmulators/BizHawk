using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class SeparatorWatch : Watch
	{
		internal SeparatorWatch()
			:base(null, 0, WatchSize.Separator, DisplayType.Separator, true, string.Empty)
		{ }

		public static SeparatorWatch Instance
		{
			get { return new SeparatorWatch(); }
		}

		public override int Value
		{
			get { return 0; }
		}

		public override int ValueNoFreeze
		{
			get { return 0; }
		}

		public override int Previous
		{
			get { return 0; }
		}		

		public override string ValueString
		{
			get { return string.Empty; }
		}

		public override string PreviousStr
		{
			get { return string.Empty; }
		}

		public override string ToString()
		{
			return "----";
		}								

		public override bool Poke(string value)
		{
			return false;
		}

		public override void ResetPrevious()
		{
			return;
		}

		public override string Diff { get { return string.Empty; } }

		public override uint MaxValue
		{
			get { return 0; }
		}

		public override void Update() { return; }

		public override IEnumerable<DisplayType> AvailableTypes()
		{
			yield return DisplayType.Separator;
		}
	}
}
