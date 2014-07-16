using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace test
{
    public partial class Form1 : Form
    {
        List<WindowsControlPanelItems.ControlPanelItem> myList = new List<WindowsControlPanelItems.ControlPanelItem>();
        public Form1()
        {
            InitializeComponent();
         
            myList = WindowsControlPanelItems.List.Create();
            
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            //Warning, spawns a lot of windows.
            foreach (var item in myList)
            {
                Debug.WriteLine(item.localizedString);
                this.Icon = item.largeIcon;
                //Process.Start(item.executablePath);
                Thread.Sleep(100);
            }

            MessageBox.Show("Test Complete");
        }
    }
}
