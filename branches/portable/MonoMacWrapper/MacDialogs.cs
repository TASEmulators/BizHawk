using System;
using MonoMac.AppKit;

namespace MonoMacWrapper
{
	public class MacOpenFileDialog : BizHawk.IOpenFileDialog
	{
		private MonoMac.AppKit.NSOpenPanel _openPanel;
		
		public MacOpenFileDialog()
		{
			_openPanel = new NSOpenPanel();
		}
		
		public System.Windows.Forms.DialogResult ShowDialog()
		{
			if(_openPanel.RunModal() == 1)
			{
				return System.Windows.Forms.DialogResult.OK;
			}
			return System.Windows.Forms.DialogResult.Cancel;
		}

		public string InitialDirectory 
		{
			get 
			{
				return _openPanel.Directory;
			}
			set 
			{
				_openPanel.Directory = value;
			}
		}

		public string Filter 
		{
			get 
			{
				return string.Empty; //Todo
			}
			set 
			{
				//Todo
			}
		}

		public bool RestoreDirectory 
		{
			get 
			{
				return true; //This feature is built into NSOpenPanel somehow
			}
			set 
			{
				//Not supported
			}
		}

		public bool Multiselect 
		{
			get 
			{
				return _openPanel.AllowsMultipleSelection;
			}
			set 
			{
				_openPanel.AllowsMultipleSelection = value;
			}
		}

		public string FileName 
		{
			get 
			{
				return _openPanel.Filename;
			}
		}

		public string[] FileNames 
		{
			get 
			{
				return _openPanel.Filenames;
			}
		}
		
	}
}

