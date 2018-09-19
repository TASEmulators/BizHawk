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
		/// Initializes a new instance of the <see cref="SeparatorWatch"/> class.
		/// </summary>
		internal SeparatorWatch()
			: base(null, 0, WatchSize.Separator, DisplayType.Separator, true, "")
		{
		}

		/// <summary>
		/// Gets the separator instance
		/// </summary>
		public static SeparatorWatch Instance => new SeparatorWatch();

        public static SeparatorWatch NewSeparatorWatch(string description)
        {
            return new SeparatorWatch
            {
                Notes = description
            };
        }

		/// <summary>
		/// Get the appropriate DisplayType
		/// </summary>
		/// <returns>DisplayType.Separator nothing else</returns>
		public override IEnumerable<DisplayType> AvailableTypes()
		{
			yield return DisplayType.Separator;
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override int Value => 0;

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override int ValueNoFreeze => 0;

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override int Previous => 0;

        /// <summary>
        /// Ignore that stuff
        /// </summary>
        public override string ValueString => Notes; //"";

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override string PreviousStr => "";

		/// <summary>
		/// TTransform the current instance into a displayable (short representation) string
		/// It's used by the "Display on screen" option in the RamWatch window
		/// </summary>
		/// <returns>A well formatted string representation</returns>
		public override string ToDisplayString()
		{
            if (Notes == null || Notes == "")
                return "----";
            else
                return Notes;			
		}

        /// <summary>
		/// Transforms the current instance into a string
		/// </summary>
		/// <returns>A <see cref="string"/> representation of the current <see cref="Watch"/></returns>
		public override string ToString()
        {
            return $"0\tS\t_\t1\t\t{Notes.Trim('\r', '\n')}";
            //return $"{(Domain == null && Address == 0 ? "0" : Address.ToHexString((Domain?.Size ?? 0xFF - 1).NumHexDigits()))}\t{SizeAsChar}\t{TypeAsChar}\t{Convert.ToInt32(BigEndian)}\t{Domain?.Name}\t{Notes.Trim('\r', '\n')}";
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
		}

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override string Diff => "";

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override uint MaxValue => 0;

		/// <summary>
		/// Ignore that stuff
		/// </summary>
		public override void Update()
		{
            //AddressString = (Notes == null || Notes == "") ? "" : Notes;

        }
	}
}
