using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using SkyDrmRestHelp;
using System.Threading;
using System.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using Microsoft.SharePoint.Client;

namespace RmxForSPOWeb.Common
{
    class SkyDrmSessionMgr
    {
        static protected CLog theLog = CLog.GetLogger("SkyDrmSessionMgr");

        static public readonly string m_strSkyDrmSessionKey = "skydrmsession";
        static public readonly string m_strSkyDrmCookiePrepend = "skydrm_";
        static public readonly string m_strSkyDrmCookieClientID = "clientId";
        static public readonly string m_strSkyDrmCookiePlatformID = "platformId";
        static public readonly string m_strSkyDrmCookieUserID = "userId";
        static public readonly string m_strSkyDrmCookieUserTicket = "ticket";
        static private Dictionary<string, LoginData> m_dicSkydrmSession = new Dictionary<string, LoginData>();
        static private ReaderWriterLockSlim m_rwlSession = new ReaderWriterLockSlim();
        static private SkyDrmRestMgr m_skydrmRestMgr = new SkyDrmRestMgr();

        [DataContract]
        private class RMXLoginPara
        {
            [DataMember]
            public int appId { get; set; }
            [DataMember]
            public long ttl { get; set; }
            [DataMember]
            public string nonce { get; set; }
            [DataMember]
            public string email { get; set; }
            [DataMember]
            public string userAttributes { get; set; }
            [DataMember]
            public string signature { get; set; }
        }

        [DataContract]
        private class LoginPara
        {
            [DataMember]
            public RMXLoginPara parameters { get; set; }
        }

        static SkyDrmSessionMgr()
        {
            Init();
        }

        public static bool Init()
        {
            // m_skydrmRestMgr.Init("https://autorms-centos7303-rmx.qapf1.qalab01.nextlabs.com:8444", "t-b730a60a909040d999bfed32d10449e8");

            // m_skydrmRestMgr.Init("https://skydrm.edrm.cloudaz.com:8444", "e607a70e-1134-4ddf-9ce1-5554599b3bc1_system");

            RMXConfig cfg = RMXConfig.Instance();
            m_skydrmRestMgr.Init(cfg.SecureViewURL);
           
            return true;
        }


        public static bool IsUnAuth(int nStatusCode)
        {
            return nStatusCode == (int)HttpStatusCode.Unauthorized;
        }

        public static RemoteViewResult RemoteView(string strFileName, byte[] data, LoginData ld)
        {
            try
            {
                theLog.Debug("RemoteView begin:" + strFileName);
                RemoteViewResult rvRes = m_skydrmRestMgr.RemoteView(strFileName, data, ld);
                theLog.Debug("RemoteView end:" + strFileName);
                return rvRes;
            }
            catch (Exception exp)
            {
                theLog.Error("RemoteView Error: " + exp.ToString());
                return null;
            }
        }

        public static LoginResult LoginToSkyDrm(string userName, string passwd)
        {

            try
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] output = md5.ComputeHash(Encoding.Default.GetBytes(passwd));
                string strPwdMd5 = BitConverter.ToString(output).Replace("-", "");

                theLog.Debug("LoginToSkyDrm begin:" + userName);
                LoginResult ls = m_skydrmRestMgr.Login(0, userName, strPwdMd5);
                theLog.Debug("LoginToSkyDrm end:" + userName);

                return ls;
            }
            catch (Exception exp)
            {
                theLog.Error("LoginToSkyDrm Error: " + exp.ToString());
                return null;
            }

            /*

            LoginResult lr = new LoginResult();
            lr.statusCode = 200;
            lr.message = "test for success.";
            lr.loginData = new LoginData();
            
            return lr;
            */
        }

        public static void AddedSkyDrmSessionInfo(string strKey, LoginData ld)
        {
            try
            {
                m_rwlSession.EnterWriteLock();
                m_dicSkydrmSession.Add(strKey, ld);
            }
            catch (Exception exp)
            {
                theLog.Error("AddedSkyDrmSessionInfo Error: " + exp.ToString());
            }
            finally
            {
                m_rwlSession.ExitWriteLock();
            }
        }

        public static LoginData GetSkyDrmLoginData(HttpCookieCollection cookCollection)
        {
            LoginData ld = null;
            //find session cookie
            HttpCookie ck = cookCollection.Get(m_strSkyDrmSessionKey);
            if (ck != null)
            {
                if (string.Equals(ck.Name, m_strSkyDrmSessionKey, StringComparison.OrdinalIgnoreCase))
                {
                    //find login data
                    string value = ck.Value;

                    try
                    {
                        m_rwlSession.EnterReadLock();
                        if (m_dicSkydrmSession.ContainsKey(value))
                        {
                            ld = m_dicSkydrmSession[value];
                        }
                    }
                    finally
                    {
                        m_rwlSession.ExitReadLock();
                    } 
                }
            }

            return ld;
        }

        public static SkyDrmRestHelp.ClassificationResult GetClassificationResult(LoginData ld,string tenantName)
        {
            try
            {
                theLog.Debug("GetClassificationResult begin:");
                SkyDrmRestHelp.ClassificationResult result = m_skydrmRestMgr.GetClassificationProfile(ld, tenantName);
                theLog.Debug("GetClassificationResult end:");
                return result;
            }
            catch(Exception ex)
            {
                theLog.Error("GetClassificationResult Error: " + ex.ToString());
                return null;
            }     
        }

        public static string SignSkyDrmLoginData(string strData)
        {
            RMXConfig  rmxCfg = RMXConfig.Instance();
            byte[] byteCertificate = rmxCfg.CertificateFileContent;
            if (byteCertificate!=null)
            {
                try
                {
                    X509Certificate2 privateCert = new X509Certificate2(byteCertificate, rmxCfg.CertificatePassword, X509KeyStorageFlags.Exportable);
                    RSACryptoServiceProvider privateKey = (RSACryptoServiceProvider)privateCert.PrivateKey;
                    using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                    {
                        RSA.ImportParameters(privateKey.ExportParameters(true));
                        byte[] byteData = System.Text.Encoding.ASCII.GetBytes(strData);
                        byte[] Signature = RSA.SignData(byteData, "SHA256");
                        string strSignature = System.Convert.ToBase64String(Signature);
                        return strSignature;
                    }
                }
                catch (System.Exception ex)
                {
                   theLog.Error("SignSkyDrmLoginData exception:" + ex.ToString());
                }
            }

            theLog.Error("SignSkyDrmLoginData error, certificate not uploaded.");
            return string.Empty;
        }

        private static string GetLoginNonce(string strAppID, string strAppKey)
        {
            NonceResult nonce = m_skydrmRestMgr.GetRmxLoginNonce(strAppID, strAppKey);
            if (nonce != null && nonce.results != null)
            {
                return nonce.results.nonce;
            }
            return string.Empty;
        }

        public static LoginData LoginSkyDrmByTrustApp(ClientContext clientContext, Web web, HttpResponseBase Response)
        {
            LoginData ld = null;
            try
            {
                RMXConfig rmxCfg = RMXConfig.Instance();

                //get nonce
                string strNonce = GetLoginNonce(rmxCfg.AppID.ToString(), rmxCfg.AppKey);
                if (string.IsNullOrEmpty(strNonce))
                {
                    theLog.Error("LoginSkyDrmByTrustApp, get nonce failed.");
                    return ld;
                }

                long nTTL = 6 * 3600 * 1000;

                //get user attribute
                User user = web.CurrentUser;
                Dictionary<string, string[]> dicUserAttr = RMXUtility.GetUserAttributeFromProfile(clientContext);
                string jsonUserAttr = RMXUtility.JsonSerializeObject(dicUserAttr);

                //calculate signature
                string strSignData =  rmxCfg.AppID + "." +
                                       rmxCfg.AppKey + "." +
                                       user.Email + "." +
                                       nTTL.ToString() + "." +
                                       strNonce + "." +
                                       jsonUserAttr;
                string strSignature = SignSkyDrmLoginData(strSignData);


                //construct login parameters
                LoginPara para = new LoginPara();
                para.parameters = new RMXLoginPara()
                {
                    appId = rmxCfg.AppID,
                    email = user.Email,
                    ttl = nTTL,
                    nonce = strNonce,
                    userAttributes = jsonUserAttr,
                    signature = strSignature
                };
                string jsonLoginPara = RMXUtility.JsonSerializeObject(para);

                //login
                LoginResult ls = m_skydrmRestMgr.TrustAppLoginRMS(jsonLoginPara);

                if (ls != null)
                {
                    ld = ls.loginData;
                    if (ld != null)
                    {
                        string strSessionGuid = Guid.NewGuid().ToString();
                        AddedSkyDrmSessionInfo(strSessionGuid, ld);

                        //response cookie
                        {
                            HttpCookie ck = new HttpCookie(SkyDrmSessionMgr.m_strSkyDrmSessionKey);
                            ck.Value = strSessionGuid;
                            Response.Cookies.Add(ck);
                        }
                        {
                            //clientId
                            string strClientIDKey = SkyDrmSessionMgr.m_strSkyDrmCookiePrepend + SkyDrmSessionMgr.m_strSkyDrmCookieClientID;
                            HttpCookie ck = new HttpCookie(strClientIDKey);
                            ck.Value = ld.clientId;
                            Response.Cookies.Add(ck);
                        }
                        {
                            //platformId
                            string strPlatformIDKey = SkyDrmSessionMgr.m_strSkyDrmCookiePrepend + SkyDrmSessionMgr.m_strSkyDrmCookiePlatformID;
                            HttpCookie ck = new HttpCookie(strPlatformIDKey);
                            ck.Value = ld.platformId;
                            Response.Cookies.Add(ck);
                        }
                        {
                            //userId
                            string strUserIdKey = SkyDrmSessionMgr.m_strSkyDrmCookiePrepend + SkyDrmSessionMgr.m_strSkyDrmCookieUserID;
                            HttpCookie ck = new HttpCookie(strUserIdKey);
                            ck.Value = ld.userId.ToString();
                            Response.Cookies.Add(ck);
                        }
                        {
                            //userTicket
                            string strUserTicketKey = SkyDrmSessionMgr.m_strSkyDrmCookiePrepend + SkyDrmSessionMgr.m_strSkyDrmCookieUserTicket;
                            HttpCookie ck = new HttpCookie(strUserTicketKey);
                            ck.Value = ld.ticket;
                            Response.Cookies.Add(ck);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                theLog.Error("Exception on LoginSkyDrmByTrustApp:" + ex.ToString());
            }


            return ld;
        }
    }

}