using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// UI for instrument list
	public class InstrumentListPanelUI : HelperSingleton<InstrumentListPanelUI>
	{
		public List<InstrumentListUIObject> mInstrumentIcons { get; private set; }
		private MusicGenerator mMusicGenerator = null;
		private RectTransform mAddInstrumentPoint = null;
		private Vector3 mBaseAddInstrumentPos;
		[SerializeField]
		private float mIconPadding = 1.05f;
		[SerializeField]
		private GameObject mInstrumentUIObjectBase = null;

		public override void Awake()
		{
			base.Awake();
			CreateInstrumentUIObjectBase();
			mInstrumentIcons = new List<InstrumentListUIObject>();
		}

		public void Init(MusicGenerator managerIN)
		{
			mMusicGenerator = managerIN;
			Tooltips tooltiops = UIManager.Instance.mTooltips;
			Component[] components = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components)
			{
				if (cp.name == "AddInstrumentPoint")
				{
					mAddInstrumentPoint = cp.gameObject.GetComponent<RectTransform>();
					mBaseAddInstrumentPos = mAddInstrumentPoint.localPosition;
				}
				if (cp.name == "NewInstrumentButton")
				{
					tooltiops.AddTooltip("NewInstrument", cp.gameObject.GetComponent<RectTransform>());
				}
			}
		}

		/// creates our base ui object to instantiate other instruments.
		private void CreateInstrumentUIObjectBase()
		{
			string platform = "/Windows";
			if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
				platform = "/Linux";
			else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
				platform = "/Mac";
			else if (Application.platform == RuntimePlatform.IPhonePlayer)
				platform = "/IOS";
			string path = null;
			string fileName = "instrumentuiobject";
#if !UNITY_EDITOR && UNITY_IOS
			fileName = "instrumentuiobject";
			path = Application.streamingAssetsPath + "/MusicGenerator/IOS/instrumentuiobject";
#elif !UNITY_EDITOR && UNITY_ANDROID
			fileName = "instrumentuiobject";
			path = Application.streamingAssetsPath + "/MusicGenerator/Android/instrumentuiobject";
#else
			path = Application.streamingAssetsPath + "/MusicGenerator" + platform + "/instrumentuiobject";
#endif 

			var myLoadedAssetBundle = AssetBundle.LoadFromFile(path);

			if (myLoadedAssetBundle != null)
				mInstrumentUIObjectBase = myLoadedAssetBundle.LoadAsset<GameObject>(fileName);
			else
				throw new System.ArgumentNullException("InstrumentUIObject base file does not exist.");
		}

		/// Adds an instrument to our ui object list:
		public void AddInstrument(Instrument instrumentIN)
		{
			InstrumentSet set = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ? MeasureEditor.Instance.mCurrentInstSet : mMusicGenerator.mInstrumentSet;
			List<Instrument> instruments = set.mInstruments;
			if (instruments.Count <= MusicGenerator.mMaxInstruments)
			{
				mInstrumentIcons.Add((Instantiate(mInstrumentUIObjectBase, transform)as GameObject).GetComponent<InstrumentListUIObject>());
				InstrumentListUIObject icon = mInstrumentIcons[mInstrumentIcons.Count - 1];
				icon.Init(mMusicGenerator);
				icon.transform.position = mAddInstrumentPoint.transform.position;
				mAddInstrumentPoint.localPosition -= new Vector3(0, mAddInstrumentPoint.rect.height * mIconPadding, 0);
				icon.mInstrument = instrumentIN;
				Color color = StaffPlayerUI.Instance.mColors[(int)icon.mInstrument.mData.mStaffPlayerColor];
				icon.mPanelBack.color = color;
			}
		}

		/// Adds an instrument to the Music generator and creates its ui object.
		public void AddMusicGeneratorInstrument(bool isPercussion)
		{
			InstrumentSet set = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ? MeasureEditor.Instance.mCurrentInstSet : mMusicGenerator.mInstrumentSet;
			List<Instrument> instruments = set.mInstruments;
			if (instruments.Count < MusicGenerator.mMaxInstruments)
			{
				mMusicGenerator.AddInstrument(set);
				Instrument instrument = set.mInstruments[set.mInstruments.Count - 1];
				AddInstrument(instrument);
				InstrumentListUIObject icon = mInstrumentIcons[mInstrumentIcons.Count - 1];

				icon.mInstrument = instruments[instruments.Count - 1];
				Color color = StaffPlayerUI.Instance.mColors[(int)icon.mInstrument.mData.mStaffPlayerColor];
				icon.mPanelBack.color = color;

				icon.SetDropdown(isPercussion);
			}
		}

		/// Loads a new instrument from UI "AddNewInstrument" button.
		public void LoadNewInstrument(Instrument instrumentIN, Color colorIN)
		{
			bool isPercussion = instrumentIN.mData.InstrumentType.Contains("p_") ? true : false;
			AddInstrument(instrumentIN);
			int iconIndex = mInstrumentIcons.Count - 1;
			InstrumentListUIObject icon = mInstrumentIcons[iconIndex];
			icon.mInstrument = instrumentIN;
			icon.ToggleSelected();
			icon.SetDropdown(isPercussion);
			int instIndex = (int)instrumentIN.InstrumentIndex;
			mInstrumentIcons[instIndex].mGroupText.text = ("Group: " + (instrumentIN.mData.Group + 1).ToString());
			mInstrumentIcons[instIndex].mPanelBack.color = colorIN;
		}

		/// Removes an instrument from our list. Fixes icon positions:
		public void RemoveInstrument(int indexIN)
		{
			for (int i = indexIN; i < mInstrumentIcons.Count; i++)
			{
				mAddInstrumentPoint.localPosition +=
					new Vector3(0, mAddInstrumentPoint.rect.height * mIconPadding, 0);
			}
			for (int i = indexIN + 1; i < mInstrumentIcons.Count; i++)
			{
				mInstrumentIcons[i].transform.position = mAddInstrumentPoint.transform.position;
				mAddInstrumentPoint.localPosition -=
					new Vector3(0, mAddInstrumentPoint.rect.height * mIconPadding, 0);
			}
			Destroy(mInstrumentIcons[indexIN].gameObject);
			mInstrumentIcons.RemoveAt(indexIN);
			InstrumentPanelUI.Instance.SetInstrument(null);
		}

		/// deletes all ui instrument objects.
		public void ClearInstruments()
		{
			if (mInstrumentIcons.Count == 0)
				return;
			for (int i = mInstrumentIcons.Count - 1; i >= 0; i--)
			{
				Destroy(mInstrumentIcons[i].gameObject);
			}
			mInstrumentIcons.Clear();
			mAddInstrumentPoint.localPosition = mBaseAddInstrumentPos;
		}
	}
}