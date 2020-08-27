using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// Our actual tooltip object to display
	public class TooltipObject : MonoBehaviour
	{
		public string mDescription = "";
		public bool mIsHovered = false;
		public RectTransform mParentRect = null;
		public Text mTooltipText = null;
		public GameObject mTooltipObject = null;

		public void Init()
		{
			Component[] components = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components)
			{
				if (cp.name == "Text")
					mTooltipText = cp.GetComponent<Text>();
				if (cp.name == "TooltipObject")
					mTooltipObject = cp.gameObject;
			}
			mTooltipText.text = mDescription;
			mTooltipObject.SetActive(false);
		}
	}

	public class Tooltips : HelperSingleton<Tooltips>
	{
		public GameObject mTooltipBase = null;
		private Dictionary<string, string> mTooltips = new Dictionary<string, string>();
		[SerializeField]
		private Canvas mTooltipCanvas = null;

		private TooltipObject mTooltipBaseObject;
		private List<Tooltip> mTooltipsObjects = new List<Tooltip>();

		public override void Awake()
		{
			base.Awake();
		}
#if !UNITY_EDITOR && UNITY_ANDROID
		public IEnumerator Init()
		{
			TooltipSave save = null;
			yield return StartCoroutine(MusicFileConfig.LoadTooltips((x) =>
			{
				save = x;
				for (int i = 0; i < save.mTooltips.Count; i++)
				{
					mTooltips.Add(save.mTooltips[i].mTooltips[0], save.mTooltips[i].mTooltips[1]);
				}
				GameObject tooltip = Instantiate(mTooltipBase, Vector3.zero, Quaternion.identity, mTooltipCanvas.transform);
				mTooltipBaseObject = tooltip.AddComponent<TooltipObject>();
				mTooltipBaseObject.Init();
			}));
			yield return null;
			/// For saving the tooltips:
			//MusicGenerator.Instance.mConfigurations.SaveTooltips("tooltips", save);
		}
#else
		public void Init()
		{
			TooltipSave save = MusicFileConfig.LoadTooltips();
			for (int i = 0; i < save.mTooltips.Count; i++)
			{
				mTooltips.Add(save.mTooltips[i].mTooltips[0], save.mTooltips[i].mTooltips[1]);
			}
			GameObject tooltip = Instantiate(mTooltipBase, Vector3.zero, Quaternion.identity, mTooltipCanvas.transform);
			mTooltipBaseObject = tooltip.AddComponent<TooltipObject>();
			mTooltipBaseObject.Init();
			/// For saving the tooltips:
			//MusicGenerator.Instance.mConfigurations.SaveTooltips("tooltips", save);
		}
#endif //!UNITY_EDITOR && UNITY_ANDROID

		public void AddTooltip(string jsonIndexIN, RectTransform parentIN)
		{
			Tooltip tooltip = parentIN.gameObject.AddComponent<Tooltip>();
			tooltip.Init(this, mTooltips[jsonIndexIN], parentIN);
			mTooltipsObjects.Add(tooltip);
		}

		/// This will handle creating references for the tooltip / ui element (i.e. slider, dropdown, etc.)
		public void AddUIElement<T>(ref T objIN, Component cp, string nameIN)where T : Component
		{
			objIN = cp.gameObject.GetComponentInChildren<T>();
			if (objIN == null)
				objIN = cp.gameObject.GetComponent<T>();

			AddTooltip(nameIN, objIN.gameObject.GetComponent<RectTransform>());
		}

		void Update()
		{
			if (Input.GetKey("left shift"))
			{
				for (int i = 0; i < mTooltipsObjects.Count; i++)
				{
					if (mTooltipsObjects[i].IsHovered())
					{
						mTooltipsObjects[i].Show();
						return;
					}
				}
			}
			else
			{
				HideTooltip();
			}
		}

		public void ShowTooltip(string description, Vector3 position)
		{
			mTooltipBaseObject.mTooltipObject.SetActive(true);
			mTooltipBaseObject.transform.position = position;
			mTooltipBaseObject.mTooltipText.text = description;
		}

		public void HideTooltip()
		{
			if (mTooltipBaseObject != null && mTooltipBaseObject.mTooltipObject.activeSelf)
				mTooltipBaseObject.mTooltipObject.SetActive(false);
		}
	}
}