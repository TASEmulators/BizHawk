using System;
using System.Windows.Forms;

namespace BizHawk
{
	public static class HawkUIFactory
	{
		public static Type OpenDialogClass = typeof(DefaultOpenFileDialog);
		
		public static IOpenFileDialog CreateOpenFileDialog()
		{
			if(typeof(IOpenFileDialog).IsAssignableFrom(OpenDialogClass))
			{
				return (IOpenFileDialog)OpenDialogClass.GetConstructor(Type.EmptyTypes).Invoke(null);
			}
			return new DefaultOpenFileDialog();
		}
	}
	
	public class DefaultOpenFileDialog : IOpenFileDialog
	{
		//Can't extend OpenFileDialog because it's sealed, so I need to encapsulate it.
		private OpenFileDialog _capsule;
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
			get { return _capsule.Multiselect; }
			set { _capsule.Multiselect = value; }
		}
		public System.Windows.Forms.DialogResult ShowDialog()
		{
			return _capsule.ShowDialog();
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
        public int FilterIndex
        {
            get { return _capsule.FilterIndex; }
            set { _capsule.FilterIndex = value; }
        }
	}
}

