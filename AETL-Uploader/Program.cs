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
            AETL_Object obj;
            {
                string fileName = Path.GetFileName(filePath);
                string locationID = fileName.Substring(0, fileName.IndexOf('-'));
                fileName = fileName.Substring(fileName.IndexOf('-') + 1);
                string projectID = fileName.Substring(0, fileName.IndexOf('-'));


                //if this is a monthly video being uploaded then use the filename, otherwise if its a start-to-end video use full.mp4
                if (filePath.Contains("MONTHLY"))
                {
                    obj = new AETL_Monthly();
                    obj.Filename = fileName.Substring(fileName.IndexOf('-') + 1);
                }
                else
                {
                    obj = new AETL_Full();
                    obj.Filename = "full.mp4";
                }

                obj.ProjectID = projectID;
                obj.LocationID = locationID;
                obj.Bucket = Properties.Settings.Default.Bucket;
                obj.Filepath = filePath;
            }
         
            using (StorageClient storageClient = StorageClient.Create(googleCredential))
            {
                //this is the directory we will upload to
                string bucketDirectory = "";

                //check if the subfolder exists for our object
                obj.GetSubFolder(storageClient);

                //check if the file already exists.... if it does and its not a GENERIC project then we can leave.
                if (obj.CheckIfFileExists(storageClient) && obj.Type != ProjectType.GENERIC)
                    return;

                //otherwise lets upload the data
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        await storageClient.UploadObjectAsync(obj.GetObject(), fileStream).ConfigureAwait(false);
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
