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

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] //LoadImage IntPtr
        static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint uType,
        int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, //LoadImage String
        int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)] //LoadImage no value
        static extern IntPtr LoadImage(IntPtr hinst, uint uType,
        int cxDesired, int cyDesired, uint fuLoad);

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
            IntPtr dataFilePointer;
            uint stringTableIndex;
            IntPtr iconIndex;
            StringBuilder resource;
            ProcessStartInfo executablePath;
            IntPtr largeIconPtr = IntPtr.Zero;
            Icon largeIcon;

            RegistryKey nameSpace = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace");
            RegistryKey clsid = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\CLSID");
            RegistryKey currentKey;

            foreach (string key in nameSpace.GetSubKeyNames())
            {
                currentKey = clsid.OpenSubKey(key);
                if (currentKey != null)
                {
                    if (currentKey.GetValue("System.ApplicationName") != null && currentKey.GetValue("LocalizedString") != null)
                    {
                        applicationName = currentKey.GetValue("System.ApplicationName").ToString();
                        Debug.WriteLine(key.ToString() + " (" + applicationName + ")");
                        localizedString = currentKey.GetValue("LocalizedString").ToString().Split(new char[] { ',' }, 2);
                        if (localizedString[0][0] == '@')
                        {
                            localizedString[0] = localizedString[0].Substring(1);
                        }
                        localizedString[0] = Environment.ExpandEnvironmentVariables(localizedString[0]);
                        if (localizedString.Length > 1)
                        {
                            dataFilePointer = LoadLibraryEx(localizedString[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

                            stringTableIndex = sanitizeUint(localizedString[1]);

                            resource = new StringBuilder(255);
                            LoadString(dataFilePointer, stringTableIndex, resource, resource.Capacity + 1);

                            localizedString[0] = resource.ToString();

                            if (currentKey.GetValue("InfoTip") != null)
                            {
                                infoTip = currentKey.GetValue("InfoTip").ToString().Split(new char[] { ',' }, 2);
                                if (infoTip[0][0] == '@')
                                {
                                    infoTip[0] = infoTip[0].Substring(1);
                                }
                                infoTip[0] = Environment.ExpandEnvironmentVariables(infoTip[0]);

                                dataFilePointer = LoadLibraryEx(infoTip[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE); //IMAGEFILE

                                stringTableIndex = sanitizeUint(infoTip[1]);

                                resource = new StringBuilder(255);
                                LoadString(dataFilePointer, stringTableIndex, resource, resource.Capacity + 1);

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

                            FreeLibrary(dataFilePointer); //We are finished with extracting strings. Prepare to load icon file.
                            dataFilePointer = IntPtr.Zero;
                            largeIcon = null;

                            if (currentKey.OpenSubKey("DefaultIcon") != null)
                            {
                                if (currentKey.OpenSubKey("DefaultIcon").GetValue(null) != null)
                                {
                                    iconString = new List<string>(currentKey.OpenSubKey("DefaultIcon").GetValue(null).ToString().Split(new char[] { ',' }, 2));
                                    if (iconString[0][0] == '@')
                                    {
                                        iconString[0] = iconString[0].Substring(1);
                                    }

                                    dataFilePointer = LoadLibraryEx(iconString[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

                                    if (iconString.Count < 2)
                                    {
                                        iconString.Add("0");
                                    }


                                    iconIndex = (IntPtr)sanitizeUint(iconString[1]);
                                    IntPtr dummy = IntPtr.Zero;
                                    largeIconPtr = LoadImage(dataFilePointer, iconIndex, 1, 256, 256, 0);
                                    if (largeIconPtr == IntPtr.Zero) //Big problem, how to load default resource. It should exist at zero, but tests below don't work.
                                    {
                                        largeIconPtr = LoadImage(dataFilePointer, IntPtr.Zero, 1, 256, 256, 0);
                                        Debug.WriteLine("IntPtr.Zero => " + largeIconPtr.ToString());

                                        largeIconPtr = LoadImage(dataFilePointer, 1, 256, 256, 0);
                                        Debug.WriteLine("Not passing anything => " + largeIconPtr.ToString());

                                        largeIconPtr = LoadImage(dataFilePointer, "#0",1, 256, 256, 0);
                                        Debug.WriteLine("Passing 0 => " + largeIconPtr.ToString());
                                    }

                                    try
                                    {
                                        largeIcon = (Icon)Icon.FromHandle(largeIconPtr).Clone();
                                    }
                                    catch (Exception)
                                    {
                                        //Silently fail for now.
                                    }
                                }                                
                            }
                            

                            executablePath = new ProcessStartInfo();
                            executablePath.FileName = Environment.ExpandEnvironmentVariables(CONTROL);
                            executablePath.Arguments = "-name " + applicationName;
                            controlPanelItems.Add(new ControlPanelItem(localizedString[0], infoTip[0], applicationName, executablePath, largeIcon));
                            FreeLibrary(dataFilePointer);
                            if (largeIconPtr != IntPtr.Zero)
                            {
                                DestroyIcon(largeIcon.Handle);
                            }
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
