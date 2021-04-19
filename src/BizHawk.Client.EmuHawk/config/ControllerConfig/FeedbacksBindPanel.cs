#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Client.EmuHawk
{
	public class FeedbacksBindPanel : UserControl
	{
		private readonly FlowLayoutPanel _flpMain = new SingleColumnFLP();

		private readonly IDictionary<string, FeedbackBind> _realConfigObject;

		public FeedbacksBindPanel(IDictionary<string, FeedbackBind> realConfigObject, ICollection<string>? realConfigButtons = null)
		{
			_realConfigObject = realConfigObject;
			_flpMain.Controls.Add(new LabelEx { Text = "To bind, click \"Bind!\", move an axis (e.g. analog stick) on the desired gamepad, and choose from the dropdown.\nNote: haptic feedback won't work if your gamepad is shown as \"J#\" or if your input method is OpenTK." });
			var adapter = Input.Instance.Adapter;
			foreach (var buttonName in realConfigButtons ?? realConfigObject.Keys)
			{
				_flpMain.Controls.Add(new FeedbackBindControl(buttonName, _realConfigObject[buttonName], adapter));
			}
			SuspendLayout();
			Controls.Add(_flpMain);
			ResumeLayout();
		}

		/// <param name="saveConfigObject">if non-null, save to possibly different config object than originally initialized from</param>
		public void Save(IDictionary<string, FeedbackBind>? saveConfigObject = null)
		{
			var saveTo = saveConfigObject ?? _realConfigObject;
			foreach (var c in _flpMain.Controls.OfType<FeedbackBindControl>())
			{
				if (string.IsNullOrEmpty(c.BoundGamepadPrefix)) continue;
				foreach (var channel in c.BoundChannels.Split('+'))
				{
					saveTo[c.VChannelName] = new(c.BoundGamepadPrefix, channel, c.Prescale);
				}
			}
		}
	}
}
