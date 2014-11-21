#pragma strict

var prefab : GameObject;
var target : Transform;
var other : GameObject;

static var numberSpawned = 0;

var createMesh : Mesh;

public var testArrayItems = new String[10];

function Start () {

	
}

function Update () {
	if(LevelSerializer.IsDeserializing)
	   return;
	 if(Time.timeScale == 0)
	    return;
	if(Random.Range(0,100) < 2) {
		numberSpawned++;
		if(Random.value < 0.4f)
		{
			var direction = target.transform.forward * ((Random.value * 8) + 2);
			direction = direction + target.transform.up * 8;
			direction = direction + ( target.transform.right * ( - 4 + ((Random.value * 8))));
			if(prefab != null)
				Instantiate(prefab, direction, Quaternion.identity);
		}
		else
		{
			var direction2 = target.transform.forward * ((Random.value * 8) + 2);
			direction2 = direction2 + target.transform.up * 8;
			direction2 = direction2 + ( target.transform.right * ( - 4 + ((Random.value * 8))));

			var go = new GameObject();
			go.AddComponent(EmptyObjectIdentifier);
			var body = go.AddComponent(Rigidbody);
			body.rigidbody.angularVelocity = Vector3(Random.value * 20, Random.value * 20, Random.value * 20);
			go.AddComponent(SphereCollider);
			if(Random.value < 0.1)
			{
				var l = go.AddComponent(Light);
				l.light.type = LightType.Point;
				l.light.intensity = 2;
				l.light.color = Color(Random.value/2 + 0.5,Random.value/2 + 0.5,Random.value/2 + 0.5);
			}
			go.AddComponent("SaveAndReload");
			go.AddComponent("ColorMe");
			var mesh = go.AddComponent(MeshFilter);
			mesh.sharedMesh = createMesh;
			var renderer = go.AddComponent(MeshRenderer);
			renderer.material = new Material(Shader.Find("Diffuse"));
			go.transform.position = direction2;
		}
		
		
	}
	
	
}


function OnGUI()
{
	GUILayout.BeginArea(Rect(0,Screen.height-60,100,100));
	GUILayout.Label(numberSpawned.ToString());
	GUILayout.EndArea();
	
}