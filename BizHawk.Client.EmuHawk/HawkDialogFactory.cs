using System;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public static class HawkDialogFactory
	{
		public static Type OpenDialogClass = typeof(DefaultOpenFileDialog);
		public static Type SaveDialogClass = typeof(DefaultSaveFileDialog);
		public static Type FolderBrowserClass = typeof(DefaultFolderBrowserDialog);

		public static IOpenFileDialog CreateOpenFileDialog()
		{
			if(typeof(IOpenFileDialog).IsAssignableFrom(OpenDialogClass))
			{
				return (IOpenFileDialog)OpenDialogClass.GetConstructor(Type.EmptyTypes).Invoke(null);
			}
			return new DefaultOpenFileDialog();
		}

		public static ISaveFileDialog CreateSaveFileDialog()
		{
			if(typeof(ISaveFileDialog).IsAssignableFrom(SaveDialogClass))
			{
				return (ISaveFileDialog)SaveDialogClass.GetConstructor(Type.EmptyTypes).Invoke(null);
			}
			return new DefaultSaveFileDialog();
		}

		public static IFolderBrowserDialog CreateFolderBrowserDialog()
		{
			if(typeof(IFolderBrowserDialog).IsAssignableFrom(FolderBrowserClass))
			{
				return (IFolderBrowserDialog)FolderBrowserClass.GetConstructor(Type.EmptyTypes).Invoke(null);
			}
			return new DefaultFolderBrowserDialog();
		}
	}

	public class DefaultSaveFileDialog : DefaultOpenFileDialog, ISaveFileDialog
	{
		public DefaultSaveFileDialog()
		{
			_capsule = new SaveFileDialog();
		}

		public string DefaultExt 
		{ 
			get
			{ 
				return ((SaveFileDialog)_capsule).DefaultExt; 
			}
			set
			{ 
				((SaveFileDialog)_capsule).DefaultExt = value;
			} 
		}

		public bool OverwritePrompt 
		{ 
			get
			{ 
				return ((SaveFileDialog)_capsule).OverwritePrompt; 
			}
			set
			{ 
				((SaveFileDialog)_capsule).OverwritePrompt = value;
			} 
		}
	}

	public class DefaultOpenFileDialog : IOpenFileDialog
	{
		//Can't extend OpenFileDialog because it's sealed, so I need to encapsulate it.
		protected FileDialog _capsule;
		public DefaultOpenFileDialog()
		{
			_capsule = new OpenFileDialog();
		}
		public string InitialDirectory 
		{
			get { return _capsule.InitialDirectory; }
			set { _capsule.InitialDirectory = value; }
		}
		public string Filter 
		{
			get { return _capsule.Filter; }
			set { _capsule.Filter = value; }
		}
		public bool RestoreDirectory 
		{
			get { return _capsule.RestoreDirectory; }
			set { _capsule.RestoreDirectory = value; }
		}
		public bool Multiselect
		{
			get 
			{
				if (_capsule is OpenFileDialog)
				{
					return ((OpenFileDialog)_capsule).Multiselect;
				}
				return false;
			}
			set 
			{
				if (_capsule is OpenFileDialog)
				{
					((OpenFileDialog)_capsule).Multiselect = value;
				}
			}
		}
		public bool AddExtension
		{
			get { return _capsule.AddExtension; }
			set { _capsule.AddExtension = value; }
		}
		public System.Windows.Forms.DialogResult ShowDialog()
		{
			return _capsule.ShowDialog();
		}
		public System.Windows.Forms.DialogResult ShowDialog(System.Windows.Forms.Form form)
		{
			return _capsule.ShowDialog(form);
		}
		public string FileName 
		{
			get { return _capsule.FileName; }
			set { _capsule.FileName = value; }
		}
		public string[] FileNames
		{
			get { return _capsule.FileNames; }
		}
		public string Title
		{
			get { return _capsule.Title; }
			set { _capsule.Title = value; }
		}
		public int FilterIndex
		{
			get { return _capsule.FilterIndex; }
			set { _capsule.FilterIndex = value; }
		}
		public void Dispose()
		{
			_capsule.Dispose();
		}
	}

	public class DefaultFolderBrowserDialog : IFolderBrowserDialog
	{
		private FolderBrowserDialog _capsule;
		public DefaultFolderBrowserDialog()
		{
			_capsule = new FolderBrowserDialog();
		}
		public string Description 
		{
			get { return _capsule.Description; }
			set { _capsule.Description = value; }
		}
		public string SelectedPath 
		{
			get { return _capsule.SelectedPath; }
			set { _capsule.SelectedPath = value; }
		}
		public System.Windows.Forms.DialogResult ShowDialog()
		{
			return _capsule.ShowDialog();
		}
	}
}
