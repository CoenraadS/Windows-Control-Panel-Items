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
        int count;
        string folder;
        public Form1()
        {
            folder = @"Images\";
            myList = WindowsControlPanelItems.List.Create(48);
            count = 0;
            InitializeComponent();                    
            
        }

        private void buttonTest_Click(object sender, EventArgs e)
        {
            //Warning, spawns a lot of windows.
            foreach (var item in myList)
            {
                Debug.WriteLine(item.LocalizedString);
                Process.Start(item.ExecutablePath);
            }

            MessageBox.Show("Test Complete");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = myList[count].Icon.ToBitmap();
            //myList[count].Icon.ToBitmap().Save(folder + myList[count].ApplicationName + ".bmp");
            count++;
        }
    }
}
