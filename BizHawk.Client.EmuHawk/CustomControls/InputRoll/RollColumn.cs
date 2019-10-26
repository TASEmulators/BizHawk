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
	}
}
