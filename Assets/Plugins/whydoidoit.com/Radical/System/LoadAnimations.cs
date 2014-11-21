using UnityEngine;
using System.Collections;
using System.Linq;

[AddComponentMenu("System/Load Animations")]
public class LoadAnimations : MonoBehaviour {

	public new string name;
	
	void Awake()
	{
		var clips = Resources.LoadAll("Animations/" + name, typeof(AnimationClip)).Cast<AnimationClip>();
		foreach (var c in clips)
		{
			animation.AddClip(c, c.name.Contains("@") ? c.name.Substring(c.name.LastIndexOf("@") + 1) : c.name);
		}

		foreach (var a in animation.Cast<AnimationState>())
		{
			a.enabled = true;
		}
	}
	
}
