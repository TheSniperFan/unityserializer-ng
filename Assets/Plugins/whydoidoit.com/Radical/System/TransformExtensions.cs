using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// Helper functions for transforms
/// </summary>
public static class TransformExtensions 
{
	
	/// <summary>
	/// Get the first ancestor of a particular type
	/// </summary>
	/// <returns>
	/// The instance of the type or null
	/// </returns>
	/// <param name='gameObject'>
	/// The game object
	/// </param>
	/// <typeparam name='T'>
	/// The type to get 
	/// </typeparam>
	public static T FirstAncestorOfType<T>(this GameObject gameObject) where T : Component
	{
		var t = gameObject.transform.parent;
		T component = null;
		while (t != null && (component = t.GetComponent<T>()) == null)
		{
			t = t.parent;
		}
		return component;
	}
	
	/// <summary>
	/// Get the last ancestor of a particular type
	/// </summary>
	/// <returns>
	/// The instance of the type or null
	/// </returns>
	/// <param name='gameObject'>
	/// The game object
	/// </param>
	/// <typeparam name='T'>
	/// The type to get 
	/// </typeparam>
	public static T LastAncestorOfType<T>(this GameObject gameObject) where T : class
	{
		var t = gameObject.transform.parent;
		T component = null;
		while (t != null)
		{
			var c = t.gameObject.FindImplementor<T>();
			if (c != null)
			{
				component = c;
			}
			t = t.parent;
		}
		return component;
	}
	
	/// <summary>
	/// Gets components in children, including disabled ones
	/// </summary>
	/// <returns>
	/// An array of the children found
	/// </returns>
	/// <param name='parent'>
	/// The parent to search from
	/// </param>
	/// <typeparam name='T'>
	/// The type to find
	/// </typeparam>
	public static T[] GetAllComponentsInChildren<T>(this Transform parent) where T : Component
    {
        var list = new List<T>();
		var p = parent.GetComponent<T>();
		if(p) list.Add(p);
        foreach(var child in parent.Cast<Transform>())
        {
            var candidate = child.GetComponent<T>();
            if(candidate)
            {
                list.Add(candidate);
            }
            list.AddRange(GetAllComponentsInChildren<T>(child));
        }
        return list.ToArray();
    }
	
	/// <summary>
	/// Gets components in children, including disabled ones
	/// </summary>
	/// <returns>
	/// An array of the children found
	/// </returns>
	/// <param name='comp'>
	/// The parent to search from
	/// </param>
	/// <typeparam name='T'>
	/// The type to find
	/// </typeparam>
	public static T[] GetAllComponentsInChildren<T>(this Component comp) where T : Component
	{
		return GetAllComponentsInChildren<T>(comp.transform);
	}

	/// <summary>
	/// Gets components in children, including disabled ones
	/// </summary>
	/// <returns>
	/// An array of the children found
	/// </returns>
	/// <param name='go'>
	/// The parent to search from
	/// </param>
	/// <typeparam name='T'>
	/// The type to find
	/// </typeparam>
	public static T[] GetAllComponentsInChildren<T>(this GameObject go) where T : Component
	{
		return GetAllComponentsInChildren<T>(go.transform);
	}
	
}

