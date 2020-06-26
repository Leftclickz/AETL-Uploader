using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Google.Apis.Auth.OAuth2;
using System.Threading;
using System.Collections.Generic;
using Google.Cloud.Storage.V1;
using AETL_Uploader.Properties;

namespace AETL_Uploader
{
    static class Program
    {
        private static GoogleCredential googleCredential;
        private static volatile bool programRunning = true;

        private static volatile bool IsInCommandMode = false;
        private static List<string> CommandList = null;

        static void Main(string[] args)
        {
            Console.WriteLine("-- AETL-Uploader --");
            Console.WriteLine("VERSION: 0.2a");

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-i")
                {
                    IsInCommandMode = true;
                    CommandList = new List<string>();

                    int baseBuf = i + 1;

                    //Add each string after the -i command until a new command is found
                    while (baseBuf < args.Length)
                    {
                        //Escape out of this sequence if another command is input
                        if (args[baseBuf][0] == '-')
                        {
                            i = baseBuf;
                            break;
                        }

                        CommandList.Add(args[baseBuf]);
                        baseBuf++;
                        i++;
                    }

                    i--;
                }
                else if (args[i] == "-b")
                {
                    if (i+1 == args.Length)
                    {
                        Console.Error.WriteLine("-b command requires a string (the name of the google bucket).\n Example: \"-b AETL\".");
                        programRunning = false;
                    }
                    else
                    {
                        Settings.Default.Bucket = args[i + 1];
                        i++;
                    }
                }
                else if (args[i] == "-sub")
                {
                    if (i + 1 == args.Length)
                    {
                        Console.Error.WriteLine("-b command requires a string (path to the google bucket subfolder that will be uploaded to).\n Example: \"-sub timelapses\"");
                        programRunning = false;
                    }
                    else
                    {
                        Settings.Default.BucketSubfolder = args[i + 1];
                        i++;
                    }
                }
                else if (args[i] == "-h")
                {
                    if (i + 1 == args.Length)
                    {
                        Console.Error.WriteLine("-h command requires a full filepath to the mp4 hotfolder for uploads. \nExample: -h \"O:\\Projects\\Videos\"\"");
                        programRunning = false;
                    }
                    else
                    {
                        Settings.Default.HotFolder = args[i + 1];
                        i++;
                    }
                }
                else if (args[i] == "-cred")
                {
                    if (i + 1 == args.Length)
                    {
                        Console.Error.WriteLine("-cred command requires a full filepath that leads to the json credential file.\nExample: -h \"O:\\Projects\\json\\file.json\"\"");
                        programRunning = false;
                    }
                    else
                    {
                        Settings.Default.HotFolder = args[i + 1];
                        i++;
                    }
                }
            }

            if (programRunning)
            {
                //Set access to the google bucket
                try
                {
                    SetCredentials();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    Console.Out.WriteLine("Error: AETL-Uploader failed to connect to the Google bucket.");
                    programRunning = false;
                }
            }

            //This is the standard program hot-folder operation procedure where files are constantly parsed for upload.
            if (IsInCommandMode == false)
            {
                while (programRunning)
                {
                    ParseFilesForUploadAsync().Wait();
                    Thread.Sleep(10000);
                }
            }
            //Otherwise this program will do only specific uploads and close when complete
            else 
            {
            }
        }

        public static async Task UploadTargettedFilesAsync()
        {
            List<Task> uploadTasks = new List<Task>();
            Console.Out.WriteLine("Upload: " + CommandList.Count.ToString() + " files set for upload. \n");

            //queue all files for upload
            foreach (string s in CommandList)
            {
                uploadTasks.Add(Task.Run(() => UploadMediaToCloud(s)));
            }

            //wait until they complete
            await Task.WhenAll(uploadTasks);

            //notify user
            Console.Out.WriteLine("\nUpdate loop complete. \n");
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

            Console.Out.WriteLine("Credentials set.");
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
                        Console.Out.WriteLine("Attempting file upload: " + filePath);
                        Google.Apis.Storage.v1.Data.Object output  = await storageClient.UploadObjectAsync(obj.GetObject(), fileStream).ConfigureAwait(false);

                        //report success
                        Console.Out.WriteLine("File uploaded: " + filePath);
                        Console.Out.WriteLine("Object created: " + output.TimeCreated);             
                    }

                    //cleanup the file now that its been uploaded
                    Thread.Sleep(10000);
                    File.Delete(filePath);

                    return;
                }
                catch (Google.GoogleApiException e)
                {
                    Console.Error.WriteLine("Google API error: " + e.Message);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Unhandled exception: " + e.Message);
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
                Console.Out.WriteLine("Creating subdirectory of bucket: " + folder);
                client.UploadObject(obj, new MemoryStream(Encoding.UTF8.GetBytes("")));
                return folder;
            }
            catch (Google.GoogleApiException e)
            {
                Console.Out.WriteLine("Google API Error: " + e.Message);
                return null;
            }
        }
    }
}
