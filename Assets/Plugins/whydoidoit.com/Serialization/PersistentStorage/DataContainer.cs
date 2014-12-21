using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using UnityEngine;

namespace UnitySerializerNG.FilePreferences {

    [Serializable]
    public class DataContainer<T> {
        private static GameObject QuitObject;

        private static string root = Path.GetFullPath(Application.persistentDataPath) + Path.DirectorySeparatorChar + "persistentData";
        private string path;

        private Dictionary<string, T> dict = new Dictionary<string, T>();

        public DataContainer(string filename) {
            path = root + Path.DirectorySeparatorChar + filename;

            if (File.Exists(path)) {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                try {
                    dict = (Dictionary<string, T>)formatter.Deserialize(stream);
                }
                catch (SerializationException e) {
                    Debug.LogException(e);
                    stream.Close();
                    RebuildFile();
                }
                finally {
                    stream.Close();
                }
            }
            else {
                RebuildFile();
            }

            QuitObject = new GameObject("FilePrefs_QuitObject", typeof(SaveOnQuit));
        }

        private void RebuildFile() {
            File.Delete(path);

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

            try {
                formatter.Serialize(stream, dict);
            }
            catch (SerializationException e) {
                Debug.LogException(e);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
            finally {
                stream.Close();
            }
        }

        public void Save() {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

            try {
                formatter.Serialize(stream, dict);
            }
            catch (SerializationException e) {
                Debug.LogException(e);
            }
            catch (Exception e) {
                Debug.LogException(e);
            }
            finally {
                stream.Close();
            }
        }

        public T Get(string key) {
            try {
                return dict[key];
            }
            catch (KeyNotFoundException) {
                return default(T);
            }
        }

        public void Set(string key, T value) {
            dict[key] = value;
        }

        public void Clear() {
            dict.Clear();
        }

        public bool Remove(string key) {
            return dict.Remove(key);
        }

        public bool Find(string key) {
            return dict.ContainsKey(key);
        }

        // Only for debugging purposes!
        //public int Count() {
        //    return dict.Count;
        //}

        //public void PrintAll() {
        //    string s = "";
        //    foreach (KeyValuePair<string, T> key in dict) {
        //        s += key.Value.ToString() + ", ";
        //    }

        //    Debug.Log(s);
        //}
    }
}