using System;
using MonoMac.AppKit;
using System.Collections.Generic;

namespace MonoMacWrapper
{
	public class MacOpenFileDialog : BizHawk.IOpenFileDialog
	{
		private MonoMac.AppKit.NSOpenPanel _openPanel;
		private static MonoMac.Foundation.NSUrl _directoryToRestore;
		private bool _restoreDir;
		private string _filter;
		private int _filterIndex;
		
		public MacOpenFileDialog()
		{
			_openPanel = new NSOpenPanel();
			_directoryToRestore = null;
		}
		
		public System.Windows.Forms.DialogResult ShowDialog()
		{
			if(_restoreDir && _directoryToRestore != null)
			{
				_openPanel.DirectoryUrl = _directoryToRestore;
			}
			if(_openPanel.RunModal() == 1)
			{
				_directoryToRestore = _openPanel.DirectoryUrl;
				return System.Windows.Forms.DialogResult.OK;
			}
			return System.Windows.Forms.DialogResult.Cancel;
		}

		public string InitialDirectory 
		{
			get 
			{
				return _openPanel.DirectoryUrl.Path;
			}
			set 
			{
				_openPanel.DirectoryUrl = new MonoMac.Foundation.NSUrl(value, true);
			}
		}

		public string Filter 
		{
			get 
			{
				return _filter;
			}
			set
			{
				_filter = value;
				ParseFilter();
			}
		}

		public int FilterIndex
		{
			get { return _filterIndex; }
			set { _filterIndex = value; }
		}
		
		public void ParseFilter()
		{
			List<string> fileTypes = new List<string>();
			string[] pieces = _filter.Split('|');
			if(pieces.Length > 1)
			{
				string piece = pieces[1]; //Todo: Handle the actual drop down for type options
				string[] types = piece.Split(';');
				foreach(string tp in types)
				{
					string trimmedTp = tp.Trim();
					if(trimmedTp.StartsWith("*."))
					{
						fileTypes.Add(trimmedTp.Substring(2));
					}
				}
			}
			
			if(fileTypes.Count > 0)
				_openPanel.AllowedFileTypes = fileTypes.ToArray();
		}

		public bool RestoreDirectory 
		{
			get
			{
				return _restoreDir;
			}
			set 
			{
				_restoreDir = value;
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
				return _openPanel.Url.Path;
			}
			set
			{
				//Can't set a pre-selected file
			}
		}

		public string[] FileNames 
		{
			get 
			{
				string[] retval = new string[_openPanel.Urls.Length];
				for(int i=0; i<_openPanel.Urls.Length; i++)
				{
					retval[i] = _openPanel.Urls[i].Path;
				}
				return retval;
			}
		}
		
	}
}

