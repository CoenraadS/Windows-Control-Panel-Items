using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;

namespace test
{
    public partial class Form1 : Form
    {
        List<WindowsControlPanelItems.ControlPanelItem> myList;

        public Form1()
        {
            myList = WindowsControlPanelItems.List.Create(48);
            InitializeComponent();                    

            foreach (var item in myList)
            {
                comboBox1.Items.Add(item.LocalizedString.ToString());
            }
            
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


        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start(myList[comboBox1.SelectedIndex].ExecutablePath);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (myList[comboBox1.SelectedIndex].Icon != null)
            pictureBox1.Image = myList[comboBox1.SelectedIndex].Icon.ToBitmap();
            labelName.Text = myList[comboBox1.SelectedIndex].LocalizedString;
            labelInfo.Text = myList[comboBox1.SelectedIndex].InfoTip;
        }
    }
}
