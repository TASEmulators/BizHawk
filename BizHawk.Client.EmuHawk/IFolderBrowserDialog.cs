using System;

namespace BizHawk.Client.EmuHawk
{
	public interface IFolderBrowserDialog : IHasShowDialog
	{
		string Description { get; set; }
		string SelectedPath { get; set; }
		System.Windows.Forms.DialogResult ShowDialog();
	}
}
