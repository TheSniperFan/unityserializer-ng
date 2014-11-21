using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

public class WeakReference<T> where T : class
{
	WeakReference r;

	public T Target
	{
		get
		{
			return r.IsAlive ? (T)r.Target : null;
		}
		set
		{
			r = new WeakReference(value);
		}
	}
	
	public bool IsAlive
	{
		get
		{
			return r.IsAlive;
		}
	}
	
	public static implicit operator T(WeakReference<T> re)
	{
		return re.Target;
	}
	
	public static implicit operator WeakReference<T>(T value)
	{
		return new WeakReference<T>() { Target = value};
	}
	
}

[Serializable]
public class ObservedList<T> : List<T>
{
	public event Action<int> Changed = delegate {};

	public new T this[int index]
	{
		get
		{
			return base[index];
		}
		set
		{
			base[index] = value;
			Changed(index);
		}
	}
	
	
}




public class Lookup<TK, TR> : Dictionary<TK, TR> where TR : class
{
	public new virtual TR this[TK index]
	{
		get
		{
			if (ContainsKey(index))
			{
				return base[index];
			}
			return null;
			
		}
		set
		{
			base[index] = value;
		}
	}
	
	public T Get<T>(TK index) where T : class
	{
		return this[index] as T;
	}
	
	
	
}


public interface IChanged
{
	void Changed(object index);
}
public interface INeedParent
{
	void SetParent(IChanged parent, object index);
}
	
public class Index<TK, TR> : Lookup<TK,TR>, IChanged  where TR : class, new()
{

	public event Action<TK,TR, TR> Setting;
	public event Action<TK,TR> Getting = delegate {};
	
	public void Changed(object index)
	{
		if (Setting != null)
		{
			TR current = null;
			if (base.ContainsKey((TK)index))
			{
				current = base[(TK)index];
			}
			Setting((TK)index, current, current);
		}
	}
	
	public override TR this[TK index]
	{
		get
		{
			if (ContainsKey(index))
			{
				return base[index];
			}
			var ret = new TR();
			if (ret is INeedParent)
			{
				(ret as INeedParent).SetParent(this,index);
			}
			base[index] = ret;
			Getting(index, ret);
			return ret;
		}
		set
		{
			if (Setting != null)
			{
				TR current = null;
				if (base.ContainsKey(index))
				{
					current = base[index];
				}
				Setting(index, current, value);
			}
			base[index] = value;
		}
	}
}



public class GUIBackgroundColor : IDisposable
{
	Color old;

	public GUIBackgroundColor(Color color)
	{
		old = GUI.backgroundColor;
		GUI.backgroundColor = color;
	}

	public void Dispose()
	{
		GUI.backgroundColor = old;
	}
}



public class GUIArea : IDisposable
{
	private static int rotated;
	
	public class Rotated : IDisposable
	{
		public Rotated()
		{
			GUIArea.rotated++;
		}
		
		public void Dispose()
		{
			GUIArea.rotated--;
		}
	}
	
	public static Rect GetStandardArea()
	{
		return new Rect(10, 10, 940, 620);
	}
	
	public GUIArea()
		: this( null)
	{
	}
	public GUIArea(Rect? area)
	{
		var a = area ?? GUIArea.GetStandardArea();
		if (rotated > 0)
		{
			a.y += a.height;
			var w = a.width;
			a.width = a.height;
			a.height = w;
		}
		
		GUILayout.BeginArea(a);
		if (rotated > 0)
		{
			GUIUtility.RotateAroundPivot(-90f, Vector2.zero);
		}
		
	}
	
	public void Dispose()
	{
		GUILayout.EndArea();
	}
}


public class GUIScale : IDisposable
{
	static int count = 0;
	static Matrix4x4 cached;
	public GUIScale()
	{
		if(count++ == 0)
		{
			cached = GUI.matrix;
			if(Screen.width < 500)
				GUIUtility.ScaleAroundPivot(new Vector2(0.5f, 0.5f), Vector2.zero);
			if(Screen.width > 1100)
				GUIUtility.ScaleAroundPivot(new Vector2(2f, 2f), Vector2.zero);
		}
	}
	public void Dispose()
	{
		if(--count == 0)
		{
			GUI.matrix = cached;
		}
	}
}

public class Horizontal : IDisposable
{
	
	public Horizontal()
	{
		GUILayout.BeginHorizontal();
	}
	
	public Horizontal(GUIStyle style)
	{
		GUILayout.BeginHorizontal(style);
	}
	
	public Horizontal(params GUILayoutOption[] options)
	{
		GUILayout.BeginHorizontal(options);
	}
	
	public void Dispose()
	{
		GUILayout.EndHorizontal();
	}
}

public class Vertical : IDisposable
{
	public Vertical(params GUILayoutOption[] options)
	{
		GUILayout.BeginVertical(options);
	}
	
	public void Dispose()
	{
		GUILayout.EndVertical();
	}
}

public class ScrollView : IDisposable
{
	
	public ScrollView(ref Vector2 scroll)
	{
		
		scroll = GUILayout.BeginScrollView(scroll);
	}
	
	public virtual void Dispose()
	{
		GUILayout.EndScrollView();
	}
	
	
}

public class Box : IDisposable
{
	public Box(GUIStyle style)
	{
		GUILayout.BeginVertical(style);
	}
	public Box(params GUILayoutOption[] options)
	{
		GUILayout.BeginVertical("box", options);
	}
	
	public Box()
	{
		GUILayout.BeginVertical("box");
	}
	
	
	public virtual void Dispose()
	{
		GUILayout.EndVertical();
	}
}

public class HorizontalCentered : IDisposable
{
	public HorizontalCentered()
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
	}
	
	public HorizontalCentered(GUIStyle style)
	{
		GUILayout.BeginHorizontal(style);
		GUILayout.FlexibleSpace();
	}
	
	public void Dispose()
	{
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}
}

public class VerticalCentered : IDisposable
{
	public VerticalCentered()
	{
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
	}
	
	public VerticalCentered(params GUILayoutOption[] options)
	{
		GUILayout.BeginVertical(options);
		GUILayout.FlexibleSpace();
	}
	
	public void Dispose()
	{
		GUILayout.FlexibleSpace();
		GUILayout.EndVertical();
	}
}

public class RightAligned : IDisposable
{
	public RightAligned()
	{
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
	}
	
	public void Dispose()
	{
		GUILayout.EndHorizontal();
	}
}

public class LeftAligned : IDisposable
{
	public LeftAligned()
	{
		GUILayout.BeginHorizontal();
	}
	
	public void Dispose()
	{
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
	}
}

public class BottomAligned : IDisposable
{
	public BottomAligned()
	{
		GUILayout.BeginVertical();
		GUILayout.FlexibleSpace();
	}
	
	public void Dispose()
	{
		GUILayout.EndVertical();
	}
}

public class SceneIndex : MonoBehaviour
{
}


public static class Radical
{
	
		
	public static void ActivateChildren(this Component co)
	{
		co.gameObject.SetActiveRecursively(true);
	}

	
		public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector) where TSource : class
	{
		return source.MaxBy(selector, Comparer<TKey>.Default);
	}

	public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector, IComparer<TKey> comparer) where TSource : class
	{
		using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
		{
			if (!sourceIterator.MoveNext())
			{
				return null;
			}
			TSource max = sourceIterator.Current;
			TKey maxKey = selector(max);
			while (sourceIterator.MoveNext())
			{
				TSource candidate = sourceIterator.Current;
				TKey candidateProjected = selector(candidate);
				if (comparer.Compare(candidateProjected, maxKey) > 0)
				{
					max = candidate;
					maxKey = candidateProjected;
				}
			}
			return max;
		}
	}
	
	
	public static IEnumerable<TResult> Zip<T1, T2, TResult>(this IEnumerable<T1> seq1, IEnumerable<T2> seq2, Func<T1, T2, TResult> resultSelector)
	{
		var results = new List<TResult>();
		var enm1 = seq1.GetEnumerator();
		var enm2 = seq2.GetEnumerator();
		while (enm1.MoveNext() && enm2.MoveNext())
		{
			results.Add(resultSelector(enm1.Current, enm2.Current));
		}
		return results;
	}
	

		
	public static bool CalledFrom(string name)
	{
		var st = new StackTrace();
		foreach(var frame in st.GetFrames())
		{
			if(frame.GetMethod().Name.Contains(name))
				return true;
		}
		return false;
	}
	
	public static int MaskLayers( params int[] layers)
	{
		var result = 0;
		foreach(var i in layers)
		{
			result |= 1 << i;
		}
		return result;
	}
	
	public static int MaskLayers( params string[] layers)
	{
		var result = 0;
		foreach(var l in layers)
		{
			result |= 1 << LayerMask.NameToLayer(l);
		}
		return result;
	}
	
	
	public static void PlayOneShot(this GameObject gameObject, AudioClip clip)
	{
		if(clip==null)
			return;
		if(!gameObject.audio)
		{
			gameObject.AddComponent<AudioSource>();
			gameObject.audio.playOnAwake = false;
		}
		gameObject.audio.PlayOneShot(clip);
		
	
	}
	
	public static void PlayAudio(this GameObject gameObject, AudioClip clip)
	{
		if(clip==null)
			return;
		if(!gameObject.audio)
		{
			gameObject.AddComponent<AudioSource>();
			gameObject.audio.playOnAwake = false;
		}
		gameObject.audio.clip = clip;
		gameObject.audio.loop = true;
		gameObject.audio.Play();
	}
	
	public static void FadeVolume(this GameObject component, float toLevel = 1, float time = 1f, float? fromLevel = null)
	{
		component.gameObject.StartExtendedCoroutine(VolumeFader(component.audio, toLevel, time, fromLevel));
	}
	
	static IEnumerator VolumeFader(AudioSource source, float level, float time, float? fromLevel)
	{
		var currentVolume = fromLevel ?? source.volume;
		var t = 0f;
		while(t < 1)
		{
			t+= Time.deltaTime/time;
			source.volume = Mathf.Lerp(currentVolume, level, t);
			yield return null;
		}
	}
	
	public static void DeactivateChildren(this Component co)
	{
		foreach(var c in co.transform.GetComponentsInChildren<Transform>().Except(new [] {co.transform}))
			c.gameObject.active = false;
	}

	
	public static void DestroyChildren(this Transform t)
	{
		foreach(var c in t.Cast<Transform>())
		{
			GameObject.Destroy(c.gameObject);
		}
	}
	
	public class PreferenceAccess
	{
		public bool this[string name]
		{
			get
			{
				name = "Pref " + name;
				return PlayerPrefs.HasKey(name) ? (PlayerPrefs.GetInt(name)==1) : false;
			}
			set
			{
				name = "Pref " + name;
				PlayerPrefs.SetInt(name, value ? 1 : 0);
			}
		}
	}
	
	public static PreferenceAccess Preferences = new PreferenceAccess();
	

	static Lookup<string, GameObject> _gameObjects;
	static Lookup<string, GameObject> _fullPaths;
	
	public static bool AllowDeferredLogging = false;
	
	
	
	
	public static Transform FindChildIncludingDeactivated(this Transform t, string name)
	{
		var all = t.GetComponentsInChildren<Transform>(true);
		return all.FirstOrDefault(c => c.name == name);
	}

	public static string GetId(this GameObject go)
	{
		var ui = go.GetComponent<UniqueIdentifier>();
		return ui == null ? go.GetFullName() : ui.Id;

	}
	
	public static GameObject FindGameObject(string name) 
	{
		IndexScene();
		return name.Contains("/") ? _fullPaths[name] : _gameObjects[name];
		
	}

	
	public static string GetFullName(this GameObject gameObject)
	{
		var list = new Stack<string>();
		var t = gameObject.transform;
		while (t != null)
		{
			list.Push(t.name);
			t = t.parent;
		}
		var sb = new StringBuilder();
		while (list.Count > 0)
		{
			sb.AppendFormat("/{0}", list.Pop());
		}
		return sb.ToString();
		
	}
	static void IndexScene()
	{
		if (GameObject.Find("_SceneIndex") != null)
			return;
		_gameObjects = new Lookup<string, GameObject>();
		_fullPaths = new Lookup<string, GameObject>();
		foreach (var g in 
			//Resources.FindObjectsOfTypeAll(typeof(Transform)).Cast<Transform>().Select(t=>t.gameObject)
			GameObject.FindObjectsOfType(typeof(GameObject))
			.Cast<GameObject>()
			.Where(g=>g.transform.parent == null)
			.SelectMany(g=>g.GetComponentsInChildren(typeof(Transform),true)
                .Cast<Transform>()			
			    .Select(t=>t.gameObject))
			)
		{
			_gameObjects[g.name] = g;
			_fullPaths[g.GetFullName().Substring(1)] = g;
		}
		new GameObject("_SceneIndex");
	}
	
	public static void ReIndexScene()
	{
		var go = GameObject.Find("_SceneIndex");
		if (go != null)
		{
			GameObject.Destroy(go);
		}
	}
	
	public static T Find<T>(string name) where T : Component
	{
		IndexScene();
		var go = name.Contains("/") ? _fullPaths[name] : _gameObjects[name];
		if (go == null)
			return null;
		return go.GetComponent<T>();
	}
	
	public static T Find<T>(this GameObject go, string name) where T : Component
	{
		go = go.transform.FindChild(name).gameObject;
		return go.GetComponentInChildren<T>();
	}
	
	public static Index<T, List<TR>> ToIndex<TSource, T, TR>(this IEnumerable<TSource> source, Func<TSource, T> keySelector, Func<TSource, TR> elementSelector) where T : class where  TR : class
	{
		var x = new Index<T, List<TR>>();
		foreach (var v in source)
		{
			x[keySelector(v)].Add(elementSelector == null ? v as TR : elementSelector(v));
		}
		return x;
	}
	
	public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector)
	{
		return source.MinBy(selector, Comparer<TKey>.Default);
	}

	public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
    Func<TSource, TKey> selector, IComparer<TKey> comparer)
	{
		using (IEnumerator<TSource> sourceIterator = source.GetEnumerator())
		{
			if (!sourceIterator.MoveNext())
			{
				throw new InvalidOperationException("Sequence was empty");
			}
			TSource min = sourceIterator.Current;
			TKey minKey = selector(min);
			while (sourceIterator.MoveNext())
			{
				TSource candidate = sourceIterator.Current;
				TKey candidateProjected = selector(candidate);
				if (comparer.Compare(candidateProjected, minKey) < 0)
				{
					min = candidate;
					minKey = candidateProjected;
				}
			}
			return min;
		}
	}
	
	
	
	public static IEnumerable<TResult> Discrete<TResult, T1>( this IEnumerable<TResult> seq, Func<TResult,T1> func ) 
	{
		return seq.GroupBy(func).Select(g=>g.First());
	}
	
	public static Index<T, List<TSource>> ToIndex<TSource, T>(this IEnumerable<TSource> source, Func<TSource, T> keySelector) where T : class
	{
		var x = new Index<T, List<TSource>>();
		foreach (var v in source)
		{
			x[keySelector(v)].Add(v);
		}
		return x;
	}

	private static int _indent=0;
	public static readonly bool DebugBuild;
	public static int _deferredLoggingEnabled = 0;
	
	public class Logging : IDisposable
	{
		public Logging()
		{
			
			_deferredLoggingEnabled++;
		}
		
		public void Dispose()
		{
			_deferredLoggingEnabled--;
			if (_deferredLoggingEnabled == 0)
			{
				Radical.CommitLog();
			}
		}
	}
	
	public static bool DeferredLoggingEnabled
	{
		get
		{
			return _deferredLoggingEnabled > 0;
		}
	}
	
	public static T FindInterface<T>(this GameObject go) where T : class
	{
		return go.GetComponents<Component>().OfType<T>().FirstOrDefault();
	}
	
	public static T FindImplementor<T> (this GameObject go) where T : class
	{
		return RecurseFind<T> (go);
	
	}
	
	public static T[] FindImplementors<T>(this GameObject go) where T : class
	{
		return go.GetComponentsInChildren<Component>().OfType<T>().ToArray();
	}
	
	private static T RecurseFind<T> (GameObject go) where T : class
	{
		var component = go.GetComponents<Component> ().FirstOrDefault (c => c is T);
		if (component != null) {
			return component as T;
		}
		if (go.transform.parent != null) {
			return RecurseFind<T> (go.transform.parent.gameObject);
		}
		return null;
		
	}
	
	///<summary>Finds the index of the first occurence of an item in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="item">The item to find.</param>
        ///<returns>The index of the first matching item, or -1 if the item was not found.</returns>
        public static int IndexOf<T>(this IEnumerable<T> items, T item)
        {
            return items.FindIndex(i => EqualityComparer<T>.Default.Equals(item, i));
        }

        ///<summary>Finds the index of the first item matching an expression in an enumerable.</summary>
        ///<param name="items">The enumerable to search.</param>
        ///<param name="predicate">The expression to test the items against.</param>
        ///<returns>The index of the first matching item, or -1 if no items match.</returns>
        public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            if (items == null) throw new ArgumentNullException("items");
            if (predicate == null) throw new ArgumentNullException("predicate");

            int retVal = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return retVal;
                retVal++;
            }
            return -1;
        }

        
	
	
	public static Color RGBA(int r, int g, int b, int a)
	{
		return new Color((float)(r / 255f), (float)(g / 255f), (float)(b / 255f), (float)(a / 255f));
	}
	
	public static Vector3 mergeMix = new Vector3(0, 1, 0);
	
	
	public static Quaternion Merge(this Quaternion first, Vector3 second)
	{
		return Quaternion.Euler(Merge(first.eulerAngles, second));
	}
	
	public static Vector3 Merge(this Vector3 first, Vector3 second)
	{
		return new Vector3((first.x * (1 - mergeMix.x)) + (second.x * mergeMix.x), (first.y * (1 - mergeMix.y)) + (second.y * mergeMix.y), (first.z * (1 - mergeMix.z)) + (second.z * mergeMix.z));
	}
	
	static Radical()
	{
		DebugBuild = UnityEngine.Debug.isDebugBuild;
	}
	
	public static T GetInterface<T>(this Transform tra) where T : class
	{
		return tra.gameObject.GetInterface<T>();
	}
	
	public static T GetInterface<T>(this GameObject go) where T : class
	{
		foreach (var c in go.GetComponents<MonoBehaviour>())
		{
			if (c is T)
			{
				return c as T;
			}
		}
		
		return null;
	}
	
	
	
	public static IList<T> GetInterfaces<T>(this GameObject go) where T : class
	{
		var l = new List<T>();
		foreach (var c in go.GetComponents<MonoBehaviour>())
		{
			if (c is T)
			{
				l.Add(c as T);
			}
		}
		
		return l;
	}
	
	public static void IndentLog()
	{
		_indent++;
	}
	
	public static void OutdentLog()
	{
		_indent--;
	}
	
	private static List<string> logEntries = new List<string>();
	
	public static void LogNode(object message)
	{
		LogNow(message.ToString());
	}
	public static void LogNow(string message, params object[] parms)
	{
		if (!DebugBuild)
			return;
		UnityEngine.Debug.Log(string.Format(message, parms));
	}
	
	public static void LogWarning(string message)
	{
		LogWarning ( message, null);
	}
	public static void LogWarning(string message, UnityEngine.Object context)
	{
		if (!DebugBuild)
			return;
		if (context != null)
		{
			UnityEngine.Debug.LogWarning(message, context);
		}
		else
		{
			UnityEngine.Debug.LogWarning(message);
		}
	}
	
	public static void LogError(string message)
	{
		LogError( message, null);
	}
	public static void LogError(string message, UnityEngine.Object context)
	{
		if (!DebugBuild)
		{
			return;
		}
		if (context != null)
			UnityEngine.Debug.LogError(message, context);
		else
			UnityEngine.Debug.LogError(message);
	}
	
	public static bool IsLogging()
	{
		if (DebugBuild == false || ! DeferredLoggingEnabled)
		{
			return false;
		}
		return true;
		
	}
	
	public static void Log(string message, params object[] parms)
	{
		if (DebugBuild == false || ! DeferredLoggingEnabled || !AllowDeferredLogging)
		{
			return;
		}
		logEntries.Add((new string(' ', 4 * _indent)) + string.Format(message, parms));
		if (logEntries.Count > 50000)
		{
			logEntries.RemoveAt(0);
		}
	}
	
	public static void ClearLog()
	{
		logEntries.Clear();
	}
	
	public static void CommitLog()
	{
		if (logEntries.Count == 0)
		{
			return;
		}
		var sb = logEntries.Aggregate((current, next) => current + "\n" + next);
		UnityEngine.Debug.Log(sb);
		logEntries.Clear();
	}
	
	public static GameObject Instantiate(Transform template)
	{
		return Instantiate( template, null);
	}
	public static GameObject Instantiate(Transform template, GameObject parent)
	{
		var go = (GameObject.Instantiate(template) as Transform).gameObject;
		if (parent != null)
		{
			go.transform.SetParent(parent.transform);
		}
		return go;
	}
	
	public static GameObject SetParent(this GameObject child, GameObject parent)
	{
		return SetParent ( child, parent, false);
	}
	public static GameObject SetParent(this GameObject child, GameObject parent, bool setScale)
	{
		child.transform.SetParent(parent.transform, setScale);
		return child;
	}
	
	public static Transform SetParent(this Transform child, GameObject parent)
	{
		return SetParent( child, parent, false);
	}
	public static Transform SetParent(this Transform child, GameObject parent, bool setScale)
	{
		child.SetParent(parent.transform, setScale);
		return child;
	}
	
	public static Transform SetParent(this Transform child, Transform parent)
	{
		return SetParent ( child, parent, false);
	}
	public static Transform SetParent(this Transform child, Transform parent, bool setScale)
	{
		try
		{
			Vector3 pos = child.localPosition;
			Quaternion rot = child.localRotation;
			Vector3 scale = child.localScale;
		
			child.parent = parent;
			child.localPosition = pos;
			child.localRotation = rot;
			if (setScale)
			{
				child.localScale = scale;
			}
		}
		catch
		{
		}
		return child;
	}
	
	public static Quaternion SmoothDamp(this Vector3 current, Vector3 target, ref Vector3 velocity, float time)
	{
		Vector3 result = Vector3.zero;
		result.x = Mathf.SmoothDampAngle(current.x, target.x, ref velocity.x, time);
		result.y = Mathf.SmoothDampAngle(current.y, target.y, ref velocity.y, time);
		result.z = Mathf.SmoothDampAngle(current.z, target.z, ref velocity.z, time);
		return Quaternion.Euler(result);
	}
	
	public static GameObject AddChild(this GameObject parent, Transform template)
	{
		return Instantiate(template, parent);
	}
	
	public static void EnsureComponent(this GameObject obj, Type t)
	{
		if (obj.GetComponent(t) == null)
		{
			obj.AddComponent(t);
		}
	}
	
	public static void RemoveComponent(this GameObject obj, Type t)
	{
		foreach (var c in obj.GetComponents(t))
		{
			Component.DestroyImmediate(c);
		}
	}
	
}

public static class TextHelper
{
	public static string FixTo(this string str, float width)
	{
		return FixTo ( str, width, "label");
	}
	public static string FixTo(this string str, float width, string type)
	{
		var widthOfTab = GUI.skin.GetStyle(type).CalcSize(new GUIContent("\t")).x;
		var widthOfDot = GUI.skin.GetStyle(type).CalcSize(new GUIContent(".")).x;
		var widthOfSpace = Mathf.Max(1, GUI.skin.GetStyle(type).CalcSize(new GUIContent(". .")).x - (2 * widthOfDot));
		var widthOfString = GUI.skin.GetStyle(type).CalcSize(new GUIContent(str)).x;
		return str + new String(' ', (int)((width - widthOfTab - widthOfString) / widthOfSpace) + 1) + "\t";
	}
}

public class ValueLookup<TK, TR> : Dictionary<TK, TR> where TR : struct
{
    public new virtual TR this[TK index]
    {
        get
        {
            if (ContainsKey(index))
            {
                return base[index];
            }
            return default(TR);

        }
        set
        {
            base[index] = value;
        }
    }

    public T Get<T>(TK index) where T : class
    {
        return this[index] as T;
    }



}


