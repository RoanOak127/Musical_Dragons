using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// Object in the Instrument panel list to represent an instrument:
	public class InstrumentListUIObject : MonoBehaviour
	{
		private MusicGenerator mMusicGenerator = null;
		public Instrument mInstrument = null;
		private InstrumentPanelUI mInstrumentPanelUI = null;
		private Image mSelectIcon = null;
		private InstrumentListPanelUI mInstrumentListPanelUI = null;
		public Toggle mMuteToggle = null;
		private bool mbIsSelected = false;
		public Image mPanelBack = null;
		public Text mGroupText = null;
		private Toggle mSoloToggle = null;
		private Tooltips mTooltips = null;
		private MeasureEditor mMeasureEditor = null;
		public Dropdown mInstrumentDropdown = null;
		public Image mColorButton = null;

		public void Init(MusicGenerator managerIN)
		{
			mMusicGenerator = managerIN;
			mTooltips = UIManager.Instance.mTooltips;
			mMeasureEditor = UIManager.Instance.mMeasureEditor;
			mInstrumentPanelUI = UIManager.Instance.mInstrumentPanelUI;
			mInstrumentListPanelUI = UIManager.Instance.mInstrumentListPanelUI;

			Component[] components = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components)
			{
				if (cp.name == "SelectButton")
				{
					mSelectIcon = cp.gameObject.GetComponentInChildren<Image>();
					mSelectIcon.color = Color.white;
					mTooltips.AddTooltip("Edit", mSelectIcon.GetComponent<RectTransform>());
				}
				if (cp.name == "MuteToggle")
				{
					mMuteToggle = cp.gameObject.GetComponentInChildren<Toggle>();
					mTooltips.AddTooltip("Mute", mMuteToggle.GetComponent<RectTransform>());
				}
				if (cp.name == "GroupText")
				{
					mGroupText = cp.gameObject.GetComponent<Text>();
					mTooltips.AddTooltip("Group", mGroupText.GetComponent<RectTransform>());
				}
				if (cp.name == "SoloToggle")
				{
					mSoloToggle = cp.gameObject.GetComponentInChildren<Toggle>();
					mTooltips.AddTooltip("Solo", mSoloToggle.GetComponent<RectTransform>());
				}
				if (cp.name == "ColorButton")
				{
					mPanelBack = cp.gameObject.GetComponent<Image>();
				}
			}
			mTooltips.AddTooltip("InstrumentDropdown", GetComponentInChildren<Dropdown>().GetComponent<RectTransform>());
		}

		/// Sets the instrument dropdown option
		public void SetDropdown(bool isPercussion = false)
		{
			mInstrumentDropdown = GetComponentInChildren<Dropdown>();
			for (int i = 0; i < mMusicGenerator.BaseInstrumentPaths.Count; i++)
			{
				if ((isPercussion && mMusicGenerator.BaseInstrumentPaths[i].Contains("P_") == false) ||
					(isPercussion == false && mMusicGenerator.BaseInstrumentPaths[i].Contains("P_")))
					continue;

				Dropdown.OptionData data = new Dropdown.OptionData();
				data.text = mMusicGenerator.BaseInstrumentPaths[i];
				mInstrumentDropdown.options.Add(data);
			}

			/// workaround to make sure we get a percussion instrument
			if (isPercussion && !mInstrument.mData.InstrumentType.Contains("p_"))
				mInstrumentDropdown.value = 0;

			for (int i = 0; i < mInstrumentDropdown.options.Count; i++)
			{
				if (mInstrumentDropdown.options[i].text.ToLower() == mInstrument.mData.InstrumentType)
				{
					mInstrumentDropdown.value = i;
				}
			}
		}

		/// Destroys instrument.
		public void RemoveInstrument()
		{
			InstrumentSet set = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ? MeasureEditor.Instance.mCurrentInstSet : mMusicGenerator.mInstrumentSet;
			mMusicGenerator.RemoveInstrument((int)mInstrument.InstrumentIndex, set);
			mInstrumentListPanelUI.RemoveInstrument((int)mInstrument.InstrumentIndex);
			if (mbIsSelected)
				mInstrumentPanelUI.mInstrument = null;
			Destroy(this.gameObject);
		}

		/// Changes the instrument.
		/// if we're using async, we handle this differently as a new instrument may need to be loaded.
		public void ChangeInstrument()
		{
			if (mMusicGenerator.UseAsyncLoading)
				StartCoroutine(AsyncInstrumentChange());
			else
			{
				NonAsyncInstrumentChange();
			}
		}

		/// non async changes the instrument.
		private bool NonAsyncInstrumentChange()
		{
			if (mMusicGenerator.mState < eGeneratorState.ready)
			{
				return true;
			}

			InstrumentSet set = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ?
				MeasureEditor.Instance.mCurrentInstSet : mMusicGenerator.mInstrumentSet;

			int typeToRemove = (int)mInstrument.InstrumentTypeIndex;
			int instrumentIndex = 0;
			string name = mInstrumentDropdown.options[mInstrumentDropdown.value].text;
			if (name == "dummyEntry")
				return false;
			string instrumentType = mInstrumentDropdown.options[mInstrumentDropdown.value].text;

			instrumentType = instrumentType.ToLower();

			if (mMusicGenerator.LoadedInstrumentNames.Contains(instrumentType) == false)
				instrumentIndex = mMusicGenerator.LoadBaseClips(instrumentType);
			else
				instrumentIndex = mMusicGenerator.LoadedInstrumentNames.IndexOf(instrumentType);

			List<Instrument> instruments = set.mInstruments;

			int value = GetComponentInChildren<Dropdown>().value;
			if (mInstrument != null)
			{
				instruments[(int)mInstrument.InstrumentIndex].InstrumentTypeIndex = instrumentIndex;
				instruments[(int)mInstrument.InstrumentIndex].mData.InstrumentType = instrumentType;
				mMuteToggle.isOn = instruments[(int)mInstrument.InstrumentIndex].mData.mIsMuted;
			}
			mMusicGenerator.RemoveBaseClip(typeToRemove, set);
			mMusicGenerator.CleanUpInstrumentTypeIndices(set);
			return true;
		}

		/// Asynchronously changes the instrument (new music assets may need to be loaded).
		private IEnumerator AsyncInstrumentChange()
		{
			if (mMusicGenerator.mState < eGeneratorState.ready)
			{
				yield break;
			}
			InstrumentSet set = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ?
				MeasureEditor.Instance.mCurrentInstSet : mMusicGenerator.mInstrumentSet;

			int typeToRemove = (int)mInstrument.InstrumentTypeIndex;
			int instrumentIndex = 0;
			string name = mInstrumentDropdown.options[mInstrumentDropdown.value].text;
			if (name == "dummyEntry")
				yield return null;
			string instrumentType = mInstrumentDropdown.options[mInstrumentDropdown.value].text;

			instrumentType = instrumentType.ToLower();

			if (mMusicGenerator.LoadedInstrumentNames.Contains(instrumentType) == false)
			{
				yield return StartCoroutine(MusicGenerator.Instance.AsyncLoadBaseClips(instrumentType, ((x) => { instrumentIndex = x; })));
			}
			else
				instrumentIndex = mMusicGenerator.LoadedInstrumentNames.IndexOf(instrumentType);

			List<Instrument> instruments = set.mInstruments;

			int value = GetComponentInChildren<Dropdown>().value;
			if (mInstrument != null)
			{
				instruments[(int)mInstrument.InstrumentIndex].InstrumentTypeIndex = instrumentIndex;
				instruments[(int)mInstrument.InstrumentIndex].mData.InstrumentType = instrumentType;
				mMuteToggle.isOn = instruments[mInstrument.InstrumentIndex].mData.mIsMuted;
			}
			mMusicGenerator.RemoveBaseClip(typeToRemove, set);
			mMusicGenerator.CleanUpInstrumentTypeIndices(set);
			yield return null;
		}

		void Update()
		{
			///just makes sure the panel mute reflects this mute:
			if (mMuteToggle.isOn)
			{
				List<Instrument> instruments = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ?
					MeasureEditor.Instance.mCurrentInstSet.mInstruments : mMusicGenerator.mInstrumentSet.mInstruments;

				instruments[(int)mInstrument.InstrumentIndex].mData.mIsSolo = false;
				mSoloToggle.isOn = false;
			}
		}

		/// Toggles the mute button. adjusts solo buttons.
		public void ToggleMute(Toggle toggleIN)
		{
			List<Instrument> instruments = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ?
				MeasureEditor.Instance.mCurrentInstSet.mInstruments : mMusicGenerator.mInstrumentSet.mInstruments;
			int instrumentIndex = (int)mInstrument.InstrumentIndex;
			if (toggleIN.isOn)
				instruments[instrumentIndex].mData.mIsMuted = true;
			else
			{
				instruments[instrumentIndex].mData.mIsMuted = false;
				for (int i = 0; i < mInstrumentListPanelUI.mInstrumentIcons.Count; i++)
				{
					if (i != mInstrument.InstrumentIndex)
						mInstrumentListPanelUI.mInstrumentIcons[i].mSoloToggle.isOn = false;
				}
			}

			if (mbIsSelected)
				mInstrumentPanelUI.SetMute(toggleIN.isOn);
		}

		//Toggles instrument 'selected'/'unselected' for editing in the instrument panel
		public void ToggleSelected()
		{
			List<InstrumentListUIObject> instrumentsIcons = mInstrumentListPanelUI.mInstrumentIcons;
			for (int i = 0; i < instrumentsIcons.Count; i++)
			{
				instrumentsIcons[i].mSelectIcon.color = Color.white;
				instrumentsIcons[i].mbIsSelected = false;
			}
			mbIsSelected = true;

			mInstrumentPanelUI.SetInstrument(mInstrument);
			mSelectIcon.color = Color.red;
			if (mMusicGenerator.mState >= eGeneratorState.editorInitializing)
				mMeasureEditor.ChangeInstrument(mInstrument);
		}

		/// toggles whether this instrument is playing solo
		public void Solo()
		{
			bool isOn = mSoloToggle.isOn;
			List<Instrument> instruments = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ?
				MeasureEditor.Instance.mCurrentInstSet.mInstruments : mMusicGenerator.mInstrumentSet.mInstruments;
			Instrument instrument = instruments[(int)mInstrument.InstrumentIndex];
			List<InstrumentListUIObject> instrumentIcons = mInstrumentListPanelUI.mInstrumentIcons;

			instrument.mData.mIsSolo = isOn;
			if (isOn)
			{
				mMuteToggle.isOn = false;
				instrument.mData.mIsMuted = false;
			}

			if (isOn) //if we're on we mute all the other instruments
			{
				for (int i = 0; i < instrumentIcons.Count; i++)
				{
					if (i != mInstrument.InstrumentIndex)
					{
						instruments[i].mData.mIsMuted = true;
						instrumentIcons[i].mMuteToggle.isOn = true;
						instrumentIcons[i].mSoloToggle.isOn = false;

						if (i == mInstrumentPanelUI.mInstrument.InstrumentIndex)
							mInstrumentPanelUI.mMuteToggle.isOn = true;
					}
				}
			}
			else
			{
				//check to see if something else is solo
				bool otherSolo = false;
				for (int i = 0; i < instruments.Count; i++)
				{
					if (instruments[i].mData.mIsSolo)
						otherSolo = true;
				}
				if (!otherSolo)
				{
					for (int i = 0; i < instruments.Count; i++)
					{
						instruments[i].mData.mIsMuted = false;
						instrumentIcons[i].mMuteToggle.isOn = false;
						instrumentIcons[i].mSoloToggle.isOn = false;
					}
				}
			}
		}
	}
}