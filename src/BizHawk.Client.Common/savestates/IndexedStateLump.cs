namespace BizHawk.Client.Common
{
	/// <summary>
	/// describes a BinaryStateLump virtual name that has a numerical index
	/// </summary>
	public class IndexedStateLump : BinaryStateLump
	{
		private readonly BinaryStateLump _root;
		private int _idx;
		public IndexedStateLump(BinaryStateLump root)
		{
			_root = root;
			Ext = _root.Ext;
			Calc();
		}

		private void Calc()
		{
			Name = _root.Name + _idx;
		}

		public void Increment()
		{
			_idx++;
			Calc();
		}
	}
}
