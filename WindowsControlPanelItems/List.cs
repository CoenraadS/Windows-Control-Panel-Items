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
        private const uint GROUP_ICON = 14;
        private const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
        private const string CONTROL = @"%SystemRoot%\System32\control.exe";

        private delegate bool EnumResNameDelegate(
        IntPtr hModule,
        IntPtr lpszType,
        IntPtr lpszName,
        IntPtr lParam);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool EnumResourceNamesWithID(IntPtr hModule, uint lpszType, EnumResNameDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr LoadImage(IntPtr hinst, IntPtr lpszName, uint uType,
        int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        static Queue<IntPtr> iconQueue;

        public static List<ControlPanelItem> Create(int iconSize)
        {
            List<WindowsControlPanelItems.ControlPanelItem> controlPanelItems = new List<WindowsControlPanelItems.ControlPanelItem>();
            string applicationName;
            string[] localizedString;
            string[] infoTip = new string[2]; //Make compile happy
            List<string> iconString;
            IntPtr dataFilePointer;
            uint stringTableIndex;
            IntPtr iconIndex;
            StringBuilder resource;
            ProcessStartInfo executablePath;
            IntPtr iconPtr = IntPtr.Zero;
            Icon myIcon;

            RegistryKey nameSpace = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace");
            RegistryKey clsid = Registry.ClassesRoot.OpenSubKey("CLSID");
            RegistryKey currentKey;

            List<string> log = new List<string>();
            log.Add("=== Control Panel ===");

            foreach (string key in nameSpace.GetSubKeyNames())
            {
                currentKey = clsid.OpenSubKey(key);
                if (currentKey != null)
                {
                    executablePath = new ProcessStartInfo();
                    applicationName = "";
                    log.Add(key);
                    if (currentKey.GetValue("System.ApplicationName") != null)
                    {
                        applicationName = currentKey.GetValue("System.ApplicationName").ToString();
                        log.Add("System.ApplicationName: " + applicationName);
                        executablePath.FileName = Environment.ExpandEnvironmentVariables(CONTROL);
                        executablePath.Arguments = "-name " + applicationName;
                    }

                    if (currentKey.GetValue("LocalizedString") != null)
                    {
                        localizedString = currentKey.GetValue("LocalizedString").ToString().Split(new char[] { ',' }, 2);
                        log.Add("LocalizedString: " + currentKey.GetValue("LocalizedString").ToString());
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
                                log.Add("InfoTip LoadString(): " + currentKey.GetValue("InfoTip").ToString());
                                infoTip = currentKey.GetValue("InfoTip").ToString().Split(new char[] { ',' }, 2);

                                if (infoTip.Length == 2)
                                {
                                    if (infoTip[0][0] == '@')
                                    {
                                        infoTip[0] = infoTip[0].Substring(1);
                                    }
                                    infoTip[0] = Environment.ExpandEnvironmentVariables(infoTip[0]);

                                    dataFilePointer = LoadLibraryEx(infoTip[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

                                    stringTableIndex = sanitizeUint(infoTip[1]);

                                    resource = new StringBuilder(255);
                                    LoadString(dataFilePointer, stringTableIndex, resource, resource.Capacity + 1);

                                    infoTip[0] = resource.ToString();
                                }
                            }
                            else
                            {
                                infoTip[0] = "";
                                log.Add("InfoTip not found");
                            }

                            FreeLibrary(dataFilePointer); //We are finished with extracting strings. Prepare to load icon file.
                            dataFilePointer = IntPtr.Zero;
                            myIcon = null;
                            iconPtr = IntPtr.Zero;

                            if (currentKey.OpenSubKey("DefaultIcon") != null)
                            {
                                if (currentKey.OpenSubKey("DefaultIcon").GetValue(null) != null)
                                {
                                    log.Add("DefaultIcon: " + currentKey.OpenSubKey("DefaultIcon").GetValue(null).ToString());
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

                                    if (iconIndex == IntPtr.Zero)
                                    {
                                        iconQueue = new Queue<IntPtr>();
                                        EnumResourceNamesWithID(dataFilePointer, GROUP_ICON, new EnumResNameDelegate(EnumRes), IntPtr.Zero); //Iterate through resources. 

                                        while (iconPtr == IntPtr.Zero && iconQueue.Count > 0)
                                        {
                                            iconPtr = LoadImage(dataFilePointer, iconQueue.Dequeue(), 1, iconSize, iconSize, 0);
                                        }
                                    }
                                    else
                                    {
                                        iconPtr = LoadImage(dataFilePointer, iconIndex, 1, iconSize, iconSize, 0);
                                    }

                                    try
                                    {
                                        myIcon = Icon.FromHandle(iconPtr);
                                    }
                                    catch (Exception ex)
                                    {
                                        log.Add(ex.Message);
                                        log.Add("iconPtr: " + iconPtr.ToString());
                                    }
                                }
                            }
                            controlPanelItems.Add(new ControlPanelItem(localizedString[0], infoTip[0], applicationName, executablePath, myIcon));
                            FreeLibrary(dataFilePointer);
                            if (iconPtr != IntPtr.Zero)
                            {
                                DestroyIcon(myIcon.Handle);
                            }
                        }
                    }
                }
            }

            return controlPanelItems;
        }

        private static uint sanitizeUint(string args) //Remove all chars before and after first set of digits.
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

        private static bool IS_INTRESOURCE(IntPtr value)
        {
            if (((uint)value) > ushort.MaxValue)
                return false;
            return true;
        }
        private static uint GET_RESOURCE_ID(IntPtr value)
        {
            if (IS_INTRESOURCE(value) == true)
                return (uint)value;
            throw new System.NotSupportedException("value is not an ID!");
        }
        private static string GET_RESOURCE_NAME(IntPtr value)
        {
            if (IS_INTRESOURCE(value) == true)
                return value.ToString();
            return Marshal.PtrToStringUni((IntPtr)value);
        }

        private static bool EnumRes(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam)
        {
            //Debug.WriteLine("Type: " + GET_RESOURCE_NAME(lpszType));
            //Debug.WriteLine("Name: " + GET_RESOURCE_NAME(lpszName));
            iconQueue.Enqueue(lpszName);
            return true;
        }
    }
}
