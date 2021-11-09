#nullable enable

namespace BizHawk.Client.Common
{
	public interface IAutofireConfigDialogViewAdapter
	{
		public bool ConsiderLag { get; set; }

		public int PatternOff { get; set; }

		public int PatternOn { get; set; }
	}
}
