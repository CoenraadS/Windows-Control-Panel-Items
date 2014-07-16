using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;

namespace WindowsControlPanelItems
{
    public static class List
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        const string CONTROL = @"%SystemRoot%\System32\control.exe";

        public static List<ControlPanelItem> Create()
        {
            List<WindowsControlPanelItems.ControlPanelItem> controlPanelItems = new List<WindowsControlPanelItems.ControlPanelItem>();
            string applicationName;
            string[] localizedString;
            string[] infoTip = new string[1];
            List<string> iconString;
            IntPtr hMod;
            uint stringTableIndex;
            int iconIndex;
            StringBuilder resource;
            ProcessStartInfo executablePath;
            IntPtr largeIconPtr = IntPtr.Zero;
            IntPtr smallIconPtr = IntPtr.Zero;
            Icon largeIcon;
            Icon smallIcon;

            RegistryKey nameSpace = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace");
            RegistryKey clsid = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\CLSID");
            RegistryKey currentKey;

            foreach (string key in nameSpace.GetSubKeyNames())
            {
                currentKey = clsid.OpenSubKey(key);
                if (currentKey != null)
                {
                    Debug.Write(key.ToString());
                    if (currentKey.GetValue("System.ApplicationName") != null && currentKey.GetValue("LocalizedString") != null)
                    {
                        applicationName = currentKey.GetValue("System.ApplicationName").ToString();
                        Debug.WriteLine(" (" + applicationName + ")");
                        localizedString = currentKey.GetValue("LocalizedString").ToString().Split(new char[] { ',' }, 2);
                        localizedString[0] = localizedString[0].Substring(1); //First char is always '@'
                        localizedString[0] = Environment.ExpandEnvironmentVariables(localizedString[0]);
                        if (localizedString.Length > 1)
                        {
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

                            smallIcon = null;
                            largeIcon = null;

                            if (currentKey.OpenSubKey("DefaultIcon") != null)
                            {
                                if (currentKey.OpenSubKey("DefaultIcon").GetValue(null) != null)
                                {
                                    iconString = new List<string>(currentKey.OpenSubKey("DefaultIcon").GetValue(null).ToString().Split(new char[] { ',' }, 2));

                                    if (iconString.Count < 2)
                                    {
                                        iconString.Add("0");
                                    }


                                    iconIndex = (int)sanitizeUint(iconString[1]);
                                    if (iconIndex == 1) //-1 is reserved in the ExtractIconEx function (against MSDN documentation...).
                                    {
                                        iconIndex = 0;
                                    }
                                    else
                                    {
                                        iconIndex = iconIndex * -1; //Negative index points to specific icon.
                                    }

                                    ExtractIconEx(iconString[0], iconIndex, out largeIconPtr, out smallIconPtr, 1);

                                    try
                                    {
                                        largeIcon = Icon.FromHandle(largeIconPtr);
                                        smallIcon = Icon.FromHandle(smallIconPtr);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex.Message);
                                    }
                                }                                
                            }
                            

                            executablePath = new ProcessStartInfo();
                            executablePath.FileName = Environment.ExpandEnvironmentVariables(CONTROL);
                            executablePath.Arguments = "-name " + applicationName;
                            controlPanelItems.Add(new ControlPanelItem(localizedString[0], infoTip[0], applicationName, executablePath, smallIcon, largeIcon));

                            if (largeIconPtr != IntPtr.Zero)
                            {
                                DestroyIcon(largeIcon.Handle);
                            }
                            if (smallIconPtr != IntPtr.Zero)
                            {
                                DestroyIcon(smallIcon.Handle);
                            }
                            FreeLibrary(hMod);
                        }
                    }
                }
            }

            return controlPanelItems;
        }

        public static uint sanitizeUint(string args) //Remove all chars before and after first set of digits.
        {
            int x = 0;

            while (x < args.Length && !Char.IsDigit(args[x]))
            {
                args = args.Substring(1);
            }

            x = 0;

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
