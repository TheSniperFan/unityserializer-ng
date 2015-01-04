using UnityEngine;
using System.Collections;

public class VariablesToSave : MonoBehaviour {
	
	public const int SomeValue = 1;
	
	public struct SomeStruct
	{
		public int value;
	}
	
	public SomeStruct myStruct;
	
	static VariablesToSave()
	{
		DelegateSupport.RegisterFunctionType<VariablesToSave, string>(); 
		DelegateSupport.RegisterFunctionType<VariablesToSave, bool>(); 
		DelegateSupport.RegisterFunctionType<VariablesToSave, int>(); 
	}

	
	private static int _randomNumber = UnityEngine.Random.Range(10,200);
	
	public static int RandomNumber
	{
		get
		{
			return _randomNumber;
		}
		set
		{
			_randomNumber = value;
		}
	}
	public string oneVariable;
	public int anotherVariable;
	
	public static bool hasInitialized;
	
	void Awake()
	{
		if(!hasInitialized)
		{
			hasInitialized = true;
			useMe = true;
			myStruct.value = UnityEngine.Random.Range(0,100000);
		}
	}
	
	public bool useMe;
	
	void Update()
	{
	}
	
}
