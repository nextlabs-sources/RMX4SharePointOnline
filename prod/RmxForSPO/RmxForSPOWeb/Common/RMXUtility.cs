using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.UserProfiles;
using Microsoft.IdentityModel.S2S.Protocols.OAuth2;
using Microsoft.SharePoint;
using System.Text.RegularExpressions;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace RmxForSPOWeb.Common
{
    public class RMXUtility
    {
        private static CLog theLog = null;
        private static HttpServerUtility m_HttpServerUtility = null;
        private static string m_strApplicationPath = null;
        private static string m_strAppDataPath = null;

        private static byte[] m_iv = { 0x77, 0x34, 0x97, 0x78, 0x90, 0xdc, 0xbe, 0xEF};

        public static void SetHttpServerUtility(HttpServerUtility server)
        {
            m_HttpServerUtility = server;
            theLog =  CLog.GetLogger("RMXUtility");
        }

        public static ClientContext GetSharePointApponlyClientContext(string siteUrl)
        {
            try
            {
                Uri obUri = new Uri(siteUrl);
                OAuth2AccessTokenResponse response = TokenHelper.GetAppOnlyAccessToken(TokenHelper.SharePointPrincipal, obUri.Authority, TokenHelper.GetRealmFromTargetUrl(obUri));
                if (response != null)
                {
                    ClientContext clientContextAppOnly = TokenHelper.GetClientContextWithAccessToken(siteUrl, response.AccessToken);
                    if (clientContextAppOnly == null)
                    {
                        theLog.Debug(string.Format("GetSharePointApponlyClientContext  GetClientContextWithAccessToken return NULL. siteURL:{0}, accesssToken:{1}", siteUrl, response.AccessToken));
                    }
                    return clientContextAppOnly;
                }
                else
                {
                    theLog.Error(string.Format("GetSharePointApponlyClientContext GetAppOnlyAccessToken return NULL, siteUrl:{0}", siteUrl));
                }
            }
            catch (System.Exception ex)
            {
                theLog.Error(string.Format("GetSharePointApponlyClientContext Exception siteUrl:{0}, Exception:{1}", siteUrl, ex.ToString() ));
            }
            
            return null;
        }


        public static ClientContext GetSharePointCurrentUserClientContext(HttpContextBase httpContext)
        {
            try
            {
                //get SharePoint context
                SharePointContext spContext = SharePointContextProvider.Current.GetSharePointContext(httpContext);
                if (spContext==null)
                {
                    throw new Exception("GetSharePointContext return null");
                }

                ClientContext clientContext = spContext.CreateUserClientContextForSPHost();
                return clientContext;
            }
            catch (System.Exception ex)
            {
                theLog.Error("GetSharePointCurrentUserClientContext exception:" + ex.ToString());
            }
            return null;
        }

        public static string GetRMXAppDataFolder()
        {
            if (m_strAppDataPath==null)
            {
                m_strAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\NextLabs\RMX4SPO\";
                if (System.IO.Directory.Exists(m_strAppDataPath))
                {
                    System.IO.Directory.CreateDirectory(m_strAppDataPath);
                }
            }
            return m_strAppDataPath;
        }

        public static string GetRMXAppFolder()
        {
            if (m_strApplicationPath==null)
            {
                m_strApplicationPath = m_HttpServerUtility.MapPath("/");
            }
            return m_strApplicationPath;
        }

        public static string GetRMXConfigFolder()
        {
            string strFolder = RMXUtility.GetRMXAppDataFolder() + @"config";
            if (!System.IO.Directory.Exists(strFolder))
            {
                System.IO.Directory.CreateDirectory(strFolder);
            }

            return strFolder + "\\";
        }

        public static bool IsSameSPUser(User user1, User user2)
        {
            try
            {
                if (user1 != null && user2 != null)
                {
                    return user1.UserPrincipalName.Equals(user2.UserPrincipalName, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch { }
            return false;
        }

        public static string GetUserPrincipalName(User spUser)
        {
            //check file lock status
            string strPrincipalName = "";
            try
            {
                strPrincipalName = spUser.UserPrincipalName;
            }
            catch (Exception)
            {
                //when file is not locked by user, we will get this exception.
                //when file is locked by user, we didn't get this exception. so we didn't need to process this exception.

            }
            return strPrincipalName;
        }


        public static bool IsNewDocument(string strDocName)
        {
            string pattern = RmxModule.m_strNewDocumentNameFormat;

            try
            {
                if (Regex.Matches(strDocName, pattern).Count > 0)
                {
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                theLog.Error("Exception on IsNewDocument:" + ex.ToString());
            }
           

            return false;
        }

        public static string DesEncrypt(string strText, string strEncrKey)
         {
             try
             {
                byte[] byKey = Encoding.Default.GetBytes(strEncrKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
              
                byte[] inputByteArray = Encoding.UTF8.GetBytes(strText);
                 MemoryStream ms = new MemoryStream();
                 CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(byKey, m_iv), CryptoStreamMode.Write);
                 cs.Write(inputByteArray, 0, inputByteArray.Length);
                 cs.FlushFinalBlock();
                 return Convert.ToBase64String(ms.ToArray());
             }
             catch(Exception ex)
             {
                theLog.Error("DesEncrypt:" + ex.ToString());
                 return "";
             }
         }

        public static string DesDecrypt(string strText, string sDecrKey)
         {
             try
             {
                 byte[] byKey = Encoding.Default.GetBytes(sDecrKey.Substring(0,8));
                 byte[] inputByteArray = new Byte[strText.Length];
 
                 DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Convert.FromBase64String(strText);
                 MemoryStream ms = new MemoryStream();
                 CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(byKey, m_iv), CryptoStreamMode.Write);
                 cs.Write(inputByteArray, 0, inputByteArray.Length);
                 cs.FlushFinalBlock();
                 return System.Text.Encoding.UTF8.GetString(ms.ToArray());
             }
            catch (Exception ex)
            {
                theLog.Error("DesDecrypt:" + ex.ToString());
                return "";
            }
        }

        public static string MD5Encrypt(string strText)
        {
            try
            {
               MD5 md5 = new MD5CryptoServiceProvider();
                byte[] result = md5.ComputeHash(System.Text.Encoding.Default.GetBytes(strText));
                return System.Text.Encoding.Default.GetString(result);
            }
            catch (System.Exception ex)
            {
                theLog.Error("MD5Encrypt:" + ex.ToString());
                return "";
            }
           
        }

        public static string JsonSerializeObject(object obj)
        {
            string strResult = "";
            try
            {
                var set = new DataContractJsonSerializerSettings();
                set.UseSimpleDictionaryFormat = true;
                DataContractJsonSerializer jsonSeria = new DataContractJsonSerializer(obj.GetType(), set);
                MemoryStream msObj = new MemoryStream();
                jsonSeria.WriteObject(msObj, obj);
                msObj.Position = 0;

                StreamReader sr = new StreamReader(msObj);
                strResult = sr.ReadToEnd();

                return strResult;
            }
            catch (System.Exception ex)
            {
                theLog.Error("exception JsonSerializeObject:" + ex.ToString());
            }

            return strResult;
        }

        public static Dictionary<string, string[]> GetUserAttributeFromProfile(ClientContext clientContext)
        {
            Dictionary<string, string[]> dicAttrs = new Dictionary<string, string[]>();

            try
            {
                PeopleManager peopleManager = new PeopleManager(clientContext);
                PersonProperties personProperties = peopleManager.GetMyProperties();
                clientContext.Load(personProperties, p => p.AccountName, p => p.UserProfileProperties);
                clientContext.ExecuteQuery();
                foreach (var property in personProperties.UserProfileProperties)
                {
                    if ((!string.IsNullOrWhiteSpace(property.Key)) &&
                        (!string.IsNullOrWhiteSpace(property.Value)) )
                    {
                        string[] strAttrValue = new string[1];
                        strAttrValue[0] = property.Value;
                        dicAttrs.Add(property.Key, strAttrValue);
                    } 
                }
            }
            catch (System.Exception ex)
            {
                theLog.Error("Exception on get user profile:" + ex.ToString());
            }

            return dicAttrs;
        }

    }
}