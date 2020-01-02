using System;
using System.IO;

namespace ChangeFileEncoding
{
    internal static class FileInfoExtension
    {
        private static readonly string[] __DataLengths = { "B", "kB", "MB", "GB", "TB" };

        public static (string Unit, double Length, int Ratio) GetDataLength(this FileInfo File)
        {
            var length = File.Length;
            const double kb = 1024;
            var power = Math.Log(length, kb);
            var index = (int)Math.Truncate(power);
            if (power - index > 0.9) index++;
            var ratio = 1 << (index * 10);
            var data_length = (double)length / ratio;

            if (data_length >= 100) data_length = Math.Round(data_length);
            else if (data_length >= 10) data_length = Math.Round(data_length, 1);
            else data_length = Math.Round(data_length, 2);

            return (Unit: __DataLengths[Math.Min(index, __DataLengths.Length)], Length: data_length, Ratio: ratio);
        }
    }
}
