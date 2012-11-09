using System;

namespace BizHawk
{
	public interface IOpenFileDialog
	{
		string InitialDirectory {get;set;}
		string Filter {get;set;}
		bool RestoreDirectory {get;set;}
		bool Multiselect {get; set;}
		System.Windows.Forms.DialogResult ShowDialog();
		string FileName {get; set;}
		string[] FileNames {get;}
        int FilterIndex { get; set; }
	}
}

