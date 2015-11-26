using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class SeparatorWatch : Watch
	{
		public static SeparatorWatch Instance
		{
			get { return new SeparatorWatch(); }
		}

		public override long? Address
		{
			get { return null; }
		}

		public override int? Value
		{
			get { return null; }
		}

		public override int? ValueNoFreeze
		{
			get { return null; }
		}

		public override int? Previous
		{
			get { return null; }
		}

		public override string AddressString
		{
			get { return string.Empty; }
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

		public override bool IsSeparator
		{
			get { return true; }
		}

		public override WatchSize Size
		{
			get { return WatchSize.Separator; }
		}

		public static List<DisplayType> ValidTypes
		{
			get { return new List<DisplayType> { DisplayType.Separator }; }
		}

		public override DisplayType Type
		{
			get { return DisplayType.Separator; }
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
	}
}
