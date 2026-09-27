using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace XB1ControllerBatteryIndicator
{
    internal static class AppLogger
    {
        private const long MaxLogBytes = 1024 * 1024;
        private static readonly object SyncRoot = new object();
        private static readonly string DirectoryPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "XB1ControllerBatteryIndicator");
        private static readonly string FilePath = Path.Combine(DirectoryPath, "log.txt");
        private static readonly string BackupPath = Path.Combine(DirectoryPath, "log.old.txt");

        public static string LogDirectory
        {
            get { return DirectoryPath; }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string context, Exception exception)
        {
            var details = exception == null ? string.Empty : exception.ToString();
            Write("ERROR", context + (details.Length > 0 ? Environment.NewLine + details : string.Empty));
        }

        public static void EnsureLogDirectory()
        {
            lock (SyncRoot)
            {
                Directory.CreateDirectory(DirectoryPath);
            }
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(DirectoryPath);
                    RotateIfNeeded();

                    var line = string.Format(
                        CultureInfo.InvariantCulture,
                        "[{0}] [{1}] {2}{3}",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                        level,
                        message,
                        Environment.NewLine);

                    File.AppendAllText(FilePath, line, Encoding.UTF8);
                }
            }
            catch
            {
                // Diagnostics must never affect the tray application itself.
            }
        }

        private static void RotateIfNeeded()
        {
            if (!File.Exists(FilePath))
                return;

            var info = new FileInfo(FilePath);
            if (info.Length < MaxLogBytes)
                return;

            if (File.Exists(BackupPath))
                File.Delete(BackupPath);

            File.Move(FilePath, BackupPath);
        }
    }
}
