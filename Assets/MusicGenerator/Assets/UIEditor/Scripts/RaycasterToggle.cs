using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProcGenMusic
{
	public class RaycasterToggle : MonoBehaviour
	{
		[SerializeField, Tooltip("Our GraphicRaycaster to toggle")]
		private CanvasGroup mCanvasGroup;

		private bool mIsEnabled = false;

		private SpriteRenderer mBackgroundSprite;

		[SerializeField, Tooltip("Our highlighted color")]
		private Color mHighlightedColor;

		[SerializeField, Tooltip("Our disabled color")]
		private Color mDisabledColor;

		void Awake()
		{
			mBackgroundSprite = GetComponent<SpriteRenderer>();
			Color color = mBackgroundSprite.material.color;
			mDisabledColor = color;
		}

		void Start()
		{
			mCanvasGroup.interactable = false;
			mCanvasGroup.blocksRaycasts = false;
		}

		public void ToggleRaycaster(bool isEnabled)
		{
			mIsEnabled = isEnabled;
		}

		void Update()
		{
			if (mIsEnabled && mCanvasGroup.interactable == false)
			{
				mCanvasGroup.interactable = true;
				mCanvasGroup.blocksRaycasts = true;
				mBackgroundSprite.material.color = mHighlightedColor;
			}
			else if (mIsEnabled == false && mCanvasGroup.interactable)
			{
				mBackgroundSprite.material.color = mDisabledColor;
				mCanvasGroup.interactable = false;
				mCanvasGroup.blocksRaycasts = false;
			}
			// this is set true by the panelActivator every fram if we're hovered. This will toggle it off automatically.
			mIsEnabled = false;
		}
	}
}