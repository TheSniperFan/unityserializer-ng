using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace RadicalLibrary
{

public static class QuadBez {
	
	public static Vector3 Interp(Vector3 st, Vector3 en, Vector3 ctrl, float t) {
		float d = 1f - t;
		return d * d * st + 2f * d * t * ctrl + t * t * en;
	}
	
	
	public static Vector3 Velocity(Vector3 st, Vector3 en, Vector3 ctrl, float t) {
		return (2f * st - 4f * ctrl + 2f * en) * t + 2f * ctrl - 2f * st;
	}
	
	
	public static void GizmoDraw(Vector3 st, Vector3 en, Vector3 ctrl, float t) {
		Gizmos.color = Color.red;
		Gizmos.DrawLine(st, ctrl);
		Gizmos.DrawLine(ctrl, en);
		
		Gizmos.color = Color.white;
		Vector3 prevPt = st;
		
		for (int i = 1; i <= 20; i++) {
			float pm = (float) i / 20f;
			Vector3 currPt = Interp(st,en,ctrl,pm);
			Gizmos.DrawLine(currPt, prevPt);
			prevPt = currPt;
		}
		
		Gizmos.color = Color.blue;
		Vector3 pos = Interp(st,en, ctrl,t);
		Gizmos.DrawLine(pos, pos + Velocity(st,en,ctrl,t));
	}
	

}


public static class CubicBez {
	public static Vector3 Interp(Vector3 st, Vector3 en, Vector3 ctrl1, Vector3 ctrl2, float t) {
		float d = 1f - t;
		return d * d * d * st + 3f * d * d * t * ctrl1 + 3f * d * t * t * ctrl2 + t * t * t * en;
	}
	
	
	public static Vector3 Velocity(Vector3 st, Vector3 en, Vector3 ctrl1, Vector3 ctrl2,float t) {
		return (-3f * st + 9f * ctrl1 - 9f * ctrl2 + 3f * en) * t * t
			+  (6f * st - 12f * ctrl1 + 6f * ctrl2) * t
			-  3f * st + 3f * ctrl1;
	}
	
	
	public static void GizmoDraw(Vector3 st, Vector3 en, Vector3 ctrl1, Vector3 ctrl2,float t) {
		Gizmos.color = Color.red;
		Gizmos.DrawLine(st, ctrl1);
		Gizmos.DrawLine(ctrl2, en);
		
		Gizmos.color = Color.white;
		Vector3 prevPt = st;
		
		for (int i = 1; i <= 20; i++) {
			float pm = (float) i / 20f;
			Vector3 currPt = Interp(st,en,ctrl1, ctrl2, pm);
			Gizmos.DrawLine(currPt, prevPt);
			prevPt = currPt;
		}
		
		Gizmos.color = Color.blue;
		Vector3 pos = Interp(st,en,ctrl1, ctrl2, t);
		Gizmos.DrawLine(pos, pos + Velocity(st,en,ctrl1,ctrl2,t));
	}
}


public static class CRSpline {

	
	public static Vector3 Interp(Vector3[] pts, float t) {
		int numSections = pts.Length - 3;
		int currPt = Mathf.Min(Mathf.FloorToInt(t * (float) numSections), numSections - 1);
		float u = t * (float) numSections - (float) currPt;
				
		Vector3 a = pts[currPt];
		Vector3 b = pts[currPt + 1];
		Vector3 c = pts[currPt + 2];
		Vector3 d = pts[currPt + 3];
		
		return .5f * (
			(-a + 3f * b - 3f * c + d) * (u * u * u)
			+ (2f * a - 5f * b + 4f * c - d) * (u * u)
			+ (-a + c) * u
			+ 2f * b
		);
	}
	
	public static Vector3 InterpConstantSpeedOld(Vector3[] pts, float t) {
		int numSections = pts.Length - 3;
		float mag = 0;
		float [] sizes = new float[pts.Length-1];
		for(var i = 0; i < pts.Length-1; i++) 
		{
			var m = (pts[i+1] - pts[i]).magnitude; 
			sizes[i] = m;
			mag += m;
		}
		
		int currPt=1;
		float s=0;
		double u = 0;
		do
		{
			double tAtBeginning = s/mag;
			double tAtEnd = (s + sizes[currPt+0])/mag;
			u = (t-tAtBeginning) / (tAtEnd - tAtBeginning);
			if(double.IsNaN(u) || u<0 || u>1)
			{
				s+=sizes[currPt];
				currPt++;
			}
			else
				break;
		} while(currPt < numSections+1);
		u = Mathf.Clamp01((float)u);
		
		
		Vector3 a = pts[currPt-1];
		Vector3 b = pts[currPt + 0];
		Vector3 c = pts[currPt + 1];
		Vector3 d = pts[currPt + 2];
		
		//return CubicBez.Interp(a,d,b,c,(float)u);
		return .5f * (
			(-a + 3f * b - 3f * c + d) * (float)(u * u * u)
			+ (2f * a - 5f * b + 4f * c - d) * (float)(u * u)
			+ (-a + c) * (float)u
			+ 2f * b
		);
		
	}
	
	public static Vector3 InterpConstantSpeed(Vector3[] pts, float t) {
		int numSections = pts.Length - 3;
		float mag = 0;
		float [] sizes = new float[pts.Length-1];
		for(var i = 0; i < pts.Length-1; i++) 
		{
			var m = (pts[i+1] - pts[i]).magnitude; 
			sizes[i] = m;
			mag += m;
		}
		
		int currPt=1;
		float s=0;
		double u = 0;
		do
		{
			double tAtBeginning = s/mag;
			double tAtEnd = (s + sizes[currPt+0])/mag;
			u = (t-tAtBeginning) / (tAtEnd - tAtBeginning);
			if(double.IsNaN(u) || u<0 || u>1)
			{
				s+=sizes[currPt];
				currPt++;
			}
			else
				break;
		} while(currPt < numSections+1);
		u = Mathf.Clamp01((float)u);
		
		
		Vector3 a = pts[currPt-1];
		Vector3 b = pts[currPt + 0];
		Vector3 c = pts[currPt + 1];
		Vector3 d = pts[currPt + 2];
		
		//return CubicBez.Interp(a,d,b,c,(float)u);
		return .5f * (
			(-a + 3f * b - 3f * c + d) * (float)(u * u * u)
			+ (2f * a - 5f * b + 4f * c - d) * (float)(u * u)
			+ (-a + c) * (float)u
			+ 2f * b
		);
		
	}
	
	
	public static Vector3 Velocity(Vector3[] pts, float t) {
		int numSections = pts.Length - 3;
		int currPt = Mathf.Min(Mathf.FloorToInt(t * (float) numSections), numSections - 1);
		float u = t * (float) numSections - (float) currPt;
				
		Vector3 a = pts[currPt];
		Vector3 b = pts[currPt + 1];
		Vector3 c = pts[currPt + 2];
		Vector3 d = pts[currPt + 3];

		return 1.5f * (-a + 3f * b - 3f * c + d) * (u * u)
				+ (2f * a -5f * b + 4f * c - d) * u
				+ .5f * c - .5f * a;
	}
	
	
	public static void GizmoDraw(Vector3[] pts, float t) {
		Gizmos.color = Color.white;
		Vector3 prevPt = Interp(pts, 0);
		
		for (int i = 1; i <= 20; i++) {
			float pm = (float) i / 20f;
			Vector3 currPt = Interp(pts, pm);
			Gizmos.DrawLine(currPt, prevPt);
			prevPt = currPt;
		}
		
		Gizmos.color = Color.blue;
		Vector3 pos = Interp(pts, t);
		Gizmos.DrawLine(pos, pos + Velocity(pts, t));
	}
}

public class Interesting
{
}

public static class Spline
{
	public static Vector3 Interp(Path pts, float t)
	{
		return Interp ( pts, t, EasingType.Linear);
	}
	public static Vector3 Interp(Path pts, float t, EasingType ease)
	{
		return Interp ( pts, t, ease, true);
	}
	public static Vector3 Interp(Path pts, float t, EasingType ease, bool easeIn)
	{
		return Interp ( pts, t, ease, easeIn, true);
	}
	public static Vector3 Interp(Path pts, float t, EasingType ease, bool easeIn, bool easeOut)
	{
		t = Ease(t, ease, easeIn, easeOut);
		
		if(pts.Length == 0)
		{
			return Vector3.zero;
		}
		else if(pts.Length ==1 )
		{
			return pts[0];
		}
		else if(pts.Length == 2)
		{
			return Vector3.Lerp(pts[0], pts[1], t);
		}
		else if(pts.Length == 3)
		{
			return QuadBez.Interp(pts[0], pts[2], pts[1], t);
		}
		else if(pts.Length == 4)
		{
			return CubicBez.Interp(pts[0], pts[3], pts[1], pts[2], t); 
		}
		else
		{
			return CRSpline.Interp(Wrap(pts), t);
		}
		
		
	}
	
	private static float Ease(float t)
	{
		return Ease ( t, EasingType.Linear);
	}
	private static float Ease(float t, EasingType ease)
	{
		return Ease ( t, ease, true);
	}
	private static float Ease(float t, EasingType ease, bool easeIn)
	{
		return Ease ( t, ease, easeIn, true);
	}
	private static float Ease(float t, EasingType ease, bool easeIn, bool easeOut)
	{
		t = Mathf.Clamp01(t);
		if(easeIn && easeOut)
		{
			t = Easing.EaseInOut(t, ease);
		} else if(easeIn)
		{
			t = Easing.EaseIn(t, ease);
		} else if(easeOut)
		{
			t = Easing.EaseOut(t, ease);
		}
		return t;
	}
	
	public static Vector3 InterpConstantSpeed(Path pts, float t)
	{
		return InterpConstantSpeed( pts, t, EasingType.Linear);
	}
	public static Vector3 InterpConstantSpeed(Path pts, float t, EasingType ease)
	{
		return InterpConstantSpeed( pts, t, ease, true);
	}
	public static Vector3 InterpConstantSpeed(Path pts, float t, EasingType ease, bool easeIn)
	{
		return InterpConstantSpeed( pts, t, ease, easeIn, true);
	}
	public static Vector3 InterpConstantSpeed(Path pts, float t, EasingType ease, bool easeIn, bool easeOut )
	{
		t = Ease(t, ease, easeIn, easeOut);
		
		if (pts.Length == 0)
		{
			return Vector3.zero;
		}
		else
		if (pts.Length == 1)
		{
			return pts[0];
		}
		else
		if (pts.Length == 2)
		{
			return Vector3.Lerp(pts[0], pts[1], t);
		}
		else
		if (pts.Length == 3)
		{
			return QuadBez.Interp(pts[0], pts[2], pts[1], t);
		}
		else
		if (pts.Length == 4)
		{
			return CubicBez.Interp(pts[0], pts[3], pts[1], pts[2], t); 
		}
		else
		{
			return CRSpline.InterpConstantSpeed(Wrap(pts), t);
		}
		
		
	}
	
	static float Clamp(float f)
	{
		
		return Mathf.Clamp01(f);
	}
	
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, 1f);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, float maxSpeed)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, maxSpeed, 100);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, float maxSpeed, float smoothnessFactor)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, maxSpeed, smoothnessFactor, EasingType.Linear);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, float maxSpeed, float smoothnessFactor, EasingType ease)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, maxSpeed, smoothnessFactor, ease, true);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, float maxSpeed, float smoothnessFactor, EasingType ease, bool easeIn)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, maxSpeed, smoothnessFactor, ease, easeIn, true);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, float maxSpeed, float smoothnessFactor, EasingType ease, bool easeIn, bool easeOut)
	{
		maxSpeed *= Time.deltaTime;
		pathPosition = Clamp(pathPosition);
		var goal = InterpConstantSpeed(pts, pathPosition, ease, easeIn, easeOut);
		float distance;
		while((distance = (goal - currentPosition).magnitude) <= maxSpeed && pathPosition != 1)
		{
			//currentPosition = goal;
			//maxSpeed -= distance;
			pathPosition = Clamp(pathPosition + 1/smoothnessFactor);
			goal = InterpConstantSpeed(pts, pathPosition, ease, easeIn, easeOut);
		}
		if(distance != 0)
		{
			currentPosition = Vector3.MoveTowards(currentPosition, goal, maxSpeed);
		}
		return currentPosition;
	}
	
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, ref Quaternion rotation)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, ref rotation, 1f);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, ref Quaternion rotation,  float maxSpeed)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, ref rotation, maxSpeed, 100);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, ref Quaternion rotation,  float maxSpeed, float smoothnessFactor)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, ref rotation, maxSpeed, smoothnessFactor, EasingType.Linear);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, ref Quaternion rotation,  float maxSpeed, float smoothnessFactor, EasingType ease)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, ref rotation, maxSpeed, smoothnessFactor, ease, true);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, ref Quaternion rotation,  float maxSpeed, float smoothnessFactor, EasingType ease, bool easeIn)
	{
		return MoveOnPath( pts, currentPosition, ref pathPosition, ref rotation, maxSpeed, smoothnessFactor, ease, easeIn, true);
	}
	public static Vector3 MoveOnPath(Path pts, Vector3 currentPosition, ref float pathPosition, ref Quaternion rotation,  float maxSpeed, float smoothnessFactor, EasingType ease, bool easeIn, bool easeOut)
	{
		var result = MoveOnPath(pts, currentPosition, ref pathPosition, maxSpeed, smoothnessFactor, ease, easeIn, easeOut);
		
		rotation =  result.Equals(currentPosition) ? Quaternion.identity : Quaternion.LookRotation(result - currentPosition);
		return result;
	}
	
	public static Quaternion RotationBetween(Path pts, float t1, float t2)
	{
		return RotationBetween( pts, t1, t2, EasingType.Linear);
	}
	public static Quaternion RotationBetween(Path pts, float t1, float t2, EasingType ease)
	{
		return RotationBetween( pts, t1, t2, ease, true);
	}
	public static Quaternion RotationBetween(Path pts, float t1, float t2, EasingType ease, bool easeIn)
	{
		return RotationBetween( pts, t1, t2, ease, easeIn, true);
	}
	public static Quaternion RotationBetween(Path pts, float t1, float t2, EasingType ease, bool easeIn, bool easeOut)
	{
		return Quaternion.LookRotation(Interp(pts, t2, ease, easeIn, easeOut) - Interp(pts, t1, ease, easeIn, easeOut));
	}
	
	
	public static Vector3 Velocity(Path pts, float t) 
	{
		return Velocity ( pts, t, EasingType.Linear);
	}
	public static Vector3 Velocity(Path pts, float t, EasingType ease) 
	{
		return Velocity ( pts, t, ease, true);
	}
	public static Vector3 Velocity(Path pts, float t, EasingType ease, bool easeIn ) 
	{
		return Velocity ( pts, t, ease, easeIn, true);
	}
	public static Vector3 Velocity(Path pts, float t, EasingType ease, bool easeIn, bool easeOut) 
	{
		t = Ease(t);
		if(pts.Length == 0)
		{
			return Vector3.zero;
		}
		else if(pts.Length ==1 )
		{
			return pts[0];
		}
		else if(pts.Length == 2)
		{
			return Vector3.Lerp(pts[0], pts[1], t);
		}
		else if(pts.Length == 3)
		{
			return QuadBez.Velocity(pts[0], pts[2], pts[1], t);
		}
		else if(pts.Length == 3)
		{
			return CubicBez.Velocity(pts[0], pts[3], pts[1], pts[2], t); 
		}
		else
		{
			return CRSpline.Velocity(Wrap(pts), t);
		}
	}
	
	public static Vector3[] Wrap(Vector3[] path)
	{
		return (new Vector3[] { path[0] }).Concat(path).Concat(new Vector3[] { path[path.Length-1]}).ToArray();
	}
		
	public static void GizmoDraw( Vector3[] pts, float t) 
	{
		GizmoDraw( pts, t, EasingType.Linear);
	}
	public static void GizmoDraw( Vector3[] pts, float t, EasingType ease) 
	{
		GizmoDraw( pts, t, ease, true);
	}
	public static void GizmoDraw( Vector3[] pts, float t, EasingType ease, bool easeIn) 
	{
		GizmoDraw( pts, t, ease, easeIn, true);
	}
	public static void GizmoDraw( Vector3[] pts, float t, EasingType ease, bool easeIn, bool easeOut) 
	{
		Gizmos.color = Color.white;
		Vector3 prevPt = Interp(pts, 0);
		
		for (int i = 1; i <= 20; i++) {
			float pm = (float) i / 20f;
			Vector3 currPt = Interp(pts, pm, ease, easeIn, easeOut);
			Gizmos.DrawLine(currPt, prevPt);
			prevPt = currPt;
		}
		
		Gizmos.color = Color.blue;
		Vector3 pos = Interp(pts, t, ease, easeIn, easeOut);
		Gizmos.DrawLine(pos, pos + Velocity(pts, t, ease, easeIn, easeOut));
	}
	
	public class Path
	{
		private Vector3[] _path;
		
		public Vector3[] path
		{
			get
			{
				return _path;
			}
			set
			{
				_path = value;
			}
		}
		
		public int Length
		{
			get
			{
				return path != null ? path.Length : 0;
			}
		}
		
		public Vector3 this[int index]
		{
			get
			{
				return path[index];
			}
		}
		
		public static implicit operator Path(Vector3[] path)
		{
			return new Path() {path = path};
		}
		public static implicit operator Vector3[](Path p)
		{
			return p != null ? p.path : new Vector3[0];
		}
		public static implicit operator Path(Transform[] path)
		{
			return new Path() {path = path.Select(p=>p.position).ToArray()};
		}
		public static implicit operator Path(GameObject[] path)
		{
			return new Path() {path = path.Select(p=>p.transform.position).ToArray()};
		}
		
		
	}

}
}

