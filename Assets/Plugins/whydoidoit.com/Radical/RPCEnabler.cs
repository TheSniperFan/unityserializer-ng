// /* ------------------

//       (c) whydoidoit.com 2012
//           by Mike Talbot 
//     ------------------- */
// 
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Serialization;

//Class that enables calling a routine in a type safe manner and having
//it decide how to pass the call across the network
public static class RPCEnabler
{
	
	
	public static bool Others(this NetworkView networkView, string routineName, params object[] parameters)
	{
		
		if (!networkView.isMine)
		{
		
			networkView.RPC(routineName, RPCMode.Others, parameters);
		}
		return !networkView.isMine;
		
	}
	
	public static bool Server(this NetworkView networkView, string routineName, params object[] parameters)
	{
		networkView.RPC(routineName, RPCMode.Server, parameters);
		return Network.isServer;
	}
	

}
