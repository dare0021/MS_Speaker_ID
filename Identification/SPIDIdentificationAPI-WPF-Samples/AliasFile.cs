using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPIDIdentificationAPI_WPF_Samples
{
    static class AliasFile
    {
        private static string speakerAliasFilePath = "speakerAliases";

        private static Dictionary<Guid, string> ReadFile()
        {
            var outval = new Dictionary<Guid, string>();
            if (!File.Exists(speakerAliasFilePath))
            {
                var f = File.Create(speakerAliasFilePath);
                f.Close();
                return outval;
            }
            StreamReader speakerAliasFile = File.OpenText(speakerAliasFilePath);
            string s;
            while ((s = speakerAliasFile.ReadLine()) != null)
            {
                var kvp = s.Split('\t');
                outval[new Guid(kvp[0])] = kvp[1];
            }
            speakerAliasFile.Close();
            return outval;
        }

        private static void SaveFile(Dictionary<Guid, string> dict)
        {
            if (File.Exists(speakerAliasFilePath))
            {
                File.Delete(speakerAliasFilePath);
            }
            var f = File.Create(speakerAliasFilePath);
            StreamWriter speakerAliasFile = new StreamWriter(f);
            string filestr = "";
            foreach (var key in dict.Keys)
            {
                filestr += key.ToString() + "\t" + dict[key] + "\n";
            }
            filestr = filestr.Substring(0, filestr.Length - 1);
            speakerAliasFile.Write(filestr);
            speakerAliasFile.Close();
        }

        public static string RetrieveAlias(Guid id)
        {
            var dict = ReadFile();
            if (!dict.Keys.Contains(id))
                return null;
            return dict[id];
        }

        public static void AddAlias(Guid id, string name)
        {
            var dict = ReadFile();
            dict[id] = name;
            SaveFile(dict);
        }

        public static void RemoveAlias(Guid id)
        {
            var dict = ReadFile();
            dict.Remove(id);
            SaveFile(dict);
        }

        public static void DeleteFile()
        {
            if(File.Exists(speakerAliasFilePath))
            {
                File.Delete(speakerAliasFilePath);
            }
        }
    }
}
