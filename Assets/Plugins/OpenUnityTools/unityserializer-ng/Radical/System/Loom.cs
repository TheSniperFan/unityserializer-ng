using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

[ExecuteInEditMode]
/// <summary>
/// Multithreading support
/// </summary>
public class Loom : MonoBehaviour
{
	private static Loom _current;
	private int _count;
	/// <summary>
	/// Return the current instance
	/// </summary>
	/// <value>
	/// 
	/// </value>
	public static Loom Current
	{
		get
		{
			if(!_initialized) Initialize();
			return _current;
		}
	}
	
	static bool _initialized;
	static int _threadId=-1;
	
	public static void Initialize()
	{
		
		var go = !_initialized;
		
		if(go && _threadId != -1 && _threadId != Thread.CurrentThread.ManagedThreadId)
			return;
		
		if (go)
		{
			var g = new GameObject("Loom");
			g.hideFlags = HideFlags.HideAndDontSave;
			GameObject.DontDestroyOnLoad(g);
			_current = g.AddComponent<Loom>();
			Component.DontDestroyOnLoad(_current);
			_initialized = true;
			_threadId = Thread.CurrentThread.ManagedThreadId;
		}
			
	}
	
	void OnDestroy()
	{
		_initialized = false;
	}
	
	private List<Action> _actions = new List<Action>();
	public class DelayedQueueItem
	{
		public float time;
		public Action action;
	}
	private List<DelayedQueueItem> _delayed = new  List<DelayedQueueItem>();
	
	/// <summary>
	/// Queues an action on the main thread
	/// </summary>
	/// <param name='action'>
	/// The action to execute
	/// </param>
	public static void QueueOnMainThread(Action action)
	{
		QueueOnMainThread( action, 0f);
	}
	/// <summary>
	/// Queues an action on the main thread after a delay
	/// </summary>
	/// <param name='action'>
	/// The action to run
	/// </param>
	/// <param name='time'>
	/// The amount of time to delay
	/// </param>
	public static void QueueOnMainThread(Action action, float time)
	{
		if(time != 0)
		{
			lock(Current._delayed)
			{
				Current._delayed.Add(new DelayedQueueItem { time = Time.time + time, action = action});
			}
		}
		else
		{
			lock (Current._actions)
			{
				Current._actions.Add(action);
			}
		}
	}
	
	/// <summary>
	/// Runs an action on another thread
	/// </summary>
	/// <param name='action'>
	/// The action to execute on another thread
	/// </param>
	public static void RunAsync(Action action)
	{
		var t = new Thread(RunAction);
		t.Priority = System.Threading.ThreadPriority.Normal;
		t.Start(action);
	}
	
	private static void RunAction(object action)
	{
		((Action)action	)();
	}
	

	// Use this for initialization
	void Start()
	{
	
	}
	
	// Update is called once per frame
	void Update()
	{
		var actions = new List<Action>();
		lock (_actions)
		{
			actions.AddRange(_actions);
			_actions.Clear();
			foreach(var a in actions)
			{
				a();
			}
		}
		lock(_delayed)
		{
			foreach(var delayed in _delayed.Where(d=>d.time <= Time.time).ToList())
			{
				_delayed.Remove(delayed);
				delayed.action();
			}
		}
		
		
	}
}

