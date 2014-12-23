using UnityEngine;

namespace UnitySerializerNG.FilePreferences {
    public class SaveOnQuit : MonoBehaviour {
        private static int instances;

        private void Awake() {
            if (Application.isPlaying) {
                instances++;

                if (instances > 1)
                    DestroyObject(gameObject);
            }
        }

        private void OnApplicationQuit() {
            FilePrefs.Save();
        }
    }
}