using UnityEngine;
using System.Collections;

public class SaveAndLoad : MonoBehaviour {
    [SerializeField]
    private int[] array;
    [SerializeField]
    private object arrayObject;
    [SerializeField]
    private byte[] data;
    [SerializeField]
    private GameObject theObject;

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.S)) {
            data = LevelSerializer.SerializeLevel(false, theObject.GetComponent<UniqueIdentifier>().Id);
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            LevelSerializer.LoadNow(data, false, false);
        }
    }
}
