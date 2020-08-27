using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Just a helper class for the staff player notes.
public class StaffPlayerNote : MonoBehaviour {
	public SpriteRenderer mBaseImage = null;
	void Awake()
	{
		Component[] components =	GetComponentsInChildren (typeof(SpriteRenderer),true);
		foreach (Component cp in components)
		{
			if(cp.name == "noteImage")
				mBaseImage = cp.GetComponent<SpriteRenderer>();
		}
	}
}
