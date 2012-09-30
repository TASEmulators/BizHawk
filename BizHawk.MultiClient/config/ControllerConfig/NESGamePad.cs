using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace BizHawk.MultiClient
{
	class NESGamePad : Panel
	{
		public InputWidget UpBox = new InputWidget();
		public InputWidget DownBox = new InputWidget();
		public InputWidget LeftBox = new InputWidget();
		public InputWidget RightBox = new InputWidget();
		public InputWidget ABox = new InputWidget();
		public InputWidget BBox = new InputWidget();
		public InputWidget SelectBox = new InputWidget();
		public InputWidget StartBox = new InputWidget();

		public Label UpLabel = new Label();
		public Label DownLabel = new Label();
		public Label LeftLabel = new Label();
		public Label RightLabel = new Label();
		public Label ALabel = new Label();
		public Label BLabel = new Label();
		public Label SelectLabel = new Label();
		public Label StartLabel = new Label();

		public int ControllerNumber = 1;
		public bool Autofire = false;

		public NESGamePad()
		{
			this.BorderStyle = BorderStyle.Fixed3D;
			this.Size = new Size(174, 74);
			ControllerNumber = 1;

			UpBox.Location = new Point(15, 15);
			UpBox.Size = new Size(200, 23);

			DownBox.Location = new Point(15, 45);
			DownBox.Size = new Size(200, 23);

			LeftBox.Location = new Point(15, 75);
			LeftBox.Size = new Size(200, 23);

			RightBox.Location = new Point(15, 105);
			RightBox.Size = new Size(200, 23);

			ABox.Location = new Point(15, 135);
			ABox.Size = new Size(200, 23);

			BBox.Location = new Point(15, 165);
			BBox.Size = new Size(200, 23);

			SelectBox.Location = new Point(15, 195);
			SelectBox.Size = new Size(200, 23);

			StartBox.Location = new Point(15, 225);
			StartBox.Size = new Size(200, 23);

			UpLabel.Text = "Up";
			UpLabel.Location = new Point(220, 18);

			DownLabel.Text = "Down";
			DownLabel.Location = new Point(220, 48);

			LeftLabel.Text = "Left";
			LeftLabel.Location = new Point(220, 78);
			
			RightLabel.Text = "Right";
			RightLabel.Location = new Point(220, 108);

			ALabel.Text = "A";
			ALabel.Location = new Point(220, 138);

			BLabel.Text = "B";
			BLabel.Location = new Point(220, 168);

			SelectLabel.Text = "Select";
			SelectLabel.Location = new Point(220, 198);

			StartLabel.Text = "Start";
			StartLabel.Location = new Point(220, 228);

			this.Controls.Add(this.UpBox);
			this.Controls.Add(this.DownBox);
			this.Controls.Add(this.LeftBox);
			this.Controls.Add(this.RightBox);
			this.Controls.Add(this.ABox);
			this.Controls.Add(this.BBox);
			this.Controls.Add(this.SelectBox);
			this.Controls.Add(this.StartBox);

			this.Controls.Add(this.UpLabel);
			this.Controls.Add(this.DownLabel);
			this.Controls.Add(this.LeftLabel);
			this.Controls.Add(this.RightLabel);
			this.Controls.Add(this.ALabel);
			this.Controls.Add(this.BLabel);
			this.Controls.Add(this.SelectLabel);
			this.Controls.Add(this.StartLabel);

			this.BorderStyle = BorderStyle.None;
		}


	}
}
