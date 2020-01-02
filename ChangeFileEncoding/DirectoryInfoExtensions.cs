using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChangeFileEncoding
{
    internal static class DirectoryInfoExtensions
    {
        public static IEnumerable<FileInfo> EnumerateFiles(this DirectoryInfo Directory, IEnumerable<string> SearchPatterns, SearchOption Options = SearchOption.TopDirectoryOnly) => 
            SearchPatterns.SelectMany(mask => Directory.EnumerateFiles(mask, Options));
    }
}
