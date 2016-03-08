using System;

namespace BizHawk.Emulation.Common
{
	public class CoreComm
	{
		public CoreComm(Action<string> showMessage, Action<string> NotifyMessage)
		{
			ShowMessage = showMessage;
			Notify = NotifyMessage;
		}

		public ICoreFileProvider CoreFileProvider;

		public double VsyncRate
		{
			get
			{
				return VsyncNum / (double)VsyncDen;
			}
		}

		public int VsyncNum = 60;
		public int VsyncDen = 1;

		//a core should set these if you wish to provide rom status information yourself. otherwise it will be calculated by the frontend in a way you may not like, using RomGame-related concepts.
		public string RomStatusAnnotation;
		public string RomStatusDetails;

		public int ScreenLogicalOffsetX, ScreenLogicalOffsetY;

		// size hint to a/v out resizer.  this probably belongs in VideoProvider?  but it's somewhat different than VirtualWidth...
		public int NominalWidth = 640;
		public int NominalHeight = 480;

		//I know we want to get rid of CoreComm, but while it's still here, I'll use it for this
		public string LaunchLibretroCore;

		/// <summary>
		/// show a message.  reasonably annoying (dialog box), shouldn't be used most of the time
		/// </summary>
		public Action<string> ShowMessage { get; private set; }

		/// <summary>
		/// show a message.  less annoying (OSD message). Should be used for ignorable helpful messages
		/// </summary>
		public Action<string> Notify { get; private set; }

		public Func<int,int,bool,object> RequestGLContext;
		public Action<object> ReleaseGLContext;
		public Action<object> ActivateGLContext;
		public Action DeactivateGLContext; //this shouldnt be necessary.. frontend should be changing context before it does anything.. but for now..
	}
}
