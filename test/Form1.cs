using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            List<WindowsControlPanelItems.ControlPanelItem> myList = new List<WindowsControlPanelItems.ControlPanelItem>();

            myList = WindowsControlPanelItems.List.Create();

            //Warning, spawns a lot of windows.
            foreach (var item in myList)
            {
                Debug.WriteLine(item.localizedString);
                Process.Start(item.executablePath);
            }

            MessageBox.Show("Test Complete");
        }
    }
}
