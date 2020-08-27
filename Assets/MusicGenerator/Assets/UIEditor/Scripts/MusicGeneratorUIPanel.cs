using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// Music generator user interface panel.
	public class MusicGeneratorUIPanel : HelperSingleton<MusicGeneratorUIPanel>
	{
		private Dropdown mMode = null;
		private Dropdown mScale = null;
		public Dropdown mKey = null;
		private Slider mTempo = null;
		private Text mTempoText = null;
		private Slider mVol = null;
		private Text mVolText = null;
		private Dropdown mRepeatThemeOptions = null;
		private Dropdown mRepeatLength = null;
		private MusicGenerator mMusicGenerator = null;
		public Dropdown mProgressionRateDropdown = null;
		private Slider mGroupOdds1 = null;
		private Text mGroupOdds1Text = null;
		private Slider mGroupOdds2 = null;
		private Text mGroupOdds2Text = null;
		private Slider mGroupOdds3 = null;
		private Text mGroupOdds3Text = null;
		private Slider mGroupOdds4 = null;
		private Text mGroupOdds4Text = null;
		private Slider mNewThemeOdds = null;
		private Text mNewThemeOutput = null;
		private Slider mRepeatThemeOdds = null;
		private Text mRepeatThemeOutput = null;
		private Slider mKeyChangeOdds = null;
		private Text mKeyChangeOddsOutput = null;
		private Slider mProgressionChangeOdds = null;
		private Text mProgressionChangeOutput = null;

		private InstrumentListPanelUI mInstrumentListPanelUI = null;
		private InstrumentPanelUI mInstrumentPanelUI = null;
		private StaffPlayerUI mStaffPlayerUI = null;
		[SerializeField]
		public List<string> mPresetFileNames = null;

		public void SetKey(int keyIN) { mKey.value = keyIN; }

		private Animator mAnimator = null;
		private AdvancedSettingsPanel mAdvSettingsPanel = null;
		private GlobalEffectsPanel mGlobalEffectsPanel = null;
		private MeasureEditor mMeasureEditor = null;
		private Tooltips mTooltips = null;
		[SerializeField]

		private string mFileCurrentlyWriting = "";

#if !UNITY_EDITOR && UNITY_ANDROID
		public IEnumerator Init(MusicGenerator managerIN, System.Action<bool> callback)
		{
			LoadReferences(managerIN);

			mInstrumentListPanelUI.Init(mMusicGenerator);
			yield return StartCoroutine(AddPresets());
			FinishInitialization();
			callback(true);
			yield return null;
		}
#else
		public void Init(MusicGenerator managerIN)
		{
			LoadReferences(managerIN);
			mInstrumentListPanelUI.Init(mMusicGenerator);
			AddPresets();
			FinishInitialization();
		}
#endif //!UNITY_EDITOR && UNITY_ANDROID

		private void LoadReferences(MusicGenerator managerIN)
		{
			mMusicGenerator = managerIN;
			mTooltips = UIManager.Instance.mTooltips;
			mAdvSettingsPanel = UIManager.Instance.mAdvancedSettingsPanel;
			mGlobalEffectsPanel = UIManager.Instance.mGlobalEffectsPanel;
			mInstrumentPanelUI = UIManager.Instance.mInstrumentPanelUI;
			mInstrumentListPanelUI = UIManager.Instance.mInstrumentListPanelUI;
			mStaffPlayerUI = UIManager.Instance.mStaffPlayer;
			mAnimator = GetComponentInParent<Animator>();
			mMeasureEditor = UIManager.Instance.mMeasureEditor;
		}

		private void FinishInitialization()
		{
			Component[] components = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components)
			{
				if (cp.name == "Mode")
					mTooltips.AddUIElement(ref mMode, cp, "Mode");
				if (cp.name == "Scale")
					mTooltips.AddUIElement(ref mScale, cp, "Scale");
				if (cp.name == "Tempo")
					mTooltips.AddUIElement(ref mTempo, cp, "Tempo");
				if (cp.name == "TempoOutput")
					mTempoText = cp.GetComponentInChildren<Text>();
				if (cp.name == "Key")
					mTooltips.AddUIElement(ref mKey, cp, "Key");
				if (cp.name == "MasterVol")
					mTooltips.AddUIElement(ref mVol, cp, "MasterVol");
				if (cp.name == "ProgressionRate")
					mTooltips.AddUIElement(ref mProgressionRateDropdown, cp, "ProgressionRate");
				if (cp.name == "RepeatLength")
					mTooltips.AddUIElement(ref mRepeatLength, cp, "RepeatLength");
				if (cp.name == "NewThemeOdds")
					mTooltips.AddUIElement(ref mNewThemeOdds, cp, "NewThemeOdds");
				if (cp.name == "RepeatThemeOdds")
					mTooltips.AddUIElement(ref mRepeatThemeOdds, cp, "ThemeRepeat");
				if (cp.name == "KeyChange")
					mTooltips.AddUIElement(ref mKeyChangeOdds, cp, "KeyChangeOdds");
				if (cp.name == "ProgressionChange")
					mTooltips.AddUIElement(ref mProgressionChangeOdds, cp, "ProgressionChangeOdds");

				if (cp.name == "VolumeOutput")
					mVolText = cp.GetComponentInChildren<Text>();
				if (cp.name == "RepeatAndThemeOptions")
					mTooltips.AddUIElement(ref mRepeatThemeOptions, cp, "ThemeRepeat");
				if (cp.name == "GroupOdds1")
					mTooltips.AddUIElement(ref mGroupOdds1, cp, "GroupOdds");
				if (cp.name == "GroupOdds2")
					mTooltips.AddUIElement(ref mGroupOdds2, cp, "GroupOdds");
				if (cp.name == "GroupOdds3")
					mTooltips.AddUIElement(ref mGroupOdds3, cp, "GroupOdds");
				if (cp.name == "GroupOdds4")
					mTooltips.AddUIElement(ref mGroupOdds4, cp, "GroupOdds");

				if (cp.name == "Group1OddsOutput")
					mGroupOdds1Text = cp.GetComponentInChildren<Text>();
				if (cp.name == "Group2OddsOutput")
					mGroupOdds2Text = cp.GetComponentInChildren<Text>();
				if (cp.name == "Group3OddsOutput")
					mGroupOdds3Text = cp.GetComponentInChildren<Text>();
				if (cp.name == "Group4OddsOutput")
					mGroupOdds4Text = cp.GetComponentInChildren<Text>();
				if (cp.name == "NewThemeOutput")
					mNewThemeOutput = cp.GetComponentInChildren<Text>();
				if (cp.name == "RepeatThemeOutput")
					mRepeatThemeOutput = cp.GetComponentInChildren<Text>();
				if (cp.name == "KeyChangeOutput")
					mKeyChangeOddsOutput = cp.GetComponentInChildren<Text>();
				if (cp.name == "ProgressionChangeOutput")
					mProgressionChangeOutput = cp.GetComponentInChildren<Text>();
			}
			NonAsyncLoadUI();

			GetComponentInParent<CanvasGroup>().interactable = false;
			GetComponentInParent<CanvasGroup>().blocksRaycasts = false;
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		/// Loads a new configuration.
		private IEnumerator NonAsyncLoadNewConfiguration(string configName)
		{
			yield return (StartCoroutine(NonAsyncLoadConfig(configName)));
			NonAsyncLoadUI();
			mGlobalEffectsPanel.Init(mMusicGenerator);
			mMusicGenerator.SetState(eGeneratorState.ready);
			yield return null;
		}
#else
		/// Loads a new configuration.
		private void NonAsyncLoadNewConfiguration(string configName)
		{
			NonAsyncLoadConfig(configName);
			NonAsyncLoadUI();
			mGlobalEffectsPanel.Init(mMusicGenerator);
		} 
#endif //!UNITY_EDITOR && UNITY_ANDROID

#if !UNITY_EDITOR && UNITY_ANDROID
		private IEnumerator NonAsyncLoadConfig(string configName)
		{
			PrepareConfigLoading();
			yield return StartCoroutine(mMusicGenerator.mMusicFileConfig.LoadConfig(configName, eGeneratorState.initializing));
		}
#else // !UNITY_EDITOR && UNITY_ANDROID
		private void NonAsyncLoadConfig(string configName)
		{
			PrepareConfigLoading();
			mMusicGenerator.mMusicFileConfig.LoadConfig(configName, eGeneratorState.ready);
		}
#endif
		private void PrepareConfigLoading()
		{
			mMusicGenerator.ClearInstruments(mMusicGenerator.mInstrumentSet);
			mMusicGenerator.ResetPlayer();
			mMusicGenerator.SetState(eGeneratorState.initializing);
		}
		
		private void NonAsyncLoadUI()
		{
			for (int i = 0; i < mMusicGenerator.mInstrumentSet.mInstruments.Count; i++)
				LoadNewInstrument(mMusicGenerator.mInstrumentSet.mInstruments[i]);

			SetGeneratorUIValues();
			mInstrumentListPanelUI.mInstrumentIcons[0].ToggleSelected();
			mMusicGenerator.SetState(eGeneratorState.ready);
			mMusicGenerator.ResetPlayer();
		}

		/// Async loads a new configuration.
		private IEnumerator AsyncLoadNewConfiguration(string configName)
		{
			mStaffPlayerUI.PlayLoadingSequence(true);
			mMusicGenerator.ClearInstruments(mMusicGenerator.mInstrumentSet);
			mMusicGenerator.ResetPlayer();
			yield return null;
			mMusicGenerator.SetState(eGeneratorState.initializing);
			yield return StartCoroutine(mMusicGenerator.mMusicFileConfig.AsyncLoadConfig(configName, eGeneratorState.initializing));
			for (int i = 0; i < mMusicGenerator.mInstrumentSet.mInstruments.Count; i++)
			{
				LoadNewInstrument(mMusicGenerator.mInstrumentSet.mInstruments[i]);
				yield return null;
			}
			SetGeneratorUIValues();
			yield return null;
			mInstrumentListPanelUI.mInstrumentIcons[0].ToggleSelected();
			mMusicGenerator.SetState(eGeneratorState.ready);
			mStaffPlayerUI.PlayLoadingSequence(false);
			yield return null;
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		private IEnumerator AddPresets()
		{
			if (System.IO.Directory.Exists(Application.persistentDataPath + "/MusicGenerator/InstrumentSaves"))
			{
				foreach (string folder in System.IO.Directory.GetDirectories(Application.persistentDataPath + "/MusicGenerator/InstrumentSaves"))
				{
					if (mPresetFileNames.Contains(name) == false)
						mPresetFileNames.Add(name);
				}
			}
			string data = null;
			yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentSaves/presets.txt", (x) => { data = x.downloadHandler.text; });
			mPresetFileNames = new List<string>(data.Split(','));
			if (mPresetFileNames != null)
			{
				for (int i = 0; i < mPresetFileNames.Count; i++)
					mStaffPlayerUI.AddPresetOption(mPresetFileNames[i]);
			}

			yield return null;
		}
#else
		/// Adds files from persistentDataPath and streaming assets folder.
		private void AddPresets()
		{
			mPresetFileNames = new List<string>();

			string persistentDir = Application.persistentDataPath + "/InstrumentSaves/";
			string streamingDir = Application.streamingAssetsPath + "/MusicGenerator/InstrumentSaves/";

			if (Directory.Exists(persistentDir) == false)
			{
				Directory.CreateDirectory(Application.persistentDataPath + "/InstrumentSaves/");
			}
#if !UNITY_EDITOR && UNITY_IOS
			var info = new DirectoryInfo(streamingDir);
			var folders = info.GetDirectories();
			for (int index = 0; index < folders.Length; index++)
			{
				if (mPresetFileNames.Contains(folders[index].Name) == false)
				{
					mPresetFileNames.Add(folders[index].Name);
				}
			}
#else
			foreach (string folder in System.IO.Directory.GetDirectories(streamingDir))
			{
				string name = new DirectoryInfo(folder).Name;
				if (mPresetFileNames.Contains(name) == false)
				{
					mPresetFileNames.Add(name);
				}
			}
#endif

#if UNITY_EDITOR
			mMusicGenerator.mMusicFileConfig.ExportMobilePresets(mPresetFileNames);
#endif //UNITY_EDITOR
			foreach (string folder in System.IO.Directory.GetDirectories(persistentDir))
			{
				string name = new DirectoryInfo(folder).Name;
				if (!mPresetFileNames.Contains(name))
					mPresetFileNames.Add(name);
			}

			for (int i = 0; i < mPresetFileNames.Count; i++)
				mStaffPlayerUI.AddPresetOption(mPresetFileNames[i]);
		}

#endif // !UNITY_EDITOR && UNITY_ANDROID

		/// used to adjust the volume slider (currently, by fade in UIManager)
		public void FadeVolume(float volIN)
		{
			mVol.value = volIN;
		}

		/// Updates the Generator UI Panel:
		/// Sets the MusicGenerator() values, based on UI values.
		void Update()
		{
			if (mMusicGenerator == null || mMusicGenerator.mState < eGeneratorState.ready)
				return;

			/// Check to see if we're in the process of saving a new config and adds preset when it's finihsed.
			if (mFileCurrentlyWriting != "")
			{
				bool completed = MusicHelpers.CheckConfigWriteComplete(mFileCurrentlyWriting);
				if (completed)
				{
					Debug.Log(mFileCurrentlyWriting + " save complete");
					mStaffPlayerUI.AddPresetOption(mFileCurrentlyWriting);
					mFileCurrentlyWriting = "";
				}
			}

			/// update our generator values from UI sliders, dropdowns, etc:
			if (mMusicGenerator.mVolumeState == eVolumeState.idle)
			{
				if (mMusicGenerator.mGeneratorData.mMasterVolume != mVol.value)
				{
					mVolText.text = ((int)mVol.value).ToString();
					mMusicGenerator.mGeneratorData.mMasterVolume = mVol.value;
				}
			}

			mMusicGenerator.mGeneratorData.mThemeRepeatOptions = (eThemeRepeatOptions)mRepeatThemeOptions.value;

			if (mMusicGenerator.mGeneratorData.mProgressionChangeOdds != mProgressionChangeOdds.value)
			{
				mMusicGenerator.mGeneratorData.mProgressionChangeOdds = mProgressionChangeOdds.value;
				mProgressionChangeOutput.text = ((int)mMusicGenerator.mGeneratorData.mProgressionChangeOdds).ToString();
			}

			if (mMusicGenerator.mState < eGeneratorState.editing)
			{
				if (mMusicGenerator.mInstrumentSet.mData.Tempo != mTempo.value)
				{
					mMusicGenerator.mInstrumentSet.mData.Tempo = mTempo.value;
					mTempoText.text = ((int)mTempo.value).ToString();
				}

				mMusicGenerator.mGeneratorData.mScale = (eScale)mScale.value;
				mMusicGenerator.mGeneratorData.mMode = (eMode)mMode.value;
				mMusicGenerator.mGeneratorData.mKey = (eKey)mKey.value;
			}
			else
			{
				if (mMusicGenerator.mInstrumentSet.mData.Tempo != mMeasureEditor.mTempo.value)
				{
					mMusicGenerator.mInstrumentSet.mData.Tempo = mMeasureEditor.mTempo.value;
					mTempoText.text = ((int)mTempo.value).ToString();
				}

				mMusicGenerator.mGeneratorData.mMode = (eMode)mMeasureEditor.mMode.value;
				mMusicGenerator.mGeneratorData.mScale = (eScale)mMeasureEditor.mScale.value;
				mMusicGenerator.mGeneratorData.mKey = (eKey)mMeasureEditor.mKey.value;
			}

			if (mMusicGenerator.mGeneratorData.mKeyChangeOdds != mKeyChangeOdds.value)
			{
				mMusicGenerator.mGeneratorData.mKeyChangeOdds = mKeyChangeOdds.value;
				mKeyChangeOddsOutput.text = ((int)mKeyChangeOdds.value).ToString();
			}

			if (mMusicGenerator.mGeneratorData.mGroupOdds[0] != mGroupOdds1.value)
			{
				mMusicGenerator.mGeneratorData.mGroupOdds[0] = mGroupOdds1.value;
				mGroupOdds1Text.text = ((int)mGroupOdds1.value).ToString();
			}

			if (mMusicGenerator.mGeneratorData.mGroupOdds[1] != mGroupOdds2.value)
			{
				mMusicGenerator.mGeneratorData.mGroupOdds[1] = mGroupOdds2.value;
				mGroupOdds2Text.text = ((int)mGroupOdds2.value).ToString();
			}
			if (mMusicGenerator.mGeneratorData.mGroupOdds[2] != mGroupOdds3.value)
			{
				mMusicGenerator.mGeneratorData.mGroupOdds[2] = mGroupOdds3.value;
				mGroupOdds3Text.text = ((int)mGroupOdds3.value).ToString();
			}
			if (mMusicGenerator.mGeneratorData.mGroupOdds[3] != mGroupOdds4.value)
			{
				mMusicGenerator.mGeneratorData.mGroupOdds[3] = mGroupOdds4.value;
				mGroupOdds4Text.text = ((int)mGroupOdds4.value).ToString();
			}

			mMusicGenerator.mInstrumentSet.mData.RepeatMeasuresNum = mRepeatLength.value + 1;

			if (mMusicGenerator.mGeneratorData.mSetThemeOdds != mNewThemeOdds.value)
			{
				mMusicGenerator.mGeneratorData.mSetThemeOdds = mNewThemeOdds.value;
				mNewThemeOutput.text = ((int)mMusicGenerator.mGeneratorData.mSetThemeOdds).ToString();
			}
			if (mMusicGenerator.mGeneratorData.mPlayThemeOdds != mRepeatThemeOdds.value)
			{
				mMusicGenerator.mGeneratorData.mPlayThemeOdds = mRepeatThemeOdds.value;
				mRepeatThemeOutput.text = ((int)mMusicGenerator.mGeneratorData.mPlayThemeOdds).ToString();
			}

			mMusicGenerator.mInstrumentSet.SetProgressionRate(mProgressionRateDropdown.value);
		}

		/// Quits the application
		public void QuitGenerator()
		{
			Application.Quit();
		}

		/// Loads the configuration from UI presets dropdown.
		public void LoadConfigFromUI()
		{
			if (mMusicGenerator.UseAsyncLoading)
				StartCoroutine(AsyncLoadNewConfiguration(mPresetFileNames[mStaffPlayerUI.mPreset.value]));
			else
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				StartCoroutine(NonAsyncLoadNewConfiguration(mPresetFileNames[mStaffPlayerUI.mPreset.value]));
#else
				NonAsyncLoadNewConfiguration(mPresetFileNames[mStaffPlayerUI.mPreset.value]);
#endif //!UNITY_EDITOR && UNITY_ANDROID
			}
		}

		/// Loads a new instrument from UI "AddNewInstrument" button.
		private void LoadNewInstrument(Instrument instrumentIN)
		{
			mInstrumentListPanelUI.LoadNewInstrument(instrumentIN, mStaffPlayerUI.mColors[(int)instrumentIN.mData.mStaffPlayerColor]);
			int instIndex = (int)instrumentIN.InstrumentIndex;
			mInstrumentPanelUI.SetInstrument(mMusicGenerator.mInstrumentSet.mInstruments[instIndex]);
		}

		/// When initially setting, we grab our values from the generator:
		private void SetGeneratorUIValues()
		{
			mScale.value = (int)mMusicGenerator.mGeneratorData.mScale;
			mKey.value = (int)mMusicGenerator.mGeneratorData.mKey;
			mTempo.value = mMusicGenerator.mInstrumentSet.mData.Tempo;

			mStaffPlayerUI.ChangeTimeSignature((int)mMusicGenerator.mInstrumentSet.mTimeSignature.Signature);
			float volOUT = 0.0f;
			mMusicGenerator.mMixer.GetFloat("MasterVol", out volOUT);
			mVol.value = volOUT;

			mRepeatThemeOptions.value = (int)mMusicGenerator.mGeneratorData.mThemeRepeatOptions;

			mRepeatLength.value = (int)mMusicGenerator.mInstrumentSet.mData.RepeatMeasuresNum - 1;

			switch ((int)mMusicGenerator.mInstrumentSet.mData.mProgressionRate)
			{
				case 1:
					mProgressionRateDropdown.value = 0;
					break;
				case 2:
					mProgressionRateDropdown.value = 1;
					break;
				case 4:
					mProgressionRateDropdown.value = 2;
					break;
				case 8:
					mProgressionRateDropdown.value = 3;
					break;
				case 16:
					mProgressionRateDropdown.value = 4;
					break;
				default:
					break;
			}

			mGroupOdds1.value = mMusicGenerator.mGeneratorData.mGroupOdds[0];
			mGroupOdds2.value = mMusicGenerator.mGeneratorData.mGroupOdds[1];
			mGroupOdds3.value = mMusicGenerator.mGeneratorData.mGroupOdds[2];
			mGroupOdds4.value = mMusicGenerator.mGeneratorData.mGroupOdds[3];

			mNewThemeOdds.value = mMusicGenerator.mGeneratorData.mSetThemeOdds;

			mRepeatThemeOdds.value = mMusicGenerator.mGeneratorData.mPlayThemeOdds;
			mKeyChangeOdds.value = mMusicGenerator.mGeneratorData.mKeyChangeOdds;
			mMode.value = (int)mMusicGenerator.mGeneratorData.mMode;

			mAdvSettingsPanel.mTonicInfluence.value = mMusicGenerator.mChordProgressions.mData.TonicInfluence;
			mAdvSettingsPanel.mSubdominantInfluence.value = mMusicGenerator.mChordProgressions.mData.SubdominantInfluence;
			mAdvSettingsPanel.mDominantInfluence.value = mMusicGenerator.mChordProgressions.mData.DominantInfluence;
			mAdvSettingsPanel.mTritoneSubInfluence.value = mMusicGenerator.mChordProgressions.mData.TritoneSubInfluence;
			mAdvSettingsPanel.mAscendDescendKey.value = mMusicGenerator.mGeneratorData.mKeyChangeAscendDescend;
			mProgressionChangeOdds.value = mMusicGenerator.mGeneratorData.mProgressionChangeOdds;
			mAdvSettingsPanel.mGroupRate.value = (int)mMusicGenerator.mGeneratorData.mGroupRate;
			mAdvSettingsPanel.mDynamicStyle.value = (int)mMusicGenerator.mGeneratorData.mDynamicStyle;
			mAdvSettingsPanel.mVolumeFadeRate.value = (int)mMusicGenerator.mGeneratorData.mVolFadeRate;

			UpdateEffectsSliders();
			bool[] excludes = mMusicGenerator.mChordProgressions.mData.mExcludedProgSteps;
			for (int i = 0; i < excludes.Length; i++)
				mAdvSettingsPanel.mExcludedSteps[i].isOn = excludes[i];

			mAdvSettingsPanel.CheckAvoidSteps();

			mRepeatThemeOutput.text = ((int)mMusicGenerator.mGeneratorData.mPlayThemeOdds).ToString();
			mNewThemeOutput.text = ((int)mMusicGenerator.mGeneratorData.mSetThemeOdds).ToString();
			mGroupOdds4Text.text = ((int)mGroupOdds4.value).ToString();
			mGroupOdds3Text.text = ((int)mGroupOdds3.value).ToString();
			mGroupOdds2Text.text = ((int)mGroupOdds2.value).ToString();
			mGroupOdds1Text.text = ((int)mGroupOdds1.value).ToString();
			mKeyChangeOddsOutput.text = ((int)mKeyChangeOdds.value).ToString();
			mTempoText.text = ((int)mTempo.value).ToString();
			mTempoText.text = ((int)mTempo.value).ToString();
			mProgressionChangeOutput.text = ((int)mMusicGenerator.mGeneratorData.mProgressionChangeOdds).ToString();
			mVolText.text = ((int)mVol.value).ToString();
		}

		public void UpdateEffectsSliders()
		{
			mGlobalEffectsPanel.mDistortion.mGeneratorValue = mMusicGenerator.mGeneratorData.mDistortion;
			mGlobalEffectsPanel.mCenterFrequency.mGeneratorValue = mMusicGenerator.mGeneratorData.mCenterFreq;
			mGlobalEffectsPanel.mOctaveRange.mGeneratorValue = mMusicGenerator.mGeneratorData.mOctaveRange;
			mGlobalEffectsPanel.mFrequencyGain.mGeneratorValue = mMusicGenerator.mGeneratorData.mFreqGain;
			mGlobalEffectsPanel.mLowpassCutoffFreq.mGeneratorValue = mMusicGenerator.mGeneratorData.mLowpassCutoffFreq;
			mGlobalEffectsPanel.mLowpassResonance.mGeneratorValue = mMusicGenerator.mGeneratorData.mLowpassResonance;
			mGlobalEffectsPanel.mHighpassCutoffFreq.mGeneratorValue = mMusicGenerator.mGeneratorData.mHighpassCutoffFreq;
			mGlobalEffectsPanel.mHighpassResonance.mGeneratorValue = mMusicGenerator.mGeneratorData.mHighpassResonance;
			mGlobalEffectsPanel.mEchoDelay.mGeneratorValue = mMusicGenerator.mGeneratorData.mEchoDelay;
			mGlobalEffectsPanel.mEchoDecay.mGeneratorValue = mMusicGenerator.mGeneratorData.mEchoDecay;
			mGlobalEffectsPanel.mEchoDry.mGeneratorValue = mMusicGenerator.mGeneratorData.mEchoDry;
			mGlobalEffectsPanel.mEchoWet.mGeneratorValue = mMusicGenerator.mGeneratorData.mEchoWet;
			mGlobalEffectsPanel.mNumEchoChannels.mGeneratorValue = mMusicGenerator.mGeneratorData.mNumEchoChannels;
			mGlobalEffectsPanel.mReverb.mGeneratorValue = mMusicGenerator.mGeneratorData.mReverb;
			mGlobalEffectsPanel.mRoomSize.mGeneratorValue = mMusicGenerator.mGeneratorData.mRoomSize;
			mGlobalEffectsPanel.mReverbDecay.mGeneratorValue = mMusicGenerator.mGeneratorData.mReverbDecay;
			mGlobalEffectsPanel.ForceSliderUpdate();
		}
		/// Toggles the panel animation:
		public void GeneratorPanelToggle()
		{
			if (mAnimator.GetInteger("mState") == 0)
			{
				mAnimator.SetInteger("mState", 1);
				GetComponentInParent<CanvasGroup>().interactable = true;
				GetComponentInParent<CanvasGroup>().blocksRaycasts = true;
			}
			else
			{
				mAnimator.SetInteger("mState", 0);
				GetComponentInParent<CanvasGroup>().interactable = false;
				GetComponentInParent<CanvasGroup>().blocksRaycasts = false;
			}
		}
	}
}