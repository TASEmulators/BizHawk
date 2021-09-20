
namespace BizHawk.Client.EmuHawk
{
	partial class MainWindow
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.label8 = new System.Windows.Forms.Label();
            this.EventsThisGameListBox = new System.Windows.Forms.ListBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.EventNameTextBox = new System.Windows.Forms.TextBox();
            this.RAMValuesToCheckTextBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.RAMMinChangeNumericInput = new System.Windows.Forms.NumericUpDown();
            this.label13 = new System.Windows.Forms.Label();
            this.RAMMaxChangeNumericInput = new System.Windows.Forms.NumericUpDown();
            this.label14 = new System.Windows.Forms.Label();
            this.EventDelayNumericInput = new System.Windows.Forms.NumericUpDown();
            this.EventEnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.EventRAMBaseComboBox = new System.Windows.Forms.ComboBox();
            this.label15 = new System.Windows.Forms.Label();
            this.GamesWithEventsListBox = new System.Windows.Forms.ListBox();
            this.ResetGameToDefaultButton = new System.Windows.Forms.Button();
            this.ShowOrAddCurrentGame = new System.Windows.Forms.Button();
            this.AddNewEventButton = new System.Windows.Forms.Button();
            this.EventCalculationExample = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.EventDomainComboBox = new System.Windows.Forms.ComboBox();
            this.SaveEventChangesButton = new System.Windows.Forms.Button();
            this.RemoveEventButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.livesFrameCount = new System.Windows.Forms.TextBox();
            this.livesBytes0 = new System.Windows.Forms.TextBox();
            this.livesValue0 = new System.Windows.Forms.TextBox();
            this.livesDomain0 = new System.Windows.Forms.TextBox();
            this.livesBytes1 = new System.Windows.Forms.TextBox();
            this.livesValue1 = new System.Windows.Forms.TextBox();
            this.livesDomain1 = new System.Windows.Forms.TextBox();
            this.livesBytes2 = new System.Windows.Forms.TextBox();
            this.livesValue2 = new System.Windows.Forms.TextBox();
            this.livesDomain2 = new System.Windows.Forms.TextBox();
            this.livesBytes3 = new System.Windows.Forms.TextBox();
            this.livesValue3 = new System.Windows.Forms.TextBox();
            this.livesDomain3 = new System.Windows.Forms.TextBox();
            this.saveLivesSettingsButton = new System.Windows.Forms.Button();
            this.horizontalLine1 = new BizHawk.Client.EmuHawk.HorizontalLine();
            this.horizontalLine2 = new BizHawk.Client.EmuHawk.HorizontalLine();
            this.blockZeroCheckBox = new System.Windows.Forms.CheckBox();
			this.label16 = new System.Windows.Forms.Label();
			this.EventSteppedChangeEventBufferNumericInput = new System.Windows.Forms.NumericUpDown();
			((System.ComponentModel.ISupportInitialize)(this.RAMMinChangeNumericInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RAMMaxChangeNumericInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.EventDelayNumericInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.EventSteppedChangeEventBufferNumericInput)).BeginInit();
            this.SuspendLayout();
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(12, 10);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(114, 13);
            this.label8.TabIndex = 18;
            this.label8.Text = "Games with events";
            // 
            // EventsThisGameListBox
            // 
            this.EventsThisGameListBox.FormattingEnabled = true;
            this.EventsThisGameListBox.Location = new System.Drawing.Point(15, 282);
            this.EventsThisGameListBox.Name = "EventsThisGameListBox";
            this.EventsThisGameListBox.Size = new System.Drawing.Size(326, 56);
            this.EventsThisGameListBox.TabIndex = 19;
            this.EventsThisGameListBox.Tag = "eventSelect";
            this.EventsThisGameListBox.SelectedIndexChanged += new System.EventHandler(this.EventsThisGameListBox_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(12, 265);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(123, 13);
            this.label9.TabIndex = 20;
            this.label9.Tag = "eventSelect";
            this.label9.Text = "Events for this game";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(354, 282);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(66, 13);
            this.label10.TabIndex = 21;
            this.label10.Tag = "eventEdit";
            this.label10.Text = "Event Name";
            this.label10.Click += new System.EventHandler(this.label10_Click);
            // 
            // EventNameTextBox
            // 
            this.EventNameTextBox.Location = new System.Drawing.Point(426, 279);
            this.EventNameTextBox.Name = "EventNameTextBox";
            this.EventNameTextBox.Size = new System.Drawing.Size(362, 20);
            this.EventNameTextBox.TabIndex = 22;
            this.EventNameTextBox.Tag = "eventEdit";
            this.EventNameTextBox.TextChanged += new System.EventHandler(this.EventNameTextBox_TextChanged);
            // 
            // RAMValuesToCheckTextBox
            // 
            this.RAMValuesToCheckTextBox.Location = new System.Drawing.Point(473, 328);
            this.RAMValuesToCheckTextBox.Name = "RAMValuesToCheckTextBox";
            this.RAMValuesToCheckTextBox.Size = new System.Drawing.Size(314, 20);
            this.RAMValuesToCheckTextBox.TabIndex = 23;
            this.RAMValuesToCheckTextBox.Tag = "eventEdit";
            this.RAMValuesToCheckTextBox.TextChanged += new System.EventHandler(this.RAMValuesToCheckTextBox_TextChanged);
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(354, 331);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(113, 13);
            this.label11.TabIndex = 24;
            this.label11.Tag = "eventEdit";
            this.label11.Text = "RAM values to check:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(351, 435);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(64, 13);
            this.label12.TabIndex = 25;
            this.label12.Tag = "eventEdit";
            this.label12.Text = "Min Change";
            // 
            // RAMMinChangeNumericInput
            // 
            this.RAMMinChangeNumericInput.Location = new System.Drawing.Point(421, 433);
            this.RAMMinChangeNumericInput.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.RAMMinChangeNumericInput.Minimum = new decimal(new int[] {
            2147483647,
            0,
            0,
            -2147483648});
            this.RAMMinChangeNumericInput.Name = "RAMMinChangeNumericInput";
            this.RAMMinChangeNumericInput.Size = new System.Drawing.Size(138, 20);
            this.RAMMinChangeNumericInput.TabIndex = 26;
            this.RAMMinChangeNumericInput.Tag = "eventEdit";
            this.RAMMinChangeNumericInput.ValueChanged += new System.EventHandler(this.RAMMinChangeNumericInput_ValueChanged);
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(567, 437);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(67, 13);
            this.label13.TabIndex = 27;
            this.label13.Tag = "eventEdit";
            this.label13.Text = "Max Change";
            this.label13.Click += new System.EventHandler(this.label13_Click);
            // 
            // RAMMaxChangeNumericInput
            // 
            this.RAMMaxChangeNumericInput.Location = new System.Drawing.Point(635, 433);
            this.RAMMaxChangeNumericInput.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.RAMMaxChangeNumericInput.Minimum = new decimal(new int[] {
            2147483647,
            0,
            0,
            -2147483648});
            this.RAMMaxChangeNumericInput.Name = "RAMMaxChangeNumericInput";
            this.RAMMaxChangeNumericInput.Size = new System.Drawing.Size(152, 20);
            this.RAMMaxChangeNumericInput.TabIndex = 28;
            this.RAMMaxChangeNumericInput.Tag = "eventEdit";
            this.RAMMaxChangeNumericInput.ValueChanged += new System.EventHandler(this.numericUpDown2_ValueChanged);
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(351, 461);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(34, 13);
            this.label14.TabIndex = 29;
            this.label14.Tag = "eventEdit";
            this.label14.Text = "Delay";
            // 
            // EventDelayNumericInput
            // 
            this.EventDelayNumericInput.Location = new System.Drawing.Point(391, 459);
            this.EventDelayNumericInput.Name = "EventDelayNumericInput";
            this.EventDelayNumericInput.Size = new System.Drawing.Size(168, 20);
            this.EventDelayNumericInput.TabIndex = 30;
            this.EventDelayNumericInput.Tag = "eventEdit";
            this.EventDelayNumericInput.ValueChanged += new System.EventHandler(this.EventDelayNumericInput_ValueChanged);
            // 
            // EventEnabledCheckBox
            // 
            this.EventEnabledCheckBox.AutoSize = true;
            this.EventEnabledCheckBox.Location = new System.Drawing.Point(425, 305);
            this.EventEnabledCheckBox.Name = "EventEnabledCheckBox";
            this.EventEnabledCheckBox.Size = new System.Drawing.Size(65, 17);
            this.EventEnabledCheckBox.TabIndex = 31;
            this.EventEnabledCheckBox.Tag = "eventEdit";
            this.EventEnabledCheckBox.Text = "Enabled";
            this.EventEnabledCheckBox.UseVisualStyleBackColor = true;
            this.EventEnabledCheckBox.CheckedChanged += new System.EventHandler(this.EventEnabledCheckBox_CheckedChanged);
            // 
            // EventRAMBaseComboBox
            // 
            this.EventRAMBaseComboBox.FormattingEnabled = true;
            this.EventRAMBaseComboBox.Items.AddRange(new object[] {
            "256",
            "100",
            "10"});
            this.EventRAMBaseComboBox.Location = new System.Drawing.Point(391, 354);
            this.EventRAMBaseComboBox.Name = "EventRAMBaseComboBox";
            this.EventRAMBaseComboBox.Size = new System.Drawing.Size(113, 21);
            this.EventRAMBaseComboBox.TabIndex = 32;
            this.EventRAMBaseComboBox.Tag = "eventEdit";
            this.EventRAMBaseComboBox.SelectedIndexChanged += new System.EventHandler(this.EventRAMBaseComboBox_SelectedIndexChanged);
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(354, 354);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(31, 13);
            this.label15.TabIndex = 33;
            this.label15.Tag = "eventEdit";
            this.label15.Text = "Base";
            this.label15.Click += new System.EventHandler(this.label15_Click);
            // 
            // GamesWithEventsListBox
            // 
            this.GamesWithEventsListBox.FormattingEnabled = true;
            this.GamesWithEventsListBox.Location = new System.Drawing.Point(12, 26);
            this.GamesWithEventsListBox.Name = "GamesWithEventsListBox";
            this.GamesWithEventsListBox.Size = new System.Drawing.Size(776, 186);
            this.GamesWithEventsListBox.TabIndex = 38;
            this.GamesWithEventsListBox.SelectedIndexChanged += new System.EventHandler(this.GamesWithEventsListBox_SelectedIndexChanged);
            // 
            // ResetGameToDefaultButton
            // 
            this.ResetGameToDefaultButton.Location = new System.Drawing.Point(12, 378);
            this.ResetGameToDefaultButton.Name = "ResetGameToDefaultButton";
            this.ResetGameToDefaultButton.Size = new System.Drawing.Size(152, 23);
            this.ResetGameToDefaultButton.TabIndex = 39;
            this.ResetGameToDefaultButton.Tag = "eventSelect";
            this.ResetGameToDefaultButton.Text = "Reset Game to Default";
            this.ResetGameToDefaultButton.UseVisualStyleBackColor = true;
            this.ResetGameToDefaultButton.Click += new System.EventHandler(this.ResetGameToDefaultButton_Click);
            // 
            // ShowOrAddCurrentGame
            // 
            this.ShowOrAddCurrentGame.Location = new System.Drawing.Point(12, 218);
            this.ShowOrAddCurrentGame.Name = "ShowOrAddCurrentGame";
            this.ShowOrAddCurrentGame.Size = new System.Drawing.Size(152, 23);
            this.ShowOrAddCurrentGame.TabIndex = 40;
            this.ShowOrAddCurrentGame.Text = "Show/Add Current Game";
            this.ShowOrAddCurrentGame.UseVisualStyleBackColor = true;
            this.ShowOrAddCurrentGame.Click += new System.EventHandler(this.button2_Click);
            // 
            // AddNewEventButton
            // 
            this.AddNewEventButton.Location = new System.Drawing.Point(189, 344);
            this.AddNewEventButton.Name = "AddNewEventButton";
            this.AddNewEventButton.Size = new System.Drawing.Size(152, 23);
            this.AddNewEventButton.TabIndex = 41;
            this.AddNewEventButton.Tag = "eventSelect";
            this.AddNewEventButton.Text = "Add New Event";
            this.AddNewEventButton.UseVisualStyleBackColor = true;
            this.AddNewEventButton.Click += new System.EventHandler(this.AddNewEventButton_Click);
            // 
            // EventCalculationExample
            // 
            this.EventCalculationExample.AutoSize = true;
            this.EventCalculationExample.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EventCalculationExample.Location = new System.Drawing.Point(355, 378);
            this.EventCalculationExample.Name = "EventCalculationExample";
            this.EventCalculationExample.Size = new System.Drawing.Size(68, 11);
            this.EventCalculationExample.TabIndex = 42;
            this.EventCalculationExample.Tag = "eventEdit";
            this.EventCalculationExample.Text = "Example: ";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(521, 357);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(43, 13);
            this.label18.TabIndex = 43;
            this.label18.Tag = "eventEdit";
            this.label18.Text = "Domain";
            this.label18.Click += new System.EventHandler(this.label18_Click);
            // 
            // EventDomainComboBox
            // 
            this.EventDomainComboBox.FormattingEnabled = true;
            this.EventDomainComboBox.Items.AddRange(new object[] {
            "DEFAULT",
            "RAM",
            "WRAM",
            "CartRAM",
            "VRAM",
            "HRAM",
            "RDRAM",
            "68K RAM",
            "Work Ram High",
            "Work Ram Low",
            "IWRAM",
            "EWRAM"});
            this.EventDomainComboBox.Location = new System.Drawing.Point(570, 354);
            this.EventDomainComboBox.Name = "EventDomainComboBox";
            this.EventDomainComboBox.Size = new System.Drawing.Size(218, 21);
            this.EventDomainComboBox.TabIndex = 44;
            this.EventDomainComboBox.Tag = "eventEdit";
            this.EventDomainComboBox.Text = "DEFAULT";
            this.EventDomainComboBox.SelectedIndexChanged += new System.EventHandler(this.EventDomainComboBox_SelectedIndexChanged);
            // 
            // SaveEventChangesButton
            // 
            this.SaveEventChangesButton.Location = new System.Drawing.Point(189, 378);
            this.SaveEventChangesButton.Name = "SaveEventChangesButton";
            this.SaveEventChangesButton.Size = new System.Drawing.Size(152, 23);
            this.SaveEventChangesButton.TabIndex = 45;
            this.SaveEventChangesButton.Tag = "eventSelect";
            this.SaveEventChangesButton.Text = "Save Event Settings";
            this.SaveEventChangesButton.UseVisualStyleBackColor = true;
            this.SaveEventChangesButton.Click += new System.EventHandler(this.SaveEventChangesButton_Click);
            // 
            // RemoveEventButton
            // 
            this.RemoveEventButton.Location = new System.Drawing.Point(12, 344);
            this.RemoveEventButton.Name = "RemoveEventButton";
            this.RemoveEventButton.Size = new System.Drawing.Size(152, 23);
            this.RemoveEventButton.TabIndex = 47;
            this.RemoveEventButton.Text = "Remove Event";
            this.RemoveEventButton.UseVisualStyleBackColor = true;
            this.RemoveEventButton.Click += new System.EventHandler(this.RemoveEventButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(9, 525);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(205, 13);
            this.label1.TabIndex = 48;
            this.label1.Tag = "eventSelect";
            this.label1.Text = "Infinite Lives settings for this game";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 576);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(33, 13);
            this.label2.TabIndex = 49;
            this.label2.Tag = "eventSelect";
            this.label2.Text = "Bytes";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(99, 576);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(39, 13);
            this.label3.TabIndex = 50;
            this.label3.Tag = "eventSelect";
            this.label3.Text = "Values";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(198, 576);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(43, 13);
            this.label4.TabIndex = 51;
            this.label4.Tag = "eventSelect";
            this.label4.Text = "Domain";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(9, 550);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(62, 13);
            this.label5.TabIndex = 52;
            this.label5.Tag = "eventSelect";
            this.label5.Text = "Apply every";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(137, 550);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(38, 13);
            this.label6.TabIndex = 53;
            this.label6.Tag = "eventSelect";
            this.label6.Text = "frames";
            // 
            // livesFrameCount
            // 
            this.livesFrameCount.Location = new System.Drawing.Point(77, 547);
            this.livesFrameCount.Name = "livesFrameCount";
            this.livesFrameCount.Size = new System.Drawing.Size(55, 20);
            this.livesFrameCount.TabIndex = 54;
            this.livesFrameCount.Tag = "eventSelect";
            // 
            // livesBytes0
            // 
            this.livesBytes0.Location = new System.Drawing.Point(12, 592);
            this.livesBytes0.Name = "livesBytes0";
            this.livesBytes0.Size = new System.Drawing.Size(73, 20);
            this.livesBytes0.TabIndex = 55;
            this.livesBytes0.Tag = "eventSelect";
            // 
            // livesValue0
            // 
            this.livesValue0.Location = new System.Drawing.Point(102, 592);
            this.livesValue0.Name = "livesValue0";
            this.livesValue0.Size = new System.Drawing.Size(73, 20);
            this.livesValue0.TabIndex = 56;
            this.livesValue0.Tag = "eventSelect";
            // 
            // livesDomain0
            // 
            this.livesDomain0.Location = new System.Drawing.Point(201, 592);
            this.livesDomain0.Name = "livesDomain0";
            this.livesDomain0.Size = new System.Drawing.Size(73, 20);
            this.livesDomain0.TabIndex = 57;
            this.livesDomain0.Tag = "eventSelect";
            // 
            // livesBytes1
            // 
            this.livesBytes1.Location = new System.Drawing.Point(12, 618);
            this.livesBytes1.Name = "livesBytes1";
            this.livesBytes1.Size = new System.Drawing.Size(73, 20);
            this.livesBytes1.TabIndex = 58;
            this.livesBytes1.Tag = "eventSelect";
            // 
            // livesValue1
            // 
            this.livesValue1.Location = new System.Drawing.Point(102, 618);
            this.livesValue1.Name = "livesValue1";
            this.livesValue1.Size = new System.Drawing.Size(73, 20);
            this.livesValue1.TabIndex = 59;
            this.livesValue1.Tag = "eventSelect";
            // 
            // livesDomain1
            // 
            this.livesDomain1.Location = new System.Drawing.Point(201, 618);
            this.livesDomain1.Name = "livesDomain1";
            this.livesDomain1.Size = new System.Drawing.Size(73, 20);
            this.livesDomain1.TabIndex = 60;
            this.livesDomain1.Tag = "eventSelect";
            // 
            // livesBytes2
            // 
            this.livesBytes2.Location = new System.Drawing.Point(12, 644);
            this.livesBytes2.Name = "livesBytes2";
            this.livesBytes2.Size = new System.Drawing.Size(73, 20);
            this.livesBytes2.TabIndex = 61;
            this.livesBytes2.Tag = "eventSelect";
            // 
            // livesValue2
            // 
            this.livesValue2.Location = new System.Drawing.Point(102, 644);
            this.livesValue2.Name = "livesValue2";
            this.livesValue2.Size = new System.Drawing.Size(73, 20);
            this.livesValue2.TabIndex = 62;
            this.livesValue2.Tag = "eventSelect";
            // 
            // livesDomain2
            // 
            this.livesDomain2.Location = new System.Drawing.Point(201, 644);
            this.livesDomain2.Name = "livesDomain2";
            this.livesDomain2.Size = new System.Drawing.Size(73, 20);
            this.livesDomain2.TabIndex = 63;
            this.livesDomain2.Tag = "eventSelect";
            // 
            // livesBytes3
            // 
            this.livesBytes3.Location = new System.Drawing.Point(12, 670);
            this.livesBytes3.Name = "livesBytes3";
            this.livesBytes3.Size = new System.Drawing.Size(73, 20);
            this.livesBytes3.TabIndex = 64;
            this.livesBytes3.Tag = "eventSelect";
            // 
            // livesValue3
            // 
            this.livesValue3.Location = new System.Drawing.Point(102, 670);
            this.livesValue3.Name = "livesValue3";
            this.livesValue3.Size = new System.Drawing.Size(73, 20);
            this.livesValue3.TabIndex = 65;
            this.livesValue3.Tag = "eventSelect";
            // 
            // livesDomain3
            // 
            this.livesDomain3.Location = new System.Drawing.Point(201, 670);
            this.livesDomain3.Name = "livesDomain3";
            this.livesDomain3.Size = new System.Drawing.Size(73, 20);
            this.livesDomain3.TabIndex = 66;
            this.livesDomain3.Tag = "eventSelect";
            // 
            // saveLivesSettingsButton
            // 
            this.saveLivesSettingsButton.Location = new System.Drawing.Point(102, 703);
            this.saveLivesSettingsButton.Name = "saveLivesSettingsButton";
            this.saveLivesSettingsButton.Size = new System.Drawing.Size(172, 23);
            this.saveLivesSettingsButton.TabIndex = 67;
            this.saveLivesSettingsButton.Tag = "eventSelect";
            this.saveLivesSettingsButton.Text = "Save Infinite Lives Settings";
            this.saveLivesSettingsButton.UseVisualStyleBackColor = true;
            this.saveLivesSettingsButton.Click += new System.EventHandler(this.saveLivesSettingsButton_Click);
            // 
            // horizontalLine1
            // 
            this.horizontalLine1.Location = new System.Drawing.Point(12, 260);
            this.horizontalLine1.Name = "horizontalLine1";
            this.horizontalLine1.Size = new System.Drawing.Size(775, 2);
            this.horizontalLine1.TabIndex = 68;
            this.horizontalLine1.Text = "horizontalLine1";
            this.horizontalLine1.Click += new System.EventHandler(this.horizontalLine1_Click);
            // 
            // horizontalLine2
            // 
            this.horizontalLine2.Location = new System.Drawing.Point(12, 520);
            this.horizontalLine2.Name = "horizontalLine2";
            this.horizontalLine2.Size = new System.Drawing.Size(775, 2);
            this.horizontalLine2.TabIndex = 69;
            this.horizontalLine2.Text = "horizontalLine2";
            // 
            // blockZeroCheckBox
            // 
            this.blockZeroCheckBox.AutoSize = true;
            this.blockZeroCheckBox.Location = new System.Drawing.Point(635, 462);
            this.blockZeroCheckBox.Name = "blockZeroCheckBox";
            this.blockZeroCheckBox.Size = new System.Drawing.Size(148, 17);
            this.blockZeroCheckBox.TabIndex = 71;
            this.blockZeroCheckBox.Tag = "eventEdit";
            this.blockZeroCheckBox.Text = "ignore change if from zero";
            this.blockZeroCheckBox.UseVisualStyleBackColor = true;
            this.blockZeroCheckBox.CheckedChanged += new System.EventHandler(this.blockZeroCheckBox_CheckedChanged);
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(351, 487);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(34, 13);
			this.label16.TabIndex = 72;
			this.label16.Tag = "eventEdit";
			this.label16.Text = "Stepped change event buffer";
			// 
			// EventSteppedChangeEventBufferNumericInput
			// 
			this.EventSteppedChangeEventBufferNumericInput.Location = new System.Drawing.Point(498, 484);
			this.EventSteppedChangeEventBufferNumericInput.Name = "EventSteppedChangeEventBufferNumericInput";
			this.EventSteppedChangeEventBufferNumericInput.Size = new System.Drawing.Size(61, 20);
			this.EventSteppedChangeEventBufferNumericInput.TabIndex = 73;
			this.EventSteppedChangeEventBufferNumericInput.Tag = "eventEdit";
			this.EventSteppedChangeEventBufferNumericInput.ValueChanged += new System.EventHandler(this.EventSteppedChangeEventBufferNumericInput_ValueChanged);
			// 
			// MainWindow
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(799, 738);
            this.Controls.Add(this.EventSteppedChangeEventBufferNumericInput);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.blockZeroCheckBox);
            this.Controls.Add(this.horizontalLine2);
            this.Controls.Add(this.horizontalLine1);
            this.Controls.Add(this.saveLivesSettingsButton);
            this.Controls.Add(this.livesDomain3);
            this.Controls.Add(this.livesValue3);
            this.Controls.Add(this.livesBytes3);
            this.Controls.Add(this.livesDomain2);
            this.Controls.Add(this.livesValue2);
            this.Controls.Add(this.livesBytes2);
            this.Controls.Add(this.livesDomain1);
            this.Controls.Add(this.livesValue1);
            this.Controls.Add(this.livesBytes1);
            this.Controls.Add(this.livesDomain0);
            this.Controls.Add(this.livesValue0);
            this.Controls.Add(this.livesBytes0);
            this.Controls.Add(this.livesFrameCount);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.RemoveEventButton);
            this.Controls.Add(this.SaveEventChangesButton);
            this.Controls.Add(this.EventDomainComboBox);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.EventCalculationExample);
            this.Controls.Add(this.AddNewEventButton);
            this.Controls.Add(this.ShowOrAddCurrentGame);
            this.Controls.Add(this.ResetGameToDefaultButton);
            this.Controls.Add(this.GamesWithEventsListBox);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.EventRAMBaseComboBox);
            this.Controls.Add(this.EventEnabledCheckBox);
            this.Controls.Add(this.EventDelayNumericInput);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.RAMMaxChangeNumericInput);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.RAMMinChangeNumericInput);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.RAMValuesToCheckTextBox);
            this.Controls.Add(this.EventNameTextBox);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.EventsThisGameListBox);
            this.Controls.Add(this.label8);
            this.Name = "MainWindow";
            this.Text = "Event Shuffler Setup";
            ((System.ComponentModel.ISupportInitialize)(this.RAMMinChangeNumericInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RAMMaxChangeNumericInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.EventDelayNumericInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.EventSteppedChangeEventBufferNumericInput)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.ListBox EventsThisGameListBox;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.TextBox EventNameTextBox;
		private System.Windows.Forms.TextBox RAMValuesToCheckTextBox;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.NumericUpDown RAMMinChangeNumericInput;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.NumericUpDown RAMMaxChangeNumericInput;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.NumericUpDown EventDelayNumericInput;
		private System.Windows.Forms.CheckBox EventEnabledCheckBox;
		private System.Windows.Forms.ComboBox EventRAMBaseComboBox;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.ListBox GamesWithEventsListBox;
		private System.Windows.Forms.Button ResetGameToDefaultButton;
		private System.Windows.Forms.Button ShowOrAddCurrentGame;
		private System.Windows.Forms.Button AddNewEventButton;
		private System.Windows.Forms.Label EventCalculationExample;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.ComboBox EventDomainComboBox;
		private System.Windows.Forms.Button SaveEventChangesButton;
		private System.Windows.Forms.Button RemoveEventButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.TextBox livesFrameCount;
		private System.Windows.Forms.TextBox livesBytes0;
		private System.Windows.Forms.TextBox livesValue0;
		private System.Windows.Forms.TextBox livesDomain0;
		private System.Windows.Forms.TextBox livesBytes1;
		private System.Windows.Forms.TextBox livesValue1;
		private System.Windows.Forms.TextBox livesDomain1;
		private System.Windows.Forms.TextBox livesBytes2;
		private System.Windows.Forms.TextBox livesValue2;
		private System.Windows.Forms.TextBox livesDomain2;
		private System.Windows.Forms.TextBox livesBytes3;
		private System.Windows.Forms.TextBox livesValue3;
		private System.Windows.Forms.TextBox livesDomain3;
		private System.Windows.Forms.Button saveLivesSettingsButton;
		private HorizontalLine horizontalLine1;
		private HorizontalLine horizontalLine2;
		private System.Windows.Forms.CheckBox blockZeroCheckBox;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.NumericUpDown EventSteppedChangeEventBufferNumericInput;
	}
}

