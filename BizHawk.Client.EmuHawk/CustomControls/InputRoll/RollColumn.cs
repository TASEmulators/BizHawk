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
		public bool Visible { get; set; }

		/// <summary>
		/// Column will be drawn with an emphasized look, if true
		/// </summary>
		private bool _emphasis;
		public bool Emphasis
		{
			get { return _emphasis; }
			set { _emphasis = value; }
		}

		public RollColumn()
		{
			Visible = true;
		}
	}
}
