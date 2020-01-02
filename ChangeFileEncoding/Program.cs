using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChangeFileEncoding
{
    internal static class Program
    {
        private static DirectoryInfo __WorkDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        private static Encoding __Encoding = Encoding.UTF8;
        private static string[] __FileMasks = { "*.txt", "*.cs", "*.xml", "*.xaml", "*.htm", "*.html", "*.c", "*.cpp", "*.h", "*.js", "*.asm", };
        private static bool __UseSubDirectories = true;

        public static void Main(string[] args)
        {
            var file = new FileInfo(@"C:\Users\shmac\src\lib\Math.Core\MathCore\MathCore\Extensions"
                                    + @"\BindingListExtensions.cs");
            var length = file.GetDataLength();

            CheckCommandLineArguments(args);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            string prev_arg = null;
            var processed = false;
            foreach (var path in args)
            {
                if (prev_arg == "-p" || prev_arg == "--path")
                {
                    prev_arg = path;
                    continue;
                }

                if (File.Exists(path))
                {
                    ProcessFile(new FileInfo(path));
                    processed = true;
                }
                else if (Directory.Exists(path))
                {
                    ProcessDirectory(new DirectoryInfo(path), __FileMasks, __UseSubDirectories);
                    processed = true;
                }
                prev_arg = path;
            }

            if (!processed)
                ProcessDirectory(__WorkDirectory, __FileMasks, __UseSubDirectories);

            Console.Read();
        }

        private static void CheckCommandLineArguments(IReadOnlyList<string> args)
        {
            if (args is null || args.Count == 0) return;

            for (var i = 0; i < args.Count; i++)
                switch (args[i])
                {
                    case "-p" when i + 1 < args.Count:
                    case "--path" when i + 1 < args.Count:
                        __WorkDirectory = new DirectoryInfo(args[i + 1]);
                        break;

                    case "-e" when i + 1 < args.Count:
                    case "--encoding" when i + 1 < args.Count:
                        __Encoding = Encoding.GetEncoding(args[i + 1]);
                        break;

                    case "-f" when i + 1 < args.Count:
                    case "--files" when i + 1 < args.Count:
                        __FileMasks = args[i + 1].Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                        break;
                    case "-n":
                    case "--no-sub-dir":
                    case "--no-sub-dirs":
                        __UseSubDirectories = false;
                        break;
                    case "-s" when i + 1 < args.Count:
                    case "--sub-dir" when i + 1 < args.Count:
                    case "--sub-dirs" when i + 1 < args.Count:
                        if (args[i + 1].Equals("true", StringComparison.OrdinalIgnoreCase))
                            __UseSubDirectories = true;
                        else if (args[i + 1].Equals("false", StringComparison.OrdinalIgnoreCase))
                            __UseSubDirectories = false;
                        break;
                }
        }

        private static void ProcessDirectory(DirectoryInfo Dir, IEnumerable<string> FileMasks, bool UseSubDirectories)
        {
            var title = $"Processing files in {Dir}";
            Console.Title = title;
            Console.WriteLine(title);

            var files = FileMasks
               .SelectMany(mask => Dir.EnumerateFiles(mask, UseSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
               .Where(f => f.Length > 0);

            var timer = new Stopwatch();
            timer.Start();
            var directory_path = Dir.FullName;
            var total_files = 0;
            var processed_count = 0;
            var error_io = 0;
            var error_access = 0;
            foreach (var file in files)
                try
                {
                    total_files++;
                    if (ProcessFile(file, directory_path))
                        processed_count++;
                }
                catch (IOException)
                {
                    Console.WriteLine(" - error:I/O");
                    error_io++;
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine(" - error:access rights");
                    error_access++;
                }
            timer.Stop();

            Console.WriteLine("Processing of {0} completed.", Dir);
            Console.WriteLine("\tTotal files {0}.", total_files);
            if (processed_count == 0 && error_io == 0 && error_access == 0)
            {
                Console.WriteLine("\tNo files changed.");
                return;
            }

            Console.WriteLine("\tTime {0:0.##}c", timer.Elapsed.TotalSeconds);
            if (processed_count > 0)
                Console.WriteLine("\tConverted files {0}({1:p2})", processed_count, (double)processed_count / total_files);
            if (error_io > 0)
                Console.WriteLine("\tFiles with IO errors count {0}({1:p2})", error_io, (double)error_io / total_files);
            if (error_access > 0)
                Console.WriteLine("\tFiles with access errors count {0}({1:p2})", error_access, (double)error_access / total_files);
        }

        private static bool ProcessFile(FileInfo File, string BaseDirectory = null)
        {
            var relative_file_path = File.FullName;
            if (BaseDirectory != null && relative_file_path.StartsWith(BaseDirectory, StringComparison.OrdinalIgnoreCase))
                relative_file_path = relative_file_path.Substring(BaseDirectory.Length);

            var current_file_encoding = File.GetEncoding();
            if (current_file_encoding?.Equals(__Encoding) == true)
                return false;

            var (unit, length, _) = File.GetDataLength();
            Console.Write("{0, 20}({4,5})[{2,4:0.###}{3,3}] {1}",
                current_file_encoding?.EncodingName ?? "???",
                relative_file_path,
                length,
                unit,
                current_file_encoding?.CodePage);

            var cursor_initial_pos = Console.CursorLeft;

            var tmp_file = new FileInfo(Path.ChangeExtension(File.FullName, ".tmp"));
            System.IO.File.Move(File.FullName, tmp_file.FullName, true);
            var lines_count = 0;
            using (var tmp = new StreamReader(tmp_file.OpenRead(), current_file_encoding ?? Encoding.Default))
            {
                using var data = new StreamWriter(File.Create(), __Encoding);
                var data_length = (double)tmp.BaseStream.Length;
                var readed = 0L;
                while (!tmp.EndOfStream)
                {
                    var line = tmp.ReadLine();
                    if (line is null)
                        break;
                    lines_count++;
                    readed += line.Length + 2;
                    data.WriteLine(line);

                    var completed = Math.Min(readed, data_length) / data_length;

                    Console.CursorLeft = cursor_initial_pos;
                    Console.Write(" - {0:p2}", completed);
                }
            }

            tmp_file.Delete();

            Console.CursorLeft = cursor_initial_pos;
            Console.WriteLine(" - processed {0} lines", lines_count);
            return true;
        }
    }
}
