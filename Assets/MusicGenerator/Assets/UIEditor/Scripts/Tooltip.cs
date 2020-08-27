using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// tooltip class. Basically just a frame sprite with some text info that knows whether the mouse is hovered over or not.
	public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
	{
		public string mDescription = "";
		public bool mIsHovered = false;
		private float mXOffset = 100.0f;
		private float mYOffset = 100.0f;
		public RectTransform mParentRect = null;
		private Tooltips mParent;
		Vector3 mOffsetPosition = Vector3.zero;
		Vector3 mPosition = Vector3.zero;
		private bool mIsShown = false;

		public void Init(Tooltips parent, string description, RectTransform parentRect)
		{
			mParent = parent;
			mDescription = description;
			mParentRect = parentRect;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			mIsHovered = true;
		}
		
		public void Show()
		{
			mIsShown = true;
			mOffsetPosition.x = (Input.mousePosition.x > Screen.width / 2) ? -mXOffset : mXOffset;
			mOffsetPosition.y = (Input.mousePosition.y > Screen.height / 2) ? -mYOffset : mYOffset;
			mPosition = Input.mousePosition + mOffsetPosition;
			mParent.ShowTooltip(mDescription, mPosition);
		}

		public bool IsHovered()
		{
			return (mIsHovered && mIsShown == false);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			mIsHovered = false;
			mIsShown = false;
			mParent.HideTooltip();
		}
	}
}