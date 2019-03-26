using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WormsClone
{
	public class Team
	{
		public int Current = 0;
		public List<WormObject> Worms = new List<WormObject>();
		public List<string> WormNames = new List<string>();
		public Color TeamColor;

		public WormObject GetNextWorm()	//Do zmiany
		{
			Current++;
			if (Current >= Worms.Count)
				Current = 0;

			return Worms[Current];
		}
	}
}