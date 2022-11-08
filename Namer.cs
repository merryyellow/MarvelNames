using System;
using System.IO;

namespace MarvelNames
{
    public static class Namer
    {
        readonly static string[] names;
        readonly static Random random;
        static Namer()
        {
            random = new Random();
            var dllPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var dllDir = Path.GetDirectoryName(dllPath);
            var txtPath = Path.Combine(dllDir, "Names.txt");

            names = File.ReadAllLines(txtPath);
            // names are fetched from https://en.wikipedia.org/wiki/List_of_Ultimate_Marvel_characters
        }

        public static string GetAName() => names[random.Next(names.Length)];
    }
}
