using NetLib.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using XeytanCSharpServer.Models;
using C = System.Console;
using FileInfo = NetLib.Models.FileInfo;

namespace XeytanCSharpServer.Ui.Console.Views
{
    class FileSystemView : IClientView
    {
        public string CurrentBasePath { get; set; }
        private string BannerFormat { get; } = "XeytanCSharp/{0}/{1}>$ ";

        static readonly List<string> PathParts = new List<string>();
        public Client Client { get; set; }

        public void PrintBanner()
        {
            Debug.Assert(Client != null);
            C.Write(BannerFormat, Client.Id, CurrentBasePath ?? "");
        }

        public string Loop()
        {
            while (true)
            {
                PrintBanner();
                string instruction = System.Console.ReadLine();
                if (instruction == null) continue;
                if (!ProcessInstruction(instruction))
                    return instruction;
            }
        }

        private bool ProcessInstruction(string instruction)
        {
            string[] parts = instruction.Split();
            if (parts.Length > 1)
            {
                if (parts[0].Equals("cd", StringComparison.OrdinalIgnoreCase))
                {
                    string path = parts[1];
                    ChangeDirectory(path);
                    return true;
                }
            }

            return false;
        }

        public void SetActiveClient(Client client)
        {
            Client = client;
        }


        public static void PrintRoots(Client client, List<DiskDriveInfo> drives)
        {
            C.WriteLine("File System roots for {0}", client.PcName);
            foreach (DiskDriveInfo driveInfo in drives)
            {
                C.WriteLine($"\tName: {driveInfo.Name}");
                C.WriteLine($"\t\tLabel: {driveInfo.Label}");
                C.WriteLine($"\t\tDrive Format: {driveInfo.DriveFormat}");
            }
        }

        public static void PrintFsDirEntries(Client client, string basePath, List<FileInfo> dirEntries)
        {
            C.WriteLine("Directory entries for {0}", client.PcName);
            C.WriteLine("Base path {0}", basePath);
            foreach (FileInfo dirEntry in dirEntries)
            {
                string name = $"Name: {Path.GetFileName(dirEntry.FilePath)}";
                if (dirEntry.FileAttributes.HasFlag(FileAttributes.Directory))
                    C.WriteLine(name + "\tType: Directory");
                else
                    C.WriteLine(name + $"\tSize: {dirEntry.FileSize}\nType: File");
            }
        }

        public void ChangeDirectory(string path)
        {
            if (CurrentBasePath == null)
                CurrentBasePath = NormalizePath(path);
            else
                CurrentBasePath = NormalizePath(CurrentBasePath + path);
        }


        public static string NormalizePath(string path)
        {
            string replaced = path.Replace("\\", "/").Replace("//", "/");
            string[] parts = replaced.Split('/');


            PathParts.Clear();
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Equals(".."))
                {
                    if (PathParts.Count == 1 && Path.IsPathRooted(path))
                    {
                        // Skip
                    }
                    else
                    {
                        PathParts.RemoveAt(i - 1);
                    }
                }

                else if (!part.Equals("."))
                    PathParts.Add(part);
            }

            string result = string.Join("/", PathParts);

            if (!result.EndsWith("/"))
                result += "/";
            return result;
        }
    }
}