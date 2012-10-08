using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace BizHawk.MultiClient.config.ControllerConfig
{
    public partial class UserControlGamePad : UserControl
    {
        private Object _Controler;
        public UserControlGamePad()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            SNESControllerTemplate tmp = new SNESControllerTemplate(false);
            
            _Controler = tmp;
            initialize();
        }

        public void initialize()
        {       
            FieldInfo[] availbleComands =  _Controler.GetType().GetFields().Where(x => x.FieldType == typeof(System.String)).ToArray();

            foreach (FieldInfo item in availbleComands)
            {
                InputWidget iw = new InputWidget();
                Label label = new Label();
                label.Text = item.Name;
                iw.Text = item.GetValue(_Controler) as string;
                flowLayoutPanelCommands.Controls.Add(iw);
                flowLayoutPanelLabels.Controls.Add(label);                            
            }      
        }
    }
}
