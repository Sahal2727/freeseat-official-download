using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

internal static class SingleFileLauncher
{
    private const string ResourceName = "FreeSeat.Payload.zip";

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr window);

    [STAThread]
    private static void Main()
    {
        try
        {
            string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string runtimeRootOverride = Environment.GetEnvironmentVariable("FREESEAT_RUNTIME_ROOT");
            string installRoot = String.IsNullOrWhiteSpace(runtimeRootOverride)
                ? Path.Combine(localData, "FreeSeat", "Runtime")
                : Path.GetFullPath(runtimeRootOverride);
            string packageHash = GetPackageHash();
            string installPath = Path.Combine(installRoot, "package-" + packageHash.Substring(0, 16));
            string applicationPath = Path.Combine(installPath, "FreeSeat.App.exe");

            if (!File.Exists(applicationPath))
                ExtractPayload(installRoot, installPath, packageHash);

            Process application = Process.Start(new ProcessStartInfo(applicationPath)
            {
                WorkingDirectory = installPath,
                UseShellExecute = false
            });

            if (application != null)
            {
                DateTime deadline = DateTime.UtcNow.AddSeconds(15);
                while (DateTime.UtcNow < deadline)
                {
                    application.Refresh();
                    IntPtr window = application.MainWindowHandle;
                    if (window != IntPtr.Zero)
                    {
                        ShowWindowAsync(window, 9); // SW_RESTORE
                        SetForegroundWindow(window);
                        break;
                    }
                    if (application.HasExited)
                        break;
                    System.Threading.Thread.Sleep(200);
                }
            }
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                "FreeSeat could not prepare or start the application.\r\n\r\n" + exception.Message,
                "FreeSeat",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static string GetPackageHash()
    {
        using (Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
        {
            if (payload == null)
                throw new InvalidOperationException("The application package is missing from this executable.");

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(payload);
                StringBuilder value = new StringBuilder(hash.Length * 2);
                foreach (byte item in hash)
                    value.Append(item.ToString("x2"));
                return value.ToString();
            }
        }
    }

    private static void ExtractPayload(string installRoot, string installPath, string packageHash)
    {
        Directory.CreateDirectory(installRoot);
        string stagingPath = installPath + ".staging-" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(stagingPath);

        try
        {
            using (Stream payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
            using (ZipArchive archive = new ZipArchive(payload, ZipArchiveMode.Read))
            {
                string stagingPrefix = Path.GetFullPath(stagingPath) + Path.DirectorySeparatorChar;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string relativePath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    string destination = Path.GetFullPath(Path.Combine(stagingPath, relativePath));
                    if (!destination.StartsWith(stagingPrefix, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("The application package contains an invalid path.");

                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    string directory = Path.GetDirectoryName(destination);
                    if (!String.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);

                    using (Stream input = entry.Open())
                    using (FileStream output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        input.CopyTo(output);
                }
            }

            File.WriteAllText(Path.Combine(stagingPath, ".freeseat-package"), packageHash, Encoding.ASCII);
            if (!File.Exists(Path.Combine(stagingPath, "FreeSeat.App.exe")))
                throw new InvalidDataException("The application package does not contain FreeSeat.App.exe.");

            if (Directory.Exists(installPath))
                Directory.Delete(installPath, true);
            Directory.Move(stagingPath, installPath);
        }
        catch
        {
            if (Directory.Exists(stagingPath))
                Directory.Delete(stagingPath, true);
            throw;
        }
    }
}
