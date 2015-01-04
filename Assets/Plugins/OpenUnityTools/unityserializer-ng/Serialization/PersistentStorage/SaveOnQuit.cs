using UnityEngine;

namespace UnitySerializerNG.FilePreferences {
    public class SaveOnQuit : MonoBehaviour {
        public static int Instances {
            get;
            private set;
        }

        public SaveOnQuit() {
            Instances++;
        }

        private void OnApplicationQuit() {
            FilePrefs.Save();
        }
    }
}