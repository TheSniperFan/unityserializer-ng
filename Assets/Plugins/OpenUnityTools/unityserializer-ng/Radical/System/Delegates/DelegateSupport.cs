using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;
using System.Reflection;
using System.Linq.Expressions;

/// <summary>
/// A class that runs delegates using acceleration
/// </summary>
public static class DelegateSupport
{
	private static Dictionary<MethodInfo, Type> delegateTypes = new Dictionary<MethodInfo, Type>();
	private static Dictionary<MethodInfo, Delegate> openDelegates = new Dictionary<MethodInfo, Delegate>();
	public static Delegate ToOpenDelegate(MethodInfo mi)
	{
		Delegate result;
	
		
		if(!openDelegates.TryGetValue(mi, out result))
		{
			Type delegateType;
			if(!delegateTypes.TryGetValue(mi, out delegateType))
			{
			
			    var typeArgs = mi.GetParameters()
			        .Select(p => p.ParameterType)
			        .ToList();
				typeArgs.Insert(0, mi.DeclaringType);
			    // builds a delegate type
			    if (mi.ReturnType == typeof(void)) {
			        delegateType = Expression.GetActionType(typeArgs.ToArray());
			
			    } else {
			        typeArgs.Add(mi.ReturnType);
			        delegateType = Expression.GetFuncType(typeArgs.ToArray());
			    }
				delegateTypes[mi] = delegateType;
			}
			openDelegates[mi] = result =  Delegate.CreateDelegate(delegateType, mi);
		}
		
		return result;
	     
	}
	
	public static Delegate ToDelegate(MethodInfo mi, object target)
	{
	    Delegate result;
	    Type delegateType;
		
		
		
		if(!delegateTypes.TryGetValue(mi, out delegateType))
		{
		
		    var typeArgs = mi.GetParameters()
		        .Select(p => p.ParameterType)
		        .ToList();
		
		    // builds a delegate type
		    if (mi.ReturnType == typeof(void)) {
		        delegateType = Expression.GetActionType(typeArgs.ToArray());
		
		    } else {
		        typeArgs.Add(mi.ReturnType);
		        delegateType = Expression.GetFuncType(typeArgs.ToArray());
		    }
			delegateTypes[mi] = delegateType;
		}
		
	    // creates a binded delegate if target is supplied
	    result =  Delegate.CreateDelegate(delegateType, target, mi);
	
	
	    return result;
	}
	
	
	
	
	public class Index<TK, TR> : Dictionary<TK,TR>  where TR : class, new()
	{
		public new TR this[TK index]
		{
			get
			{
				TR val;
				if(!(TryGetValue(index, out val)))
				{
					base[index] = val = new TR();
				}
				return val;
			}
			set
			{
				base[index] = value;
			}
		}
	}

	
	private static Index<Type, Dictionary<string, Func<object, object[], Delegate, object>>> _functions = new Index<Type, Dictionary<string, Func<object, object[], Delegate,object>>>();
	private static Dictionary<MethodInfo, Func<object, object[], object>> _methods = new Dictionary<MethodInfo, Func<object, object[], object>>();
	
	/// <summary>
	/// Registers an action type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterActionType<TType>()
	{
		_functions[typeof(TType)][GetTypes(typeof(void))] = (object target, object[] parms, Delegate @delegate)=>{
			((Action<TType>)@delegate)((TType)target); return null;
		};
	}
	
	/// <summary>
	/// Registers an action type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterActionType<TType, T1>()
	{
		_functions[typeof(TType)][GetTypes(typeof(T1), typeof(void))] = (object target, object[] parms, Delegate @delegate)=>{
			((Action<TType, T1>)@delegate)((TType)target, (T1)parms[0]); return null;
		};
	}
	

	/// <summary>
	/// Registers an action type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterActionType<TType, T1, T2, T3>()
	{
		_functions[typeof(TType)][GetTypes(typeof(T1), typeof(T2), typeof(T3), typeof(void))] = (object target, object[] parms, Delegate @delegate)=>{
			((Action<TType,T1,T2,T3>)@delegate)((TType)target, (T1)parms[0], (T2)parms[1], (T3)parms[2]); return null;
		};
	}

	
	/// <summary>
	/// Registers an action type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterActionType<TType, T1, T2>()
	{
		_functions[typeof(TType)][GetTypes(typeof(T1), typeof(T2), typeof(void))] = (object target, object[] parms, Delegate @delegate)=>{
			((Action<TType, T1, T2>)@delegate)((TType)target, (T1)parms[0], (T2)parms[1]); return null;
		};
	}

	/// <summary>
	/// Registers a function type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterFunctionType<TType, TReturns>()
	{
		_functions[typeof(TType)][GetTypes(typeof(TReturns))] = (object target, object[] parms, Delegate @delegate)=>{
			return (object)((Func<TType, TReturns>)@delegate)((TType)target);
		};
	}
	
	/// <summary>
	/// Registers a function type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterFunctionType<TType, T1, TReturns>()
	{
		_functions[typeof(TType)][GetTypes(typeof(T1), typeof(TReturns))] = (object target, object[] parms, Delegate @delegate)=>{
			return (object)((Func<TType, T1, TReturns>)@delegate)((TType)target, (T1)parms[0]);
		};
	}
	
	/// <summary>
	/// Registers a function type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterFunctionType<TType, T1, T2, TReturns>()
	{
		_functions[typeof(TType)][GetTypes(typeof(T1), typeof(T2), typeof(TReturns))] = (object target, object[] parms, Delegate @delegate)=>{
			return (object)((Func<TType, T1, T2, TReturns>)@delegate)((TType)target, (T1)parms[0], (T2)parms[1]);
		};
	}
	
	/// <summary>
	/// Registers a function type for acceleration
	/// </summary>
	/// <typeparam name='TType'>
	/// The type to accelerate
	/// </typeparam>
	public static void RegisterFunctionType<TType, T1, T2, T3, TReturns>()
	{
		_functions[typeof(TType)][GetTypes(typeof(T1), typeof(T2), typeof(T3), typeof(TReturns))] = (object target, object[] parms, Delegate @delegate)=>{
			return (object)((Func<TType, T1, T2, T3, TReturns>)@delegate)((TType)target, (T1)parms[0], (T2)parms[1], (T3)parms[2]);
		};
	}
	
	/// <summary>
	/// Invokes the method at high speed
	/// </summary>
	/// <returns>
	/// The result of the invocation
	/// </returns>
	/// <param name='mi'>
	/// The method to invoke
	/// </param>
	/// <param name='target'>
	/// The target on which to invoke it
	/// </param>
	/// <param name='parameters'>
	/// The parameters to pass to the method
	/// </param>
	public static object FastInvoke(this MethodInfo mi, object target, params object[] parameters)
	{
		Func<object, object[], object> getter;
		string types;
		if(!_methods.TryGetValue(mi, out getter))
		{
			if(!mi.IsStatic && _functions.ContainsKey(mi.DeclaringType) && _functions[mi.DeclaringType].ContainsKey(types = GetTypes(mi)))
			{
				var @delegate = ToOpenDelegate(mi);
				var inner = _functions[mi.DeclaringType][types];
				var complete = _methods[mi] = (t,p) => inner(t, p, @delegate);
				return complete(target, parameters);
			}
			return mi.Invoke(target, parameters);
		}
		else
		{
			return getter(target, parameters);
		}
	}
	
	static string GetTypes(params Type[] types)
	{
		return types.Select(t=>t.FullName).Aggregate("", (v,n)=>v += n);
	}
	
	static string GetTypes(MethodInfo mi)
	{
		return GetTypes(mi.GetParameters().Select(p=>p.ParameterType).Concat(new Type[] {mi.ReturnType}).ToArray());
	}
       

}


