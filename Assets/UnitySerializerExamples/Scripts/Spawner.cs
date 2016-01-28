using UnityEngine;

public class Spawner : MonoBehaviour {
    [SerializeField]
    private GameObject prefab;
    [SerializeField]
    private Transform target;
    [SerializeField]
    private GameObject other;
    [SerializeField]
    private Material material;

    [SerializeThis]
    private static int numberSpawned = 0;

    [SerializeField]
    private Mesh createMesh;


    private void Update() {
        if (LevelSerializer.IsDeserializing)
            return;
        if (Time.timeScale == 0.0f)
            return;
        if (Random.Range(0, 100) < 2) {
            numberSpawned++;
            if (Random.value < 0.4f) {
                Vector3 direction = target.transform.forward * ((Random.value * 8.0f) + 2.0f);
                direction = direction + target.transform.up * 8.0f;
                direction = direction + (target.transform.right * (-4.0f + ((Random.value * 8.0f))));
                if (prefab != null)
                    Instantiate(prefab, direction, Quaternion.identity);
            }
            else {
                Vector3 direction2 = target.transform.forward * ((Random.value * 8.0f) + 2.0f);
                direction2 = direction2 + target.transform.up * 8.0f;
                direction2 = direction2 + (target.transform.right * (-4.0f + ((Random.value * 8.0f))));

                GameObject go = new GameObject();
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.material = material;
                MeshFilter mesh = go.AddComponent<MeshFilter>();
                mesh.sharedMesh = createMesh;
                go.AddComponent<EmptyObjectIdentifier>();
                Rigidbody body = go.AddComponent<Rigidbody>();
                body.GetComponent<Rigidbody>().angularVelocity = new Vector3(Random.value * 20.0f, Random.value * 20.0f, Random.value * 20.0f);
                go.AddComponent<SphereCollider>();
                if (Random.value < 0.1f) {
                    Light l = go.AddComponent<Light>();
                    l.GetComponent<Light>().type = LightType.Point;
                    l.shadows = LightShadows.Hard;
                    l.GetComponent<Light>().intensity = 2.0f;
                    l.GetComponent<Light>().color = new Color(Random.value / 2.0f + 0.5f, Random.value / 2.0f + 0.5f, Random.value / 2.0f + 0.5f);
                }
                go.AddComponent<ColorMe>();
                go.transform.position = direction2;
            }
        }
    }

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(0.0f, 0.0f, 175.0f, 100.0f));
        GUILayout.Label(string.Format("Spawned {0} GameObjects", numberSpawned.ToString()));
        GUILayout.EndArea();
    }
}
