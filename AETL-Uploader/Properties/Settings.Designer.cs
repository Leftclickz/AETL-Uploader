﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AETL_Uploader.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.2.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ClientID {
            get {
                return ((string)(this["ClientID"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ClientSecret {
            get {
                return ((string)(this["ClientSecret"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://www.googleapis.com/upload/storage/v1/b/")]
        public string GoogleCloudStorageBaseUrl {
            get {
                return ((string)(this["GoogleCloudStorageBaseUrl"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://speech.googleapis.com/v1/operations/")]
        public string GoogleSpeechBaseUrl {
            get {
                return ((string)(this["GoogleSpeechBaseUrl"]));
            }
            set {
                this["GoogleSpeechBaseUrl"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://speech.googleapis.com/v1/speech:longrunningrecognize")]
        public string GoogleLongRunningRecognizeBaseUrl {
            get {
                return ((string)(this["GoogleLongRunningRecognizeBaseUrl"]));
            }
            set {
                this["GoogleLongRunningRecognizeBaseUrl"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://www.googleapis.com/auth/cloud-platform")]
        public string GoogleCloudScope {
            get {
                return ((string)(this["GoogleCloudScope"]));
            }
            set {
                this["GoogleCloudScope"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("galaxy2-aetl")]
        public string Bucket {
            get {
                return ((string)(this["Bucket"]));
            }
            set {
                this["Bucket"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Users\\ceecam\\source\\git repos\\AETL-Uploader\\AETL-Uploader\\data\\booming-hue-210" +
            "413-a08567a36cdd.json")]
        public string GoogleCloudCredentialPath {
            get {
                return ((string)(this["GoogleCloudCredentialPath"]));
            }
            set {
                this["GoogleCloudCredentialPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("O:\\AETL-Encoded")]
        public string HotFolder {
            get {
                return ((string)(this["HotFolder"]));
            }
            set {
                this["HotFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("timelapses")]
        public string BucketSubfolder {
            get {
                return ((string)(this["BucketSubfolder"]));
            }
            set {
                this["BucketSubfolder"] = value;
            }
        }
    }
}
