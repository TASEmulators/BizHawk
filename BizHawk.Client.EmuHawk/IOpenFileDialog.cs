using System;

namespace BizHawk.Client.EmuHawk
{
	public interface IOpenFileDialog : IDisposable, IHasShowDialog
	{
		string InitialDirectory { get; set; }
		string Filter { get; set; }
		bool RestoreDirectory { get; set; }
		bool Multiselect { get; set; }
		bool AddExtension { get; set; }
		string FileName { get; set; }
		string[] FileNames { get; }
		string Title { get; set; }
		int FilterIndex { get; set; }
		System.Windows.Forms.DialogResult ShowDialog(System.Windows.Forms.Form form);
	}

	public interface ISaveFileDialog : IOpenFileDialog
	{
		string DefaultExt { get; set; }
		bool OverwritePrompt { get; set; }
	}

	public interface IHasShowDialog
	{
		System.Windows.Forms.DialogResult ShowDialog();
	}
}
