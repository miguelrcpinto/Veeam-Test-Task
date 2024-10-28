using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSynchronizer
{
    class Program
    {
        static string sourceFolderPath; //Path to contain Source Folder
        static string replicaFolderPath; //Path to contain Replica Folder
        static string logFilePath; //Path to the Log File
        static int syncInterval; //Interval for synchronization

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: Arguments received from console.
        //OUTPUT: None.
        //EXPLANATION: Main Functio. Starts the synchronization task and stores the paths to the specified folders, files and interval to the respective variables.
        //---------------------------------------------------------------------------------------------------------------------
        static void Main(string[] args)
        {
            // Validates that are 4 arguments, Source Folder, Replica Folder, Interval for Synchronization and Log File
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: <Source Folder Path> <Replica Folder Path> <Sync Interval in Seconds> <Log File Path>");
                return;
            }

            sourceFolderPath = args[0]; //Saves path to source folder
            replicaFolderPath = args[1]; //Saves path to replica folder
            syncInterval = int.Parse(args[2]) * 1000; // Saves and converts the interval to milliseconds
            logFilePath = args[3]; //Saves path to log file

            // Start synchronization
            Task.Run(() => StartSynchronization());
            
            Console.WriteLine("Press 'q' to quit.");
            while (Console.ReadKey().Key != ConsoleKey.Q) {}
        }

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: None.
        //OUTPUT: None.
        //EXPLANATION: Task that synchronizes the folders, Logs the completion of the task or an error and waits for a specified interval to repeat.
        //---------------------------------------------------------------------------------------------------------------------
        static async Task StartSynchronization()
        {
            while (true)
            {
                try
                {
                    SynchronizeFolders();
                    Log("Synchronization completed.");//Logs that the Synchronization was complete
                }
                catch (Exception ex)
                {
                    Log($"Error during synchronization: {ex.Message}");//Logs that an error happened
                }

                await Task.Delay(syncInterval);//Waits the respective Interval for the next synchronization
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: None.
        //OUTPUT: None.
        //EXPLANATION: Checks the existence of the Replica Folder and creates it in case it doesn't, and starts calls the
        //             functions to match the folders.
        //---------------------------------------------------------------------------------------------------------------------
        static void SynchronizeFolders()
        {
            // Ensure replica directory exists
            if (!Directory.Exists(replicaFolderPath))//Checks if the replica folder exists
            {
                Directory.CreateDirectory(replicaFolderPath);//If it doesn't exist then creats it
                Log($"Created replica directory: {replicaFolderPath}");//Logs the creation of the folder
            }

            SyncFiles();//Synchronizes file from source to replica

            SyncDir();//Synchronizes empty directories to replica

            RemoveFiles();//Deletes extra files from replica

            RemoveDir();//Deletes extra directories from replica

        }

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: None.
        //OUTPUT: None.
        //EXPLANATION: Copies/Updates all the files from the Source Folder into the Replica Folder.
        //---------------------------------------------------------------------------------------------------------------------
        static void SyncFiles()
        {
            // Synchronize files from source to replica
            var sourceFiles = Directory.GetFiles(sourceFolderPath, "*", SearchOption.AllDirectories);//Gets all the files from Source Folder
            foreach (var sourceFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceFolderPath, sourceFile);//Gets the path to get from the Source Folder to the file
                var replicaFile = Path.Combine(replicaFolderPath, relativePath);//Generates a new path combining the path to the replica folder with the relative path


                if(replicaFolderPath!=Path.GetDirectoryName(replicaFile)){//If the directory is not the same as the Replica itself
                    Directory.CreateDirectory(Path.GetDirectoryName(replicaFile));// Creates sub-directory in replica
                    Log($"Created/Updated Folder: {Path.GetDirectoryName(replicaFile)}");//Logs the creation of the Sub-Directory
                }
                

                if (!File.Exists(replicaFile) || !FilesAreEqual(sourceFile, replicaFile))//Checks if the file doesn't exist or if the existing file in replica folder is diferent then the one in the source folder
                {
                    File.Copy(sourceFile, replicaFile, true);//If one of those conditions is true then copies the file to the replica folder using the combined path
                    Log($"Copied/Updated file: {relativePath}");//Logs the copy of the file
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: None.
        //OUTPUT: None.
        //EXPLANATION: Creates a possible empty directory in the Replica Foldar that exists in the Source Folder.
        //---------------------------------------------------------------------------------------------------------------------
        static void SyncDir()
        {

            var sourceDirecs = Directory.GetDirectories(sourceFolderPath, "*", SearchOption.AllDirectories);//Gets all directories from the Source Folder
            foreach (var sourceDir in sourceDirecs)
            {
                var relativePath = Path.GetRelativePath(sourceFolderPath, sourceDir);//Gets the path to get from the Source Folder to the directory
                var replicaDir = Path.Combine(replicaFolderPath, relativePath);//Generates a new path combining the path to the replica folder with the relative path

                if(!Directory.Exists(replicaDir))//Checks if the directory already exists in the Replica Folder
                {
                    Directory.CreateDirectory(replicaDir);//Creates Directory in the Replica Folder
                    Log($"Created: {replicaDir}");//Logs the creation of the Directory
                }
                
            }
        }
        

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: None.
        //OUTPUT: None.
        //EXPLANATION: Deletes all the extra files that are located in the Replica Folder and are not in the Source Folder.
        //---------------------------------------------------------------------------------------------------------------------
        static void RemoveFiles()
        {
            // Remove extra files in replica
            var replicaFiles = Directory.GetFiles(replicaFolderPath, "*", SearchOption.AllDirectories);//Gets all files from replica folder
            foreach (var replicaFile in replicaFiles)
            {
                var relativePath = Path.GetRelativePath(replicaFolderPath, replicaFile);//Gets the path to get from the Replica Folder to the file
                var sourceFile = Path.Combine(sourceFolderPath, relativePath);//Generates a new path combining the path to the source folder with the relative path

                if (!File.Exists(sourceFile))//Checks if the file exists in the source folder
                {
                    File.Delete(replicaFile);//If it doesn't exist then deletes the file
                    Log($"Deleted file: {relativePath}");//Logs the deletion of the file 
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------
        //INPUT: None.
        //OUTPUT: None.
        //EXPLANATION: Deletes all the extra directories that are located in the Replica Folder and are not in the Source Folder.
        //-------------------------------------------------------------------------------------------------------------------------
        static void RemoveDir()
        {
            // Remove extra directories in replica
            var replicaDirs = Directory.GetDirectories(replicaFolderPath, "*", SearchOption.AllDirectories);//Gets all directories from replica folder
            foreach (var replicaDir in replicaDirs)
            {
                var relativePath = Path.GetRelativePath(replicaFolderPath, replicaDir);//Gets the path to get from the Replica Folder to the directory
                var sourceDir = Path.Combine(sourceFolderPath, relativePath);//Generates a new path combining the path to the source folder with the relative path

                if (!Directory.Exists(sourceDir))//Checks if the directory exists in the Source Folder
                {
                    Directory.Delete(replicaDir, true);//If it doesn't then deletes the directory
                    Log($"Deleted directory: {relativePath}");//Logs the deletion of the directory
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: Two strings containing the file paths of the files to be compared.
        //OUTPUT: Boolean. True if the files are equal and False if not.
        //EXPLANATION: This function compares the hashes from two files in order to determine if they match. To compare these
        //             hashes, it's used the MD5 Hashing Algorithm.
        //---------------------------------------------------------------------------------------------------------------------
        static bool FilesAreEqual(string file1, string file2)//Check if the files are equal
        {
            using (var hashAlgorithm = MD5.Create())//Creates an MD5 hashing algorithm in order to compare the content of each file
            {
                using (var stream1 = File.OpenRead(file1))//Opens the first file for reading
                using (var stream2 = File.OpenRead(file2))//Opens the second file for reading
                {
                    var hash1 = hashAlgorithm.ComputeHash(stream1);//Creates the hash for file 1
                    var hash2 = hashAlgorithm.ComputeHash(stream2);//Creates the hash for file 2
                    return BitConverter.ToString(hash1) == BitConverter.ToString(hash2);//Returns True if hash are equal, False if not. In case of true then the files are equal
                }
            }
        }

        //---------------------------------------------------------------------------------------------------------------------
        //INPUT: String cointaing the message to be logged.
        //OUTPUT: None.
        //EXPLANATION: Logs the message and the timestamp from when it's generated into the console and a file containing all
        //             the logs throughout the syncronization.
        //---------------------------------------------------------------------------------------------------------------------
        static void Log(string message)
        {
            var logMessage = $"{DateTime.Now}: {message}";//Creates the message to be written on the file. This message has two contents the timestamp and the info of the message
            Console.WriteLine(logMessage);//Writes in the console the message
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);//Writes on the file the message
        }

    }
}