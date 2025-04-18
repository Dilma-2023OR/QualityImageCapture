using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QualityImageCapture
{
    public partial class MessagePass : Form
    {
        public MessagePass(string msg, Color color)
        {
            InitializeComponent();
            tableLayoutPanel1.BackColor = color;
            lblMessage.Text = msg;
        }

        private void MessagePass_Load(object sender, EventArgs e)
        {
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            timer.Stop();
            this.Close();
        }
    }
}
