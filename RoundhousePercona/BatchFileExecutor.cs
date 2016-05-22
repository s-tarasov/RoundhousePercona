using System;
using System.Diagnostics;
using System.IO;

namespace RoundhousePercona
{
    internal static class BatchFileExecutor
    {
        public static int execute_bat_file(string filePath, string arguments, Action<string> onOutput, Action<string> onError)
        {
            var fullPath = Path.GetFullPath(filePath);
            if (!File.Exists(fullPath)) {
                throw new FileNotFoundException("File not found:"+ filePath, fullPath);
            }

            var process_info = new ProcessStartInfo("cmd.exe", "/c \"\"" + fullPath + "\" " + arguments + "\"");
            process_info.WorkingDirectory = Path.GetDirectoryName(fullPath);
            process_info.CreateNoWindow = true;
            process_info.UseShellExecute = false;
            process_info.RedirectStandardError = true;
            process_info.RedirectStandardOutput = true;

            using (var process = Process.Start(process_info)) {
                process.OutputDataReceived += (s, e) => { if (e.Data != null) onOutput(e.Data); };
                process.BeginOutputReadLine();
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) onError(e.Data); };
                process.BeginErrorReadLine();
                process.WaitForExit();

                return process.ExitCode;
            } 
        }
    }
}
