using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;

public class ColorMe : MonoBehaviourEx {
    // Use this for initialization
    private void Start() {
        if (!GetComponent<UniqueIdentifier>().IsDeserializing) {
            StartCoroutine("DoColorMe");
        }
    }

    private IEnumerator DoColorMe() {
        Renderer renderer = GetComponent<Renderer>();
        Light light = GetComponent<Light>();

        Color color = renderer.material.color;
        Color target = Color.blue;
        while (true) {
            var t = 0.0f;
            while (t < 1.0f) {

                renderer.material.color = Color.Lerp(color, target, t);
                if (light)
                    light.color = renderer.material.color;
                t += Time.deltaTime / 3.0f;
                yield return null;
            }
            t = 0.0f;
            while (t < 1.0f) {
                renderer.material.color = Color.Lerp(target, color, t);
                if (light)
                    light.color = renderer.material.color;
                t += Time.deltaTime / 3.0f;
                yield return null;
            }
        }
    }
}
