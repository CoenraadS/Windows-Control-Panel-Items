using System;
using System.Diagnostics;
using System.Drawing;

namespace WindowsControlPanelItems
{
    public class ControlPanelItem
    {
        public string localizedString { get; private set; }
        public string infoTip { get; private set; }
        public string applicationName { get; private set; }
        public ProcessStartInfo executablePath { get; private set; }
        public Icon smallIcon { get; private set; }
        public Icon largeIcon { get; private set; }

        public ControlPanelItem(string newLocalizedString, string newInfoTip, string newApplicationName, ProcessStartInfo newExecutablePath, Icon newSmallIcon, Icon newLargeIcon)
        {
            localizedString = newLocalizedString;
            infoTip = newInfoTip;
            applicationName = newApplicationName;
            executablePath = newExecutablePath;
            smallIcon = (Icon)newSmallIcon.Clone();
            largeIcon = (Icon)newLargeIcon.Clone();
        }
    }
}
