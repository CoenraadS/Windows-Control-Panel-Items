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
            string[] localizedStringRaw;
            string localizedString;
            string[] infoTipRaw;
            string infoTip;
            List<string> iconString;
            IntPtr dataFilePointer;
            uint stringTableIndex;
            IntPtr iconIndex;
            StringBuilder resource;
            ProcessStartInfo executablePath;
            IntPtr iconPtr;
            Icon myIcon;

            RegistryKey nameSpace = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ControlPanel\\NameSpace");
            RegistryKey clsid = Registry.ClassesRoot.OpenSubKey("CLSID");
            RegistryKey currentKey;

            foreach (string key in nameSpace.GetSubKeyNames())
            {
                currentKey = clsid.OpenSubKey(key);
                if (currentKey != null)
                {
                    executablePath = new ProcessStartInfo();
                    dataFilePointer = IntPtr.Zero;
                    applicationName = "";
                    localizedString = "";
                    infoTip = "";

                    if (currentKey.GetValue("System.ApplicationName") != null)
                    {
                        //CPL Files (usually native MS items)
                        applicationName = currentKey.GetValue("System.ApplicationName").ToString();
                        executablePath.FileName = Environment.ExpandEnvironmentVariables(CONTROL);
                        executablePath.Arguments = "-name " + applicationName;
                    }
                    else if (currentKey.OpenSubKey("Shell\\Open\\Command") != null && currentKey.OpenSubKey("Shell\\Open\\Command").GetValue(null) != null)
                    {
                        //Other files (usually third party items)
                        executablePath.FileName = Environment.ExpandEnvironmentVariables(currentKey.OpenSubKey("Shell\\Open\\Command").GetValue(null).ToString());
                    }
                    else
                    {
                        continue; //Cannot have items without executable path.
                    }

                    if (currentKey.GetValue("LocalizedString") != null)
                    {
                        localizedStringRaw = currentKey.GetValue("LocalizedString").ToString().Split(new char[] { ',' }, 2);

                        if (localizedStringRaw.Length > 1)
                        {
                            if (localizedStringRaw[0][0] == '@')
                            {
                                localizedStringRaw[0] = localizedStringRaw[0].Substring(1);
                            }

                            localizedStringRaw[0] = Environment.ExpandEnvironmentVariables(localizedStringRaw[0]);

                            dataFilePointer = LoadLibraryEx(localizedStringRaw[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE); //Load file with strings

                            stringTableIndex = sanitizeUint(localizedStringRaw[1]);

                            resource = new StringBuilder(255);
                            LoadString(dataFilePointer, stringTableIndex, resource, resource.Capacity + 1); //Extract needed string

                            localizedString = resource.ToString();

                            /*This shouldn't be necessary, but some apps (e.g. Bootcamp)
                             * don't follow Microsoft's standard. Have to make a choice whether
                             * empty string == failure, or use default name. I'm using default name */

                            if (String.IsNullOrEmpty(localizedString))
                            {
                                if (currentKey.GetValue(null) != null)
                                {
                                    localizedString = currentKey.GetValue(null).ToString();
                                }
                                else
                                {
                                    continue; //Cannot have item without title.
                                }
                            }
                        }
                        else
                        {
                            localizedString = localizedStringRaw[0];
                        }
                    }
                    else if (currentKey.GetValue(null) != null)
                    {
                        localizedString = currentKey.GetValue(null).ToString();
                    }
                    else
                    {
                        continue; //Cannot have item without title.
                    }

                    FreeLibrary(dataFilePointer);

                    if (currentKey.GetValue("InfoTip") != null)
                    {
                        infoTipRaw = currentKey.GetValue("InfoTip").ToString().Split(new char[] { ',' }, 2);

                        if (infoTipRaw.Length == 2)
                        {
                            if (infoTipRaw[0][0] == '@')
                            {
                                infoTipRaw[0] = infoTipRaw[0].Substring(1);
                            }
                            infoTipRaw[0] = Environment.ExpandEnvironmentVariables(infoTipRaw[0]);

                            dataFilePointer = LoadLibraryEx(infoTipRaw[0], IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE); //Load file with strings

                            stringTableIndex = sanitizeUint(infoTipRaw[1]);

                            resource = new StringBuilder(255);
                            LoadString(dataFilePointer, stringTableIndex, resource, resource.Capacity + 1); //Extract needed string

                            infoTip = resource.ToString();
                        }
                    }
                    else
                    {
                        infoTip = "";
                    }


                    FreeLibrary(dataFilePointer); //We are finished with extracting strings. Prepare to load icon file.

                    dataFilePointer = IntPtr.Zero;
                    myIcon = null;
                    iconPtr = IntPtr.Zero;

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
                                //Silently fail for now..
                            }
                        }
                    }
                    controlPanelItems.Add(new ControlPanelItem(localizedString, infoTip, executablePath, myIcon));
                    FreeLibrary(dataFilePointer);
                    if (iconPtr != IntPtr.Zero)
                    {
                        DestroyIcon(myIcon.Handle);
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
