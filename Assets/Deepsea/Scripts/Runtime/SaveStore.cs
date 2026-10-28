using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Deepsea
{
    public sealed class SaveStore
    {
        readonly string path;
        public SaveStore(string filePath) { path = filePath; }
        public static SaveStore Default()
        { return new SaveStore(Path.Combine(Application.persistentDataPath, "guichao-save-v1.json")); }

        public HarbourProgress Load()
        {
            foreach (string candidate in new[] { path, path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    HarbourProgress data = JsonUtility.FromJson<HarbourProgress>(File.ReadAllText(candidate));
                    if (data == null || data.schema != 1) throw new InvalidDataException("Unsupported save schema");
                    data.Normalize();
                    return data;
                }
                catch (Exception e) { Debug.LogWarning("Could not read harbour save: " + e.Message); }
            }
            return new HarbourProgress();
        }

        public bool Save(HarbourProgress data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Copy(path, path + ".bak", true);
                    // Replace atomically on the desktop platforms supported by this sample.
                    File.Replace(temporary, path, null);
                }
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("Could not save harbour progress: " + e.Message);
                return false;
            }
        }
    }
}
