﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// Measure editor class. Handles the logic for the measure editor
	/// Here be dragons.
	public class MeasureEditor : HelperSingleton<MeasureEditor>
	{
		private Animator mAnimator = null;
		private MusicGenerator mMusicGenerator = null;

		public Dropdown mNumberOfMeasures = null;
		public Dropdown mCurrentMeasure = null;
		public Dropdown mPlayClipDropdown = null;
		public Dropdown mProgressionRate = null;

		public Dropdown mMode = null;
		public Dropdown mScale = null;
		public Dropdown mKey = null;
		public Slider mTempo = null;
		public Toggle mClipIsRepeating = null;
		public Toggle mShowEditorHints = null;
		public Toggle mShowAllInstruments = null;
		public SingleClip mLoadedClip = null;
		private StaffPlayerUI mStaffPlayer = null;
		private InstrumentPanelUI mInstrumentPanel = null;
		private MusicGeneratorUIPanel mGeneratorPanel = null;
		public InstrumentSet mCurrentInstSet = null;
		public ClipSave mCurrenClipSave = null;
		private string mFileCurrentlyWriting = "";

		public void Init(MusicGenerator managerIN)
		{
			GetComponentInParent<CanvasGroup>().interactable = false;
			GetComponentInParent<CanvasGroup>().blocksRaycasts = false;
			mStaffPlayer = StaffPlayerUI.Instance;
			mInstrumentPanel = InstrumentPanelUI.Instance;
			mGeneratorPanel = MusicGeneratorUIPanel.Instance;

			mAnimator = GetComponentInParent<Animator>();
			mMusicGenerator = managerIN;
			Tooltips tooltips = UIManager.Instance.mTooltips;
			Component[] components = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components)
			{
				if (cp.name == "CurrentEditorMeasure")
					tooltips.AddUIElement<Dropdown>(ref mCurrentMeasure, cp, "CurrentEditorMeasure");
				if (cp.name == "PlayClipDropdown")
					tooltips.AddUIElement<Dropdown>(ref mPlayClipDropdown, cp, "PlayClipDropdown");
				if (cp.name == "ShowEditorHints")
					tooltips.AddUIElement<Toggle>(ref mShowEditorHints, cp, "ShowEditorHints");
				if (cp.name == "ShowAllInstruments")
					tooltips.AddUIElement<Toggle>(ref mShowAllInstruments, cp, "ShowAllInstruments");
				if (cp.name == "NumberOfMeasures")
					tooltips.AddUIElement<Dropdown>(ref mNumberOfMeasures, cp, "NumberOfMeasures");
				if (cp.name == "ProgressionRate")
					tooltips.AddUIElement<Dropdown>(ref mProgressionRate, cp, "ProgressionRate");
				if (cp.name == "Mode")
					tooltips.AddUIElement<Dropdown>(ref mMode, cp, "Mode");
				if (cp.name == "Scale")
					tooltips.AddUIElement<Dropdown>(ref mScale, cp, "Scale");
				if (cp.name == "Key")
					tooltips.AddUIElement<Dropdown>(ref mKey, cp, "Key");
				if (cp.name == "Tempo")
					tooltips.AddUIElement<Slider>(ref mTempo, cp, "Tempo");
				if (cp.name == "ClipRepeat")
					tooltips.AddUIElement<Toggle>(ref mClipIsRepeating, cp, "ClipRepeat");
			}
		}

		/// changes our selected instrument
		public void ChangeInstrument(Instrument instrumentIN)
		{
			if (mMusicGenerator.mState >= eGeneratorState.editorInitializing)
			{
				StaffPlayerUI staffPlayer = mStaffPlayer;
				staffPlayer.SetMeasure(instrumentIN);
				if (mShowEditorHints.isOn)
				{
					List<Instrument> instruments = mCurrentInstSet.mInstruments;
					int instrumentIndex = (int)mInstrumentPanel.mInstrument.InstrumentIndex;
					staffPlayer.ShowHighlightedNotes(instruments[instrumentIndex]);
				}
				UIToggleAllInstruments();
			}
		}

		/// sets the staff player measure, shows highlighted notes:
		public void SetMeasure()
		{
			if (mMusicGenerator.mState >= eGeneratorState.editorInitializing)
			{
				StaffPlayerUI staffPlayer = mStaffPlayer;
				Instrument instrument = mInstrumentPanel.mInstrument;
				staffPlayer.SetMeasure(instrument);

				if (mLoadedClip.mInstrumentSet != null && mShowEditorHints.isOn && mLoadedClip.mInstrumentSet.mInstruments.Count > 0)
				{
					List<Instrument> instruments = mLoadedClip.mInstrumentSet.mInstruments;
					staffPlayer.ShowHighlightedNotes(instruments[(int)instrument.InstrumentIndex]);
				}
				UIToggleAllInstruments();
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (mMusicGenerator == null)
			{
				return;
			}
			if (mFileCurrentlyWriting != "")
			{
				bool completed = MusicHelpers.CheckClipwriteComplete(mFileCurrentlyWriting);
				if (completed)
				{
					Debug.Log(mFileCurrentlyWriting + " clip save complete");
					AddPresetOption(mFileCurrentlyWriting + ".txt");
					mFileCurrentlyWriting = "";
					mPlayClipDropdown.value = mPlayClipDropdown.options.Count - 1;
				}
			}

			else if (mMusicGenerator.mState >= eGeneratorState.editorInitializing)
			{
				InstrumentSet instrumentSet = mLoadedClip.mInstrumentSet;
				instrumentSet.mData.Tempo = mTempo.value;
				mCurrentInstSet.mData.RepeatMeasuresNum = mNumberOfMeasures.value;
				mCurrentInstSet.mRepeatCount = mCurrentMeasure.value;
				if (mInstrumentPanel.mInstrument != null)
				{
					int currentInstIndex = (int)mInstrumentPanel.mInstrument.InstrumentIndex;
					mStaffPlayer.SetEditorNotes(instrumentSet.mInstruments[currentInstIndex]);
				}
			}
		}

		/// Updates the helper notes showing/not showing, whether mShowEditorHints.isOn.
		public void ToggleHelperNotes(Dropdown colorDropdownIN = null)
		{
			if (mMusicGenerator.mState >= eGeneratorState.editing)
			{
				if (mShowEditorHints.isOn)
				{
					if (mLoadedClip.mInstrumentSet.mInstruments.Count == 0)
					{
						return;
					}

					int currentInstIndex = (int)mInstrumentPanel.mInstrument.InstrumentIndex;
					if (colorDropdownIN != null)
						mLoadedClip.mInstrumentSet.mInstruments[currentInstIndex].mData.mStaffPlayerColor = (eStaffPlayerColors)colorDropdownIN.value;

					mStaffPlayer.ClearNotes(true, true);
					Instrument instrument = mLoadedClip.mInstrumentSet.mInstruments[currentInstIndex];
					mStaffPlayer.ShowHighlightedNotes(instrument);
					if (mShowAllInstruments.isOn)
					{
						InstrumentSet set = mLoadedClip.mInstrumentSet;
						for (int i = 0; i < set.mInstruments.Count; i++)
						{
							int[][] notes = set.mInstruments[i].mClipNotes[mCurrentMeasure.value];
							mStaffPlayer.ShowSetEditorNotes(notes, i);
						}
					}
					else
						mStaffPlayer.ShowSetEditorNotes(instrument.mClipNotes[mCurrentMeasure.value], instrument.InstrumentIndex);
				}
				else
					mStaffPlayer.ClearNotes(false, true);
			}
		}

		public void Stop()
		{
			mCurrentInstSet.Reset();
			mLoadedClip.mClipMeasure.ResetMeasure(mCurrentInstSet, null, true);
			mCurrentMeasure.value = 0;
			mCurrentInstSet.mRepeatCount = 0;
			ToggleHelperNotes();
		}

		/// Toggles our helper notes:
		public void UIToggleHelperNotes()
		{
			//due to the default parameter, my build of Unity loathes calling ToggleHelperNotes from the UI with no value passed in.
			//hence this workaround:
			StartCoroutine(FunWorkaroundForHelperNotesUI());
		}

		private IEnumerator FunWorkaroundForHelperNotesUI()
		{
			yield return new WaitForFixedUpdate();
			ToggleHelperNotes();
		}

		/// Toggles all the instruments not currently being edited
		public void UIToggleAllInstruments(bool clear = false)
		{
			StaffPlayerUI staffPlayer = mStaffPlayer;
			InstrumentPanelUI instrumentPanel = mInstrumentPanel;
			if (clear)
				staffPlayer.ClearNotes(true, false);
			if (mInstrumentPanel.mInstrument != null)
			{
				InstrumentSet set = mLoadedClip.mInstrumentSet;
				if (set == null || set.mInstruments.Count == 0)
					return;
				int currentInstrument = (int)set.mInstruments[(int)instrumentPanel.mInstrument.InstrumentIndex].InstrumentIndex;

				if (set.mInstruments.Count > currentInstrument)
				{
					int[][] clipNotes = set.mInstruments[currentInstrument].mClipNotes[mCurrentMeasure.value];
					if (mShowAllInstruments.isOn)
					{
						for (int i = 0; i < set.mInstruments.Count; i++)
						{
							int[][] notes = set.mInstruments[i].mClipNotes[mCurrentMeasure.value];
							staffPlayer.ShowSetEditorNotes(notes, i);
						}
					}
					else
						staffPlayer.ShowSetEditorNotes(clipNotes, currentInstrument);
				}
			}
		}

		/// Toggles the entire measure editor panel:
		public void MeasureEditorToggle()
		{
			if (mMusicGenerator.mState == eGeneratorState.initializing)
				return;

			StopAllCoroutines();
			bool isEditing = mAnimator.GetInteger("mState") == 0;
			mMusicGenerator.SetState(isEditing ? eGeneratorState.editorInitializing : eGeneratorState.stopped);
			mAnimator.SetInteger("mState", isEditing ? 4 : 0);
			mStaffPlayer.mPreset.gameObject.SetActive(isEditing ? false : true);
			GetComponentInParent<CanvasGroup>().interactable = isEditing;
			GetComponentInParent<CanvasGroup>().blocksRaycasts = isEditing;
			if (isEditing)
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				StartCoroutine(LoadPresets());
#else
				LoadPresets();
#endif //!UNITY_EDITOR && UNITY_ANDROID
				//StartCoroutine(AsyncStageClipForPlaying());
			}
			else
			{
				ClearCurrentInstruments();
				mGeneratorPanel.LoadConfigFromUI();
				mStaffPlayer.ClearNotes(true, true);
				mStaffPlayer.ChangeTimeSignature((int)mCurrentInstSet.mTimeSignature.Signature);
			}
		}

		//saves clip notes:
		public void SaveClip(string fileNameIN)
		{
			mCurrenClipSave = GetSaveClip();
			mFileCurrentlyWriting = fileNameIN;

			string stringSave = JsonUtility.ToJson(mCurrenClipSave);
			mMusicGenerator.mMusicFileConfig.SaveClipConfiguration(fileNameIN, stringSave);
			ExportMobilePresets();
		}

		private ClipSave GetSaveClip()
		{
			ClipSave clipsave = new ClipSave();

			clipsave.mTempo = mTempo.value;
			clipsave.mProgressionRate = mProgressionRate.value;
			clipsave.mNumberOfMeasures = mNumberOfMeasures.value;
			clipsave.mKey = MusicGenerator.Instance.mGeneratorData.mKey;
			clipsave.mScale = MusicGenerator.Instance.mGeneratorData.mScale;
			clipsave.mMode = MusicGenerator.Instance.mGeneratorData.mMode;
			clipsave.mClipIsRepeating = mClipIsRepeating.isOn;

			List<ClipInstrumentSave> instrumentSaves = clipsave.mClipInstrumentSaves;
			for (int i = 0; i < mCurrentInstSet.mInstruments.Count; i++)
			{
				ClipInstrumentSave save = new ClipInstrumentSave();
				Instrument instrument = mCurrentInstSet.mInstruments[i];
				/// unity isn't saving multidimensional lists, so we're converting here.
				for (int x = 0; x < instrument.mClipNotes.Length; x++)
				{
					save.mClipMeasures.Add(new ClipNotesMeasure());
					for (int y = 0; y < instrument.mClipNotes[x].Length; y++)
					{
						save.mClipMeasures[x].timestep.Add(new ClipNotesTimeStep());
						for (int z = 0; z < instrument.mClipNotes[x][y].Length; z++)
						{
							save.mClipMeasures[x].timestep[y].notes.Add(instrument.mClipNotes[x][y][z]);
						}
					}
				}

				save.mInstrumentType = instrument.mData.InstrumentType;
				save.mVolume = instrument.mData.Volume;
				save.mStaffPlayerColor = (int)instrument.mData.mStaffPlayerColor;
				save.mTimestep = instrument.mData.mTimeStep;
				save.mSuccessionType = instrument.mData.mSuccessionType;
				save.mStereoPan = instrument.mData.mStereoPan;

				instrumentSaves.Add(save);
			}

			return clipsave;
		}

		///async loads a music clip
		public IEnumerator AsyncLoadClip(System.Action<bool> callback, string fileName = "")
		{
			fileName = (fileName == "") ? "AAADefault.txt" : fileName;
			mLoadedClip = gameObject.AddComponent<SingleClip>();
#if !UNITY_EDITOR && UNITY_ANDROID
			yield return StartCoroutine(mMusicGenerator.mMusicFileConfig.LoadClipConfigurations(fileName, (x) => { mCurrenClipSave = x; }));
#else
			mCurrenClipSave = mMusicGenerator.mMusicFileConfig.LoadClipConfigurations(fileName);
#endif //!UNITY_EDITOR && UNITY_ANDROID

			mTempo.value = mCurrenClipSave.mTempo;
			mProgressionRate.value = mCurrenClipSave.mProgressionRate;
			mNumberOfMeasures.value = mCurrenClipSave.mNumberOfMeasures;
			mKey.value = (int)mCurrenClipSave.mKey;
			mScale.value = (int)mCurrenClipSave.mScale;
			mMode.value = (int)mCurrenClipSave.mMode;
			mClipIsRepeating.isOn = mCurrenClipSave.mClipIsRepeating;

			bool isFinished = false;
			StartCoroutine(mLoadedClip.AsyncInit(mCurrenClipSave, ((x) => { isFinished = x; })));
			yield return new WaitUntil(() => isFinished);
			callback(isFinished);
			yield return null;
		}

#if !UNITY_EDITOR && UNITY_ANDROID

		///loads a music clip
		public IEnumerator LoadClip(string fileName = "", System.Action<bool> callback = null)
		{
			fileName = (fileName == "") ? "AAADefault.txt" : fileName;
			GameObject go = new GameObject();
			go.name = fileName;
			go.AddComponent<SingleClip>();
			//go.transform.parent = transform;
			mLoadedClip = go.GetComponent<SingleClip>();
			yield return StartCoroutine(mMusicGenerator.mMusicFileConfig.LoadClipConfigurations(fileName, (x) => { mCurrenClipSave = x; }));

			mTempo.value = mCurrenClipSave.mTempo;
			mProgressionRate.value = mCurrenClipSave.mProgressionRate;
			mNumberOfMeasures.value = mCurrenClipSave.mNumberOfMeasures;
			mKey.value = (int)mCurrenClipSave.mKey;
			mScale.value = (int)mCurrenClipSave.mScale;
			mMode.value = (int)mCurrenClipSave.mMode;
			mClipIsRepeating.isOn = mCurrenClipSave.mClipIsRepeating;

			mLoadedClip.Init(mCurrenClipSave);

			callback(true);
			yield return null;
		}
#else
		///loads a music clip
		public void LoadClip(string fileName = "")
		{
			fileName = (fileName == "") ? "AAADefault.txt" : fileName;
			GameObject go = new GameObject();
			go.name = fileName;
			go.AddComponent<SingleClip>();
			//go.transform.parent = transform;
			mLoadedClip = go.GetComponent<SingleClip>();
			mCurrenClipSave = mMusicGenerator.mMusicFileConfig.LoadClipConfigurations(fileName);

			mTempo.value = mCurrenClipSave.mTempo;
			mProgressionRate.value = mCurrenClipSave.mProgressionRate;
			mNumberOfMeasures.value = mCurrenClipSave.mNumberOfMeasures;
			mKey.value = (int)mCurrenClipSave.mKey;
			mScale.value = (int)mCurrenClipSave.mScale;
			mMode.value = (int)mCurrenClipSave.mMode;
			mClipIsRepeating.isOn = mCurrenClipSave.mClipIsRepeating;

			mLoadedClip.Init(mCurrenClipSave);
		}
#endif //!UNITY_EDITOR && UNITY_ANDROID

		///stages a clip for playing. Async version (will load assets on the fly).
		public IEnumerator AsyncStageClipForPlaying()
		{
			ClearCurrentInstruments();

			bool isFinished = false;
			StartCoroutine(AsyncLoadClip(((x) => { isFinished = x; }), mPlayClipDropdown.options[mPlayClipDropdown.value].text));
			yield return new WaitUntil(() => isFinished);

			mCurrentInstSet = mLoadedClip.mInstrumentSet;

			InstrumentListPanelUI listPanel = InstrumentListPanelUI.Instance;

			for (int i = 0; i < mCurrentInstSet.mInstruments.Count; i++)
			{
				listPanel.AddInstrument(mCurrentInstSet.mInstruments[i]);
				InstrumentListUIObject icon = listPanel.mInstrumentIcons[listPanel.mInstrumentIcons.Count - 1];
				icon.mInstrument = mCurrentInstSet.mInstruments[i];
				icon.ToggleSelected();
				bool isPercussion = icon.mInstrument.mData.InstrumentType.Contains("p_") ? true : false;
				icon.SetDropdown(isPercussion);

				InstrumentPanelUI.Instance.SetInstrument(icon.mInstrument);
				mInstrumentPanel.mInstrument = mCurrentInstSet.mInstruments[i];
			}
			InstrumentPanelUI.Instance.SetInstrument(mCurrentInstSet.mInstruments[0]);
			mInstrumentPanel.mInstrument = mCurrentInstSet.mInstruments[0];

			mClipIsRepeating.isOn = mLoadedClip.mIsRepeating;
			mCurrentMeasure.value = 0;
			mLoadedClip.SetState(eClipState.Stop);
			mMusicGenerator.ClipLoaded.Invoke(mCurrenClipSave);
			mMusicGenerator.ResetPlayer();
			mMusicGenerator.SetState(eGeneratorState.editing);
			ToggleHelperNotes();
			UIToggleAllInstruments();
			yield return null;
		}
#if !UNITY_EDITOR && UNITY_ANDROID
		public void StageClipForPlaying()
		{
			StartCoroutine(StageMobileClipForPlaying());
		}
		/// non-async clip staging. Loads clip and gets it ready to play.
		public IEnumerator StageMobileClipForPlaying()
		{
			ClearCurrentInstruments();
			bool finished = false;
			yield return StartCoroutine(LoadClip(mPlayClipDropdown.options[mPlayClipDropdown.value].text, (x) => { finished = x; }));
			yield return new WaitUntil(() => finished);
			FinalizeStagingClip();
		}
#else
		public void StageClipForPlaying()
		{
			ClearCurrentInstruments();

			LoadClip(mPlayClipDropdown.options[mPlayClipDropdown.value].text);
			FinalizeStagingClip();
		}
#endif //!UNITY_EDITOR && UNITY_ANDROID
		private void FinalizeStagingClip()
		{
			mCurrentInstSet = mLoadedClip.mInstrumentSet;

			InstrumentListPanelUI listPanel = InstrumentListPanelUI.Instance;

			for (int i = 0; i < mCurrentInstSet.mInstruments.Count; i++)
			{

				listPanel.AddInstrument(mCurrentInstSet.mInstruments[i]);
				InstrumentListUIObject icon = listPanel.mInstrumentIcons[listPanel.mInstrumentIcons.Count - 1];
				icon.ToggleSelected();
				bool isPercussion = icon.mInstrument.mData.InstrumentType.Contains("p_") ? true : false;
				icon.SetDropdown(isPercussion);

				InstrumentPanelUI.Instance.SetInstrument(icon.mInstrument);
				mInstrumentPanel.mInstrument = mCurrentInstSet.mInstruments[i];
			}
			InstrumentPanelUI.Instance.SetInstrument(mCurrentInstSet.mInstruments[0]);
			mInstrumentPanel.mInstrument = mCurrentInstSet.mInstruments[0];

			mClipIsRepeating.isOn = mLoadedClip.mIsRepeating;
			mCurrentMeasure.value = 0;
			mLoadedClip.SetState(eClipState.Stop);
			mMusicGenerator.ClipLoaded.Invoke(mCurrenClipSave);
			mMusicGenerator.ResetPlayer();
			mMusicGenerator.SetState(eGeneratorState.editing);
			ToggleHelperNotes();
			UIToggleAllInstruments();
			mStaffPlayer.ChangeTimeSignature((int)mCurrentInstSet.mTimeSignature.Signature);
		}

		/// Clears all instruments and clips.
		private void ClearCurrentInstruments()
		{
			mMusicGenerator.ClearInstruments(mMusicGenerator.mInstrumentSet);
			if (mCurrentInstSet != null)
			{
				mMusicGenerator.ClearInstruments(mCurrentInstSet);
				if (mLoadedClip != null)
				{
					Destroy(mLoadedClip.gameObject);
				}
			}
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		/// loads all available presets from the the streaming assets and persisten data paths.
		public IEnumerator LoadPresets()
		{
			if (System.IO.Directory.Exists(Application.persistentDataPath + "/MusicGenerator/InstrumentClips"))
			{
				foreach (string filename in System.IO.Directory.GetFiles(Application.persistentDataPath + "/MusicGenerator/InstrumentClips"))
				{
					if (filename.Contains(".meta") == false && filename.Contains("presets") == false)
						AddPresetOption(Path.GetFileName(filename));
				}
			}

			string data = null;
			yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentClips/presets.txt", (x) => { data = x.downloadHandler.text; });
			List<string> presetFileNames = new List<string>(data.Split(','));
			if (presetFileNames != null)
			{
				for (int i = 0; i < presetFileNames.Count; i++)
					AddPresetOption(presetFileNames[i]);
			}
			yield return StartCoroutine(StageMobileClipForPlaying());
		}
#else
		/// loads all available presets from the the streaming assets and persisten data paths.
		public void LoadPresets()
		{
			if (System.IO.Directory.Exists(Application.streamingAssetsPath + "/MusicGenerator/InstrumentClips"))
			{
				foreach (string filename in System.IO.Directory.GetFiles(Application.streamingAssetsPath + "/MusicGenerator/InstrumentClips"))
				{
					if (filename.Contains(".meta") == false && filename.Contains("presets") == false)
						AddPresetOption(Path.GetFileName(filename));
				}
			}
			if (System.IO.Directory.Exists(Application.persistentDataPath + "/InstrumentClips"))
			{
				foreach (string filename in System.IO.Directory.GetFiles(Application.persistentDataPath + "/InstrumentClips"))
				{
					if (filename.Contains(".meta") == false && filename.Contains("presets") == false)
						AddPresetOption(Path.GetFileName(filename));
				}
			}
			ExportMobilePresets();
			StageClipForPlaying();
		}
#endif //!UNITY_EDITOR && UNITY_ANDROID

		private void ExportMobilePresets()
		{
			StringBuilder presetFileNames = new StringBuilder();
			for (int i = 0; i < mPlayClipDropdown.options.Count; i++)
			{
				presetFileNames.Append(mPlayClipDropdown.options[i].text + ",");
			}
#if UNITY_EDITOR
			mMusicGenerator.mMusicFileConfig.ExportMobileClipPresets(presetFileNames.ToString());
#endif	//UNITY_EDITOR
		}

		/// adds our preset to the preset dropdown
		public void AddPresetOption(string fileNameIN)
		{
			bool fileExists = false;
			for (int i = 0; i < mPlayClipDropdown.options.Count; i++)
			{
				if (mPlayClipDropdown.options[i].text == fileNameIN)
					fileExists = true;
			}
			if (fileExists == false)
			{
				Dropdown.OptionData newOption = new Dropdown.OptionData();
				newOption.text = fileNameIN;
				mPlayClipDropdown.options.Add(newOption);
			}
		}

		/// resets the measure editor
		public void Reset()
		{
			for (int i = 0; i < mCurrentInstSet.mInstruments.Count; i++)
			{
				mCurrentInstSet.mInstruments[i].ClearClipNotes();
			}
			mStaffPlayer.ClearNotes(true, false);
			mCurrentMeasure.value = 0;
		}

		/// Generates a new chord progression. This should not be called outside this class. It's public so the editor can call it :P
		public void GenerateChordProgression()
		{
			mMusicGenerator.ChordProgression = mMusicGenerator.mChordProgressions.GenerateProgression(mMusicGenerator.mGeneratorData.mMode, mMusicGenerator.mGeneratorData.mScale, 0);
			UIToggleHelperNotes();
		}
	}
}