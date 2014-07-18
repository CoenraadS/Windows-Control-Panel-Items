using System;
using System.Diagnostics;
using System.Drawing;

namespace WindowsControlPanelItems
{
    public class ControlPanelItem
    {
        public string LocalizedString { get; private set; }
        public string InfoTip { get; private set; }
        public ProcessStartInfo ExecutablePath { get; private set; }
        public Icon Icon { get; private set; }

        public ControlPanelItem(string newLocalizedString, string newInfoTip, ProcessStartInfo newExecutablePath, Icon newIcon)
        {
            LocalizedString = newLocalizedString;
            InfoTip = newInfoTip;
            ExecutablePath = newExecutablePath;
            Icon = newIcon;
        }
    }
}
