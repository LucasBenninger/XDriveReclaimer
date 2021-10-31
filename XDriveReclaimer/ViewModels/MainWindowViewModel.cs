using System;
using System.Diagnostics;
using System.IO.Enumeration;
using System.Reactive;
using ReactiveUI;

namespace XDriveReclaimer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string usbDeviceList;

        public string UsbDeviceList
        {
            get => usbDeviceList;
            set => this.RaiseAndSetIfChanged(ref usbDeviceList, value);
        }

        private string driveName;

        public string DriveName
        {
            get => driveName;
            set => this.RaiseAndSetIfChanged(ref driveName, value);
        }

        public MainWindowViewModel()
        {
            GetDriveListCommand = ReactiveCommand.Create(GetDriveList);
            FixDriveCommand = ReactiveCommand.Create(FixDrive);
            MountDriveCommand = ReactiveCommand.Create(MountDrive);
        }

        public ReactiveCommand<Unit, Unit> MountDriveCommand { get; set; }

        public ReactiveCommand<Unit, Unit> FixDriveCommand { get; set; }
        public ReactiveCommand<Unit, Unit> GetDriveListCommand { get; set; }

        private void FixDrive()
        {
            UsbDeviceList = "";
            using (Process process = new Process())
            {
                string command = $"pkexec lilo -M {driveName} mbr";
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                process.Start();
                process.OutputDataReceived += HandleOutput;
                process.BeginOutputReadLine();
                process.WaitForExit();
            }
        }

        private void GetDriveList()
        {
            UsbDeviceList = "";
            using (Process process = new Process())
            {
                string command = "lsblk -o name,size,model,vendor";
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                process.Start();
                process.OutputDataReceived += HandleOutput;
                process.BeginOutputReadLine();
                process.WaitForExit();
            }
        }

        private void MountDrive()
        {
            UsbDeviceList = "";
            // Create Mount Folder
            string folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "XDriveReclaimer");
            Console.WriteLine(folder);
            if (!System.IO.Directory.Exists(folder))
            {
             try
             {
                 System.IO.Directory.CreateDirectory(folder);
             }
             catch (Exception e)
             {
                 UsbDeviceList = e.Message;
                Console.WriteLine(e.Message);
             }   
            }
            string Command =
                $"offset=`pkexec head -c 4k {driveName} | grep -aobuP '\\x00\\x00\\x00NTFS' | sed 's/\\:.*//'` && pkexec mount {driveName} -o offset=$offset {folder}";
            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{Command}\"",
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                process.Start();
                process.OutputDataReceived += HandleOutput;
                process.BeginOutputReadLine();
                process.WaitForExit();
                UsbDeviceList = $"Drive mounted to {folder}";
            }
        }

        private void HandleOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null && e.Data.StartsWith("loop"))
                return;
            Console.WriteLine(e.Data);
            UsbDeviceList = UsbDeviceList + "\n"+ e.Data;
        }
    }
}