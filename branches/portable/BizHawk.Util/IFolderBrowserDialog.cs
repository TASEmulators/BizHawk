using System;

namespace BizHawk
{
	public interface IFolderBrowserDialog
	{
		string Description {get; set;}
		string SelectedPath {get; set;}
		System.Windows.Forms.DialogResult ShowDialog();

	}
}

