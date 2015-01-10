//http://www.whydoidoit.com
//Copyright (C) 2012 Mike Talbot
//
//Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
//The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;
using System;
using Serialization;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// A waitable coroutine return value
/// </summary>
public class CoroutineReturn
{
	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="CoroutineReturn"/> is finished.
	/// </summary>
	/// <value>
	/// <c>true</c> if finished; otherwise, <c>false</c>.
	/// </value>
	public virtual bool finished { get; set; }
	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="CoroutineReturn"/> is cancelled
	/// </summary>
	/// <value>
	/// <c>true</c> if cancel; otherwise, <c>false</c>.
	/// </value>
	public virtual bool cancel { get; set; }
}

[SerializeAll]
/// <summary>
/// Wait for animation to complete
/// </summary>
public class WaitForAnimation : CoroutineReturn
{
	private GameObject _go;
	private string _name;
	private float _time;
	private float _weight;
	[DoNotSerialize]
	private int startFrame;
	
	/// <summary>
	/// Gets the name of the animation
	/// </summary>
	/// <value>
	/// The name.
	/// </value>
	public string name
	{
		get
		{
			return _name;
		}
	}
	
	public WaitForAnimation()
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="WaitForAnimation"/> class.
	/// </summary>
	/// <param name='go'>
	/// The game object to run on
	/// </param>
	/// <param name='name'>
	/// The name of the animation to wait for 
	/// </param>
	public WaitForAnimation(GameObject go, string name)
		: this( go, name, 1f, -1)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="WaitForAnimation"/> class.
	/// </summary>
	/// <param name='go'>
	/// The game object to run on
	/// </param>
	/// <param name='name'>
	/// The name of the animation to wait for 
	/// </param>
	/// <param name='time'>
	/// The proportion of the animation to wait for
	/// </param>
	public WaitForAnimation(GameObject go, string name, float time)
		: this( go, name, time, -1)
	{
	}
	/// <summary>
	/// Initializes a new instance of the <see cref="WaitForAnimation"/> class.
	/// </summary>
	/// <param name='go'>
	/// The game object to run on
	/// </param>
	/// <param name='name'>
	/// The name of the animation to wait for 
	/// </param>
	/// <param name='time'>
	/// The proportion of the animation to wait for
	/// </param>
	/// <param name='weight'>
	/// The weight to target
	/// </param>
	public WaitForAnimation(GameObject go, string name, float time, float weight)
	{
		startFrame = Time.frameCount;
		_go = go;
		_name = name;
		_time = Mathf.Clamp01(time);
		_weight = weight;
	}
	
	[DoNotSerialize]
	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="WaitForAnimation"/> is finished.
	/// </summary>
	/// <value>
	/// <c>true</c> if finished; otherwise, <c>false</c>.
	/// </value>
	public override bool finished
	{
		get
		{
			if (LevelSerializer.IsDeserializing)
			{
				return false;
			}
			if (Time.frameCount <= startFrame + 4)
			{
				return false;
			}
			
			var anim = _go.GetComponent<Animation>()[_name];
		
			bool ret = true;
				
			if (anim.enabled)
			{
				
				if (_weight == -1)
				{
					ret = anim.normalizedTime >= _time;
				
				}
				else
				{
					if (_weight < 0.5)
					{
						ret = anim.weight <= Mathf.Clamp01(_weight + 0.001f);
					}
					ret = anim.weight >= Mathf.Clamp01(_weight - 0.001f);
				}
			
			}
			if(!_go.GetComponent<Animation>().IsPlaying(_name))
			{
				ret = true;
			}
			if(ret)
			{
				if(anim.weight == 0 || anim.normalizedTime == 1)
				{
					anim.enabled = false;
				}
			}
			return ret;
				
		}
		set
		{
			base.finished = value;
		}
	}
	
}

public static class TaskHelpers
{
	public static WaitForAnimation WaitForAnimation(this GameObject go, string name)
	{
		return WaitForAnimation(go, name, 1f);
	}
	public static WaitForAnimation WaitForAnimation(this GameObject go, string name, float time)
	{
		return new WaitForAnimation(go, name, time, -1);
	}

	public static WaitForAnimation WaitForAnimationWeight(this GameObject go, string name)
	{
		return WaitForAnimationWeight(go, name, 0f);
	}
	public static WaitForAnimation WaitForAnimationWeight(this GameObject go, string name, float weight)
	{
		return new WaitForAnimation(go, name, 0, weight);
	}
}

public interface IYieldInstruction
{
	YieldInstruction Instruction { get; }
}


public class RadicalWaitForSeconds : IYieldInstruction
{

	private float _time;
	private float _seconds;
	
	public RadicalWaitForSeconds()
	{
	}
	
	public float TimeRemaining
	{
		get
		{
			return Mathf.Clamp((_time + _seconds) - Time.time, 0, 10000000);
		}
		set
		{
			_time = Time.time;
			_seconds = value;
		}
	}
	
	public RadicalWaitForSeconds(float seconds)
	{
		_time = Time.time;
		_seconds = seconds;
	}
	
	#region IYieldInstruction implementation
	public YieldInstruction Instruction
	{
		get
		{
			return new WaitForSeconds(TimeRemaining);
		}
	}
	#endregion
}

/// <summary>
/// Interface that notifies a waiting 
/// coroutine function that it is stopped
/// </summary>
public interface INotifyStartStop
{
	/// <summary>
	/// This instance has stopped
	/// </summary>
	void Stop();
	/// <summary>
	/// This instance has started
	/// </summary>
	void Start();
}

/// <summary>
/// A coroutine that can be serialized and
/// resumed later
/// </summary>
public class RadicalRoutine : IDeserialized
{
	/// <summary>
	/// Cancel the coroutine if true
	/// </summary>
	public bool cancel;
	private IEnumerator extended;
	/// <summary>
	/// The enumerator of the running coroutine
	/// </summary>
	public IEnumerator enumerator;
	/// <summary>
	/// The object to notify when completed
	/// </summary>
	public object Notify;
	/// <summary>
	/// The method that will be called when
	/// the routine is complete
	/// </summary>
	public string Method;
	/// <summary>
	/// True when the coroutine is finished
	/// </summary>
	public bool finished;
	public object Target;
	bool isTracking;
	MonoBehaviour _trackedObject;
	/// <summary>
	/// Gets or sets the tracked object - which is the object
	/// that is running the coroutine
	/// </summary>
	/// <value>
	/// The tracked object.
	/// </value>
	public MonoBehaviour trackedObject
	{
		get
		{
			return _trackedObject;
		}
		set
		{
			_trackedObject = value;
			isTracking = _trackedObject != null;
		}
	}
	/// <summary>
	/// Event raised if the routine is cancelled
	/// </summary>
	public event Action Cancelled = delegate {};
	/// <summary>
	/// Event raised when the coroutine finished
	/// </summary>
	public event Action Finished = delegate {};
	/// <summary>
	/// Cancel this coroutine
	/// </summary>
	public void Cancel()
	{
		cancel = true;
		if (extended is INotifyStartStop)
		{
			(extended as INotifyStartStop).Stop();
		}
	}
	/// <summary>
	/// Run the specified extendedCoRoutine
	/// </summary>
	/// <param name='extendedCoRoutine'>
	/// The coroutine to make serializable
	/// </param>
	public static RadicalRoutine Run(IEnumerator extendedCoRoutine)
	{
		return Run ( extendedCoRoutine, "", null);
	}
	/// <summary>
	/// Run the specified coroutine, invoking a particular method on completion
	/// </summary>
	/// <param name='extendedCoRoutine'>
	/// The coroutine to exten
	/// </param>
	/// <param name='methodName'>
	/// Method to call
	/// </param>
	public static RadicalRoutine Run(IEnumerator extendedCoRoutine, string methodName)
	{
		return Run ( extendedCoRoutine, methodName, null);
	}
	public static RadicalRoutine Run(IEnumerator extendedCoRoutine, string methodName, object target)
	{
		var rr = new RadicalRoutine();
		rr.Method = methodName;
		rr.Target = target;
		rr.extended = extendedCoRoutine;
		if (rr.extended is INotifyStartStop)
		{
			(rr.extended as INotifyStartStop).Start();
		}
		rr.enumerator = rr.Execute(extendedCoRoutine);
		RadicalRoutineHelper.Current.Run(rr);
		return rr;
		
	}
	
	/// <summary>
	/// Creates a radical coroutine for a specific function
	/// </summary>
	/// <param name='extendedCoRoutine'>
	/// The function to use as a coroutine
	/// </param>
	public static RadicalRoutine Create(IEnumerator extendedCoRoutine)
	{
		var rr = new RadicalRoutine();
		rr.extended = extendedCoRoutine;
		if (rr.extended is INotifyStartStop)
		{
			(rr.extended as INotifyStartStop).Start();
		}
		rr.enumerator = rr.Execute(extendedCoRoutine);
		return rr;
	}
	/// <summary>
	/// Run this instance.
	/// </summary>
	public void Run()
	{
		Run ( "", null);
	}
	/// <summary>
	/// Run this instance, calling a method at completion
	/// </summary>
	/// <param name='methodName'>
	/// Method to call
	/// </param>
	public void Run(string methodName)
	{
		Run ( methodName, null);
	}
	/// <summary>
	/// Runs the routine calling a method on a target when complete
	/// </summary>
	/// <param name='methodName'>
	/// The method to call
	/// </param>
	/// <param name='target'>
	/// The target object to call it on
	/// </param>
	public void Run(string methodName, object target)
	{
		Method = methodName;
		Target = target;
		if(trackedObject != null)
		{
			var helper = trackedObject.GetComponent<RadicalRoutineHelper>() ?? trackedObject.gameObject.AddComponent<RadicalRoutineHelper>();
			helper.Run(this);
		}
		else
			RadicalRoutineHelper.Current.Run(this);
	}
	
	private IEnumerator Execute(IEnumerator extendedCoRoutine)
	{
		return Execute( extendedCoRoutine, null);
	}
	private IEnumerator Execute(IEnumerator extendedCoRoutine, Action complete)
	{
		var stack = new Stack<IEnumerator>();
		stack.Push(extendedCoRoutine);
		while(stack.Count > 0)
		{
			extendedCoRoutine = stack.Pop();
			while (!cancel && extendedCoRoutine != null && (!isTracking || (trackedObject != null && trackedObject.enabled) ) && (!LevelSerializer.IsDeserializing ? extendedCoRoutine.MoveNext() : true))
			{
				var v = extendedCoRoutine.Current;
				var cr = v as CoroutineReturn;
				if (cr != null)
				{
					if (cr.cancel)
					{
						cancel = true;
						break;
					}
					while (!cr.finished)
					{
						if (cr.cancel)
						{
							cancel = true;
							break;
						}
						yield return null;
					}
					if (cancel)
						break;
				}
				else
				if (v is IYieldInstruction)
				{
					yield return (v as IYieldInstruction).Instruction;
				} if(v is IEnumerator)
				{
					stack.Push(extendedCoRoutine);
					extendedCoRoutine = v as IEnumerator;
				} else if(v is RadicalRoutine)
				{
					var rr = v as RadicalRoutine;
					while(!rr.finished)
						yield return null;
				}
				else
				{
					yield return v;
				}
				
				
			}
		}
		finished = true;
		Cancel();
	
		if (cancel)
		{
			Cancelled();
		}
	
		Finished();
		if (complete != null)
			complete();
		
		
		
	}
	
	
	#region IDeserialized implementation
	public void Deserialized()
	{
		
	}
	#endregion
}

/// <summary>
/// Radical routine extension class
/// </summary>
public static class RadicalRoutineExtensions
{
	public class RadicalRoutineBehaviour : MonoBehaviour
	{
	}
	
	/// <summary>
	/// Starts a coroutine that can be serialized and deserialized
	/// </summary>
	/// <returns>
	/// The running coroutine
	/// </returns>
	/// <param name='behaviour'>
	/// The monobehaviour to start the routine on
	/// </param>
	/// <param name='coRoutine'>
	/// The function to use as a coroutine
	/// </param>
	public static RadicalRoutine StartExtendedCoroutine(this MonoBehaviour behaviour, IEnumerator coRoutine)
	{
		var routine = RadicalRoutine.Create(coRoutine);
		routine.trackedObject = behaviour;
		routine.Run();
		return routine;
		
	}
	
	/// <summary>
	/// Starts a coroutine that can be serialized and deserialized
	/// </summary>
	/// <returns>
	/// The running coroutine
	/// </returns>
	/// <param name='GameObject'>
	/// The Game Object to start the routine on
	/// </param>
	/// <param name='coRoutine'>
	/// The function to use as a coroutine
	/// </param>
	public static RadicalRoutine StartExtendedCoroutine(this GameObject go, IEnumerator coRoutine)
	{
		var behaviour = go.GetComponent<MonoBehaviour>() ?? go.AddComponent<RadicalRoutineBehaviour>();
		return behaviour.StartExtendedCoroutine(coRoutine);
	}
	
	/// <summary>
	/// Starts a coroutine that can be serialized and deserialized
	/// </summary>
	/// <returns>
	/// The running coroutine
	/// </returns>
	/// <param name='co'>
	/// The Component to start the routine on
	/// </param>
	/// <param name='coRoutine'>
	/// The function to use as a coroutine
	/// </param>
	public static RadicalRoutine StartExtendedCoroutine(this Component co, IEnumerator coRoutine)
	{
		return co.gameObject.StartExtendedCoroutine(coRoutine);
	}
}
