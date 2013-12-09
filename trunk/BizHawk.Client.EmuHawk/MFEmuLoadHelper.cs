using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;
using System.IO;

namespace BizHawk.Client.EmuHawk
{
	class MFEmuLoadHelper : IEmuLoadHelper
	{
		IWin32Window parent;
		FirmwareManager firmware;

		public MFEmuLoadHelper(IWin32Window parent, FirmwareManager firmware)
		{
			this.parent = parent;
			this.firmware = firmware;
		}

		public void ShowMessage(string msg)
		{
			MessageBox.Show(parent, msg, "Load Warning");
		}

		public byte[] GetFirmware(string sysID, string firmwareID, bool required, string msg = null)
		{
			byte[] ret = null;
			string path = firmware.Request(sysID, firmwareID);
			if (path != null && File.Exists(path))
			{
				try
				{
					ret = File.ReadAllBytes(path);
				}
				catch (IOException)
				{
				}
			}

			if (ret == null)
			{

				if (required)
				{
					string fullmsg = string.Format(
						"Couldn't find required firmware \"{0}:{1}\".  This is fatal{2}", sysID, firmwareID, msg != null ? ": " + msg : ".");
					throw new Exception(fullmsg);
				}
				else
				{
					if (msg != null)
					{
						string fullmsg = string.Format(
							"Couldn't find firmware \"{0}:{1}\".  Will attempt to continue: {2}", sysID, firmwareID, msg);
						ShowMessage(msg);
					}
				}
			}
			return ret;
		}
	}
}
