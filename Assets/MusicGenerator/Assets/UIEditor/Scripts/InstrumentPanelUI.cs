using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProcGenMusic
{
	/// Just a simple UI panel for the instruments:
	/// Apologies to anyone who has to venture here. This is terribly setup :/
	public class InstrumentPanelUI : HelperSingleton<InstrumentPanelUI>
	{
		public Instrument mInstrument = null;
		public delegate void UpdateAction<T>(T value);

		//The following variables are basically all references to
		//the UI for their instrument's variables:
		public Toggle mUseSevenths = null;

		private Slider mPatternLengthSlider = null;
		private Text mPatternLengthOutput = null;

		private Slider mPatternReleaseSlider = null;
		private Text mPatternReleaseOutput = null;

		private Slider mOddsOfPlayingSlider = null;
		private Text mOddsOfPlayingValueText = null;

		private Slider mStrumLength = null;
		private Text mStrumLengthOutput = null;

		private Slider mStrumVariation = null;
		private Text mStrumVariationOutput = null;

		private Slider mLeadVariation = null;
		private Text mLeadVariationOutput = null;

		private Slider mLeadMaxSteps = null;
		private Text mLeadMaxStepsOutput = null;

		private Slider mMultiplierSlider = null;
		private Text mMultiplierText = null;

		private Slider mVolumeSlider = null;
		private Text mVolumeText = null;

		public Toggle mMuteToggle = null;

		private Slider mReverbSlider = null;
		private Text mReverbOutput = null;

		private Slider mRoomSizeSlider = null;
		private Text mRoomSizeOutput = null;

		private Slider mChorusSlider = null;
		private Text mChorusOutput = null;

		private Slider mFlangerSlider = null;
		private Text mFlangerOutput = null;

		private Slider mDistortionSlider = null;
		private Text mDistortionOutput = null;

		private Slider mEchoSlider = null;
		private Text mEchoOutput = null;

		private Slider mEchoDelaySlider = null;
		private Text mEchoDelayOutput = null;

		private Slider mEchoDecaySlider = null;
		private Text mEchoDecayOutput = null;

		private Slider mAudioGroupVolume = null;
		private Text mAudioGroupVolumeOutput = null;

		private Dropdown mTimestep = null;

		private Dropdown mSuccession = null;

		private MusicGenerator mMusicGenerator = null;
		private GameObject mMasterObject = null;

		private Slider mOddsOfPlayingChordNoteSlider = null;
		private Text mOddsOfPlayingChordNoteText = null;

		private ListArrayInt mOctavesToUse = new ListArrayInt(3);
		private Toggle mOctave1 = null;
		private Toggle mOctave2 = null;
		private Toggle mOctave3 = null;

		private Dropdown mGroup = null;
		private Dropdown mColor = null;

		private Slider mStereoPan = null;
		private InstrumentListPanelUI mInstrumentListUI = null;
		private StaffPlayerUI mStaffPlayerUI = null;

		private Toggle mFreeMelody = null;
		private Dropdown mUsePattern = null;
		private Tooltips mTooltips = null;
		private MeasureEditor mMeasureEditor = null;

		private Toggle mArpeggio = null;
		private Toggle mPentatonic = null;
		public Toggle[] mLeadAvoidSteps = new Toggle[7];

		// Just cached strings so we're not allocating GC every frame.
		private string mMixerRoomSizeString;
		private string mMixerReverbString;
		private string mMixerEchoString;
		private string mMixerEchoDelayString;
		private string mMixerEchoDecayString;
		private string mMixerFlangeString;
		private string mMixerDistortionString;
		private string mMixerChorusString;

		public void Init()
		{
			mMusicGenerator = MusicGenerator.Instance;
			mTooltips = UIManager.Instance.mTooltips;
			mInstrumentListUI = UIManager.Instance.mInstrumentListPanelUI;
			mStaffPlayerUI = UIManager.Instance.mStaffPlayer;
			mMeasureEditor = UIManager.Instance.mMeasureEditor;
			Component[] components = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components)
			{
				if (cp.name == "Arpeggio")
					mTooltips.AddUIElement(ref mArpeggio, cp, "Arpeggio");
				if (cp.name == "Pentatonic")
					mTooltips.AddUIElement(ref mPentatonic, cp, "PentatonicLead");
				if (cp.name == "LeadAvoidNotes")
				{
					foreach (Toggle toggle in cp.GetComponentsInChildren<Toggle>())
					{
						int x = 0;
						Int32.TryParse(toggle.name, out x);
						mTooltips.AddUIElement(ref mLeadAvoidSteps[x], toggle, "LeadAvoids");
					}
				}
				if (cp.name == "LeadVariation")
					mTooltips.AddUIElement<Slider>(ref mLeadVariation, cp, "LeadVariation");
				if (cp.name == "LeadMaxSteps")
					mTooltips.AddUIElement(ref mLeadMaxSteps, cp, "LeadMaxSteps");
				if (cp.name == "InstrumentPanel")
					mMasterObject = cp.gameObject;

				if (cp.name == "PatternRelease")
					mTooltips.AddUIElement<Slider>(ref mPatternReleaseSlider, cp, "PatternRelease");
				if (cp.name == "PatternLength")
					mTooltips.AddUIElement<Slider>(ref mPatternLengthSlider, cp, "PatternLength");
				if (cp.name == "StrumLength")
					mTooltips.AddUIElement<Slider>(ref mStrumLength, cp, "StrumLength");
				if (cp.name == "StrumVariation")
					mTooltips.AddUIElement<Slider>(ref mStrumVariation, cp, "StrumVariation");
				if (cp.name == "UseSevenths")
					mTooltips.AddUIElement<Toggle>(ref mUseSevenths, cp, "UseSevenths");
				if (cp.name == "OddsOfPlaying")
					mTooltips.AddUIElement<Slider>(ref mOddsOfPlayingSlider, cp, "OddsOfPlaying");
				if (cp.name == "MultiplierOdds")
					mTooltips.AddUIElement<Slider>(ref mMultiplierSlider, cp, "MultiplierOdds");
				if (cp.name == "VolumeSlider")
					mTooltips.AddUIElement<Slider>(ref mVolumeSlider, cp, "Volume");
				if (cp.name == "Mute")
					mTooltips.AddUIElement<Toggle>(ref mMuteToggle, cp, "Mute");
				if (cp.name == "Echo")
					mTooltips.AddUIElement<Slider>(ref mEchoSlider, cp, "Echo");
				if (cp.name == "EchoDecay")
					mTooltips.AddUIElement<Slider>(ref mEchoDecaySlider, cp, "EchoDecay");
				if (cp.name == "EchoDelay")
					mTooltips.AddUIElement<Slider>(ref mEchoDelaySlider, cp, "EchoDelay");
				if (cp.name == "Reverb")
					mTooltips.AddUIElement<Slider>(ref mReverbSlider, cp, "Reverb");
				if (cp.name == "RoomSize")
					mTooltips.AddUIElement<Slider>(ref mRoomSizeSlider, cp, "RoomSize");
				if (cp.name == "Timestep")
					mTooltips.AddUIElement<Dropdown>(ref mTimestep, cp, "Timestep");
				if (cp.name == "Flanger")
					mTooltips.AddUIElement<Slider>(ref mFlangerSlider, cp, "Flanger");
				if (cp.name == "Distortion")
					mTooltips.AddUIElement<Slider>(ref mDistortionSlider, cp, "Distortion");
				if (cp.name == "Chorus")
					mTooltips.AddUIElement<Slider>(ref mChorusSlider, cp, "Chorus");
				if (cp.name == "Succession")
					mTooltips.AddUIElement<Dropdown>(ref mSuccession, cp, "Succession");
				if (cp.name == "OddsOfPlayingChordNote")
					mTooltips.AddUIElement<Slider>(ref mOddsOfPlayingChordNoteSlider, cp, "ChordNote");
				if (cp.name == "Octave1")
					mTooltips.AddUIElement<Toggle>(ref mOctave1, cp, "OctavesToUse");
				if (cp.name == "Octave2")
					mTooltips.AddUIElement<Toggle>(ref mOctave2, cp, "OctavesToUse");
				if (cp.name == "Octave3")
					mTooltips.AddUIElement<Toggle>(ref mOctave3, cp, "OctavesToUse");
				if (cp.name == "Group")
					mTooltips.AddUIElement<Dropdown>(ref mGroup, cp, "Group");
				if (cp.name == "Color")
					mTooltips.AddUIElement<Dropdown>(ref mColor, cp, "Color");
				if (cp.name == "StereoPan")
					mTooltips.AddUIElement<Slider>(ref mStereoPan, cp, "StereoPan");
				if (cp.name == "UsePattern")
					mTooltips.AddUIElement<Dropdown>(ref mUsePattern, cp, "Pattern");
				if (cp.name == "FreeMelody")
					mTooltips.AddUIElement<Toggle>(ref mFreeMelody, cp, "Lead");
				if (cp.name == "AudioGroupVolume")
					mTooltips.AddUIElement<Slider>(ref mAudioGroupVolume, cp, "AudioGroupVolume");

				//output:
				if (cp.name == "PatternLengthOutput")
					mPatternLengthOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "PatternReleaseOutput")
					mPatternReleaseOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "StrumLengthOutput")
					mStrumLengthOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "StrumVariationOutput")
					mStrumVariationOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "LeadVariationOutput")
					mLeadVariationOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "LeadMaxStepsOutput")
					mLeadMaxStepsOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "OddsOfPlayingOutput")
					mOddsOfPlayingValueText = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "MultiplierOutput")
					mMultiplierText = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "VolumeOutput")
					mVolumeText = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "RoomSizeOutput")
					mRoomSizeOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "ReverbOutput")
					mReverbOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "EchoOutput")
					mEchoOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "EchoDelayOutput")
					mEchoDelayOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "EchoDecayOutput")
					mEchoDecayOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "FlangerOutput")
					mFlangerOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "DistortionOutput")
					mDistortionOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "ChorusOutput")
					mChorusOutput = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "OddsOfPlayingChordNoteOutput")
					mOddsOfPlayingChordNoteText = cp.gameObject.GetComponentInChildren<Text>();
				if (cp.name == "AudioGroupVolumeOutput")
					mAudioGroupVolumeOutput = cp.gameObject.GetComponentInChildren<Text>();
			}

			mMasterObject.SetActive(false);
			mColor.options.Clear();
			for (int i = 0; i < mStaffPlayerUI.mColors.Count; i++)
			{
				Texture2D texture = new Texture2D(1, 1);
				texture.SetPixel(0, 0, mStaffPlayerUI.mColors[i]);
				texture.Apply();
				Dropdown.OptionData data = new Dropdown.OptionData(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0)));
				mColor.options.Add(data);
			}
		}

		/// changes the color of the instrument in the UI
		public void ChangeColor()
		{
			mColor.gameObject.GetComponent<Image>().color = mStaffPlayerUI.mColors[mColor.value];
		}

		/// sets a new instrument to be displayed:
		public void SetInstrument(Instrument instrumenIN)
		{
			if (mInstrumentListUI.mInstrumentIcons.Count <= 0)
				return;

			if (instrumenIN == null)
			{
				InstrumentSet set = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ? MeasureEditor.Instance.mCurrentInstSet : mMusicGenerator.mInstrumentSet;
				List<Instrument> instruments = set.mInstruments;
				instrumenIN = instruments[instruments.Count - 1];
			}

			mMasterObject.SetActive(true);

			mInstrument = instrumenIN;
			mArpeggio.isOn = mInstrument.mData.mArpeggio;
			mPentatonic.isOn = mInstrument.mData.mIsPentatonic;
			SetLeadAvoidNotes();

			mSuccession.value = (int)mInstrument.mData.mSuccessionType;
			mStrumLength.value = mInstrument.mData.StrumLength / mMusicGenerator.mInstrumentSet.mBeatLength;;
			mStrumVariation.value = mInstrument.mData.StrumVariation / mMusicGenerator.mInstrumentSet.mBeatLength;
			mUseSevenths.isOn = mInstrument.mData.ChordSize == 4;
			mLeadMaxSteps.value = mInstrument.mData.LeadMaxSteps;
			mLeadVariation.value = mInstrument.mData.AscendDescendInfluence;
			mStereoPan.value = mInstrument.mData.mStereoPan;
			mOddsOfPlayingSlider.value = mInstrument.mData.OddsOfPlaying;
			mMultiplierSlider.value = mInstrument.mData.OddsOfPlayingMultiplierMax;
			mOddsOfPlayingValueText.text = mInstrument.mData.OddsOfPlaying.ToString();
			mMultiplierText.text = "x" + mInstrument.mData.OddsOfPlayingMultiplierMax.ToString();
			mVolumeSlider.value = mInstrument.mData.Volume;
			mVolumeText.text = mInstrument.mData.Volume.ToString();
			mMuteToggle.isOn = mInstrument.mData.mIsMuted;
			mTimestep.value = (int)mInstrument.mData.mTimeStep;
			mOddsOfPlayingChordNoteSlider.value = mInstrument.mData.OddsOfUsingChordNotes;
			mOddsOfPlayingChordNoteText.text = mInstrument.mData.OddsOfUsingChordNotes.ToString();
			mColor.value = (int)mInstrument.mData.mStaffPlayerColor;
			mInstrumentListUI.mInstrumentIcons[(int)mInstrument.InstrumentIndex].mPanelBack.color = mStaffPlayerUI.mColors[mColor.value];
			mUsePattern.value = mInstrument.mData.mUsePattern ? 1 : 0;
			mPatternLengthSlider.value = mInstrument.mData.PatternLength;
			mPatternReleaseSlider.value = mInstrument.mData.PatternRelease;
			mRoomSizeSlider.value = mInstrument.mData.RoomSize;
			mReverbSlider.value = mInstrument.mData.Reverb;
			mEchoSlider.value = mInstrument.mData.Echo;
			mEchoDelaySlider.value = mInstrument.mData.EchoDelay;
			mEchoDecaySlider.value = mInstrument.mData.EchoDecay;
			mFlangerSlider.value = mInstrument.mData.Flanger;
			mDistortionSlider.value = mInstrument.mData.Distortion;
			mChorusSlider.value = mInstrument.mData.Chorus;
			mGroup.value = (int)mInstrument.mData.Group;
			mInstrumentListUI.mInstrumentIcons[(int)mInstrument.InstrumentIndex].mGroupText.text = ("Group: " + (mGroup.value + 1).ToString());
			mAudioGroupVolume.value = mInstrument.mData.AudioSourceVolume;
			
			UpdateInstrumentEffectsStrings(mInstrument.InstrumentIndex);
			SetOctavesFrominstrument();

			ToggleChorusMelody();
			UpdateValues(true);
		}

		/// Sets our lead avoid notes from data.
		public void SetLeadAvoidNotes()
		{
			int avoid1 = mInstrument.mData.mLeadAvoidNotes[0] >= 0 ? MusicHelpers.SafeLoop(mInstrument.mData.mLeadAvoidNotes[0] + 1, 7) : -1;
			int avoid2 = mInstrument.mData.mLeadAvoidNotes[1] >= 0 ? MusicHelpers.SafeLoop(mInstrument.mData.mLeadAvoidNotes[1] + 1, 7) : -1;
			for (int i = 0; i < mLeadAvoidSteps.Length; i++)
			{
				mLeadAvoidSteps[i].isOn = i == avoid1 || i == avoid2 ? true : false;
			}
		}

		/// Updates data values from UI settings for lead avoid notes.
		public void UpdateLeadAvoidNotes()
		{
			if (mInstrument.mData.mSuccessionType != eSuccessionType.lead)
			{
				for (int i = 0; i < mLeadAvoidSteps.Length; i++)
				{
					mInstrument.mData.mLeadAvoidNotes[0] = -1;
					mInstrument.mData.mLeadAvoidNotes[1] = -1;
				}
				return;
			}

			int[] majorPent = NoteGenerator_Lead.mMajorPentatonicAvoid;
			int[] minorPent = NoteGenerator_Lead.mMinorPentatonicAvoid;

			// If our 'standard' pentatonic is enabled, force values to generic pentatonic.
			if (mPentatonic.isOn)
			{
				if (mMusicGenerator.mGeneratorData.mScale == eScale.Major || mMusicGenerator.mGeneratorData.mScale == eScale.HarmonicMajor)
				{
					mInstrument.mData.mLeadAvoidNotes[0] = majorPent[0];
					mInstrument.mData.mLeadAvoidNotes[1] = majorPent[1];
					for (int i = 0; i < mLeadAvoidSteps.Length; i++)
					{
						bool isOn = i == majorPent[0] + 1 || i == majorPent[1] + 1 ? true : false;
						mLeadAvoidSteps[i].isOn = isOn;
					}
				}
				else if (mMusicGenerator.mGeneratorData.mScale == eScale.NatMinor ||
					mMusicGenerator.mGeneratorData.mScale == eScale.HarmonicMinor ||
					mMusicGenerator.mGeneratorData.mScale == eScale.mMelodicMinor)
				{
					mInstrument.mData.mLeadAvoidNotes[0] = minorPent[0];
					mInstrument.mData.mLeadAvoidNotes[1] = minorPent[1];
					for (int i = 0; i < mLeadAvoidSteps.Length; i++)
					{
						mLeadAvoidSteps[i].isOn = i == minorPent[0] + 1 || i == minorPent[1] + 1 ? true : false;
					}
				}
				else
				{
					mPentatonic.isOn = false;
				}
				return;
			}

			int index = 0;
			mInstrument.mData.mLeadAvoidNotes[0] = -1;
			mInstrument.mData.mLeadAvoidNotes[1] = -1;

			// otherwise set custom avoid notes
			for (int i = 0; i < mLeadAvoidSteps.Length; i++)
			{
				if (mLeadAvoidSteps[i].isOn)
				{
					if (index < 2)
					{
						mInstrument.mData.mLeadAvoidNotes[index] = MusicHelpers.SafeLoop(i - 1, 7);
						index++;
					}
					else
					{
						mLeadAvoidSteps[i].isOn = false;
					}
				}
			}
		}

		/// mutes instrument;
		public void SetMute(bool isMuted)
		{
			mMuteToggle.isOn = isMuted;
		}

		/// toggles whether this is a chorus or melodic instrument
		public void ToggleChorusMelody()
		{
			bool isMelody = false;
			if (mSuccession.value != (int)eSuccessionType.rhythm)
				isMelody = true;
			mInstrument.mData.mSuccessionType = (eSuccessionType)mSuccession.value;
			mOddsOfPlayingSlider.transform.parent.gameObject.SetActive(isMelody);
			mOddsOfPlayingValueText.transform.parent.gameObject.SetActive(isMelody);
			mMultiplierSlider.transform.parent.gameObject.SetActive(isMelody);
			mMultiplierSlider.transform.parent.gameObject.SetActive(isMelody);
			mLeadMaxSteps.transform.parent.gameObject.SetActive(isMelody);
			mLeadVariation.transform.parent.gameObject.SetActive(isMelody);
			mFreeMelody.gameObject.SetActive(isMelody);
			mStrumLength.transform.parent.gameObject.SetActive(isMelody == false);
			mStrumVariation.transform.parent.gameObject.SetActive(isMelody == false);
			mOddsOfPlayingSlider.value = mInstrument.mData.OddsOfPlaying;
			mArpeggio.transform.parent.gameObject.SetActive(isMelody == false);

			mPentatonic.transform.parent.gameObject.SetActive(mSuccession.value == (int)eSuccessionType.lead);

			for (int i = 0; i < mLeadAvoidSteps.Length; i++)
				mLeadAvoidSteps[0].transform.parent.gameObject.SetActive(mSuccession.value == (int)eSuccessionType.lead);

			if (mMusicGenerator.mState >= eGeneratorState.editorInitializing)
				UIManager.Instance.mMeasureEditor.ToggleHelperNotes();
		}

		void Update()
		{
			if (mMusicGenerator == null)
			{
				return;
			}
			if (mInstrumentListUI.mInstrumentIcons.Count <= 0)
				mInstrument = null;

			if (mMusicGenerator.mState == eGeneratorState.editorInitializing)
				return;

			UpdateValues();
		}

		private void UpdateValues(bool force = false)
		{
			List<Instrument> instruments = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ?
				MeasureEditor.Instance.mCurrentInstSet.mInstruments : mMusicGenerator.mInstrumentSet.mInstruments;

			if (mInstrument != null && mMasterObject.activeSelf && mInstrumentListUI.mInstrumentIcons.Count > 0 &&
				mInstrument.InstrumentIndex < instruments.Count)
			{
				if (force || mInstrument.mData.StrumLength != mStrumLength.value * mMusicGenerator.mInstrumentSet.mBeatLength)
				{
					mInstrument.mData.StrumLength = mStrumLength.value * mMusicGenerator.mInstrumentSet.mBeatLength;
					mStrumLengthOutput.text = mInstrument.mData.StrumLength.ToString();
				}

				if (force || mInstrument.mData.StrumVariation != mStrumVariation.value * mMusicGenerator.mInstrumentSet.mBeatLength)
				{
					mInstrument.mData.StrumVariation = mStrumVariation.value * mMusicGenerator.mInstrumentSet.mBeatLength;
					mStrumVariationOutput.text = mInstrument.mData.StrumVariation.ToString();
				}

				UpdateLeadAvoidNotes();
				int chordSize = mUseSevenths.isOn ? 4 : 3;
				mInstrument.mData.ChordSize = chordSize;
				if (UpdateEffectValue<int>(force, mInstrument.mData.OddsOfPlaying, (int)mOddsOfPlayingSlider.value, ref mOddsOfPlayingValueText))
					mInstrument.mData.OddsOfPlaying = (int)mOddsOfPlayingSlider.value;

				if (mInstrument.mData.mIsMuted != mMuteToggle.isOn)
				{
					mInstrument.mData.mIsMuted = mMuteToggle.isOn;
					mInstrumentListUI.mInstrumentIcons[(int)mInstrument.InstrumentIndex].mMuteToggle.isOn = mMuteToggle.isOn;
				}

				mInstrument.mData.mIsPentatonic = mPentatonic.isOn;
				mInstrument.mData.mTimeStep = (eTimestep)mTimestep.value;

				if (mInstrument.mData.Group != (mGroup.value))
				{
					mInstrument.mData.Group = (mGroup.value);
					mInstrumentListUI.mInstrumentIcons[(int)mInstrument.InstrumentIndex].mGroupText.text = ("Group: " + (mGroup.value + 1).ToString());
				}

				mInstrument.mData.mStaffPlayerColor = (eStaffPlayerColors)mColor.value;
				mInstrumentListUI.mInstrumentIcons[(int)mInstrument.InstrumentIndex].mPanelBack.color = mStaffPlayerUI.mColors[mColor.value];
				mInstrument.mData.StereoPan = mStereoPan.value;

				mInstrument.mData.mArpeggio = mArpeggio.isOn;
				mInstrument.mData.mSuccessionType = (eSuccessionType)mSuccession.value;
				mUsePattern.value = (int)mInstrument.mData.mSuccessionType == 2 ? 0 : mUsePattern.value;
				mInstrument.mData.mUsePattern = mUsePattern.value == 1;

				if (force || mPatternLengthSlider.transform.parent.gameObject.activeSelf != (mUsePattern.value == 1))
				{
					mPatternLengthSlider.transform.parent.gameObject.SetActive(mUsePattern.value == 1);
				}
				if (UpdateEffectValue<int>(force, mInstrument.mData.PatternLength, (int)mPatternLengthSlider.value, ref mPatternLengthOutput))
					mInstrument.mData.PatternLength = (int)mPatternLengthSlider.value;
				if (force || mPatternReleaseSlider.transform.parent.gameObject.activeSelf != (mUsePattern.value == 1))
				{
					mPatternReleaseSlider.transform.parent.gameObject.SetActive(mUsePattern.value == 1);
				}
				if (UpdateEffectValue<int>(force, mInstrument.mData.PatternRelease, (int)mPatternReleaseSlider.value, ref mPatternReleaseOutput))
					mInstrument.mData.PatternRelease = (int)mPatternReleaseSlider.value;

				if (UpdateEffectValue<float>(force, mInstrument.mData.AscendDescendInfluence, mLeadVariation.value, ref mLeadVariationOutput))
					mInstrument.mData.AscendDescendInfluence = mLeadVariation.value;
				if (UpdateEffectValue<int>(force, mInstrument.mData.LeadMaxSteps, (int)mLeadMaxSteps.value, ref mLeadMaxStepsOutput))
					mInstrument.mData.LeadMaxSteps = (int)mLeadMaxSteps.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.OddsOfPlayingMultiplierMax, mMultiplierSlider.value, ref mMultiplierText, "x"))
					mInstrument.mData.OddsOfPlayingMultiplierMax = mMultiplierSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.Volume, mVolumeSlider.value, ref mVolumeText))
					mInstrument.mData.Volume = mVolumeSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.RoomSize, mRoomSizeSlider.value, ref mRoomSizeOutput))
					mInstrument.mData.RoomSize = mRoomSizeSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.Reverb, mReverbSlider.value, ref mReverbOutput))
					mInstrument.mData.Reverb = mReverbSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.Echo, mEchoSlider.value, ref mEchoOutput))
					mInstrument.mData.Echo = mEchoSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.EchoDelay, mEchoDelaySlider.value, ref mEchoDelayOutput))
					mInstrument.mData.EchoDelay = mEchoDelaySlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.EchoDecay, mEchoDecaySlider.value, ref mEchoDecayOutput))
					mInstrument.mData.EchoDecay = mEchoDecaySlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.Flanger, mFlangerSlider.value, ref mFlangerOutput))
					mInstrument.mData.Flanger = mFlangerSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.Distortion, mDistortionSlider.value, ref mDistortionOutput))
					mInstrument.mData.Distortion = mDistortionSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.Chorus, mChorusSlider.value, ref mChorusOutput))
					mInstrument.mData.Chorus = mChorusSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.OddsOfUsingChordNotes, mOddsOfPlayingChordNoteSlider.value, ref mOddsOfPlayingChordNoteText))
					mInstrument.mData.OddsOfUsingChordNotes = mOddsOfPlayingChordNoteSlider.value;
				if (UpdateEffectValue<float>(force, mInstrument.mData.AudioSourceVolume, mAudioGroupVolume.value, ref mAudioGroupVolumeOutput))
					mInstrument.mData.AudioSourceVolume = mAudioGroupVolume.value;

				UpdateMixerEffectValue(mMixerRoomSizeString, mRoomSizeSlider.value);
				UpdateMixerEffectValue(mMixerReverbString, mReverbSlider.value);
				UpdateMixerEffectValue(mMixerEchoString, mEchoSlider.value);
				UpdateMixerEffectValue(mMixerEchoDelayString, mEchoDelaySlider.value);
				UpdateMixerEffectValue(mMixerEchoDecayString, mEchoDecaySlider.value);
				UpdateMixerEffectValue(mMixerFlangeString, mFlangerSlider.value);
				UpdateMixerEffectValue(mMixerDistortionString, mDistortionSlider.value);
				UpdateMixerEffectValue(mMixerChorusString, mChorusSlider.value);

				GetOctaves();
			}
			else if (mInstrumentListUI.mInstrumentIcons.Count > 0)
			{
				mInstrumentListUI.mInstrumentIcons[mInstrumentListUI.mInstrumentIcons.Count - 1].ToggleSelected();
			}
		}

		/// Updates our effects strings. Just a workaround to avoid generating GC every tick.
		private void UpdateInstrumentEffectsStrings(int index)
		{
			mMixerRoomSizeString = "RoomSize" + mInstrument.InstrumentIndex;
			mMixerReverbString = "Reverb" + mInstrument.InstrumentIndex;
			mMixerEchoString = "Echo" + mInstrument.InstrumentIndex;
			mMixerEchoDelayString = "EchoDelay" + mInstrument.InstrumentIndex;
			mMixerEchoDecayString = "EchoDecay" + mInstrument.InstrumentIndex;
			mMixerFlangeString = "Flange" + mInstrument.InstrumentIndex;
			mMixerDistortionString = "Distortion" + mInstrument.InstrumentIndex;
			mMixerChorusString = "Chorus" + mInstrument.InstrumentIndex;
		}

		/// Updates an effect value
		private bool UpdateEffectValue<T>(bool force, T a, T b, ref Text argText, string optionalText = "")where T : IComparable
		{
			if (force || Comparer<T>.Default.Compare(a, b) != 0)
			{
				argText.text = optionalText + b;
				return true;
			}
			return false;
		}

		/// Updates a mixer effect value
		private void UpdateMixerEffectValue(string first, float second)
		{
			float check;
			mMusicGenerator.mMixer.GetFloat(first, out check);
			if (check != second)
				mMusicGenerator.mMixer.SetFloat(first, second);
		}

		///Sets octaves from instrument.
		private void SetOctavesFrominstrument()
		{
			mOctave1.isOn = mInstrument.mData.mOctavesToUse.Contains(0);
			mOctave2.isOn = mInstrument.mData.mOctavesToUse.Contains(1);;
			mOctave3.isOn = mInstrument.mData.mOctavesToUse.Contains(2);;
		}

		///returns selected octaves:
		private void GetOctaves()
		{
			mOctavesToUse.Clear();
			if (mOctave1.isOn)
				mOctavesToUse.Add(0);
			if (mOctave2.isOn)
				mOctavesToUse.Add(1);
			if (mOctave3.isOn)
				mOctavesToUse.Add(2);

			// Safety check.
			if (mOctavesToUse.Count == 0)
			{
				mOctavesToUse.Add(0);
				mOctave1.isOn = true;
			}

			mInstrument.mData.mOctavesToUse.Clear();
			for (int i = 0; i < mOctavesToUse.Count; i++)
				mInstrument.mData.mOctavesToUse.Add(mOctavesToUse[i]);
		}

		///toggles the lead setting on/off
		public void ToggleLead()
		{
			if (mFreeMelody.isOn)
			{
				mUsePattern.value = mInstrument.mData.mUsePattern ? 1 : 0;
			}
			mMeasureEditor.UIToggleHelperNotes();
		}
	}
}