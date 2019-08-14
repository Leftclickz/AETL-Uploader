using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;

enum ProjectType
{
    DEFAULT = 0,
    GENERIC = 1,
    MONTHLY = 2,
    WEEKLY = 3
}

enum Resolution
{
    DEFAULT = 0,
    FOUR_EIGHTY = 1,
    SEVEN_TWENTY = 2,
    THOSAND_EIGHTY = 3,
    NATIVE = 4
}

namespace AETL_Uploader
{
    abstract class AETL_Object
    {

        public abstract bool CheckIfFileExists(in StorageClient client);

        public bool GetSubFolder(in StorageClient client)
        {
            bool returnVal = true;
            if (GetObjectsDelimited(client, true, Properties.Settings.Default.BucketSubfolder, ProjectID).Count == 0)
                Program.AddFolder(client, Properties.Settings.Default.BucketSubfolder + "/" + ProjectID, Bucket);
            if (GetObjectsDelimited(client, true, Properties.Settings.Default.BucketSubfolder, ProjectID, LocationID).Count == 0)
            {
                Program.AddFolder(client, BucketDirectory, Bucket);
                returnVal = false;
            }

            BucketDirectory = Properties.Settings.Default.BucketSubfolder + "/" + ProjectID + "/" + LocationID;

            return returnVal;
        }

        public virtual Google.Apis.Storage.v1.Data.Object GetObject()
        {
            Object = new Google.Apis.Storage.v1.Data.Object()
            {
                Name = Filename,
                Bucket = this.Bucket,
                ContentType = "video/mp4"
            };

            return Object;
        }

        protected List<string> GetObjectsDelimited(in StorageClient client, bool IsCheckingDirectory = false, params string[] Delimiters)
        {
            string compiledArgumentPath = "";
            for (int i = 0; i < Delimiters.Length; i++)
                compiledArgumentPath += Delimiters[i] + '/';

            //remove last directory notation if the check is for a file
            if (IsCheckingDirectory) compiledArgumentPath = compiledArgumentPath.Remove(compiledArgumentPath.Length - 1);

            List<string> bucketObjects = new List<string>();
            try
            {
                var request = client.ListObjects(Bucket).Where(s => s.Name.Contains(compiledArgumentPath));
                foreach (Google.Apis.Storage.v1.Data.Object val in request)
                    bucketObjects.Add(val.Name);

                return bucketObjects;
            }
            catch (Google.GoogleApiException e)
            {
                Console.WriteLine("Google API error: " + e);
                return null;
            }
        }

        public virtual string Filepath { get; set; }
        public virtual string Filename { get; set; }
        public virtual string Bucket { get; set; }
        public virtual string ProjectID { get; set; }
        public virtual string LocationID { get; set; }
        public virtual Resolution VideoRes { get; set; }

        public abstract ProjectType Type { get; }

        private Google.Apis.Storage.v1.Data.Object Object;
        protected string BucketDirectory;
    }

    class AETL_Monthly : AETL_Object
    {
        
        public override bool CheckIfFileExists(in StorageClient client)
        {
            if (GetObjectsDelimited(client, false, Properties.Settings.Default.BucketSubfolder, ProjectID, LocationID, Filename).Count == 0)
                return false;
            return true;
        }

        public override ProjectType Type { get { return ProjectType.MONTHLY; } }
    }


    class AETL_Full : AETL_Object
    {

        public override bool CheckIfFileExists(in StorageClient client)
        {
            //if it found something then our object exists and needs to be overwritten.
            //if (GetObjectsDelimited(client, false, Properties.Settings.Default.BucketSubfolder, ProjectID, LocationID, Filename).Count > 0)
            //    client.DeleteObject(GetObject());

            return false;
        }

        public override ProjectType Type { get { return ProjectType.GENERIC; } }
    }
}
