using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
	public partial class VirtualPadC64Keyboard : UserControl , IVirtualPad
	{
		public VirtualPadC64Keyboard()
		{
			InitializeComponent();
		}

		private void VirtualPadC64Keyboard_Load(object sender, EventArgs e)
		{
			
		}

		private void Buttons_CheckedChanged(object sender, EventArgs e)
		{
			if (Global.Emulator.SystemId != "C64")
			{
				return;
			}
			else if (sender == KF1)
			{
				Global.StickyXORAdapter.SetSticky("Key F1", KF1.Checked);
			}
			else if (sender == KF3)
			{
				Global.StickyXORAdapter.SetSticky("Key F3", KF1.Checked);
			}
			else if (sender == KF5)
			{
				Global.StickyXORAdapter.SetSticky("Key F5", KF1.Checked);
			}
			else if (sender == KF7)
			{
				Global.StickyXORAdapter.SetSticky("Key F7", KF1.Checked);
			}
			
		}

		public void Clear()
		{
			if (Global.Emulator.SystemId != "C64")
			{
				return;
			}
			else
			{
				KF1.Checked = false;
				KF3.Checked = false;
				KF5.Checked = false;
				KF7.Checked = false;
				KLeftArrow.Checked = false;
				K1.Checked = false;
				K2.Checked = false;
				K3.Checked = false;
				K4.Checked = false;
				K5.Checked = false;
				K6.Checked = false;
				K7.Checked = false;
				K8.Checked = false;
				K9.Checked = false;
				K0.Checked = false;
				KPlus.Checked = false;
				KMinus.Checked = false;
				KPound.Checked = false;
				KClear.Checked = false;
				KInsert.Checked = false;
				KCtrl.Checked = false;
				KQ.Checked = false;
				KW.Checked = false;
				KE.Checked = false;
				KR.Checked = false;
				KT.Checked = false;
				KY.Checked = false;
				KU.Checked = false;
				KI.Checked = false;
				KO.Checked = false;
				KP.Checked = false;
				KAt.Checked = false;
				KAsterisk.Checked = false;
				KUpArrow.Checked = false;
				KRST.Checked = false;
				KRun.Checked = false;
				KLck.Checked = false;
				KA.Checked = false;
				KS.Checked = false;
				KD.Checked = false;
				KF.Checked = false;
				KG.Checked = false;
				KH.Checked = false;
				KJ.Checked = false;
				KK.Checked = false;
				KL.Checked = false;
				KColon.Checked = false;
				KSemicolon.Checked = false;
				KEquals.Checked = false;
				KReturn.Checked = false;
				KCommodore.Checked = false;
				KLeftShift.Checked = false;
				KZ.Checked = false;
				KX.Checked = false;
				KC.Checked = false;
				KV.Checked = false;
				KB.Checked = false;
				KN.Checked = false;
				KM.Checked = false;
				KComma.Checked = false;
				KPeriod.Checked = false;
				KSlash.Checked = false;
				KRightShift.Checked = false;
				KCursorUp.Checked = false;
				KCursorLeft.Checked = false;
				KSpace.Checked = false;

				Global.StickyXORAdapter.SetSticky("Key F1", false);
				Global.StickyXORAdapter.SetSticky("Key F3", false);
				Global.StickyXORAdapter.SetSticky("Key F5", false);
				Global.StickyXORAdapter.SetSticky("Key F7", false);
				Global.StickyXORAdapter.SetSticky("Key Left Arrow", false);
				Global.StickyXORAdapter.SetSticky("Key 1", false);
				Global.StickyXORAdapter.SetSticky("Key 2", false);
				Global.StickyXORAdapter.SetSticky("Key 3", false);
				Global.StickyXORAdapter.SetSticky("Key 4", false);
				Global.StickyXORAdapter.SetSticky("Key 5", false);
				Global.StickyXORAdapter.SetSticky("Key 6", false);
				Global.StickyXORAdapter.SetSticky("Key 7", false);
				Global.StickyXORAdapter.SetSticky("Key 8", false);
				Global.StickyXORAdapter.SetSticky("Key 9", false);
				Global.StickyXORAdapter.SetSticky("Key Plus", false);
				Global.StickyXORAdapter.SetSticky("Key Minus", false);
				Global.StickyXORAdapter.SetSticky("Key Pound", false);
				Global.StickyXORAdapter.SetSticky("Key Clear/Home", false);
				Global.StickyXORAdapter.SetSticky("Key Insert/Delete", false);
				Global.StickyXORAdapter.SetSticky("Key Control", false);
				Global.StickyXORAdapter.SetSticky("Key Q", false);
				Global.StickyXORAdapter.SetSticky("Key W", false);
				Global.StickyXORAdapter.SetSticky("Key E", false);
				Global.StickyXORAdapter.SetSticky("Key R", false);
				Global.StickyXORAdapter.SetSticky("Key T", false);
				Global.StickyXORAdapter.SetSticky("Key Y", false);
				Global.StickyXORAdapter.SetSticky("Key U", false);
				Global.StickyXORAdapter.SetSticky("Key I", false);
				Global.StickyXORAdapter.SetSticky("Key O", false);
				Global.StickyXORAdapter.SetSticky("Key P", false);
				Global.StickyXORAdapter.SetSticky("Key At", false);
				Global.StickyXORAdapter.SetSticky("Key Asterisk", false);
				Global.StickyXORAdapter.SetSticky("Key Up Arrow", false);
				Global.StickyXORAdapter.SetSticky("Key Restore", false);
				Global.StickyXORAdapter.SetSticky("Key Run/Stop", false);
				Global.StickyXORAdapter.SetSticky("Key Lck", false);
				Global.StickyXORAdapter.SetSticky("Key A", false);
				Global.StickyXORAdapter.SetSticky("Key S", false);
				Global.StickyXORAdapter.SetSticky("Key D", false);
				Global.StickyXORAdapter.SetSticky("Key F", false);
				Global.StickyXORAdapter.SetSticky("Key G", false);
				Global.StickyXORAdapter.SetSticky("Key H", false);
				Global.StickyXORAdapter.SetSticky("Key J", false);
				Global.StickyXORAdapter.SetSticky("Key K", false);
				Global.StickyXORAdapter.SetSticky("Key L", false);
				Global.StickyXORAdapter.SetSticky("Key Colon", false);
				Global.StickyXORAdapter.SetSticky("Key Semicolon", false);
				Global.StickyXORAdapter.SetSticky("Key Equal", false);
				Global.StickyXORAdapter.SetSticky("Key Return", false);
				Global.StickyXORAdapter.SetSticky("Key Commodore", false);
				Global.StickyXORAdapter.SetSticky("Key Left Shift", false);
				Global.StickyXORAdapter.SetSticky("Key Z", false);
				Global.StickyXORAdapter.SetSticky("Key X", false);
				Global.StickyXORAdapter.SetSticky("Key C", false);
				Global.StickyXORAdapter.SetSticky("Key V", false);
				Global.StickyXORAdapter.SetSticky("Key B", false);
				Global.StickyXORAdapter.SetSticky("Key N", false);
				Global.StickyXORAdapter.SetSticky("Key M", false);
				Global.StickyXORAdapter.SetSticky("Key Comma", false);
				Global.StickyXORAdapter.SetSticky("Key Period", false);
				Global.StickyXORAdapter.SetSticky("Key Slash", false);
				Global.StickyXORAdapter.SetSticky("Key Right Shift", false);
				Global.StickyXORAdapter.SetSticky("Key Cursor Up/Down", false);
				Global.StickyXORAdapter.SetSticky("Key Cursor Left/Right", false);
				Global.StickyXORAdapter.SetSticky("Key Space", false);
			}
		}

		public string GetMnemonic()
		{
			StringBuilder input = new StringBuilder("");

			input.Append(KF1.Checked ? "1" : ".");
			input.Append(KF1.Checked ? "3" : ".");
			input.Append(KF1.Checked ? "5" : ".");
			input.Append(KF1.Checked ? "7" : ".");

			input.Append("|");
			return input.ToString();
		}

		public void SetButtons(string buttons)
		{
			if (buttons.Length < 66)
			{
				return;
			}

			if (buttons[0] == '.') KF1.Checked = false; else KF1.Checked = true;
			if (buttons[1] == '.') KF3.Checked = false; else KF3.Checked = true;
			if (buttons[2] == '.') KF5.Checked = false; else KF5.Checked = true;
			if (buttons[3] == '.') KF7.Checked = false; else KF7.Checked = true;
		}
	}
}
