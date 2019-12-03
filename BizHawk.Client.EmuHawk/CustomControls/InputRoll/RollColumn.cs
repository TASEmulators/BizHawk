namespace BizHawk.Client.EmuHawk
{
	public class RollColumn
	{
		public string Group { get; set; }
		public int? Width { get; set; }
		public int? Left { get; set; }
		public int? Right { get; set; }
		public string Name { get; set; }
		public string Text { get; set; }
		public ColumnType Type { get; set; }
		public bool Visible { get; set; } = true;

		/// <summary>
		/// Column will be drawn with an emphasized look, if true
		/// </summary>
		public bool Emphasis { get; set; }

		/// <summary>
		/// Column header text will be drawn rotated, if true
		/// </summary>
		public bool Rotatable { get; set; }

		/// <summary>
		/// If drawn rotated, specifies the desired height, or null to auto-size
		/// </summary>
		public int? RotatedHeight { get; set; }
	}
}
