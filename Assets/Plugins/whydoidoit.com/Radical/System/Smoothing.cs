using UnityEngine;
using System.Collections;
using System;
using Serialization;

namespace RadicalLibrary
{
	
	public enum SmoothingMode
	{
		slerp = 3,
		damp = 1,
		lerp = 2,
		smooth = 0
	}
	
	
	[SerializeAll]
	public class SmoothVector3
	{
		
		public EasingType Ease = EasingType.Linear;
		
		public SmoothVector3()
		{
		}
		
		public override string ToString ()
		{
			return string.Format ("[SmoothVector3: IsComplete={0}, Target={1}, Current={2}, x={3}, y={4}, z={5}, Value={6}]", IsComplete, Target, Current, x, y, z, Value);
		}
		
		public SmoothVector3(float x, float y, float z) : this (new Vector3(x,y,z))
		{
		}
		
		public SmoothVector3(Vector3 value)
		{
			_current = value;
			_target = value;
			_current = value;
			_start = value;
			_startTime = Time.time;
		}
		
		public bool IsComplete
		{
			get
			{
				return ((Time.time - _startTime) / Duration)>=1;
			}
		}
		
		public SmoothingMode Mode = SmoothingMode.lerp;
		
		private Vector3 _target;
		public Vector3 Target
		{
			get
			{
				return _target;
			}
		}
		private Vector3 _start;
		private Vector3 _velocity;
		private float _startTime;
		private Vector3 _current;
		
		
		public float Duration = 0.1f;
		public Vector3 Current
		{
			get
			{
				return _current;
			}
			set
			{
				_current = value;
				_start = value;
			}
		}
		
		public bool Lock;
			
		public float x
		{
			get
			{
				return Current.x;
			}
			set
			{
				Value = new Vector3(value, _target.y, _target.z);
			}
		}
		
		public float y
		{
			get
			{
				return Current.y;
			}
			set
			{
				Value = new Vector3(_target.x, value, _target.y);
			}
		}
		
		public float z
		{
			get
			{
				return Current.z;
			}
			set
			{
				Value = new Vector3(_target.x, _target.y, value);
			}
				
		}
		
		public Vector3 Value
		{
			get
			{
				if(Duration == 0)
				{
					Duration = 0.1f;
				}
				if(_startTime == 0)
				{
					_startTime = Time.time;
				}
				var t = Easing.EaseInOut(((double)(Time.time - _startTime) / (double)Duration), Ease, Ease);
				switch(Mode)	
				{
					case SmoothingMode.damp:
						_current = Vector3.SmoothDamp(_current, _target, ref _velocity, Duration,float.PositiveInfinity, Time.time - _startTime);
						_startTime = Time.time;
						break;
					case SmoothingMode.lerp:
						_current = Vector3.Lerp(_start, _target, t);
						break;
					case SmoothingMode.slerp:
						_current = Vector3.Slerp(_start, _target, t);
						break;
					case SmoothingMode.smooth:
						_current = new Vector3(Mathf.SmoothStep(_start.x, _target.x, t), Mathf.SmoothStep(_start.y, _target.y, t),
							Mathf.SmoothStep(_start.z, _target.z, t));
						break;
				}
				if(Lock)
				{
					if(_target.x == _start.x)
					{
						_current.x = _target.x;
					}
					if(_target.y == _start.y)
					{
						_current.y = _target.y;
					}
					if(_target.x == _start.z)
					{
						_current.z = _target.z;
					}
				}
				return _current;
			}
			set
			{
				if(value.Equals(_target))
					return;
				
				_start = Value;
				_startTime =Time.time;
				_target = value;
			}
		}
		
		public static implicit operator Vector3(SmoothVector3 obj)
		{
			return obj.Value;
		}
		
		public static implicit operator SmoothVector3(Vector3 obj)
		{
			return new SmoothVector3(obj);
		}
		
	}
	
	[SerializeAll]
	public class SmoothFloat
	{
		public SmoothingMode Mode = SmoothingMode.lerp;
		public EasingType Ease = EasingType.Linear;
	
		
		public override string ToString ()
		{
			return string.Format ("[SmoothFloat: Value={0}]", Current);
		}
		
		public SmoothFloat()
		{
		}
		
		private float _target;
		private float _start;
		private float _velocity;
		private float _startTime;
		
		public float Duration = 0.5f;
		private float _current;
		[DoNotSerialize]
		public float Current
		{
			get
			{
				return _current;
			}
			set
			{
				_current = value;
				_target =value;
			}
		}
		
		public float Target
			
		{
			get
			{
				return _target;
			}
		}
		
		public SmoothFloat(float f)
		{
			Current = f;
			_start = f;
			_velocity = 0;
			_target = f;
			_startTime = Time.time;
		}
		
		public float Value
		{
			get
			{
				if(Duration == 0)
					Duration = 0.5f;
				if(_startTime == 0)
				{
					_startTime = Time.time;
				}
				var t = Mathf.Clamp01(Easing.EaseInOut(Mathf.Clamp01((Time.time - _startTime) / Duration), Ease, Ease));
				
				switch(Mode)	
				{
					case SmoothingMode.damp:
						_current = Mathf.SmoothDamp(Current, _target, ref _velocity, Duration, float.PositiveInfinity, Time.time-_startTime);
						_startTime = Time.time;
						break;
					case SmoothingMode.lerp:
						_current = Mathf.Lerp(_start, _target, t);
						break;
					case SmoothingMode.slerp:
						_current =Mathf.Lerp(_start, _target, t);
						break;
					case SmoothingMode.smooth:
						_current = Mathf.SmoothStep(_start, _target, t);
						break;
				}
				
				return Current;
			}
			set
			{
				if(value.Equals(_target))
					return;
	
				_start = Value;
				_startTime =Time.time;
				_target = value;
			}
		}
		
		public static implicit operator float(SmoothFloat obj)
		{
			return obj.Value;
		}
		
		public static implicit operator SmoothFloat(float f)
		{
			return new SmoothFloat(f);
		}
		
		public bool IsComplete
		{
			get
			{
				return ((Time.time - _startTime) / Duration)>=1;
			}
		}
		
	
	
	}
	
	[SerializeAll]
	public class SmoothQuaternion
	{
		public SmoothingMode Mode = SmoothingMode.slerp;
		public EasingType Ease = EasingType.Linear;
	
		
		public SmoothQuaternion()
		{
		}
		
		private Quaternion _target;
		private Quaternion _start;
		private Vector3 _velocity;
		private float _startTime;
		
		public float Duration = 0.2f;
		public Quaternion Current;
		
		public SmoothQuaternion(Quaternion q)
		{
			Current = q;
			_start = q;
			_target = q;
			_startTime = Time.time;
		}
		
		public Quaternion Value
		{
			get
			{
				if(Duration ==0)
				{
					Duration = 0.1f;
				}
				if(_startTime == 0)
				{
					_startTime = Time.time;
				}
				var t = Easing.EaseInOut(((Time.time - _startTime) / Duration), Ease, Ease);
				
				switch(Mode)	
				{
					case SmoothingMode.damp:
						Current = Quaternion.Euler(Vector3.SmoothDamp(Current.eulerAngles, _target.eulerAngles, ref _velocity, Duration, float.PositiveInfinity, Time.time - _startTime));
						_startTime = Time.time;
						break;
					case SmoothingMode.lerp:
						
						Current = Quaternion.Lerp(_start, _target, t);
						break;
					case SmoothingMode.slerp:
						Current = Quaternion.Slerp(_start, _target, t);
						break;
					case SmoothingMode.smooth:
						Current = Quaternion.Euler(new Vector3(Mathf.SmoothStep(_start.x, _target.x, t), Mathf.SmoothStep(_start.y, _target.y, t),
							Mathf.SmoothStep(_start.z, _target.z, t)));
						break;
				}
				
				return Current;
			}
			set
			{
				if(value.Equals(_target))
				{
					return;
				}
				_start = Value;
				_startTime =Time.time;
				_target = value;
			}
		}
		
		public static implicit operator Quaternion(SmoothQuaternion obj)
		{
			return obj.Value;
		}
		
		public static implicit operator SmoothQuaternion(Quaternion q)
		{
			return new SmoothQuaternion(q);
		}
		
		
		public float x
		{
			get
			{
				return Current.x;
			}
			set
			{
				Value = new Quaternion(value, _target.y, _target.z, _target.w);
			}
		}
		
		public float y
		{
			get
			{
				return Current.y;
			}
			set
			{
				Value = new Quaternion(_target.x, value, _target.y, _target.w);
			}
		}
		
		public float z
		{
			get
			{
				return Current.z;
			}
			set
			{
				Value = new Quaternion(_target.x, _target.y, value, _target.w);
			}
				
		}
		
		public float w
		{
			get
			{
				return Current.w;
			}
			set
			{
				
				Value = new Quaternion(_target.x, _target.y, _target.z, value);
			}
		}
		
		public bool IsComplete
		{
			get
			{
				return ((Time.time - _startTime) / Duration)>=1;
			}
		}
		
	
	}
}