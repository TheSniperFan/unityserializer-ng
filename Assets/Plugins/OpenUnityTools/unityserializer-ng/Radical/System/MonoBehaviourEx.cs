using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Reflection;

/// <summary>
/// A base class that enables using extended Radical Coroutines
/// which allow serialization and resumption of the routines later
/// using the standard function prototypes
/// </summary>
public class MonoBehaviourEx : MonoBehaviour {
	
	/// <summary>
	/// Starts the coroutine in a resumable way
	/// </summary>
	/// <returns>
	/// The RadicalRoutine that enables cancellation etc
	/// </returns>
	/// <param name='func'>
	/// The coroutine to start
	/// </param>
	public new RadicalRoutine StartCoroutine(IEnumerator func)
	{
		return this.StartExtendedCoroutine(func);
	}
	
	/// <summary>
	/// Starts the coroutine in a resumable way
	/// </summary>
	/// <returns>
	/// The RadicalRoutine that enables cancellation etc
	/// </returns>
	/// <param name='func'>
	/// The coroutine to start
	/// </param>
	public new RadicalRoutine StartCoroutine(string func)
	{
		var m = this.GetType().GetMethod(func, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
		if(m.ReturnType == typeof(IEnumerator))
		{
			return this.StartCoroutine((IEnumerator)m.Invoke(this, null));
		}
		return null;
		
	}
}
