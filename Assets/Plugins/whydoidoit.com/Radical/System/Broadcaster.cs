using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Reflection;
using System.Collections.Generic;

public static class Broadcaster {
	
	public class Pair
	{
		public WeakReference target;
		public WeakReference interest;
	}
	
	private static List<Pair> InterestList = new List<Pair>();
	
	public static void RegisterInterest(this object target, object interestedParty)
	{
		Cleanup();
		InterestList.Add( new Pair() { target = new WeakReference(target), interest =  new WeakReference(interestedParty)});
	}
	
	public static void UnregisterInterest(this object target, object interestedParty)
	{
		Cleanup();
		InterestList.Remove( InterestList.FirstOrDefault(p=>p.target.Target == target && p.interest.Target == interestedParty  ) );
	}
	
	public static void Broadcast(this object obj, string message)
	{
		Cleanup();
		
		foreach(var m in InterestList.Where(p=>p.target.Target == obj).Select(p=>p.interest).Where(r=>r.IsAlive).ToList())
		{
			var mth = m.Target.GetType().GetMethod(message, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
			if(mth.GetParameters().Length == 1)
			{
			    mth.Invoke(m.Target, new [] {obj});
			}
			else
			{
				mth.Invoke(m.Target, null);
			}
		}
	}
	
	static void Cleanup()
	{
		var list = InterestList.Where(k=>! k.target.IsAlive || !k.interest.IsAlive).ToList();
		foreach(var e in list)
			InterestList.Remove(e);
	}
	
}



