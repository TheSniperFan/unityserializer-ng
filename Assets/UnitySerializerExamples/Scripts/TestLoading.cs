// /* ------------------
//       ${Name} 
//       (c)3Radical 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using UnityEngine.SceneManagement;

[AddComponentMenu("Storage/Tests/Test Loading")]
public class TestLoading : MonoBehaviour
{
	
	
	void Start ()
	{
		DontDestroyOnLoad(this);
		Invoke("LoadLevel",0.3f);
	}

	void LoadLevel()
	{
		
		SceneManager.LoadScene("Example");
	}
}


