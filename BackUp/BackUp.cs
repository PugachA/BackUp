using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackUp
{
    public class BackUp : IService
    {
        private readonly Logger logger;
        private readonly string sourcePath;
        private readonly string destinationPath;
        private readonly int frequency;
        private int storageTime;
        private Timer timer;

        public BackUp(Logger logger, string sourcePath, string destinationPath, int frequency, int storageTime)
        {
            this.logger = logger;
            this.sourcePath = sourcePath;
            this.destinationPath = destinationPath;
            this.frequency = frequency;
            this.storageTime = storageTime;
        }

        public void Start()
        {
            logger.Write($"Запуск резерного копирования папки {sourcePath}.\r\n{Description()}");
            // устанавливаем метод обратного вызова
            TimerCallback tm = new TimerCallback(CheckFiles);
            // создаем таймер
            timer = new Timer(tm, null, 0, frequency);
        }

        public void Stop()
        {
            timer.Dispose();
            logger.Write($"Остановка резервного копирования папки {sourcePath}");
        }

        private void CheckFiles(object obj)
        {
            logger.Write($"Запускаем проверку папки {sourcePath}");

            try
            {
                if (!CheckBackUpProcess())
                {
                    logger.Write($"Создаем резервную копию папки {sourcePath}");
                    DirectoryCopy(sourcePath, GetDirectoryPath(this.destinationPath));
                }

                if (TryGetFilesForDelete(out List<string> dirForDelete))
                {

                    foreach (string path in dirForDelete)
                    {
                        logger.Write($"Удаление папки резервного копирования {path}");
                        Directory.Delete(path, true);
                    }
                }
            }
            catch(Exception ex)
            {
                logger.Write("ERROR " + ex.Message);
            }

        }

        private bool CheckBackUpProcess()
        {
            return Directory.Exists(GetDirectoryPath(this.destinationPath));
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }

        }

        private string GetDirectoryPath(string destination)
        {
            return Path.Combine(destination, $"BackUp_{DateTime.Now.ToString("dd/MM/yyyy")}");
        }

        private bool TryGetFilesForDelete(out List<string> dirsForDelete)
        {
            DirectoryInfo dir = new DirectoryInfo(destinationPath);

            DirectoryInfo[] dirs = dir.GetDirectories();

            dirsForDelete = new List<string>();

            foreach (DirectoryInfo d in dirs)
            {
                if ((DateTime.Now - d.CreationTime).Days >= storageTime)
                    dirsForDelete.Add(d.FullName);
            }

            return dirsForDelete.Any();
        }

        private string Description()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Настройки резервного копирования:");
            stringBuilder.AppendLine($"Папка для которой будут создваться резервные копии - {sourcePath}");
            stringBuilder.AppendLine($"Папка для хранений резервных копий - {destinationPath}");
            stringBuilder.AppendLine($"Частота проверки резервных копий - {frequency/60000} минут");
            stringBuilder.AppendLine($"Количество дней для хранения резервных копий - {storageTime}");

            return stringBuilder.ToString();
        }
    }
}
