using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	/// Handles event calls from the music generator.
	public class UIManager : HelperSingleton<UIManager>
	{
		public MeasureEditor mMeasureEditor = null;
		public MusicGeneratorUIPanel mGeneratorUIPanel = null; //< ui panel for general settings
		public Tooltips mTooltips = null; //< tooltips class
		public AdvancedSettingsPanel mAdvancedSettingsPanel = null; //< advanced settings panel
		public GlobalEffectsPanel mGlobalEffectsPanel = null; //< global effects panel
		public InstrumentListPanelUI mInstrumentListPanelUI = null; //< instrument list panel
		public InstrumentPanelUI mInstrumentPanelUI = null; //< instrument panel
		public StaffPlayerUI mStaffPlayer = null;
		private MusicGenerator mMusicGenerator = null;

		/// Awake is called when the script instance is being loaded.
		public override void Awake()
		{
			base.Awake();
			mMusicGenerator = GameObject.Find("MusicGenerator").GetComponent<MusicGenerator>();
			mMusicGenerator.Started.AddListener(OnStarted);
			mMusicGenerator.HasVisiblePlayer += OnHasVisiblePlayer;
		}

		public void OnStarted()
		{
			mMusicGenerator = MusicGenerator.Instance;
			mMusicGenerator.StateSet.AddListener(OnStateSet);
			mMusicGenerator.VolumeFaded.AddListener(OnVolumeFaded);
			mMusicGenerator.ProgressionGenerated.AddListener(OnProgressionGenerated);
			mMusicGenerator.InstrumentsCleared.AddListener(OnInstrumentsCleared);
			mMusicGenerator.KeyChanged.AddListener(OnKeyChanged);
			mMusicGenerator.NormalMeasureExited.AddListener(OnNormalMeasureExited);
			mMusicGenerator.RepeatNotePlayed.AddListener(OnRepeatNotePlayed);
			mMusicGenerator.RepeatedMeasureExited.AddListener(OnRepeatedMeasureExited);
			mMusicGenerator.BarlineColorSet.AddListener(OnSetBarlineColor);
			mMusicGenerator.UIPlayerIsEditing += (OnUIPlayerIsEditing);
			mMusicGenerator.UIStaffNotePlayed.AddListener(OnUIStaffNotePlayed);
			mMusicGenerator.ClipLoaded.AddListener(OnClipLoaded);
			mMusicGenerator.EditorClipPlayed.AddListener(OnEditorClipPlayed);
			mMusicGenerator.PlayerReset.AddListener(OnPlayerReset);
			mMusicGenerator.UIStaffNoteStrummed.AddListener(OnUIStaffNoteStrummed);
#if !UNITY_EDITOR && UNITY_ANDROID			
			StartCoroutine(Init());
#else
			Init();
#endif //#if !UNITY_EDITOR && UNITY_ANDROID
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		public IEnumerator Init()
		{
			mMusicGenerator.SetState(eGeneratorState.initializing);
			yield return StartCoroutine(mMusicGenerator.mMusicFileConfig.LoadConfig(mMusicGenerator.mDefaultConfig, eGeneratorState.initializing));

			mTooltips = Tooltips.Instance;
			yield return StartCoroutine(mTooltips.Init());

			mStaffPlayer = StaffPlayerUI.Instance;

			mInstrumentListPanelUI = InstrumentListPanelUI.Instance;

			mMeasureEditor = MeasureEditor.Instance;
			mMeasureEditor.Init(mMusicGenerator);

			mInstrumentPanelUI = InstrumentPanelUI.Instance;
			mInstrumentPanelUI.Init();

			mAdvancedSettingsPanel = AdvancedSettingsPanel.Instance;
			mAdvancedSettingsPanel.Init();

			mGlobalEffectsPanel = GlobalEffectsPanel.Instance;
			mGlobalEffectsPanel.Init(mMusicGenerator);

			mGeneratorUIPanel = MusicGeneratorUIPanel.Instance;
			bool finished = false;
			yield return StartCoroutine(mGeneratorUIPanel.Init(mMusicGenerator, (x) => { finished = x; }));
			yield return new WaitUntil(() => finished);
			mStaffPlayer.Init(mMusicGenerator);
			mStaffPlayer.ChangeTimeSignature(-1);
			mMusicGenerator.SetState(eGeneratorState.ready);
			//mInstrumentListPanelUI.Init(mMusicGenerator);
			yield return null;
		}
#else
		public void Init()
		{
			mMusicGenerator.SetState(eGeneratorState.initializing);
			mMusicGenerator.mMusicFileConfig.LoadConfig(mMusicGenerator.mDefaultConfig, eGeneratorState.ready);

			mTooltips = Tooltips.Instance;
			mTooltips.Init();

			mStaffPlayer = StaffPlayerUI.Instance;

			mInstrumentListPanelUI = InstrumentListPanelUI.Instance;

			mMeasureEditor = MeasureEditor.Instance;
			mMeasureEditor.Init(mMusicGenerator);

			mInstrumentPanelUI = InstrumentPanelUI.Instance;
			mInstrumentPanelUI.Init();

			mAdvancedSettingsPanel = AdvancedSettingsPanel.Instance;
			mAdvancedSettingsPanel.Init();

			mGlobalEffectsPanel = GlobalEffectsPanel.Instance;
			mGlobalEffectsPanel.Init(mMusicGenerator);

			mGeneratorUIPanel = MusicGeneratorUIPanel.Instance;
			mGeneratorUIPanel.Init(mMusicGenerator);
			mStaffPlayer.Init(mMusicGenerator);
			mStaffPlayer.ChangeTimeSignature(-1);
			//mInstrumentListPanelUI.Init(mMusicGenerator);
		}
#endif
		public bool OnHasVisiblePlayer(object source, EventArgs args)
		{
			return true;
		}

		public void OnRepeatedMeasureExited(eGeneratorState stateIN)
		{
			mStaffPlayer.ClearNotes();

			if (stateIN >= eGeneratorState.editing)
			{
				if (mMeasureEditor.mCurrentMeasure.value < mMeasureEditor.mNumberOfMeasures.value)
					mMeasureEditor.mCurrentMeasure.value += 1;
				else
					mMeasureEditor.mCurrentMeasure.value = 0;
				mMeasureEditor.UIToggleAllInstruments(true);
			}
		}

		public void OnVolumeFaded(float volume)
		{
			mGeneratorUIPanel.FadeVolume(volume);
		}

		public void OnStateSet(eGeneratorState stateIN)
		{
			SetState(stateIN);
		}

		public void SetState(eGeneratorState stateIN)
		{
			switch (stateIN)
			{
				case eGeneratorState.initializing:
					break;
				case eGeneratorState.ready:
					break;
				case eGeneratorState.stopped:
					{
						mMeasureEditor.mCurrentMeasure.value = 0;
						mStaffPlayer.SetBarlineColor(-1, false);
						break;
					}
				case eGeneratorState.playing:
					break;
				case eGeneratorState.repeating:
					break;
				case eGeneratorState.paused:
					{
						if (stateIN == eGeneratorState.editing)
							mMeasureEditor.mLoadedClip.mIsPlaying = false;
						break;
					}
				case eGeneratorState.editing:
					{
						mStaffPlayer.ClearNotes(true, mMeasureEditor.mShowEditorHints.isOn == false);
						break;
					}
				case eGeneratorState.editorPaused:
					break;
				case eGeneratorState.editorStopped:
					{
						mMeasureEditor.mCurrentMeasure.value = 0;
						mStaffPlayer.SetBarlineColor(-1, false);
						mMeasureEditor.Stop();
						break;
					}
				case eGeneratorState.editorPlaying:
					{
						break;
					}
				default:
					break;
			}
		}

		public void OnProgressionGenerated()
		{
			if (mMusicGenerator.mState == eGeneratorState.editing)
				mMeasureEditor.UIToggleHelperNotes();
		}

		public void OnInstrumentsCleared()
		{
			if (mInstrumentListPanelUI != null)
				mInstrumentListPanelUI.ClearInstruments();
		}

		public void OnKeyChanged(int key)
		{
			mGeneratorUIPanel.SetKey(key);
		}

		public void OnNormalMeasureExited()
		{
			mStaffPlayer.ClearNotes();
		}

		public void OnSetBarlineColor(int steps, bool isRepeating)
		{
			mStaffPlayer.SetBarlineColor(steps, isRepeating);
		}

		public bool OnUIPlayerIsEditing(object source, EventArgs args)
		{

			if (mMusicGenerator.mState >= eGeneratorState.editorPlaying)
				return true;
			return false;
		}

		public void OnRepeatNotePlayed(RepeatNoteArgs e)
		{
			int unplayed = -1;
			List<Instrument> instruments = e.instrumentSet.mInstruments;
			Instrument instrument = instruments[e.indexA];
			int note = instrument.mClipNotes[mMeasureEditor.mCurrentMeasure.value][e.repeatingCount][e.indexB];
			if (instrument.mClipNotes[mMeasureEditor.mCurrentMeasure.value][e.repeatingCount][e.indexB] != unplayed)
			{
				mMusicGenerator.PlayAudioClip(e.instrumentSet, (int)instrument.InstrumentTypeIndex, note, instrument.mData.Volume, e.indexA);
				mStaffPlayer.DisplayNote(note,
					(int)instrument.mData.mStaffPlayerColor, false, e.instrumentSet);
			}
		}

		public void OnUIStaffNotePlayed(int instIndex, int color)
		{
			mStaffPlayer.DisplayNote(instIndex, color, false, MusicGenerator.Instance.mInstrumentSet);
		}

		public void OnUIStaffNoteStrummed(int instIndex, int color)
		{
			mStaffPlayer.DisplayNote(instIndex, color, false, MusicGenerator.Instance.mInstrumentSet, true);
		}

		public void OnClipLoaded(ClipSave e)
		{
			MeasureEditor.Instance.mKey.value = (int)e.mKey;
			MeasureEditor.Instance.mScale.value = (int)e.mScale;
			MeasureEditor.Instance.mMode.value = (int)e.mMode;
			MeasureEditor.Instance.mTempo.value = e.mTempo;
		}

		public void OnEditorClipPlayed()
		{
			MeasureEditor editor = MeasureEditor.Instance;
			editor.mLoadedClip.mClipMeasure.PlayMeasure(editor.mLoadedClip.mInstrumentSet);
		}

		public void OnPlayerReset()
		{
			if (mMusicGenerator.mState >= eGeneratorState.editing)
			{
				MeasureEditor.Instance.mCurrentInstSet.Reset();
				mMeasureEditor.Stop();
			}
		}
	}
}