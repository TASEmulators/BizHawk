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

		void FirmwareWarn(string sysID, string firmwareID, bool required, string msg = null)
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


		public string GetFirmwarePath(string sysID, string firmwareID, bool required, string msg = null)
		{
			string path = firmware.Request(sysID, firmwareID);
			if (path != null && !File.Exists(path))
				path = null;

			if (path == null)
				FirmwareWarn(sysID, firmwareID, required, msg);
			return path;
		}

		public byte[] GetFirmware(string sysID, string firmwareID, bool required, string msg = null)
		{
			byte[] ret = null;
			string path = GetFirmwarePath(sysID, firmwareID, required, msg);
			if (path != null && File.Exists(path))
			{
				try
				{
					ret = File.ReadAllBytes(path);
				}
				catch (IOException) { }
			}

			if (ret == null && path != null)
				FirmwareWarn(sysID, firmwareID, required, msg);
			return ret;
		}
	}
}
