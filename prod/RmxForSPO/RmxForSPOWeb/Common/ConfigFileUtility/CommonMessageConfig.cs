using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace RmxForSPOWeb.Common.ConfigFileUtility
{
    public class CommonMessageConfig : ConfigurationSection
    {
        private static ConfigurationProperty _property = new ConfigurationProperty(string.Empty, typeof(KeyValueElementCollection), null, ConfigurationPropertyOptions.IsDefaultCollection);
       
        [ConfigurationProperty("", Options = ConfigurationPropertyOptions.IsDefaultCollection)]
        private KeyValueElementCollection KeyValues
        {
            get { return (KeyValueElementCollection)base[_property]; }
            set { base[_property] = value; }
        }

       
        public string RPNxlFile
        {
            get
            {
                if (KeyValues["RPNxlFile"] == null) return string.Empty;
                return KeyValues["RPNxlFile"].Value;
            }
            set
            {
                if (KeyValues["RPNxlFile"] == null) KeyValues["RPNxlFile"] = new KeyValueElement() { Key = "RPNxlFile", Value = value };
                else KeyValues["RPNxlFile"].Value = value;
            }
        }
        public string RPOneNoteFile
        {
            get
            {
                if (KeyValues["RPOneNoteFile"] == null) return string.Empty;
                return KeyValues["RPOneNoteFile"].Value;
            }
            set
            {
                if (KeyValues["RPOneNoteFile"] == null) KeyValues["RPOneNoteFile"] = new KeyValueElement() { Key = "RPOneNoteFile", Value = value };
                else KeyValues["RPOneNoteFile"].Value = value;
            }
        }
        public string RPExceptionMessage
        {
            get
            {
                if (KeyValues["RPExceptionMessage"] == null) return string.Empty;
                return KeyValues["RPExceptionMessage"].Value;
            }
            set
            {
                if (KeyValues["RPExceptionMessage"] == null) KeyValues["RPExceptionMessage"] = new KeyValueElement() { Key = "RPExceptionMessage", Value = value };
                else KeyValues["RPExceptionMessage"].Value = value;
            }
        }
        public string RPUserPermission
        {
            get
            {
                if (KeyValues["RPUserPermission"] == null) return string.Empty;
                return KeyValues["RPUserPermission"].Value;
            }
            set
            {
                if (KeyValues["RPUserPermission"] == null) KeyValues["RPUserPermission"] = new KeyValueElement() { Key = "RPUserPermission", Value = value };
                else KeyValues["RPUserPermission"].Value = value;
            }
        }
        public string RPFileCheckedOut
        {
            get
            {
                if (KeyValues["RPFileCheckedOut"] == null) return string.Empty;
                return KeyValues["RPFileCheckedOut"].Value;
            }
            set
            {
                if (KeyValues["RPFileCheckedOut"] == null) KeyValues["RPFileCheckedOut"] = new KeyValueElement() { Key = "RPFileCheckedOut", Value = value };
                else KeyValues["RPFileCheckedOut"].Value = value;
            }
        }
        public string RPFileLocked
        {
            get
            {
                if (KeyValues["RPFileLocked"] == null) return string.Empty;
                return KeyValues["RPFileLocked"].Value;
            }
            set
            {
                if (KeyValues["RPFileLocked"] == null) KeyValues["RPFileLocked"] = new KeyValueElement() { Key = "RPFileLocked", Value = value };
                else KeyValues["RPFileLocked"].Value = value;
            }
        }
        public string RPFolder
        {
            get
            {
                if (KeyValues["RPFolder"] == null) return string.Empty;
                return KeyValues["RPFolder"].Value;
            }
            set
            {
                if (KeyValues["RPFolder"] == null) KeyValues["RPFolder"] = new KeyValueElement() { Key = "RPFolder", Value = value };
                else KeyValues["RPFolder"].Value = value;
            }
        }
        public string RPNoAttachment
        {
            get
            {
                if (KeyValues["RPNoAttachment"] == null) return string.Empty;
                return KeyValues["RPNoAttachment"].Value;
            }
            set
            {
                if (KeyValues["RPNoAttachment"] == null) KeyValues["RPNoAttachment"] = new KeyValueElement() { Key = "RPNoAttachment", Value = value };
                else KeyValues["RPNoAttachment"].Value = value;
            }
        }
        public string RPFullOfAttachment
        {
            get
            {
                if (KeyValues["RPFullOfAttachment"] == null) return string.Empty;
                return KeyValues["RPFullOfAttachment"].Value;
            }
            set
            {
                if (KeyValues["RPFullOfAttachment"] == null) KeyValues["RPFullOfAttachment"] = new KeyValueElement() { Key = "RPFullOfAttachment", Value = value };
                else KeyValues["RPFullOfAttachment"].Value = value;
            }
        }
        public string RPOtherList
        {
            get
            {
                if (KeyValues["RPOtherList"] == null) return string.Empty;
                return KeyValues["RPOtherList"].Value;
            }
            set
            {
                if (KeyValues["RPOtherList"] == null) KeyValues["RPOtherList"] = new KeyValueElement() { Key = "RPOtherList", Value = value };
                else KeyValues["RPOtherList"].Value = value;
            }
        }
        public string RPSuccessed
        {
            get
            {
                if (KeyValues["RPSuccessed"] == null) return string.Empty;
                return KeyValues["RPSuccessed"].Value;
            }
            set
            {
                if (KeyValues["RPSuccessed"] == null) KeyValues["RPSuccessed"] = new KeyValueElement() { Key = "RPSuccessed", Value = value };
                else KeyValues["RPSuccessed"].Value = value;
            }
        }
        public string RPLibraryFailed
        {
            get
            {
                if (KeyValues["RPLibraryFailed"] == null) return string.Empty;
                return KeyValues["RPLibraryFailed"].Value;
            }
            set
            {
                if (KeyValues["RPLibraryFailed"] == null) KeyValues["RPLibraryFailed"] = new KeyValueElement() { Key = "RPLibraryFailed", Value = value };
                else KeyValues["RPLibraryFailed"].Value = value;
            }
        }
        public string RPListFailed
        {
            get
            {
                if (KeyValues["RPListFailed"] == null) return string.Empty;
                return KeyValues["RPListFailed"].Value;
            }
            set
            {
                if (KeyValues["RPListFailed"] == null) KeyValues["RPListFailed"] = new KeyValueElement() { Key = "RPListFailed", Value = value };
                else KeyValues["RPListFailed"].Value = value;
            }
        }
        public string SVNotNxlFile
        {
            get
            {
                if (KeyValues["SVNotNxlFile"] == null) return string.Empty;
                return KeyValues["SVNotNxlFile"].Value;
            }
            set
            {
                if (KeyValues["SVNotNxlFile"] == null) KeyValues["SVNotNxlFile"] = new KeyValueElement() { Key = "SVNotNxlFile", Value = value };
                else KeyValues["SVNotNxlFile"].Value = value;
            }
        }
        public string SVExceptionMessage
        {
            get
            {
                if (KeyValues["SVExceptionMessage"] == null) return string.Empty;
                return KeyValues["SVExceptionMessage"].Value;
            }
            set
            {
                if (KeyValues["SVExceptionMessage"] == null) KeyValues["SVExceptionMessage"] = new KeyValueElement() { Key = "SVExceptionMessage", Value = value };
                else KeyValues["SVExceptionMessage"].Value = value;
            }
        }
        public string SVNotSupportItem
        {
            get
            {
                if (KeyValues["SVNotSupportItem"] == null) return string.Empty;
                return KeyValues["SVNotSupportItem"].Value;
            }
        }
        public string SVGetFileContentFailed
        {
            get
            {
                if (KeyValues["SVGetFileContentFailed"] == null) return string.Empty;
                return KeyValues["SVGetFileContentFailed"].Value;
            }
            set
            {
                if (KeyValues["SVGetFileContentFailed"] == null) KeyValues["SVGetFileContentFailed"] = new KeyValueElement() { Key = "SVGetFileContentFailed", Value = value };
                else KeyValues["SVGetFileContentFailed"].Value = value;
            }
        }
        public string SVTryAgainLater
        {
            get
            {
                if (KeyValues["SVTryAgainLater"] == null) return string.Empty;
                return KeyValues["SVTryAgainLater"].Value;
            }
            set
            {
                if (KeyValues["SVTryAgainLater"] == null) KeyValues["SVTryAgainLater"] = new KeyValueElement() { Key = "SVTryAgainLater", Value = value };
                else KeyValues["SVTryAgainLater"].Value = value;
            }
        }
        public string SVRemoteViewFailed
        {
            get
            {
                if (KeyValues["SVRemoteViewFailed"] == null) return string.Empty;
                return KeyValues["SVRemoteViewFailed"].Value;
            }
            set
            {
                if (KeyValues["SVRemoteViewFailed"] == null) KeyValues["SVRemoteViewFailed"] = new KeyValueElement() { Key = "SVRemoteViewFailed", Value = value };
                else KeyValues["SVRemoteViewFailed"].Value = value;
            }
        }

        public string CheckInComment
        {
            get
            {
                if (KeyValues["CheckInComment"] == null) return string.Empty;
                return KeyValues["CheckInComment"].Value;
            }
        }

        public string FailedLoginSkydrm
        {
            get
            {
                if (KeyValues["FailedLoginSkydrm"] == null) return string.Empty;
                return KeyValues["FailedLoginSkydrm"].Value;
            }
        }
    }
}