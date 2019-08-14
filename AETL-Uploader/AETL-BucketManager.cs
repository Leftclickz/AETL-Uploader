using System.IO;
using System.Threading;
using System.Linq;
using Google.Cloud.Storage.V1;
using Google.Apis.Storage.v1;
using Google.Apis.Storage.v1.Data;
using GoogleCred = Google.Apis.Auth.OAuth2.GoogleCredential;
using Google.Apis.Auth.OAuth2;
using System.Threading.Tasks;

namespace AETL_Uploader
{
    class AETL_BucketManager
    {
        public AETL_BucketManager() { }

        public AETL_BucketManager(string CredentialFileLocation)
        {
            AttachCredential(CredentialFileLocation);
        }

        public AETL_BucketManager(EncryptionKey Key)
        {
            AttachEncryption(Key);
        }

        public AETL_BucketManager(string CredentialFileLocation, EncryptionKey Key)
        {
            AttachCredential(CredentialFileLocation);
            AttachEncryption(Key);
        }

        public void AttachCredential(string CredentialFileLocation)
        {
            if (File.Exists(CredentialFileLocation))
                m_CredentialFile = GoogleCred.FromFile(CredentialFileLocation);
            else
                System.Console.WriteLine("Credential file " + CredentialFileLocation + " not found.");

        }

        public void AttachEncryption(EncryptionKey Key)
        {
            m_EncryptionKey = Key;
        }

        public void Initialize()
        {
            m_Client = StorageClient.Create(m_CredentialFile, m_EncryptionKey);
        }

        public async Task UploadAsync(string FilePathToUpload, string bucket)
        {
            if (IsValid())
            {
                await m_Client.UploadObjectAsync(bucket, FilePathToUpload, "video/mp4", new FileStream(FilePathToUpload, FileMode.Open));
            }
            return;
        }

        private bool IsValid()
        {
            if (m_Client == null)
            {
                System.Console.WriteLine("Client is not initialized yet.");
                return false;
            }

            return true;
        }

        public GoogleCred m_CredentialFile
        {
            get { return m_CredentialFile; }
            set { m_CredentialFile = value; }
        }

        private EncryptionKey m_EncryptionKey = null;
        private string m_ProjectID = null;

        public StorageClient m_Client
        {
            get { return m_Client; }
            set { m_Client = value; }
        }
      
    }
}
