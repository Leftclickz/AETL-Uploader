using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using System.Collections.Generic;
using Google.Cloud.Storage.V1;
using System.Linq;

namespace AETL_Uploader
{
    static class Program
    {
        private static GoogleCredential googleCredential;
        private static volatile bool programRunning = true;

        static void Main(string[] args)
        {
            Console.WriteLine("AETL-Uploader");

            try
            {
                SetCredentials();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                programRunning = false;
            }

            while (programRunning)
            {
                ParseFilesForUploadAsync().Wait();
                Thread.Sleep(10000);
            } 
        }

        public static async Task ParseFilesForUploadAsync()
        {
            //get our raw list of video files excluding the native ones
            string[] files = Directory.GetFiles(Properties.Settings.Default.HotFolder, "*.mp4", SearchOption.AllDirectories);

            List<Task> uploadTasks = new List<Task>();

            //queue all files for upload
            foreach (string s in files)
            {
                uploadTasks.Add(Task.Run(() => UploadMediaToCloud(s)));
                break;
            }

            //wait until they completed
            await Task.WhenAll(uploadTasks);
        }

        public static void SetCredentials()
        {
            // Set google cloud credentials
            using (Stream stream = new FileStream(Properties.Settings.Default.GoogleCloudCredentialPath, FileMode.Open, FileAccess.Read))
                googleCredential = GoogleCredential.FromStream(stream).CreateScoped(Properties.Settings.Default.GoogleCloudScope);
        }

        public static async Task UploadMediaToCloud(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string locationID = fileName.Substring(0, fileName.IndexOf('-'));
            fileName = fileName.Substring(fileName.IndexOf('-') + 1);
            string projectID = fileName.Substring(0, fileName.IndexOf('-'));

            //if this is a monthly video being uploaded then use the filename, otherwise if its a start-to-end video use full.mp4
            if (fileName.Contains("MONTHLY"))
                fileName = fileName.Substring(fileName.IndexOf('-') + 1);
            else
                fileName = "full.mp4";

         
            using (StorageClient storageClient = StorageClient.Create(googleCredential))
            {
                //this is the directory we will upload to
                string bucketDirectory = "";

                //find out if our directory exists
                List<string> bucketObjects = new List<string>();
                var request = storageClient.ListObjects(Properties.Settings.Default.Bucket).Where( s => s.Name.EndsWith("/") && s.Name.Contains(locationID) && s.Name.Contains(projectID));
                foreach (Google.Apis.Storage.v1.Data.Object val in request)
                    bucketObjects.Add(val.Name);

                //if we didnt find our directory make them
                if (bucketObjects.Count == 0)
                {
                    AddFolder(storageClient, Properties.Settings.Default.BucketSubfolder + "/" + locationID, Properties.Settings.Default.Bucket);//{location_id}
                    bucketDirectory = AddFolder(storageClient, Properties.Settings.Default.BucketSubfolder + "/" + locationID + "/" + projectID, Properties.Settings.Default.Bucket);//{location_id}/{project_id}
                }
                else
                    //if its there then use it
                    bucketDirectory = bucketObjects[0];

                try
                {
                    var objectToUpload = new Google.Apis.Storage.v1.Data.Object()
                    {
                        Bucket = Properties.Settings.Default.Bucket,
                        Name = bucketDirectory + fileName,
                        ContentType = "video/mp4"
                    };

                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        await storageClient.UploadObjectAsync(objectToUpload, fileStream).ConfigureAwait(false);
                        return;
                    }
                }
                catch (Google.GoogleApiException e)
                {
                    Console.WriteLine("Google API error: " + e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unhandled exception: " + e.Message);
                }
            }
        }

        public static string AddFolder(in StorageClient client, string folder, string bucket)
        {
            if (!folder.EndsWith("/"))
                folder += "/";

            Google.Apis.Storage.v1.Data.Object obj = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = bucket,
                Name = folder,
                ContentType = "application/x-directory"
            };

            try
            {
                client.UploadObject(obj, new MemoryStream(Encoding.UTF8.GetBytes("")));
                return folder;
            }
            catch (Google.GoogleApiException e)
            {
                Console.WriteLine("Google API Error: " + e.Message);
                return null;
            }
        }
    }
}
