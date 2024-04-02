using Avalonia.Media.Imaging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace FileExplorer
{
    public class FileItem(string name, string path, Bitmap icon) : ReactiveObject
    {
        private string name = name;
        private string path = path;
        private Bitmap icon = icon;

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }

        public string Path
        {
            get => path;
            set => this.RaiseAndSetIfChanged(ref path, value);
        }

        public Bitmap Icon
        {
            get => icon;
            set => this.RaiseAndSetIfChanged(ref icon, value);
        }
    }

    public class FileExplorer : ReactiveObject
    {
        public ObservableCollection<FileItem> Files { get; } = [];
        private readonly FileSystemWatcher watcher = new();
        private readonly List<string> drives = [];
        private string currentPath = "";

        public Bitmap fileIcon = new(@"..\..\..\Assets\file.png");
        public Bitmap folderIcon = new(@"..\..\..\Assets\folder.png");
        public Bitmap driveIcon = new(@"..\..\..\Assets\drive.png");
        private Bitmap? image = null;

        public Bitmap? Image
        {
            get => image;
            set => this.RaiseAndSetIfChanged(ref image, value);
        }

        private void Clear()
        {
            if (Image != null)
            {
                Image.Dispose();
                Image = null;
            }
            Files.Clear();
        }

        private async void Fill()
        {
            Clear();
            Files.Add(new FileItem("..", "..", folderIcon));

            foreach (string dir in await Task.Run(() => Directory.GetDirectories(currentPath)))
            {
                Files.Add(new FileItem(dir[(dir.LastIndexOf('\\') + 1)..], dir, folderIcon));
            }    

            foreach (string file in await Task.Run(() => Directory.GetFiles(currentPath)))
            {
                Files.Add(new FileItem(file[(file.LastIndexOf('\\') + 1)..], file, fileIcon));
            }

            watcher.Path = currentPath;
            if (watcher.EnableRaisingEvents == false)
                watcher.EnableRaisingEvents = true;
        }

        public async void Navigate(string file)
        {
            if (file == "..")
            {
                if (drives.Contains(currentPath[..^1]))
                {
                    Clear();
                    foreach (string drive in drives)
                    {
                        Files.Add(new FileItem(drive, drive, driveIcon));
                    }
                    return;
                }

                string prevPath = currentPath[..currentPath.LastIndexOf('\\')];
                currentPath = drives.Contains(prevPath) ? prevPath + '\\' : prevPath;
                Fill();
                return;
            }

            if (drives.Contains(file))
            {
                currentPath = file + '\\';
                Fill();
                return;
            }

            string newPath = currentPath + '\\' + file;
            if (Directory.Exists(newPath))
            {
                try
                {
                    await Task.Run(() => Directory.GetDirectories(newPath));
                    await Task.Run(() => Directory.GetFiles(newPath));
                }

                catch (UnauthorizedAccessException)
                {
                    return;
                }

                currentPath += drives.Contains(currentPath[..^1]) ? file : '\\' + file;
                Fill();
                return;
            }

            try
            {
                Image = new(currentPath + '\\' + file);
            }

            catch (Exception)
            {
                return;
            }
        }

        public FileExplorer()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                string name = drive.Name[..^1];
                Files.Add(new FileItem(name, name, driveIcon));
                drives.Add(name);
            }

            watcher.Changed += (source, e) => Avalonia.Threading.Dispatcher.UIThread.Post(Fill);
            watcher.Created += (source, e) => Avalonia.Threading.Dispatcher.UIThread.Post(Fill);
            watcher.Deleted += (source, e) => Avalonia.Threading.Dispatcher.UIThread.Post(Fill);
            watcher.Renamed += (source, e) => Avalonia.Threading.Dispatcher.UIThread.Post(Fill);
        }
    }
}