using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
{
    /// <summary>
    /// A simple form that prompts the user for a single line of input
    /// </summary>
    public partial class InputPrompt : Form
    {
        public bool UserOK = false;    //Will be true if the user selects Ok
        public string UserText = "";   //What the user selected
        
        public InputPrompt()
        {
            InitializeComponent();
        }

        private void InputPrompt_Load(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            UserOK = true;
            this.Close();
        }
    }
}
