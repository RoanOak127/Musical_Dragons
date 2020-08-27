using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProcGenMusic
{
	public class HoverObj : MonoBehaviour
	{
		public Vector2 index = new Vector2(0, 0);
		public bool isOver = false;
		void Update()
		{
			isOver = false;
		}
	}
}