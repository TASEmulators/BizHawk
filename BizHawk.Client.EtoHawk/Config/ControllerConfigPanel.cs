using Eto;
using System;
using Eto.Forms;
using System.Text;
using Eto.Drawing;
using System.Collections.Generic;

namespace EtoHawk.Config
{
    public partial class ControllerConfigPanel : Panel
    {
        // the dictionary that results are saved to
        Dictionary<string, string> RealConfigObject;
        // if nonnull, the list of keys to use.  used to have the config panel operate on a smaller list than the whole dictionary;
        // for instance, to show only a single player
        List<string> RealConfigButtons;

        public List<string> buttons = new List<string>();

        //These values are bogus and I'm hoping not to use them
        public int InputMarginLeft = 0;
        public int LabelPadding = 5;

        public int MarginTop = 0;
        public int Spacing = 24;
        public int InputSize = 130;
        public int ColumnWidth = 280;
        public int LabelWidth = 60;

        protected List<InputCompositeWidget> Inputs = new List<InputCompositeWidget>();
        protected List<Label> Labels = new List<Label>();

        public ControllerConfig ParentConfig {get;set;}

        private Size _panelSize = new Size(0, 0);
        private PixelLayout _layout = new PixelLayout(); //Todo: switch to a real cross-platform layout

        public ControllerConfigPanel()
        {
            Content = _layout;
        }

        private void ControllerConfigPanel_Load(object sender, EventArgs e)
        {

        }

        public void ClearAll()
        {
            Inputs.ForEach(x => x.Clear());
        }

        /// <summary>
        /// save to config
        /// </summary>
        /// <param name="SaveConfigObject">if non-null, save to possibly different config object than originally initialized from</param>
        public void Save(Dictionary<string, string> SaveConfigObject = null)
        {
            var saveto = SaveConfigObject ?? RealConfigObject;
            for (int button = 0; button < buttons.Count; button++)
                saveto[buttons[button]] = Inputs[button].Bindings;
        }

        public bool Autotab = false;
        public void LoadSettings(Dictionary<string, string> configobj, bool autotab, List<string> configbuttons = null, int? width = null, int? height = null)
        {
            Autotab = autotab;
            if (width.HasValue && height.HasValue)
            {
                _panelSize = new Size(width.Value, height.Value);
            }
            else
            {
                _panelSize = Size;
            }

            RealConfigObject = configobj;
            RealConfigButtons = configbuttons;
            SetButtonList();
            Startup();
            SetWidgetStrings();
        }

        protected void SetButtonList()
        {
            buttons.Clear();
            IEnumerable<string> bl = RealConfigButtons ?? (IEnumerable<string>)RealConfigObject.Keys;
            foreach (string s in bl)
                buttons.Add(s);
        }

        protected void SetWidgetStrings()
        {
            for (int button = 0; button < buttons.Count; button++)
            {
                string s;
                if (!RealConfigObject.TryGetValue(buttons[button], out s))
                    s = "";
                Inputs[button].Bindings = s;
            }
        }

        protected void Startup()
        {
            int x = InputMarginLeft;
            int y = MarginTop - Spacing;
            for (int i = 0; i < buttons.Count; i++)
            {
                y += Spacing;
                //if (y > (_panelSize.Height - 62))
                if (y > (500 - 62)) //TODO: Don't hardcode any of this
                {
                    y = MarginTop;
                    x += ColumnWidth;
                }

                InputCompositeWidget iw = new InputCompositeWidget(ParentConfig)
                {
                    //Location = new Point(x, y),
                    Size = new Size(InputSize, 23),
                    //TabIndex = i,
                    AutoTab = this.Autotab
                };

                iw.ToolTip = string.Empty;

                //iw.BringToFront();

                _layout.Add(iw, x, y);
                Inputs.Add(iw);
                Label label = new Label
                {
                    //Location = new Point(x + InputSize + LabelPadding, y + UIHelper.ScaleY(3)),
                    Size = new Size(100, 15),
                    Text = buttons[i].Replace('_', ' ').Trim(),
                };

                _layout.Add(label, x + InputSize + LabelPadding, y + 3);
                Labels.Add(label);
            }
        }

        public void SetAutoTab(bool value)
        {
            Inputs.ForEach(x => x.AutoTab = value);
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearAll();
        }
    }
}
