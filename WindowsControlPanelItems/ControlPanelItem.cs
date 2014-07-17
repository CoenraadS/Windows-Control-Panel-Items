using System;
using System.Diagnostics;
using System.Drawing;

namespace WindowsControlPanelItems
{
    public class ControlPanelItem
    {
        public string LocalizedString { get; private set; }
        public string InfoTip { get; private set; }
        public string ApplicationName { get; private set; }
        public ProcessStartInfo ExecutablePath { get; private set; }
        public Icon Icon { get; private set; }

        public ControlPanelItem(string newLocalizedString, string newInfoTip, string newApplicationName, ProcessStartInfo newExecutablePath, Icon newLargeIcon)
        {
            LocalizedString = newLocalizedString;
            InfoTip = newInfoTip;
            ApplicationName = newApplicationName;
            ExecutablePath = newExecutablePath;
            Icon = newLargeIcon;
        }
    }
}
