using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using MarioAI;

namespace BizHawk.Client.EmuHawk
{
	public partial class AITools : Form, IToolForm
	{
		private Label label1;
		private Label label2;
		private Label label3;
		private Label label4;
		private Label lblX;
		private Label lblY;
		private Label lblZ;
		private Label lblCoins;

		private int counter = 0;

		[RequiredService]
		private IMemoryDomains MemoryDomains { get; set; }

		private Watch coinWatch = null;

		private Watch xWatch = null;
		
		private Watch yWatch = null;
		private Label lblPython;
		private Label label6;
		private Watch zWatch = null;

		public bool IsActive => true;

		public bool IsLoaded => true;

		public AITools()
		{
			InitializeComponent();

			this.Show();

			var aistuff = new PythonBridge();

			this.lblPython.Text = aistuff.DoStuff().ToString();

		}

		private void InitializeComponent()
		{
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.lblX = new System.Windows.Forms.Label();
            this.lblY = new System.Windows.Forms.Label();
            this.lblZ = new System.Windows.Forms.Label();
            this.lblCoins = new System.Windows.Forms.Label();
            this.lblPython = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(23, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(64, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Mario coins:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Mario X";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(23, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(43, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Mario Y";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(23, 105);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(43, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Mario Z";
            // 
            // lblX
            // 
            this.lblX.AutoSize = true;
            this.lblX.Location = new System.Drawing.Point(93, 60);
            this.lblX.Name = "lblX";
            this.lblX.Size = new System.Drawing.Size(0, 13);
            this.lblX.TabIndex = 4;
            // 
            // lblY
            // 
            this.lblY.AutoSize = true;
            this.lblY.Location = new System.Drawing.Point(93, 83);
            this.lblY.Name = "lblY";
            this.lblY.Size = new System.Drawing.Size(0, 13);
            this.lblY.TabIndex = 5;
            // 
            // lblZ
            // 
            this.lblZ.AutoSize = true;
            this.lblZ.Location = new System.Drawing.Point(93, 105);
            this.lblZ.Name = "lblZ";
            this.lblZ.Size = new System.Drawing.Size(0, 13);
            this.lblZ.TabIndex = 6;
            // 
            // lblCoins
            // 
            this.lblCoins.AutoSize = true;
            this.lblCoins.Location = new System.Drawing.Point(93, 22);
            this.lblCoins.Name = "lblCoins";
            this.lblCoins.Size = new System.Drawing.Size(0, 13);
            this.lblCoins.TabIndex = 7;
            // 
            // lblPython
            // 
            this.lblPython.AutoSize = true;
            this.lblPython.Location = new System.Drawing.Point(93, 145);
            this.lblPython.Name = "lblPython";
            this.lblPython.Size = new System.Drawing.Size(0, 13);
            this.lblPython.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(23, 145);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(40, 13);
            this.label6.TabIndex = 8;
            this.label6.Text = "Python";
            // 
            // AITools
            // 
            this.ClientSize = new System.Drawing.Size(303, 220);
            this.Controls.Add(this.lblPython);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblCoins);
            this.Controls.Add(this.lblZ);
            this.Controls.Add(this.lblY);
            this.Controls.Add(this.lblX);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "AITools";
            this.Text = "AI Debug Shit";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		public void UpdateValues(ToolFormUpdateType type)
		{		
			if (type == ToolFormUpdateType.PostFrame)
			{
				this.counter += 1;

				Console.WriteLine("After a Frame {0}", counter);

				lblCoins.Text = this.coinWatch.ValueString;
				lblX.Text = this.xWatch.ValueString;
				lblY.Text = this.yWatch.ValueString;
				lblZ.Text = this.zWatch.ValueString;
			}
		}

		public void Restart()
		{
			var domain = this.MemoryDomains.FirstOrDefault(el => el.Name == "RDRAM");

			if (domain == null)
			{
				throw new Exception("Somethign went wrong");
			}

			this.coinWatch = Watch.GenerateWatch(domain, 0x33B218, WatchSize.Word, WatchDisplayType.Unsigned, true);
			this.xWatch = Watch.GenerateWatch(domain, 0x33B1AC, WatchSize.DWord, WatchDisplayType.Float, true);
			this.yWatch = Watch.GenerateWatch(domain, 0x33B1B0, WatchSize.DWord, WatchDisplayType.Float, true);
			this.zWatch = Watch.GenerateWatch(domain, 0x33B1B4, WatchSize.DWord, WatchDisplayType.Float, true);
		}

		public bool AskSaveChanges()
		{
			Console.WriteLine("AskSaveChanges() called");

			return false;
		}
	}
}
