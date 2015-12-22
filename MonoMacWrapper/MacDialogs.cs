using System;
using MonoMac.AppKit;
using System.Collections.Generic;

namespace MonoMacWrapper
{
	public class MacSaveFileDialog : BizHawk.Client.EmuHawk.ISaveFileDialog
	{
		private MonoMac.AppKit.NSSavePanel _savePanel;
		private static MonoMac.Foundation.NSUrl _directoryToRestore;
		private bool _restoreDir;
		private string _filter;
		private string _defaultExtension;
		private int _filterIndex;
		private readonly NSPopUpButton _fileTypeDropDown;

		public MacSaveFileDialog()
		{
			_savePanel = new NSSavePanel();
			_fileTypeDropDown = new NSPopUpButton();
			_directoryToRestore = null;
		}

		public System.Windows.Forms.DialogResult ShowDialog()
		{
			if(_restoreDir && _directoryToRestore != null)
			{
				_savePanel.DirectoryUrl = _directoryToRestore;
			}
			AddNativeFilter();
			if(_savePanel.RunModal() == 1)
			{
				_directoryToRestore = _savePanel.DirectoryUrl;
				return System.Windows.Forms.DialogResult.OK;
			}
			return System.Windows.Forms.DialogResult.Cancel;
		}

		public System.Windows.Forms.DialogResult ShowDialog(System.Windows.Forms.Form form)
		{
			return ShowDialog();
		}

		private void AddNativeFilter()
		{
			if (_savePanel.AccessoryView != null)
				return;

			var fileTypeView = new NSView();
			fileTypeView.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;

			const int padding = 15;

			if (_savePanel.AllowedFileTypes.Length > 0)
			{
				var label = new NSTextField();
				label.StringValue = "File Type:";
				label.DrawsBackground = false;
				label.Bordered = false;
				label.Bezeled = false;
				label.Editable = false;
				label.Selectable = false;
				label.SizeToFit();
				fileTypeView.AddSubview(label);

				foreach (var ft in _savePanel.AllowedFileTypes)
				{
					_fileTypeDropDown.AddItem("." + ft + " files (*."+ft+")");
				}
				_fileTypeDropDown.SizeToFit();
				_fileTypeDropDown.Activated += (sender, e) =>
				{
					_savePanel.ValidateVisibleColumns();
					_savePanel.Update();
				};
				fileTypeView.AddSubview(_fileTypeDropDown);
				_fileTypeDropDown.SetFrameOrigin(new System.Drawing.PointF((float)label.Frame.Width + 10, padding));
				_fileTypeDropDown.SelectItem(_filterIndex);

				label.SetFrameOrigin(new System.Drawing.PointF(0, (float)(padding + (_fileTypeDropDown.Frame.Height - label.Frame.Height) / 2)));

				fileTypeView.Frame = new System.Drawing.RectangleF(0, 0, (float)(_fileTypeDropDown.Frame.Width + label.Frame.Width + 10), (float)(_fileTypeDropDown.Frame.Height + padding * 2));

				_savePanel.AccessoryView = fileTypeView;
			}
			else
			{
				_savePanel.AccessoryView = null;
			}
		}

		/// <summary>
		/// This isn't really applicable, since nobody ever types in the filename on an open file dialog.
		/// </summary>
		public bool AddExtension
		{
			get { return false; }
			set { }
		}

		public string InitialDirectory 
		{
			get 
			{
				return _savePanel.DirectoryUrl.Path;
			}
			set 
			{
				_savePanel.DirectoryUrl = new MonoMac.Foundation.NSUrl(value, true);
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
			get 
			{
				if (_savePanel.AccessoryView != null)
				{
					return _fileTypeDropDown.IndexOfSelectedItem;
				}
				return _filterIndex; 
			}
			set 
			{
				if (_savePanel.AccessoryView != null)
				{
					_fileTypeDropDown.SelectItem(value);
				}
				_filterIndex = value; 
			}
		}

		public void ParseFilter()
		{
			List<string> fileTypes = new List<string>();
			string[] pieces = _filter.Split('|');
			if(pieces.Length > 1)
			{
				string piece = pieces[1];
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
				_savePanel.AllowedFileTypes = fileTypes.ToArray();
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
				return false;
			}
			set{ }
		}

		public string FileName 
		{
			get 
			{
				return _savePanel.Url.Path;
			}
			set
			{
				//Can't set a pre-selected file
			}
		}

		public string Title
		{
			get
			{
				return _savePanel.Title;
			}
			set
			{
				_savePanel.Title = value;
			}
		}

		public string[] FileNames 
		{
			get 
			{
				return new string[0];
			}
		}

		public string DefaultExt 
		{ 
			get
			{ 
				return _defaultExtension;
			}
			set
			{ 
				_defaultExtension = value;
			} 
		}

		public bool OverwritePrompt 
		{ 
			get
			{ 
				return false;
			}
			set	{ } 
		}

		public void Dispose()
		{
			_savePanel.Dispose();
		}
	}

	public class MacOpenFileDialog : BizHawk.Client.EmuHawk.IOpenFileDialog
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

		public System.Windows.Forms.DialogResult ShowDialog(System.Windows.Forms.Form form)
		{
			return ShowDialog();
		}

		/// <summary>
		/// This isn't really applicable, since nobody ever types in the filename on an open file dialog.
		/// </summary>
		public bool AddExtension
		{
			get { return false; }
			set { }
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

		public string Title
		{
			get
			{
				return _openPanel.Title;
			}
			set
			{
				_openPanel.Title = value;
			}
		}

		public void Dispose()
		{
			_openPanel.Dispose();
		}
	}

	public class MacFolderBrowserDialog : BizHawk.Client.EmuHawk.IFolderBrowserDialog
	{
		private MonoMac.AppKit.NSOpenPanel _openPanel;

		public MacFolderBrowserDialog()
		{
			_openPanel = new NSOpenPanel();
			_openPanel.CanChooseDirectories = true;
			_openPanel.CanChooseFiles = false;
		}
		
		public System.Windows.Forms.DialogResult ShowDialog()
		{
			if(_openPanel.RunModal() == 1)
			{
				return System.Windows.Forms.DialogResult.OK;
			}
			return System.Windows.Forms.DialogResult.Cancel;
		}
		
		public string Description 
		{
			get { return _openPanel.Title; }
			set { _openPanel.Title = value; }
		}
		public string SelectedPath 
		{
			get { return _openPanel.Url.Path; }
			set
			{
				_openPanel.DirectoryUrl = new MonoMac.Foundation.NSUrl(value, true);
			}
		}
	}
}

