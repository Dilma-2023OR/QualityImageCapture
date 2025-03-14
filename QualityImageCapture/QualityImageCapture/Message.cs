﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QualityImageCapture
{
    public partial class Message : Form
    {
        [DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        const int WM_LBUTTONDOWN = 0xA1;
        const int HT_CAPTION = 0x2;
        private void Message_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_LBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void tLayoutHeader_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_LBUTTONDOWN, HT_CAPTION, 0);
            }
        }
        public Message(string msg)
        {
            InitializeComponent();
            message = msg;
        }

        //Public Data
        string message;

        private void Message_Load(object sender, EventArgs e)
        {
            //Location
            this.StartPosition = FormStartPosition.CenterParent;

            //Feedback
            lblMessage.Text = message;

            timerClose.Start();

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            //Close Form
            this.Close();
        }

        private void Message_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.Close();
            }
        }

        private void timerClose_Tick(object sender, EventArgs e)
        {
            //Timer Stop
            timerClose.Stop();
            this.Close();
        }
    }
}
