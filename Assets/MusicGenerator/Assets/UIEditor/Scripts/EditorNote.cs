using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// For the measure editor. staff player notes that know if they're clicked on.
	public class EditorNote : MonoBehaviour
	{
		public SpriteRenderer mBaseImage = null;
		public Vector2 index = new Vector2(0, 0);
	}
}