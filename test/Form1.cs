using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            List<WindowsControlPanelItems.ControlPanelItem> myList = new List<WindowsControlPanelItems.ControlPanelItem>();

            myList = WindowsControlPanelItems.List.Create();
            MessageBox.Show("");


        }
    }
}
