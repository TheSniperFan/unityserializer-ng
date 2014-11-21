using UnityEngine;
using System.Collections;

public class ClickMeAndDie : MonoBehaviour {
	
	int id;
	static int _id;
	
	void Start()
	{
		id = _id++;
	}
	
	// Update is called once per frame
	void Update () {
		if(Input.GetMouseButtonDown(0))
		{
			RaycastHit hit;
			if(collider.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 1000))
			{
				JSONLevelSerializer.SaveObjectTreeToServer("ftp://whydoidoit.net/testspider" + id.ToString() + ".json", gameObject,"testserializer", "T3sts3rializer", (e)=>{
					if(e==null)
					{
						Loom.QueueOnMainThread(()=>{
								JSONLevelSerializer.LoadObjectTreeFromServer("http://whydoidoit.net/testserializer/testspider" + id);
							},2f);
					}
					else
						Debug.Log(e.ToString());
				});
				Destroy(gameObject);
			}
		}
	}
}
