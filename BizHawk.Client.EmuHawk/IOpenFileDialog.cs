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
		int FilterIndex { get; set; }
		System.Windows.Forms.DialogResult ShowDialog();
	}

	public interface IHasShowDialog
	{
		System.Windows.Forms.DialogResult ShowDialog();
	}
}
