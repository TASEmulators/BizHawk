using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Common
{
	public class CoreComm
	{
		public CoreComm(Action<string> ShowMessage, Action<string> NotifyMessage)
		{
			this.ShowMessage = ShowMessage;
			this.Notify = NotifyMessage;
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

		public bool DriveLED = false;
		public bool UsesDriveLed = false;

		public bool LinkConnected = false;
		public bool UsesLinkCable = false;

		/// <summary>
		/// show a message.  reasonably annoying (dialog box), shouldn't be used most of the time
		/// </summary>
		public Action<string> ShowMessage { get; private set; }

		/// <summary>
		/// show a message.  less annoying (OSD message). Should be used for ignorable helpful messages
		/// </summary>
		public Action<string> Notify { get; private set; }

		public Func<object> RequestGLContext;
		public Action<object> ActivateGLContext;
		public Action DeactivateGLContext; //this shouldnt be necessary.. frontend should be changing context before it does anything.. but for now..
	}
}
