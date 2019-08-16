using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Client.EmuHawk.Filters;
using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <remarks>Fields for <see cref="Control">controls</see> are declared above the method in which they're initialised.</remarks>
	/// <seealso cref="MainForm"/>
	public sealed class DisplayConfigLite : Form
	{
		public bool NeedReset; //TODO can we use DialogResult.Yes/No/Abort instead of OK/Abort plus this extra bool?

		/// <remarks>HACK NullHawk's settings don't normally persist to config</remarks>
		private static NullEmulator.NullEmulatorSettings NullHawkSettings
		{
			get
			{
				return (Global.Emulator as NullEmulator)?.GetSettings()
					?? (NullEmulator.NullEmulatorSettings) Global.Config.GetCoreSettings<NullEmulator>();
			}
			set
			{
				//TODO double write? should this be if-else?
				Global.Config.PutCoreSettings<NullEmulator>(value);
				if (Global.Emulator is NullEmulator) ((dynamic) Global.Emulator).PutSettings(value);
			}
		}

		private readonly IContainer Components = new Container();
		private readonly ComponentResourceManager Resources = new ComponentResourceManager(typeof(DisplayConfigLite));

		private string PathSelection = Global.Config.DispUserFilterPath ?? "";

		public DisplayConfigLite()
		{
			var lnlblDocs = new LinkLabel
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
				AutoSize = true,
				Location = new Point(10, 355),
				Text = "DisplayConfig wiki page"
			};
			lnlblDocs.LinkClicked += (sender, e) => Process.Start("http://tasvideos.org/Bizhawk/DisplayConfig.html");

			var btnOk = new Button
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				Location = new Point(360, 355),
				Size = new Size(75, 23),
				Text = "OK",
				UseVisualStyleBackColor = true
			};
			btnOk.Click += (sender, e) => SaveControlsTo(Global.Config);

			var btnCancel = new Button
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				DialogResult = DialogResult.Cancel,
				Location = new Point(440, 355),
				Size = new Size(75, 23),
				Text = "Cancel",
				UseVisualStyleBackColor = true
			};

			SuspendLayout();
			AcceptButton = btnOk;
			AutoScaleDimensions = new SizeF(6f, 13f);
			AutoScaleMode = AutoScaleMode.Font;
			CancelButton = btnCancel;
			ClientSize = new Size(530, 390);
			Controls.AddRange(new Control[]
			{
				new TabControl
				{
					Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
					Controls = { CreateScalingTab(), CreateDispMethodTab(), CreateMiscTab(), CreateWindowTab() },
					Location = new Point(12, 12),
					Size = new Size(510, 340)
				},
				lnlblDocs,
				btnOk,
				btnCancel
			});
			FormBorderStyle = FormBorderStyle.FixedDialog;
			Name = typeof(DisplayConfigLite).Name;
			StartPosition = FormStartPosition.CenterParent;
			Text = "Display Configuration";
			ResumeLayout();
		}

		private RadioButton rbNone;
		private RadioButton rbHq2x;
		private Label lblScanlines;
		private TransparentTrackBar tbScanlineIntensity;
		private RadioButton rbScanlines;
		private RadioButton rbUser;
		private Label lblUserFilterName;

		private TLPInGroupBox CreateScalingFilterGroupBox()
		{
			rbNone = new RadioButton
			{
				AutoSize = true,
				Text = "None",
				UseVisualStyleBackColor = true
			};

			rbHq2x = new RadioButton
			{
				AutoSize = true,
				Text = "Hq2x",
				UseVisualStyleBackColor = true
			};

			lblScanlines = new Label
			{
				Anchor = AnchorStyles.Top,
				AutoSize = true,
				Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular, GraphicsUnit.Point, 0)
			};

			tbScanlineIntensity = new TransparentTrackBar
			{
				LargeChange = 32,
				Maximum = 256,
				Size = new Size(70, 30),
				TickFrequency = 32,
				TickStyle = TickStyle.TopLeft
			};
			tbScanlineIntensity.ValueChanged += (sender, e) => { lblScanlines.Text = $"{tbScanlineIntensity.Value / 256f:P}"; };
			tbScanlineIntensity.Value = Global.Config.TargetScanlineFilterIntensity;

			var flpScanlinesSliderLabel = new SingleColumnFLP
			{
				Controls = { lblScanlines, tbScanlineIntensity }
			};

			rbScanlines = new RadioButton
			{
				AutoSize = true,
				Text = "Scanlines",
				UseVisualStyleBackColor = true
			};

			rbUser = new RadioButton
			{
				AutoSize = true,
				Text = "User",
				UseVisualStyleBackColor = true
			};

			var btnSelectUserFilter = new Button
			{
				Size = new Size(75, 23),
				Text = "Select",
				UseVisualStyleBackColor = true
			};
			btnSelectUserFilter.Click += (sender, e) =>
			{
				var ofd = new OpenFileDialog { FileName = PathSelection, Filter = ".CGP (*.cgp)|*.cgp" };
				if (ofd.ShowDialog() != DialogResult.OK) return;
				var choice = Path.GetFullPath(ofd.FileName);
				using (var stream = File.OpenRead(choice))
				{
					// test the preset
					var cgp = new RetroShaderPreset(stream);
					if (cgp.ContainsGLSL)
					{
						MessageBox.Show("Specified CGP contains references to .glsl files. This is illegal. Use .cg");
						return;
					}
					RetroShaderChain filter = null;
					// try compiling it
					try
					{
						filter = new RetroShaderChain(GlobalWin.GL, cgp, Path.GetDirectoryName(choice));
					}
					catch
					{
						// ignored
					}
					if (filter?.Available != true)
					{
						new ExceptionBox(filter?.Errors).ShowDialog();
						return;
					}
				}
				rbUser.Checked = true;
				PathSelection = choice;
				UpdateUserFilterLabel();
			};

			lblUserFilterName = new Label
			{
				Anchor = AnchorStyles.Left,
				Text = "Will contain user filter name"
			};

			var grpScalingFilter = new TLPInGroupBox(2, 5)
			{
				Margin = new Padding(3, 0, 3, 0),
				Size = new Size(180, 170),
				Text = "Scaling Filter"
			};
			grpScalingFilter.Controls.Add(rbNone, 0, 0);
			grpScalingFilter.Controls.Add(rbHq2x, 0, 1);
			grpScalingFilter.Controls.Add(flpScanlinesSliderLabel, 1, 1);
			grpScalingFilter.InnerTLP.SetRowSpan(flpScanlinesSliderLabel, 2);
			grpScalingFilter.Controls.Add(rbScanlines, 0, 2);
			grpScalingFilter.Controls.Add(rbUser, 0, 3);
			grpScalingFilter.Controls.Add(btnSelectUserFilter, 1, 3);
			grpScalingFilter.Controls.Add(lblUserFilterName, 0, 4);
			grpScalingFilter.InnerTLP.SetColumnSpan(lblUserFilterName, 2);
			switch (Global.Config.TargetDisplayFilter)
			{
				case 0:
					rbNone.Checked = true;
					break;
				case 1:
					rbHq2x.Checked = true;
					break;
				case 2:
					rbScanlines.Checked = true;
					break;
				case 3:
					rbUser.Checked = true;
					break;
			}
			UpdateUserFilterLabel();

			return grpScalingFilter;
		}

		private RadioButton rbUseRaw;
		private RadioButton rbUseSystem;
		private RadioButton rbUseCustom;
		private TextBox txtCustomARWidth;
		private TextBox txtCustomARHeight;
		private RadioButton rbUseCustomRatio;
		private TextBox txtCustomARX;
		private TextBox txtCustomARY;

		private TLPInGroupBox CreateARSelectionGroupBox()
		{
			rbUseRaw = new RadioButton
			{
				AutoSize = true,
				Text = "Use 1:1 pixel size (for crispness or debugging)",
				UseVisualStyleBackColor = true
			};

			var lblNonSquareAR = new Label
			{
				AutoSize = true,
				Text = "Allowing pixel distortion (e.g. 2x1 pixels, for better AR fit):"
			};

			rbUseSystem = new RadioButton
			{
				AutoSize = true,
				Text = "Use system's recommendation",
				UseVisualStyleBackColor = true
			};

			rbUseCustom = new RadioButton
			{
				AutoSize = true,
				Text = "Use custom size:",
				UseVisualStyleBackColor = true
			};

			var textBoxSize = new Size(60, 20);
			txtCustomARWidth = new TextBox { Size = textBoxSize };

			var lblWHSeparator = new Label
			{
				AutoSize = true,
				Margin = new Padding(0, 5, 0, 0),
				Text = "x"
			};

			txtCustomARHeight = new TextBox { Size = textBoxSize };

			rbUseCustomRatio = new RadioButton
			{
				AutoSize = true,
				Text = "Use custom AR:",
				UseVisualStyleBackColor = true
			};

			txtCustomARX = new TextBox { Size = textBoxSize };

			var lblXYSeparator = new Label
			{
				AutoSize = true,
				Margin = new Padding(0, 5, 0, 0),
				Text = ":"
			};

			txtCustomARY = new TextBox { Size = textBoxSize };

			var grpARSelection = new TLPInGroupBox(4, 5)
			{
				Size = new Size(300, 130),
				Text = "Aspect Ratio Selection"
			};
			grpARSelection.Controls.Add(rbUseRaw, 0, 0);
			grpARSelection.InnerTLP.SetColumnSpan(rbUseRaw, 4);
			grpARSelection.Controls.Add(lblNonSquareAR, 0, 1);
			grpARSelection.InnerTLP.SetColumnSpan(lblNonSquareAR, 4);
			grpARSelection.Controls.Add(rbUseSystem, 0, 2);
			grpARSelection.InnerTLP.SetColumnSpan(rbUseSystem, 4);
			grpARSelection.Controls.Add(rbUseCustom, 0, 3);
			grpARSelection.Controls.Add(txtCustomARWidth, 1, 3);
			grpARSelection.Controls.Add(lblWHSeparator, 2, 3);
			grpARSelection.Controls.Add(txtCustomARHeight, 3, 3);
			grpARSelection.Controls.Add(rbUseCustomRatio, 0, 4);
			grpARSelection.Controls.Add(txtCustomARX, 1, 4);
			grpARSelection.Controls.Add(lblXYSeparator, 2, 4);
			grpARSelection.Controls.Add(txtCustomARY, 3, 4);
			if (Global.Config.DispCustomUserARWidth != -1) txtCustomARWidth.Text = Global.Config.DispCustomUserARWidth.ToString();
			if (Global.Config.DispCustomUserARHeight != -1) txtCustomARHeight.Text = Global.Config.DispCustomUserARHeight.ToString();
			if (!Global.Config.DispCustomUserARX.HawkFloatEquality(-1)) txtCustomARX.Text = Global.Config.DispCustomUserARX.ToString();
			if (!Global.Config.DispCustomUserARY.HawkFloatEquality(-1)) txtCustomARY.Text = Global.Config.DispCustomUserARY.ToString();
			switch (Global.Config.DispManagerAR)
			{
				case Config.EDispManagerAR.None:
					rbUseRaw.Checked = true;
					break;
				case Config.EDispManagerAR.System:
					rbUseSystem.Checked = true;
					break;
				case Config.EDispManagerAR.Custom:
					rbUseCustom.Checked = true;
					break;
				case Config.EDispManagerAR.CustomRatio:
					rbUseCustomRatio.Checked = true;
					break;
			}

			return grpARSelection;
		}

		//TODO is there a reason these aren't NumericUpDowns?
		private TextBox txtCropLeft;
		private TextBox txtCropTop;
		private TextBox txtCropRight;
		private TextBox txtCropBottom;

		private FLPInGroupBox CreateCropOptionsGroupBox()
		{
			Tuple<TextBox, SingleRowFLP> genLabelledTextBox(string labelText, string textBoxName, string textBoxInit)
			{
				var textBox = new TextBox
				{
					Margin = Padding.Empty,
					Name = textBoxName,
					Size = new Size(34, 20),
					Text = textBoxInit
				};
				return new Tuple<TextBox, SingleRowFLP>(
					textBox,
					new SingleRowFLP
					{
						AutoSize = true,
						Controls =
						{
							new Label
							{
								Anchor = AnchorStyles.None,
								AutoSize = true,
								Text = labelText
							},
							textBox
						},
						Margin = Padding.Empty,
						WrapContents = false
					}
				);
			}

			var left = genLabelledTextBox("Left:", "txtCropLeft", Global.Config.DispCropLeft.ToString());
			txtCropLeft = left.Item1;

			var top = genLabelledTextBox("Top:", "txtCropTop", Global.Config.DispCropTop.ToString());
			txtCropTop = top.Item1;

			var right = genLabelledTextBox("Right:", "txtCropRight", Global.Config.DispCropRight.ToString());
			txtCropRight = right.Item1;

			var bottom = genLabelledTextBox("Bottom:", "txtCropBottom", Global.Config.DispCropBottom.ToString());
			txtCropBottom = bottom.Item1;

			return new FLPInGroupBox
			{
				Controls = { left.Item2, top.Item2, right.Item2, bottom.Item2 },
				InnerFLP = { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Size = new Size(295, 20) },
				Size = new Size(310, 45),
				Text = "Cropping"
			};
		}

		private NumericUpDown nudPrescale;
		private CheckBox cbAutoPrescale;
		private RadioButton rbFinalFilterNone;
		private RadioButton rbFinalFilterBicubic;
		private RadioButton rbFinalFilterBilinear;
		private CheckBox cbLetterbox;
		private CheckBox cbPadInteger;
		private SingleColumnFLP flpAspectRatio;

		private TabPage CreateScalingTab()
		{
			nudPrescale = new NumericUpDown
			{
				Maximum = 16M,
				Minimum = 1M,
				Size = new Size(45, 20),
				Value = Global.Config.DispPrescale
			};

			cbAutoPrescale = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispAutoPrescale,
				Margin = new Padding(3, 3, 3, 0),
				Text = "Auto Prescale",
				UseVisualStyleBackColor = true
			};

			rbFinalFilterNone = new RadioButton
			{
				AutoSize = true,
				Text = "None",
				UseVisualStyleBackColor = true
			};

			rbFinalFilterBilinear = new RadioButton
			{
				AutoSize = true,
				Text = "Bilinear",
				UseVisualStyleBackColor = true
			};

			rbFinalFilterBicubic = new RadioButton
			{
				AutoSize = true,
				Text = "Bicubic (shader. buggy?)",
				UseVisualStyleBackColor = true
			};

			var grpFinalFilter = new FLPInGroupBox
			{
				Controls = { rbFinalFilterNone, rbFinalFilterBilinear, rbFinalFilterBicubic },
				InnerFLP = { AutoSize = true },
				Margin = new Padding(3, 0, 3, 0),
				Size = new Size(180, 90),
				Text = "Final Filter"
			};
			switch (Global.Config.DispFinalFilter)
			{
				case 0:
					rbFinalFilterNone.Checked = true;
					break;
				case 1:
					rbFinalFilterBilinear.Checked = true;
					break;
				case 2:
					rbFinalFilterBicubic.Checked = true;
					break;
			}

			cbLetterbox = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispFixAspectRatio,
				Text = "Maintain aspect ratio (letterbox)",
				UseVisualStyleBackColor = true
			};
			cbLetterbox.CheckedChanged += (sender, e) => flpAspectRatio.Enabled = cbLetterbox.Checked;

			cbPadInteger = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispFixScaleInteger,
				Text = "Expand pixels by integers only (e.g. no 1.3333x)",
				UseVisualStyleBackColor = true
			};

			flpAspectRatio = new SingleColumnFLP
			{
				Controls = { CreateARSelectionGroupBox(), cbPadInteger },
				Enabled = cbLetterbox.Checked
			};

			var btnDefaults = new Button
			{
				Anchor = AnchorStyles.Right,
				Text = "Defaults",
				UseVisualStyleBackColor = true
			};
			btnDefaults.Click += (sender, e) =>
			{
				nudPrescale.Value = 1;
				rbNone.Checked = true;
				cbAutoPrescale.Checked = true;
				rbFinalFilterBilinear.Checked = true;
				cbLetterbox.Checked = true;
				rbUseSystem.Checked = true;
				txtCropLeft.Text = "0";
				txtCropTop.Text = "0";
				txtCropRight.Text = "0";
				txtCropBottom.Text = "0";
			};
			new ToolTip(Components).SetToolTip(btnDefaults, "Unless someone forgets to update this button's code when they change said defaults...");

			var flpScalingTab = new FlowLayoutPanel
			{
				Controls =
				{
					new SingleRowFLP
					{
						Controls =
						{
							new Label
							{
								Anchor = AnchorStyles.None,
								AutoSize = true,
								Text = "User Prescale:"
							},
							nudPrescale,
							new Label
							{
								Anchor = AnchorStyles.None,
								AutoSize = true,
								Text = "X"
							}
						},
						Margin = new Padding(3, 0, 3, 0)
					},
					CreateScalingFilterGroupBox(),
					cbAutoPrescale,
					grpFinalFilter,

					cbLetterbox,
					flpAspectRatio,
					CreateCropOptionsGroupBox(),
					btnDefaults
				},
				FlowDirection = FlowDirection.TopDown,
				Size = new Size(535, 370)
			};
			flpScalingTab.SetFlowBreak(grpFinalFilter, true);

			return new TabPage
			{
				Controls = { flpScalingTab },
				Padding = new Padding(3),
				Size = new Size(542, 351),
				Text = "Scaling && Filtering",
				UseVisualStyleBackColor = true
			};
		}

		private RadioButton rbD3D9;
		private CheckBox cbAlternateVsync;
		private RadioButton rbOpenGL;
		private RadioButton rbGDIPlus;

		private TabPage CreateDispMethodTab()
		{
			rbD3D9 = new RadioButton
			{
				AutoSize = true,
				Enabled = OSTailoredCode.CurrentOS == OSTailoredCode.DistinctOS.Windows, // SlimDX obviously doesn't work on Unix
				Margin = new Padding(3, 0, 3, 0),
				TabIndex = 1,
				Text = "Direct3D9",
				UseVisualStyleBackColor = true
			};

			cbAlternateVsync = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispAlternateVsync,
				TabIndex = 0,
				Text = "Use alternate VSync method",
				UseVisualStyleBackColor = true
			};

			rbOpenGL = new RadioButton
			{
				AutoSize = true,
				Margin = new Padding(3, 0, 3, 0),
				TabIndex = 1,
				Text = "OpenGL",
				UseVisualStyleBackColor = true
			};

			rbGDIPlus = new RadioButton
			{
				AutoSize = true,
				Margin = new Padding(3, 0, 3, 0),
				TabIndex = 1,
				Text = "GDI+",
				UseVisualStyleBackColor = true
			};

			var indent = new Padding(24, 0, 0, 0);
			var grpDispMethodRadios = new FLPInGroupBox
			{
				Controls =
				{
					rbD3D9,
					new SingleColumnFLP
					{
						Controls =
						{
							new Label
							{
								AutoSize = true,
								Text = "• Probably the best for older graphics cards\n• May cause issues with OpenGL-based cores (i.e. Mupen64Plus)"
							},
							cbAlternateVsync,
							new Label
							{
								Margin = indent,
								Size = new Size(359, 43),
								Text = Resources.GetString("lblD3DAltVSync.Text")
							}
						},
						Enabled = rbD3D9.Enabled, // enabled with SlimDX radio button
						Margin = indent,
						Size = new Size(392, 76)
					},
					rbOpenGL,
					new Label
					{
						AutoSize = true,
						Margin = indent,
						Text = "• May have reduced performance or even malfunction on some systems\n• May have increased performance with OpenGL-based emulation cores (i.e. Mupen64Plus)"
					},
					rbGDIPlus,
					new Label
					{
						AutoSize = true,
						Margin = indent,
						Text = "• Slow and missing features, kept in to maximise compatibility\n• Works better over MS' Remote Desktop Protocol"
					}
				},
				InnerFLP = { AutoSize = true, Size = new Size(398, 228) },
				Size = new Size(480, 230)
			};
			switch (Global.Config.DispMethod)
			{
				case Config.EDispMethod.OpenGL:
					rbOpenGL.Checked = true;
					break;
				case Config.EDispMethod.GdiPlus:
					rbGDIPlus.Checked = true;
					break;
				case Config.EDispMethod.SlimDX9:
					rbD3D9.Checked = true;
					break;
			}

			return new TabPage
			{
				Controls =
				{
					new SingleColumnFLP
					{
						Controls =
						{
							grpDispMethodRadios,
							new Label
							{
								AutoSize = true,
								Text = "Changes require restart of program to take effect.\n\nPlease note that, for now, Mupen64Plus will use OpenGL regardless of this setting."
							}
						},
						Size = new Size(416, 306)
					}
				},
				Size = new Size(542, 351),
				Text = "Display Method",
				UseVisualStyleBackColor = true
			};
		}

		private RadioButton rbDisplayFull;
		private RadioButton rbDisplayMinimal;
		private RadioButton rbDisplayAbsoluteZero;
		private CheckBox cbNullHawkWhiteNoise;

		private TabPage CreateMiscTab()
		{
			rbDisplayFull = new RadioButton
			{
				AutoSize = true,
				Text = "Full - Display Everything",
				UseVisualStyleBackColor = true
			};

			rbDisplayMinimal = new RadioButton
			{
				AutoSize = true,
				Enabled = false, //TODO how long has this gone unimplemented? --yoshi
				Text = "Minimal - Display HUD Only (TBD)",
				UseVisualStyleBackColor = true
			};

			rbDisplayAbsoluteZero = new RadioButton
			{
				AutoSize = true,
				Text = "Absolute Zero - Display Nothing",
				UseVisualStyleBackColor = true
			};

			var grpDispFeatures = new FLPInGroupBox
			{
				Controls = { rbDisplayFull, rbDisplayMinimal, rbDisplayAbsoluteZero },
				Size = new Size(371, 96),
				Text = "Display Features (for speeding up replays)"
			};
			switch (Global.Config.DispSpeedupFeatures)
			{
				case 0:
					rbDisplayAbsoluteZero.Checked = true;
					break;
				case 1:
					rbDisplayMinimal.Checked = true;
					break;
				case 2:
					rbDisplayFull.Checked = true;
					break;
			}

			cbNullHawkWhiteNoise = new CheckBox
			{
				AutoSize = true,
				Checked = NullHawkSettings.SnowyDisplay,
				Text = "Enable Snowy Null Emulator",
				UseVisualStyleBackColor = true
			};

			return new TabPage
			{
				Controls = {
					new SingleColumnFLP
					{
						Controls = {
							grpDispFeatures,
							cbNullHawkWhiteNoise,
							new Label
							{
								AutoSize = true,
								Text = "Some people think the white noise is a great idea, and some people don't.\nDisabling this displays black instead."
							}
						},
						Size = new Size(404, 168)
					}
				},
				Size = new Size(542, 351),
				Text = "Misc.",
				UseVisualStyleBackColor = true
			};
		}

		private Label lblFrameTypeWindowed;
		private TransparentTrackBar tbFrameSizeWindowed;
		private CheckBox cbStatusBarWindowed;
		private CheckBox cbCaptionWindowed;
		private CheckBox cbMenuWindowed;

		private FLPInGroupBox CreateWindowedGroupBox()
		{
			lblFrameTypeWindowed = new Label { AutoSize = true };

			tbFrameSizeWindowed = new TransparentTrackBar
			{
				LargeChange = 1,
				Maximum = 2,
				Value = Global.Config.DispChrome_FrameWindowed
			};
			EventHandler updateTrackbarLabel = (sender, e) => lblFrameTypeWindowed.Text = ((Config.FrameWindowThickness) tbFrameSizeWindowed.Value).ToString("f");
			tbFrameSizeWindowed.ValueChanged += updateTrackbarLabel;
			updateTrackbarLabel(null, null);

			cbStatusBarWindowed = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispChrome_StatusBarWindowed,
				Text = "Status Bar",
				UseVisualStyleBackColor = true
			};

			cbCaptionWindowed = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispChrome_CaptionWindowed,
				Text = "Caption",
				UseVisualStyleBackColor = true
			};

			cbMenuWindowed = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispChrome_MenuWindowed,
				Text = "Menu",
				UseVisualStyleBackColor = true
			};

			return new FLPInGroupBox
			{
				Controls = {
					new SingleRowFLP
					{
						Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
						Controls = {
							new Label
							{
								AutoSize = true,
								Text = "Frame:"
							},
							lblFrameTypeWindowed
						}
					},
					tbFrameSizeWindowed,
					cbStatusBarWindowed,
					cbCaptionWindowed,
					cbMenuWindowed
				},
				Size = new Size(131, 211),
				Text = "Windowed"
			};
		}

		private CheckBox cbStatusBarFullscreen;
		private CheckBox cbFSAutohideMouse;
		private CheckBox cbMenuFullscreen;
		private CheckBox cbFullscreenHacks;

		private FLPInGroupBox CreateFullscreenGroupBox()
		{
			cbStatusBarFullscreen = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispChrome_StatusBarFullscreen,
				Text = "Status Bar",
				UseVisualStyleBackColor = true
			};

			cbFSAutohideMouse = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispChrome_Fullscreen_AutohideMouse,
				Text = "Auto-Hide Mouse Cursor",
				UseVisualStyleBackColor = true
			};

			cbMenuFullscreen = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispChrome_MenuFullscreen,
				Text = "Menu",
				UseVisualStyleBackColor = true
			};

			cbFullscreenHacks = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispFullscreenHacks,
				Text = "Enable Windows Fullscreen Hacks",
				UseVisualStyleBackColor = true
			};

			return new FLPInGroupBox
			{
				Controls =
				{
					new FlowLayoutPanel
					{
						Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
						AutoSize = true,
						Controls = { cbStatusBarFullscreen, cbFSAutohideMouse, cbMenuFullscreen, cbFullscreenHacks }
					},
					new Label { Size = new Size(240, 115), Text = Resources.GetString("lblFullscreenHacks.Text") }
				},
				Size = new Size(266, 211),
				Text = "Fullscreen"
			};
		}

		private CheckBox cbAllowDoubleclickFullscreen;

		private TabPage CreateWindowTab()
		{
			cbAllowDoubleclickFullscreen = new CheckBox
			{
				AutoSize = true,
				Checked = Global.Config.DispChrome_AllowDoubleClickFullscreen,
				Text = "Allow Double-Click Fullscreen (hold shift to force fullscreen to toggle in case using zapper, etc.)",
				UseVisualStyleBackColor = true
			};

			return new TabPage
			{
				Controls =
				{
					new SingleColumnFLP
					{
						Controls =
						{
							new SingleRowFLP
							{
								Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
								Controls = { CreateWindowedGroupBox(), CreateFullscreenGroupBox() }
							},
							cbAllowDoubleclickFullscreen
						},
						Dock = DockStyle.Fill,
						Size = new Size(536, 375)
					}
				},
				Padding = new Padding(3),
				Size = new Size(542, 351),
				Text = "Window",
				UseVisualStyleBackColor = true
			};
		}

		private void SaveControlsTo(Config config)
		{
			// set config from Scaling & Filtering tab

			config.DispPrescale = (int) nudPrescale.Value;

			if (rbNone.Checked) config.TargetDisplayFilter = 0;
			else if (rbHq2x.Checked) config.TargetDisplayFilter = 1;
			else if (rbScanlines.Checked)
			{
				config.TargetDisplayFilter = 2;
				config.TargetScanlineFilterIntensity = tbScanlineIntensity.Value;
			}
			else if (rbUser.Checked)
			{
				config.TargetDisplayFilter = 3;
				config.DispUserFilterPath = PathSelection;
				GlobalWin.DisplayManager.RefreshUserShader();
			}

			config.DispAutoPrescale = cbAutoPrescale.Checked;

			if (rbFinalFilterNone.Checked) config.DispFinalFilter = 0;
			else if (rbFinalFilterBilinear.Checked) config.DispFinalFilter = 1;
			else if (rbFinalFilterBicubic.Checked) config.DispFinalFilter = 2;

			config.DispFixAspectRatio = cbLetterbox.Checked;

			if (rbUseRaw.Checked) config.DispManagerAR = Config.EDispManagerAR.None;
			else if (rbUseSystem.Checked) config.DispManagerAR = Config.EDispManagerAR.System;
			else if (rbUseCustom.Checked) config.DispManagerAR = Config.EDispManagerAR.Custom;
			else if (rbUseCustomRatio.Checked) config.DispManagerAR = Config.EDispManagerAR.CustomRatio;

			if (txtCustomARWidth.Text == "") config.DispCustomUserARWidth = -1;
			else int.TryParse(txtCustomARWidth.Text, out config.DispCustomUserARWidth);

			if (txtCustomARHeight.Text == "") config.DispCustomUserARHeight = -1;
			else int.TryParse(txtCustomARHeight.Text, out config.DispCustomUserARHeight);

			if (txtCustomARX.Text == "") config.DispCustomUserARX = -1;
			else float.TryParse(txtCustomARX.Text, out config.DispCustomUserARX);

			if (txtCustomARY.Text == "") config.DispCustomUserARY = -1;
			else float.TryParse(txtCustomARY.Text, out config.DispCustomUserARY);

			config.DispFixScaleInteger = cbPadInteger.Checked;

			int.TryParse(txtCropLeft.Text, out config.DispCropLeft);
			int.TryParse(txtCropTop.Text, out config.DispCropTop);
			int.TryParse(txtCropRight.Text, out config.DispCropRight);
			int.TryParse(txtCropBottom.Text, out config.DispCropBottom);

			// set config from Display Method tab

			var prevDispMethod = config.DispMethod;

			if (rbOpenGL.Checked) config.DispMethod = Config.EDispMethod.OpenGL;
			else if (rbGDIPlus.Checked) config.DispMethod = Config.EDispMethod.GdiPlus;
			else if (rbD3D9.Checked)
			{
				config.DispMethod = Config.EDispMethod.SlimDX9;
				config.DispAlternateVsync = cbAlternateVsync.Checked;
			}

			if (config.DispMethod != prevDispMethod) NeedReset = true;

			// set config from Misc. tab

			if (rbDisplayAbsoluteZero.Checked) config.DispSpeedupFeatures = 0;
			else if (rbDisplayMinimal.Checked) config.DispSpeedupFeatures = 1;
			else if (rbDisplayFull.Checked) config.DispSpeedupFeatures = 2;

			//HACK this cloning is necessary
			var nhSettings = NullHawkSettings;
			nhSettings.SnowyDisplay = cbNullHawkWhiteNoise.Checked;
			NullHawkSettings = nhSettings;

			// set config from Window tab

			config.DispChrome_FrameWindowed = tbFrameSizeWindowed.Value;
			config.DispChrome_StatusBarWindowed = cbStatusBarWindowed.Checked;
			config.DispChrome_CaptionWindowed = cbCaptionWindowed.Checked;
			config.DispChrome_MenuWindowed = cbMenuWindowed.Checked;
			config.DispChrome_StatusBarFullscreen = cbStatusBarFullscreen.Checked;
			config.DispChrome_Fullscreen_AutohideMouse = cbFSAutohideMouse.Checked;
			config.DispChrome_MenuFullscreen = cbMenuFullscreen.Checked;
			config.DispFullscreenHacks = cbFullscreenHacks.Checked;
			config.DispChrome_AllowDoubleClickFullscreen = cbAllowDoubleclickFullscreen.Checked;

			DialogResult = DialogResult.OK;
		}

		private void UpdateUserFilterLabel() => lblUserFilterName.Text = Path.GetFileNameWithoutExtension(PathSelection);

		protected override void Dispose(bool disposing)
		{
			if (disposing) Components?.Dispose();
			base.Dispose(disposing);
		}
	}
}
