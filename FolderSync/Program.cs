using System;
using System.Text;
using System.Security.Cryptography;

namespace FolderSync
{
    internal class Program
    {
        static string? sourcePath;
        static string? replicaPath;
        static string? logFilePath;
        static int syncInterval = -1;

        //Folder paths, synchronization interval and log file path should be provided using the command line arguments;
        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Make sure to add <Source Path> <Replica Path> <Log file Path> and <Sync Interval>");
                Console.WriteLine("Add Source Path");
                sourcePath = Console.ReadLine();
                Console.WriteLine("Copy to(Replica Path)");
                replicaPath = Console.ReadLine();
                Console.WriteLine("Log file(Path)");
                logFilePath = Console.ReadLine();
                do
                {
                    //Synchronization should be performed periodically;
                    Console.WriteLine("Add sync interval");
                    string? input = Console.ReadLine();
                    // Try to parse the input to an integer
                    if (input != null && int.TryParse(input, out syncInterval))
                        Console.WriteLine("Sync interval set to: " + syncInterval);
                    else
                        Console.WriteLine("Invalid input. Please enter a valid integer.");
                } while (syncInterval == -1);
            }
            else
            {
                sourcePath = args[0];
                replicaPath = args[1];
                logFilePath = args[2];
                syncInterval = int.Parse(args[3]);
            }         

            while (true)
            {
                SyncFolders(sourcePath, replicaPath);
                Thread.Sleep(syncInterval * 1000);
            }
        }
        /* Synchronization must be one-way: after the synchronization content of the
        replica folder should be modified to exactly match content of the source
        folder;*/
        static void SyncFolders(string source, string replica)
        {
            try
            {
                var sourceDirectory = new DirectoryInfo(source);
                var replicaDirectory = new DirectoryInfo(replica);

                //Check if Source directory exists, end if not
                if (!sourceDirectory.Exists)
                {
                    Log($"Source directory does not exist: {source}");
                    return;
                }
                //Check if replica directory exists, if not creates one
                if (!replicaDirectory.Exists)
                {
                    replicaDirectory.Create();
                    Log($"Created replica directory: {replica}");
                }

                // Copy files from source to replica
                foreach (var file in sourceDirectory.GetFiles())
                {
                    var targetFilePath = Path.Combine(replica, file.Name);
                    //Copy files if they do not exist
                    //Copy same name files if they are different
                    if (!File.Exists(targetFilePath) || !FilesAreEqual(file.FullName, targetFilePath))
                    {
                        file.CopyTo(targetFilePath, true);
                        Log($"Copied file: {file.FullName} to {targetFilePath}");
                    }
                }

                // Delete files in replica that don't match files in source
                foreach (var file in replicaDirectory.GetFiles())
                {
                    var sourceFilePath = Path.Combine(source, file.Name);
                    if (!File.Exists(sourceFilePath))
                    {
                        file.Delete();
                        Log($"Deleted file: {file.FullName}");
                    }
                }

                // Copy directories from source to replica
                foreach (var dir in sourceDirectory.GetDirectories())
                {
                    var targetDirPath = Path.Combine(replica, dir.Name);
                    SyncFolders(dir.FullName, targetDirPath);
                }

                // Delete directories in replica that do not exist in source
                foreach (var dir in replicaDirectory.GetDirectories())
                {
                    var sourceDirPath = Path.Combine(source, dir.Name);
                    if (!Directory.Exists(sourceDirPath))
                    {
                        dir.Delete(true);
                        Log($"Deleted directory: {dir.FullName}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }
        //Compute the MD5 hash of 2 files and then compare the resulting hash values.
        //If the MD5 hashes are identical, the files are considered identical; otherwise, they are different.
        static bool FilesAreEqual(string file1, string file2)
        {
            var file1Hash = MD5.HashData(File.ReadAllBytes(file1));
            var file2Hash = MD5.HashData(File.ReadAllBytes(file2));
            return Encoding.Default.GetString(file1Hash) == Encoding.Default.GetString(file2Hash);
        }
        //File creation/copying/removal operations should be logged to a file and to the console output;
        static void Log(string message)
        {
            Console.WriteLine(message);
            //Set log file's attribute to Normal removing any special attributes from the file
            File.SetAttributes(logFilePath, FileAttributes.Normal);
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}