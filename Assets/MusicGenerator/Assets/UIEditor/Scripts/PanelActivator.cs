using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProcGenMusic
{
	public class PanelActivator : MonoBehaviour
	{
		private RaycastHit2D mRaycast;
		private int mPanelLayer;

		void Awake()
		{
			mPanelLayer = LayerMask.NameToLayer("TransparentFX");
		}
		void Update()
		{
			mRaycast = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), -Vector3.forward, 100.0f, 1 << mPanelLayer);
			if (mRaycast)
			{
				RaycasterToggle mRaycaster = mRaycast.collider.gameObject.GetComponent<RaycasterToggle>();
				if (mRaycaster != null)
				{
					mRaycaster.ToggleRaycaster(true);
				}
			}
		}
	}
}