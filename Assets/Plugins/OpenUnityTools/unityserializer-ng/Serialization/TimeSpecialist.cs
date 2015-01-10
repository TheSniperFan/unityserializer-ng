using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;

[SpecialistProvider]
public class TimeAsFloat : ISpecialist
{
	#region ISpecialist implementation
	public object Serialize (object value)
	{
		return (float)value - Time.time;
	}

	public object Deserialize (object value)
	{
		return Time.time + (float)value;
	}
	#endregion

}


