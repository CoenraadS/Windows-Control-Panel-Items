using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WindowsControlPanelItems
{
    public static class List
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        const string CONTROL = @"%SystemRoot%\System32\control.exe";

        public static List<ControlPanelItem> Create()
        {
            List<WindowsControlPanelItems.ControlPanelItem> controlPanelItems = new List<WindowsControlPanelItems.ControlPanelItem>();
            string applicationName;
            string[] localizedString = new string[2];
            string[] infoTip = new string[2];
            IntPtr hMod;
            uint stringTableIndex;
            StringBuilder resource;
            ProcessStartInfo executablePath;

            RegistryKey nameSpace = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace");
            RegistryKey currentKey;

            foreach (string key in nameSpace.GetSubKeyNames())
            {
                currentKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\CLSID").OpenSubKey(key);
                if (currentKey != null)
                {
                    if (currentKey.GetValue("System.ApplicationName") != null && currentKey.GetValue("LocalizedString") != null)
                    {
                        applicationName = currentKey.GetValue("System.ApplicationName").ToString();
                        localizedString = currentKey.GetValue("LocalizedString").ToString().Split(new char[] { ',' }, 2);
                        localizedString[0] = localizedString[0].Substring(1); //First char is always '@'
                        localizedString[0] = Environment.ExpandEnvironmentVariables(localizedString[0]);
                        localizedString[1] = localizedString[1].Substring(1); //First char is always '-'

                        hMod = LoadLibraryEx(localizedString[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

                        stringTableIndex = sanitizeUint(localizedString[1]);

                        resource = new StringBuilder(255);
                        LoadString(hMod, stringTableIndex, resource, resource.Capacity + 1);

                        localizedString[0] = resource.ToString();

                        if (currentKey.GetValue("InfoTip") != null)
                        {
                            infoTip = currentKey.GetValue("InfoTip").ToString().Split(new char[] { ',' }, 2);
                            infoTip[0] = infoTip[0].Substring(1); //First char is always '@'
                            infoTip[0] = Environment.ExpandEnvironmentVariables(infoTip[0]);
                            infoTip[1] = infoTip[1].Substring(1); //First char is always '-'

                            hMod = LoadLibraryEx(infoTip[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

                            stringTableIndex = sanitizeUint(infoTip[1]);

                            resource = new StringBuilder(255);
                            LoadString(hMod, stringTableIndex, resource, resource.Capacity + 1);

                            infoTip[0] = resource.ToString();
                        }
                        else if (currentKey.GetValue(null) != null)
                        {
                            infoTip[0] = currentKey.GetValue(null).ToString();
                        }
                        else
                        {
                            infoTip[0] = "";
                        }

                        executablePath = new ProcessStartInfo();
                        executablePath.FileName = Environment.ExpandEnvironmentVariables(CONTROL);
                        executablePath.Arguments = "-name " + applicationName;
                        controlPanelItems.Add(new ControlPanelItem(localizedString[0], infoTip[0], applicationName, executablePath));
                    }                    
                }
            }

            return controlPanelItems;
        }

        public static uint sanitizeUint(string args) //Remove all chars after digits.
        {
            int x = 0;
            while (x < args.Length && Char.IsDigit(args[x]))
            {
                x++;
            }

            if (x < args.Length)
            {
                args = args.Remove(x);
            }

            return Convert.ToUInt32(args);
        }
    }
}
