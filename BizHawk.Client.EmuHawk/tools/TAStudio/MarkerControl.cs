using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class MarkerControl : UserControl
	{
		private Marker _marker = new Marker(0);
		private int _markerIndex = 0;

		public MarkerControl()
		{
			InitializeComponent();
		}

		public void SetMarker(Marker marker, int index)
		{
			_marker = marker;
			_markerIndex = index;
			MarkerLabel.Text = "Marker " + _markerIndex;
			MarkerBox.Text = _marker.Message;
		}

		private void MarkerControl_Load(object sender, EventArgs e)
		{
			
		}
	}
}
