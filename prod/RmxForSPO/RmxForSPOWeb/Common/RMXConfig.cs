using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RmxForSPOWeb;

namespace RmxForSPOWeb.Common
{
    public class RMXConfig
    {
        private static CLog theLog = CLog.GetLogger("RMXConfig");
        private static RMXConfig m_instance = null;

        #region member
        private string strJavaPcHost;
        private string strOAUTHHost;
        private string strClientSecureID;
        private string strClientSecureKey;

        private string strSecureViewURL;
        private string strRouterURL;
        private int nAppID;
        private string strAppKey;
        private string strCertificateFileContentBase64;
        private string strCertificateFileName;
        private string strCertificatePassword;

        #endregion


        #region GET/SET
        public string JavaPcHost { get { return strJavaPcHost; }
                                   set { strJavaPcHost = value; } }
        public string OAUTHHost {  get { return strOAUTHHost; }
                                   set { strOAUTHHost = value; } }
        public string ClientSecureID {  get { return strClientSecureID; }
                                        set { strClientSecureID = value; } }
        public string ClientSecureKey {  get { return strClientSecureKey; }
                                         set { strClientSecureKey = value; } }

        public string SecureViewURL { get { return strSecureViewURL; }
                                      set { strSecureViewURL = value; } }
        public string RouterURL { get { return strRouterURL; }
                                  set { strRouterURL = value; } }
        public int AppID {  get { return nAppID; }
                            set { nAppID = value; } }
        public string AppKey {  get { return strAppKey; }
                                set { strAppKey = value; } }

        public string CertificateFileContentBase64
        {
            set { strCertificateFileContentBase64 = value; }
            get { return strCertificateFileContentBase64;  }
        }
        public byte[] CertificateFileContent
        {
            get
            {
                try
                {
                    return System.Convert.FromBase64String(strCertificateFileContentBase64);
                }
                catch (System.Exception)
                {
                    return null;
                }
            }
        }

        public string CertificateFileName
        {
            get { return strCertificateFileName; }
            set { strCertificateFileName = value; }
        }

        public string CertificatePassword
        {
            get
            {
                if ((!string.IsNullOrWhiteSpace(strCertificateFileContentBase64)) &&
                    (!string.IsNullOrWhiteSpace(strCertificatePassword)) )
                {
                    string strMD5 = RMXUtility.MD5Encrypt(strCertificateFileContentBase64);
                    return RMXUtility.DesDecrypt(strCertificatePassword, strMD5);
                }
                return "";
            }

            set
            {
                string strMD5 = RMXUtility.MD5Encrypt(strCertificateFileContentBase64);
                strCertificatePassword = RMXUtility.DesEncrypt(value, strMD5);
            }
        }
        #endregion

        private RMXConfig() { }


        public static RMXConfig Instance()
        {
            if (m_instance==null)
            {
                m_instance = new RMXConfig();
            }
            return m_instance;
        }

        public bool ReadConfigFromFile()
        {
            bool bRet = true;

            try
            {
                IniFiles configFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "GeneralSetting.ini");

                //read from JAVAPC
                {
                    string strSectionJavaPC = "JAVAPC";
                    JavaPcHost = configFile.IniReadValue(strSectionJavaPC, "JavaPcHost");
                    OAUTHHost = configFile.IniReadValue(strSectionJavaPC, "OAUTHHost");
                    ClientSecureID = configFile.IniReadValue(strSectionJavaPC, "ClientSecureID");
                    ClientSecureKey = configFile.IniReadValue(strSectionJavaPC, "ClientSecureKey");
                }


                //read from RMJAVASDK
                {
                    string strSectionRMSSDK = "RMJAVASDK";
                    SecureViewURL = configFile.IniReadValue(strSectionRMSSDK, "SecureViewURL");
                    RouterURL = configFile.IniReadValue(strSectionRMSSDK, "RouterURL");
                    AppID = int.Parse(configFile.IniReadValue(strSectionRMSSDK, "AppId"));
                    AppKey = configFile.IniReadValue(strSectionRMSSDK, "AppKey");
                    strCertificateFileContentBase64 = configFile.IniReadValue(strSectionRMSSDK, "CertContent");
                    strCertificatePassword = configFile.IniReadValue(strSectionRMSSDK, "CertSecureKey");
                    strCertificateFileName = configFile.IniReadValue(strSectionRMSSDK, "CertFileName");

                }
            }
            catch (System.Exception ex)
            {
                string strLog = string.Format("ReadConfigFromFile exception:{0}", ex.ToString());
                theLog.Error(strLog);
                bRet = false;
            }
           
          
            return bRet;
        }

        public bool WriteConfigToFile()
        {
            bool bRet = true;

            try
            {
                IniFiles configFile = new IniFiles(RMXUtility.GetRMXConfigFolder() + "GeneralSetting.ini");

                //JAVAPC
                {
                    string strSectionJavaPC = "JAVAPC";
                    configFile.IniWriteValue(strSectionJavaPC, "JavaPcHost", JavaPcHost);
                    configFile.IniWriteValue(strSectionJavaPC, "OAUTHHost", OAUTHHost);
                    configFile.IniWriteValue(strSectionJavaPC, "ClientSecureID", ClientSecureID);
                    configFile.IniWriteValue(strSectionJavaPC, "ClientSecureKey", ClientSecureKey);
                }


                //RMJAVASDK
                {
                    string strSectionRMSSDK = "RMJAVASDK";
                    configFile.IniWriteValue(strSectionRMSSDK, "SecureViewURL", SecureViewURL);
                    configFile.IniWriteValue(strSectionRMSSDK, "RouterURL", RouterURL);
                    configFile.IniWriteValue(strSectionRMSSDK, "AppId", AppID.ToString() );
                    configFile.IniWriteValue(strSectionRMSSDK, "AppKey", AppKey);
                    configFile.IniWriteValue(strSectionRMSSDK, "CertContent", strCertificateFileContentBase64);
                    configFile.IniWriteValue(strSectionRMSSDK, "CertSecureKey", strCertificatePassword);
                    configFile.IniWriteValue(strSectionRMSSDK, "CertFileName", strCertificateFileName);
                }
            }
            catch (System.Exception ex)
            {
                string strLog = string.Format("ReadConfigFromFile exception:{0}", ex.ToString());
                theLog.Error(strLog);
                bRet = false;
            }


            return bRet;
        }


    }
}