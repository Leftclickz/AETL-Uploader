using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using System.Collections.Generic;
using Google.Cloud.Storage.V1;


namespace AETL_Uploader
{
    static class Program
    {
        private static GoogleCredential googleCredential;
        private static volatile bool programRunning = true;

        static void Main(string[] args)
        {
            Console.WriteLine("-- AETL-Uploader --");
            Console.WriteLine("VERSION: 0.1a");

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

            Console.WriteLine("Update loop - " + files.Length.ToString() + " files located for upload. \n");

            //queue all files for upload
            foreach (string s in files)
                uploadTasks.Add(Task.Run(() => UploadMediaToCloud(s)));

            //wait until they complete
            await Task.WhenAll(uploadTasks);

            Console.WriteLine("\nUpdate loop complete. \n");
        }

        public static void SetCredentials()
        {
            // Set google cloud credentials
            using (Stream stream = new FileStream(Properties.Settings.Default.GoogleCloudCredentialPath, FileMode.Open, FileAccess.Read))
                googleCredential = GoogleCredential.FromStream(stream).CreateScoped(Properties.Settings.Default.GoogleCloudScope);

            Console.WriteLine("Credentials set.");
        }

        public static async Task UploadMediaToCloud(string filePath)
        {
            AETL_Object obj;
            {
                //pull data from the strings
                string fileName = Path.GetFileName(filePath);
                string projectID = fileName.Substring(0, fileName.IndexOf('-'));
                fileName = fileName.Substring(fileName.IndexOf('-') + 1);
                string locationID;

                if (fileName.IndexOf('-') == -1)
                    locationID = fileName.Substring(0, fileName.IndexOf(".mp4"));
                else
                    locationID = fileName.Substring(0, fileName.IndexOf('-'));

                //fetch resolution
                string Resolution = Path.GetDirectoryName(filePath);
                Resolution = Resolution.Substring(Resolution.IndexOf('\\') + 1);
                Resolution = Resolution.Substring(Resolution.IndexOf('\\') + 1);

                //if this is a monthly video being uploaded then use the filename, otherwise if its a start-to-end video use full.mp4
                if (filePath.Contains("MONTHLY"))
                {
                    obj = new AETL_Monthly();
                    string name = fileName.Substring(fileName.IndexOf('-') + 1);
                    name = name.Substring(0, name.IndexOf('.'));
                    obj.Filename = name;
                }
                else
                {
                    obj = new AETL_Full();
                    obj.Filename = "full";
                }

                //fill object data
                obj.ProjectID = projectID;
                obj.LocationID = locationID;
                obj.Bucket = Properties.Settings.Default.Bucket;
                obj.Filepath = filePath;
                obj.VideoRes = Resolution;
            }
         
            using (StorageClient storageClient = StorageClient.Create(googleCredential))
            {
                //check if the subfolder exists for our object
                obj.GenerateSubfolders(storageClient);

                //check if the file already exists.... if it does and its not a GENERIC project then we can leave.
                if (obj.CheckIfFileExists(storageClient) && obj.Type != ProjectType.GENERIC)
                    return;

                //otherwise lets upload the data
                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        //upload the file
                        Console.WriteLine("Attempting file upload: " + filePath);
                        Google.Apis.Storage.v1.Data.Object output  = await storageClient.UploadObjectAsync(obj.GetObject(), fileStream).ConfigureAwait(false);

                        //report success
                        Console.WriteLine("File uploaded: " + filePath);
                        Console.WriteLine("Object created: " + output.TimeCreated);             
                    }

                    //cleanup the file now that its been uploaded
                    Thread.Sleep(10000);
                    File.Delete(filePath);

                    return;
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
            //add the ending slash if it isnt there
            if (!folder.EndsWith("/"))
                folder += "/";

            //create the folder object
            Google.Apis.Storage.v1.Data.Object obj = new Google.Apis.Storage.v1.Data.Object()
            {
                Bucket = bucket,
                Name = folder,
                ContentType = "application/x-directory"
            };

            try
            {
                //attempt to upload the folder
                Console.WriteLine("Creating subdirectory of bucket: " + folder);
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
