using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace ChangeFileEncoding
{
    internal static class Program
    {
        private static DirectoryInfo __WorkDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        private static Encoding __Encoding = Encoding.UTF8;
        private static string[] __FileMasks = { "*.txt", "*.cs", "*.xml", "*.xaml", "*.htm", "*.html", "*.c", "*.cpp", "*.h", "*.js", "*.asm", };
        private static bool __UseSubDirectories = true;
        private static bool __StopAtEnd = true;
        private static bool __TestOnly;
        private static bool __Logging;
        private static string __LogFileName = "change-encoding.log";
        private static int[] __SuppressEncodingCodes = null;

        public static void Main(string[] args)
        {
            CheckCommandLineArguments(args);

            if (__Logging && File.Exists(__LogFileName))
                File.Delete(__LogFileName);

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

            if (__StopAtEnd)
            {
                Console.WriteLine("Processing completed. Press Enter to exit.");
                Console.ReadLine();
            }
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
                    case "--tool":
                    case "-f":
                        __StopAtEnd = false;
                        break;
                    case "--test":
                    case "--test-only":
                    case "-t":
                        __TestOnly = true;
                        break;
                    case "-l":
                    case "--log":
                        __Logging = true;
                        if (i + 1 < args.Count && !args[i + 1].StartsWith("-"))
                        {
                            var name = args[i + 1];
                            if (name.Contains('/') || name.Contains('\\') && !name.EndsWith(".log")) break;
                            if (Directory.Exists(name)) break;
                            if (File.Exists(name) && !name.EndsWith(".log")) break;
                            __LogFileName = name;
                        }
                        break;
                    case "-s" when i + 1 < args.Count:
                    case "--suppress" when i + 1 < args.Count:
                    case "--suppress" when i + 1 < args.Count:
                    case "--suppress-encodings" when i + 1 < args.Count:
                        __SuppressEncodingCodes = args[i + 1].Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(int.Parse)
                           .ToArray();
                        break;
                }
        }

        private static void ProcessDirectory(DirectoryInfo Dir, IEnumerable<string> FileMasks, bool UseSubDirectories)
        {
            var title = $"Processing files in {Dir.FullName.TrimInternal(95)}";
            Console.Title = title;
            Console.WriteLine(title);
            Log(title);
            Console.WriteLine("{0,20}{1,6}{2,9} {3}", "Encoding", "Code", "Length", "File");
            Log($"{"Encoding",20}{"Code",6}{"Length",9}  Relative file path");

            var files = Dir.EnumerateFiles(FileMasks, UseSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
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

            Console.WriteLine("Processing of {0} completed.", Dir.FullName.TrimInternal(95));
            Console.WriteLine("\tTotal files {0}.", total_files);
            if (__Logging)
            {
                Log($"Processing of {Dir.FullName} completed.");
                Log($"\tTotal files {total_files}.");
            }

            if (processed_count == 0 && error_io == 0 && error_access == 0)
            {
                Console.WriteLine("\tNo files changed.");
                Log("\tNo files changed.");
                return;
            }

            Console.WriteLine("\tTime {0:0.##}c", timer.Elapsed.TotalSeconds);
            if (__Logging)
                File.AppendAllText(__LogFileName, $"\tTime {timer.Elapsed.TotalSeconds:0.##}c");
            if (processed_count > 0)
            {
                Console.WriteLine(__TestOnly ? "\tFiles to convert {0}({1:p2})" : "\tConverted files {0}({1:p2})", processed_count, (double)processed_count / total_files);
                Log(string.Format(__TestOnly ? "\tFiles to convert {0}({1:p2})" : "\tConverted files {0}({1:p2})", processed_count, (double)processed_count / total_files));
            }

            if (error_io > 0)
            {
                Console.WriteLine("\tFiles with IO errors count {0}({1:p2})", error_io, (double)error_io / total_files);
                Log($"\tFiles with IO errors count {error_io}({(double)error_io / total_files:p2})");
            }

            if (error_access > 0)
            {
                Console.WriteLine("\tFiles with access errors count {0}({1:p2})", error_access, (double)error_access / total_files);
                Log($"\tFiles with access errors count {error_access}({(double)error_access / total_files:p2})");
            }
        }

        private static bool ProcessFile(FileInfo DataFile, string BaseDirectory = null)
        {
            var relative_file_path = DataFile.FullName;
            if (BaseDirectory != null && relative_file_path.StartsWith(BaseDirectory, StringComparison.OrdinalIgnoreCase))
                relative_file_path = relative_file_path.Substring(BaseDirectory.Length);

            var current_file_encoding = DataFile.GetEncoding();
            if (current_file_encoding?.Equals(__Encoding) == true)
                return false;

            var suppress = __SuppressEncodingCodes != null && current_file_encoding != null && __SuppressEncodingCodes.Contains(current_file_encoding.CodePage);

            var (unit, length, _) = DataFile.GetDataLength();

            Log(string.Format("{0, 20}({4,5})[{2,4:0.###}{3,3}] {1} {5}",
                current_file_encoding?.EncodingName ?? "???",
                relative_file_path,
                length,
                unit,
                current_file_encoding?.CodePage,
                suppress ? " - suppressed" : " - processed"));

            Console.Write("{0, 20}({4,5})[{2,4:0.###}{3,3}] {1}",
                current_file_encoding?.EncodingName ?? "???",
                relative_file_path.TrimInternal(__TestOnly ? 83 : 60),
                length,
                unit,
                current_file_encoding?.CodePage);

            if (!__TestOnly)
            {
                if (suppress)
                    Console.WriteLine(" - suppressed");
                else
                    ChangeEncoding(DataFile, current_file_encoding, __Encoding);
            }
            else
            {
                if (suppress)
                    Console.WriteLine(" - suppressed");
                else
                    Console.WriteLine();
            }

            return true;
        }

        private static void ChangeEncoding(FileInfo DataFile, Encoding CurrentEncoding, Encoding NewEncoding)
        {
            var cursor_initial_pos = Console.CursorLeft;

            var tmp_file = new FileInfo(Path.ChangeExtension(DataFile.FullName, ".tmp"));
            File.Move(DataFile.FullName, tmp_file.FullName, true);
            var lines_count = 0;
            using (var tmp = new StreamReader(tmp_file.OpenRead(), CurrentEncoding ?? Encoding.Default))
            {
                using var data = new StreamWriter(DataFile.Create(), NewEncoding);
                var data_length = (double)tmp.BaseStream.Length;
                var readed = 0L;
                while (!tmp.EndOfStream)
                {
                    var line = tmp.ReadLine();
                    if (line is null)
                        break;
                    lines_count++;
                    readed += line.Length + 2;
                    if (readed < data_length)
                        data.WriteLine(line);
                    else
                        data.Write(line);

                    var completed = Math.Min(readed, data_length) / data_length;

                    Console.CursorLeft = cursor_initial_pos;
                    Console.Write(" - {0:p2}", completed);
                }
            }

            tmp_file.Delete();

            Console.CursorLeft = cursor_initial_pos;
            Console.WriteLine(" - processed {0} lines", lines_count);
        }

        private static void Log(string Str)
        {
            if (!__Logging) return;
            var new_line = Environment.NewLine;
            File.AppendAllText(__LogFileName, Str.EndsWith(new_line) ? Str : Str + new_line);
        }
    }
}
