using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Security.AccessControl;
using System.Security.Principal;

namespace RmxForSPOWeb.Common
{
    public class IniFiles
    {
        public string inipath;
        protected static CLog theLog = CLog.GetLogger("IniFiles");

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
       
        public IniFiles(string INIPath)
        {
            inipath = INIPath;
            if(!ExistINIFile())
            {
                try
                {
                    //allow "Everyone" to access this ini file.
                    //because we may have different IIS site that use the same config file
                    FileSecurity fs = new FileSecurity();
                    fs.AddAccessRule(new FileSystemAccessRule(new NTAccount("", "Everyone"),
                                                                FileSystemRights.FullControl,
                                                                AccessControlType.Allow));
                    using (FileStream myFs = File.Create(inipath, 1024, FileOptions.None, fs))
                    {
                    }
                }
                catch (System.Exception ex)
                {
                    theLog.Error("Exception on create ini file:" + inipath + ", ex:" + ex.ToString());
                }
                
            }
        }

        public IniFiles() { }

        
        public void IniWriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, this.inipath);
        }
       
        public string IniReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(1000000);
            int i = GetPrivateProfileString(Section, Key, "", temp, 1000000, this.inipath);
            return temp.ToString();
        }
       
        public bool ExistINIFile()
        {
            return File.Exists(inipath);
        }
    }
}