using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk.tools.TAStudio
{
	public partial class MarkerControl : UserControl
	{
		private Marker _marker;
		private int _markerIndex;

		public MarkerControl()
		{
			InitializeComponent();
		}

		public void SetMarker(Marker marker, int index)
		{
			_marker = marker;
			_markerIndex = index;
		}

		private void MarkerControl_Load(object sender, EventArgs e)
		{
			MarkerLabel.Text = "Marker " + _markerIndex;
			MarkerBox.Text = _marker.Message;
		}
	}
}
