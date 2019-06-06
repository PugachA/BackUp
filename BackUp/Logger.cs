using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace BackUp
{
    public class Logger : IService
    {
        private List<FileSystemWatcher> watchers;
        private readonly object obj;
        private bool enabled;
        private readonly string[] pathToWatch;
        private readonly string pathToLogFile;

        public Logger(string[] pathToWatch, string pathToLogFile)
        {

            this.pathToWatch = pathToWatch;
            this.pathToLogFile = pathToLogFile;
            enabled = true;
            obj = new object();
            watchers = new List<FileSystemWatcher>();

            foreach (string path in pathToWatch)
            {
                FileSystemWatcher watcher = new FileSystemWatcher(path);

                watcher.Deleted += Watcher_Deleted;
                watcher.Created += Watcher_Created;
                watcher.Changed += Watcher_Changed;
                watcher.Renamed += Watcher_Renamed;

                watchers.Add(watcher);
            }

        }

        public void Start()
        {
            foreach (FileSystemWatcher watcher in watchers)
            {
                watcher.EnableRaisingEvents = true;
                this.Write($"Запущен {typeof(Logger)} для отслеживания папки {watcher.Path}");
            }

            while (enabled)
            {
                Thread.Sleep(1000);
            }
        }

        public void Stop()
        {
            foreach (FileSystemWatcher watcher in watchers)
            {
                watcher.EnableRaisingEvents = false;
                this.Write($"Остановлен {typeof(Logger)} для отслеживания папки {watcher.Path}");
            }

            enabled = false;
        }

        // переименование файлов
        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            string fileEvent = "переименован в " + e.FullPath;
            string filePath = e.OldFullPath;
            RecordEntry(fileEvent, filePath);
        }

        // изменение файлов
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "изменен";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath);
        }

        // создание файлов
        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "создан";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath);
        }

        // удаление файлов
        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string fileEvent = "удален";
            string filePath = e.FullPath;
            RecordEntry(fileEvent, filePath);
        }

        private void RecordEntry(string fileEvent, string filePath)
        {
            lock (obj)
            {
                using (StreamWriter writer = new StreamWriter(GetLogFileName(), true))
                {
                    writer.WriteLine(String.Format("{0} файл {1} был {2}",
                        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), filePath, fileEvent));
                    writer.Flush();
                }
            }
        }

        public void Write(string str)
        {
            lock (obj)
            {
                using (StreamWriter writer = new StreamWriter(GetLogFileName(), true))
                {
                    writer.WriteLine(String.Format("{0} {1}",
                        DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), str));
                    writer.Flush();
                }
            }
        }

        private string GetLogFileName()
        {
            return Path.Combine(pathToLogFile, $"log_{DateTime.Now.ToString("dd/MM/yyyy")}.txt");
        }
    }
}
