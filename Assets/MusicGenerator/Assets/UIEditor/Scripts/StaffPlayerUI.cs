using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProcGenMusic
{
	public class StaffPlacer
	{
		private RaycastHit2D mRaycast;
		private int mStaffLinesLayer = LayerMask.NameToLayer("UI"); //hijacking this, as I don't think I can add layers to the project via script, and they're project level, not scene.
		private int mEditorNoteLayer = LayerMask.NameToLayer("UI"); //hijacking this, as I don't think I can add layers to the project via script, and they're project level, not scene.

		public EditorNote GetEditorNoteToRemove()
		{
			mRaycast = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), -Vector3.forward, 200.0f, 1 << mEditorNoteLayer);
			if (mRaycast)
			{
				return mRaycast.collider.gameObject.GetComponent<EditorNote>();
			}
			return null;
		}

		public void CheckRaycast()
		{
			mRaycast = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), -Vector3.forward, 200.0f, 1 << mStaffLinesLayer);
			if (mRaycast)
			{
				HoverObj mRaycaster = mRaycast.collider.gameObject.GetComponent<HoverObj>();
				if (mRaycaster != null)
				{
					mRaycaster.isOver = true;
				}
			}
		}
	}

	public class StaffPlayerUI : HelperSingleton<StaffPlayerUI>
	{
		[SerializeField, Tooltip("Our staff player camera")]
		private Transform mStaffPlayerTransform;

		[SerializeField]
		private List<Transform> mStaffLines = new List<Transform>();
		[SerializeField]
		private List<Transform> mBarLines = new List<Transform>();
		[SerializeField]
		private Transform mBarlineFirst = null;
		[SerializeField]
		private Transform mBarlineLast = null;
		private List<HoverObj> mHoverObjects = new List<HoverObj>();
		[SerializeField]
		private List<SpriteRenderer> mBarLinesImages = new List<SpriteRenderer>();
		[SerializeField]
		private Color mBarSelected = Color.white;
		[SerializeField]
		private Color mBarRepeating = Color.white;

		private float mTotalBarlineDistance = 0.0f;
		[SerializeField]
		private Animator mLoadingSeqAnimator = null;

		private List<StaffPlayerNote> mPlayedNotes = new List<StaffPlayerNote>();
		private List<StaffPlayerNote> mShadowNotes = new List<StaffPlayerNote>();
		private List<StaffPlayerNote> mEditorNotes = new List<StaffPlayerNote>();
		private List<EditorNote> mPlacedEditorNotes = new List<EditorNote>();

		[SerializeField]
		public List<Color> mColors = new List<Color>();

		private MusicGenerator mMusicGenerator = null;
		private int mMaxPlayedNotes = 0;

		[SerializeField]
		private GameObject mBaseNoteImage = null;
		[SerializeField]
		private GameObject mBaseShadowImage = null;
		[SerializeField]
		private GameObject mBaseEditorNote = null;
		[SerializeField]
		private GameObject mBasePlayedEditorNote = null;

		private int mCurrentNote = 0;
		private int mCurrentPlacedEditorNote = 0;
		private int mCurrentEditorNote = 0;

		public InputField mExportFileName = null;

		public Dropdown mPreset = null;
		private MusicGeneratorUIPanel mGeneratorUIPanel = null;
		private MeasureEditor mMeasureEditor = null;

		[SerializeField, Tooltip("Our timer bar")]
		private SpriteRenderer mTimerBar = null;

		private InstrumentPanelUI mInstrumentPanel = null;
		public Dropdown mTimeSignatureDropdown = null;

		[SerializeField]
		private Text mCurrentProgStep = null;

		private StaffPlacer mNoteToggler;

		private int[] mCurrentChordProgression = new int[4] {-1, -1, -1, -1 };
		private int mCurrentStep = 0;

		public override void Awake()
		{
			base.Awake();
			mNoteToggler = new StaffPlacer();

			Component[] components2 = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components2)
			{
				if (cp.name == "Presets")
					mPreset = cp.gameObject.GetComponentInChildren<Dropdown>();
				if (cp.name == "Export")
				{
					mExportFileName = cp.gameObject.GetComponentInChildren<InputField>();
				}
				if (cp.name == "TimeSignature")
					mTimeSignatureDropdown = cp.gameObject.GetComponentInChildren<Dropdown>();
			}

			for (int i = 0; i < mBarLines.Count; i++)
				mBarLinesImages.Add(mBarLines[i].GetComponentInChildren<SpriteRenderer>());

			for (int i = 0; i < mStaffLines.Count; i++)
			{
				mStaffLines[i].gameObject.AddComponent<HoverObj>();
				mHoverObjects.Add(mStaffLines[i].GetComponent<HoverObj>());
			}
			mTotalBarlineDistance = mBarlineLast.localPosition.x - mBarlineFirst.localPosition.x;
		}

		/// Changes our time signature.
		public void ChangeTimeSignature(int timeSignature = -1)
		{
			if (timeSignature != -1) //if we're actually trying to force the ui to change. The UI will pass in -1.
			{
				mTimeSignatureDropdown.value = timeSignature;
				return;
			}
			eTimeSignature signature = (eTimeSignature)mTimeSignatureDropdown.value;

			InstrumentSet set = (MusicGenerator.Instance.mState >= eGeneratorState.editorInitializing) ? mMeasureEditor.mCurrentInstSet : MusicGenerator.Instance.mInstrumentSet;
			set.SetTimeSignature(signature);

			float xPos = mBarLines[0].localPosition.x;
			float nextPos = mTotalBarlineDistance / (set.mTimeSignature.mStepsPerMeasure - 1);
			for (int i = 0; i < mBarLinesImages.Count; i++)
			{
				if (i < set.mTimeSignature.mStepsPerMeasure)
				{
					mBarLinesImages[i].enabled = true;
					Vector3 pos = mBarLines[i].localPosition;
					mBarLines[i].localPosition = new Vector3(xPos, pos.y, pos.z);
					xPos += nextPos;
				}
				else
				{
					mBarLinesImages[i].enabled = false;
				}
			}
		}

		public void Init(MusicGenerator managerIN)
		{
			mMusicGenerator = managerIN;
			UIManager uimanager = UIManager.Instance;
			mGeneratorUIPanel = uimanager.mGeneratorUIPanel;
			mMeasureEditor = uimanager.mMeasureEditor;
			mInstrumentPanel = uimanager.mInstrumentPanelUI;
			mMaxPlayedNotes = MusicGenerator.mMaxInstruments * 4 * 16; //number of instruments, times size of chord * number of steps per measure 

			for (int i = 0; i < mMaxPlayedNotes; i++) //In theory, the max number of notes that might play, given maxInstruments.
			{
				mPlayedNotes.Add((Instantiate(mBaseNoteImage, mStaffPlayerTransform)as GameObject).GetComponent<StaffPlayerNote>());
				mPlayedNotes[i].transform.position = new Vector3(-10000, -10000, 0);
				mPlayedNotes[i].gameObject.SetActive(false);

				mShadowNotes.Add((Instantiate(mBaseShadowImage, mStaffPlayerTransform)as GameObject).GetComponent<StaffPlayerNote>());
				mShadowNotes[i].transform.position = new Vector3(-10000, -10000, 0);
				mShadowNotes[i].gameObject.SetActive(false);

				mEditorNotes.Add((Instantiate(mBaseEditorNote, mStaffPlayerTransform)as GameObject).GetComponent<StaffPlayerNote>());
				mEditorNotes[i].transform.position = new Vector3(-10000, -10000, 0);
				mEditorNotes[i].gameObject.SetActive(false);

				mPlacedEditorNotes.Add((Instantiate(mBasePlayedEditorNote, mStaffPlayerTransform)as GameObject).GetComponent<EditorNote>());
				mPlacedEditorNotes[i].transform.position = new Vector3(-10000, -10000, 0);
				mPlacedEditorNotes[i].gameObject.SetActive(false);
			}
			mTimeSignatureDropdown.value = (int)mMusicGenerator.mInstrumentSet.mTimeSignature.Signature;
			//ChangeTimeSignature(eTimeSignature.ThreeFour);
		}

		void Update()
		{
			if (mMusicGenerator == null)
			{
				return;
			}

			if (mMusicGenerator.mState == eGeneratorState.editorInitializing)
				return;

			InstrumentSet set = (mMusicGenerator.mState >= eGeneratorState.editorInitializing) ? mMeasureEditor.mCurrentInstSet : mMusicGenerator.mInstrumentSet;

			if (mCurrentChordProgression != mMusicGenerator.ChordProgression)
			{
				UpdateChordProgressionText(set);
			}

			float sixteenthStepTimer = set.mSixteenthStepTimer;
			int sixteenStepsTaken = set.SixteenthStepsTaken - 1;
			float sixteenthMeasure = set.mBeatLength;

			float dist = mBarLines[1].position.x - mBarLines[0].position.x;
			float perc = (sixteenthStepTimer / sixteenthMeasure);

			if (sixteenStepsTaken < 0)
			{
				mTimerBar.gameObject.transform.position = new Vector3(mBarLines[0].position.x,
					mTimerBar.transform.position.y, mTimerBar.transform.position.z);
			}
			else
			{
				mTimerBar.gameObject.transform.position = new Vector3(mBarLines[sixteenStepsTaken].position.x +
					dist * (1.0f - perc), mTimerBar.transform.position.y, mTimerBar.transform.position.z);
			}
		}

		/// Updates our chord progression text
		private void UpdateChordProgressionText(InstrumentSet set)
		{
			bool dirty = false;
			for (int i = 0; i < mMusicGenerator.ChordProgression.Length; i++)
			{
				if (mCurrentChordProgression[i] != mMusicGenerator.ChordProgression[i])
				{
					mCurrentChordProgression[i] = mMusicGenerator.ChordProgression[i];
					dirty = true;
				}
			}
			int stepsTaken = set.ProgressionStepsTaken >= 0 ? set.ProgressionStepsTaken : 0;
			if (mCurrentChordProgression[stepsTaken] != mCurrentStep)
			{
				mCurrentStep = mCurrentChordProgression[stepsTaken];
				dirty = true;
			}

			if (dirty)
			{
				mCurrentProgStep.text = "{" +
					mCurrentChordProgression[0] + "-" +
					mCurrentChordProgression[1] + "-" +
					mCurrentChordProgression[2] + "-" +
					mCurrentChordProgression[3] + "}" + " :" +
					mCurrentStep;
			}
		}

		/// Displays a note on the staff player:
		public void DisplayNote(int noteIN, int colorIN, bool useShadow, InstrumentSet setIN, bool strummed = false)
		{
			int sixteenStepsTaken = setIN.SixteenthStepsTaken;

			if (useShadow == false)
			{
				mPlayedNotes[mCurrentNote].gameObject.SetActive(true);
				if (strummed == false)
					mPlayedNotes[mCurrentNote].transform.position = new Vector3(mBarLines[sixteenStepsTaken].position.x, mStaffLines[noteIN].transform.position.y, 0);
				else
					mPlayedNotes[mCurrentNote].transform.position = new Vector3(mTimerBar.transform.position.x, mStaffLines[noteIN].transform.position.y, 0);
				mPlayedNotes[mCurrentNote].mBaseImage.color = mColors[colorIN];
			}
			else
			{
				mShadowNotes[mCurrentNote].gameObject.SetActive(true);
				if (strummed == false)
					mShadowNotes[mCurrentNote].transform.position = new Vector2(mBarLines[sixteenStepsTaken].position.x, mStaffLines[noteIN].transform.position.y);
				else
					mShadowNotes[mCurrentNote].transform.position = new Vector2(mTimerBar.transform.position.x, mStaffLines[noteIN].transform.position.y);

				Component[] components2 = mShadowNotes[mCurrentNote].GetComponentsInChildren<Image>();
				foreach (Component cp in components2)
				{
					if (cp.name != "shadow")
						cp.GetComponent<Image>().color = mColors[colorIN];
				}
			}
			IncreaseCurrentNoteCount(ref mCurrentNote, mMaxPlayedNotes);
		}

		public void PlayLoadingSequence(bool isLoading)
		{
			mLoadingSeqAnimator.SetBool("isLoading", isLoading);
		}

		/// removes all notes from the staff player:
		public void ClearNotes(bool clearSetNotes = false, bool clearHighlightedNotes = false)
		{
			for (int i = 0; i < mPlayedNotes.Count; i++)
			{
				mPlayedNotes[i].transform.position = new Vector2(-10000, -10000);
				mPlayedNotes[i].gameObject.SetActive(false);
			}
			for (int i = 0; i < mShadowNotes.Count; i++)
			{
				mShadowNotes[i].transform.position = new Vector2(-10000, -10000);
				mShadowNotes[i].gameObject.SetActive(false);
			}

			if (clearSetNotes)
			{

				for (int i = 0; i < mPlacedEditorNotes.Count; i++)
				{
					mPlacedEditorNotes[i].transform.position = new Vector2(-10000, -10000);
					mPlacedEditorNotes[i].gameObject.SetActive(false);
				}
			}
			if (clearHighlightedNotes)
			{
				for (int i = 0; i < mEditorNotes.Count; i++)
				{
					mEditorNotes[i].transform.position = new Vector2(-10000, -10000);
					mEditorNotes[i].GetComponentInChildren<TextMesh>().text = "";
					mEditorNotes[i].gameObject.SetActive(false);
				}
			}

			mCurrentNote = 0;
			mCurrentPlacedEditorNote = 0;
		}

		/// called on export button press. Exports all config files for this music generator configuration.
		public void ExportFile()
		{
			if (mMusicGenerator.mState < eGeneratorState.editing)
			{
				if (mExportFileName.textComponent.text == "")
					return;
				Debug.Log("exporting configuration " + mExportFileName.textComponent.text);

				MusicFileConfig.SaveConfiguration(mExportFileName.textComponent.text);
				if (mGeneratorUIPanel.mPresetFileNames.Contains(mExportFileName.textComponent.text) == false)
				{
					mGeneratorUIPanel.mPresetFileNames.Add(mExportFileName.textComponent.text);
					AddPresetOption(mExportFileName.textComponent.text);
				}

				mGeneratorUIPanel.UpdateEffectsSliders();
			}
			else
				mMeasureEditor.SaveClip(mExportFileName.textComponent.text);
		}

		/// Adds this preset to our options. This is called on a delay waiting for the exported file to finish writing.
		public void AddPresetOption(string fileNameIN)
		{
			bool fileExists = false;
			for (int i = 0; i < mPreset.options.Count; i++)
			{
				if (mPreset.options[i].text == fileNameIN)
					fileExists = true;
			}
			if (fileExists == false)
			{
				Dropdown.OptionData newOption = new Dropdown.OptionData();
				newOption.text = (fileNameIN == "AAADefault") ? "Default" : fileNameIN;
				mPreset.options.Add(newOption);
			}
		}

		/// Sets the color for the bar line counter.
		public void SetBarlineColor(int lineIN, bool isRepeating)
		{
			if (lineIN == 0 || lineIN == -1)
			{
				for (int i = 0; i < mBarLinesImages.Count; i++)
					mBarLinesImages[i].color = Color.white;
			}

			if (lineIN != -1)
			{
				mBarLinesImages[lineIN].color = isRepeating ? mBarRepeating : mBarSelected;
			}
		}

		/// Shows highlighted helper notes for the measure editor.
		public void ShowHighlightedNotes(Instrument instrumentIN)
		{
			if (instrumentIN.mData.mSuccessionType == eSuccessionType.lead)
				ShowLeadNotes(instrumentIN);
			else
				ShowRhythmNotes(instrumentIN);
		}

		/// Shows highlights for lead notes when in the measure editor.
		public void ShowLeadNotes(Instrument instrumentIN)
		{
			mCurrentEditorNote = 0;

			for (int i = 0; i < mMeasureEditor.mCurrentInstSet.mTimeSignature.mStepsPerMeasure; i++)
			{
				if (i % 4 != 0)
					ShowLeadNote(instrumentIN, i);
				else
					ShowRhythmNote(instrumentIN, i);
			}
		}

		/// Shows a single lead note in the editor.
		public void ShowLeadNote(Instrument instrumentIN, int step)
		{
			int totalScaleNotes = Instrument.mMajorScale.Length * 3;
			int totalNotes = MusicGenerator.mMaxInstrumentNotes;
			int scaleLength = Instrument.mMusicScales[mMeasureEditor.mScale.value].Length;
			int[] scale = Instrument.mMusicScales[mMeasureEditor.mScale.value];
			TimeSignature signature = mMeasureEditor.mCurrentInstSet.mTimeSignature;

			if (step % (signature.mTimestepNumInverse[(int)instrumentIN.mData.mTimeStep]) == 0)
			{
				for (int j = 0; j < totalScaleNotes; j++)
				{
					mEditorNotes[mCurrentEditorNote].gameObject.SetActive(true);

					int index = mMeasureEditor.mKey.value;
					for (int x = 0; x < j; x++)
					{
						int subindex = (x + mMeasureEditor.mMode.value) % scaleLength;
						index += scale[subindex];
					}
					index = index % totalNotes;
					Vector2 position = new Vector2(mBarLines[step].position.x, mStaffLines[index].transform.position.y);
					mEditorNotes[mCurrentEditorNote].transform.position = position;

					mEditorNotes[mCurrentEditorNote].mBaseImage.color = new Color(
						mColors[(int)instrumentIN.mData.mStaffPlayerColor].r,
						mColors[(int)instrumentIN.mData.mStaffPlayerColor].g,
						mColors[(int)instrumentIN.mData.mStaffPlayerColor].b, 0.4f
					);

					IncreaseCurrentNoteCount(ref mCurrentEditorNote, mMaxPlayedNotes);
				}
			}
		}

		//shows the possible notes for a rhythm instrument:
		private void ShowRhythmNotes(Instrument instrumentIN)
		{
			TimeSignature signature = mMeasureEditor.mCurrentInstSet.mTimeSignature;
			mCurrentEditorNote = 0;
			for (int i = 0; i < signature.mStepsPerMeasure; i++)
			{
				if (i % (signature.mTimestepNumInverse[(int)instrumentIN.mData.mTimeStep]) == 0)
				{
					ShowRhythmNote(instrumentIN, i);
				}
			}
		}

		/// Shows a single rhythm note for the measure editor.
		public void ShowRhythmNote(Instrument instrumentIN, int index)
		{
			for (int j = 0; j < Instrument.mMajorScale.Length * 3; j++)
			{
				if (j % 7 == 0 || j % 7 == 2 || j % 7 == 4 || j % 7 == 6)
				{
					SetSingleEditorNote(index, j, instrumentIN, mEditorNotes, mCurrentEditorNote);
					Color tempColor;
					if (j % 7 == 6)
					{
						tempColor = new Color(
							Color.black.r,
							Color.black.g,
							Color.black.b, 0.4f
						);
					}
					else
					{
						tempColor = new Color(
							mColors[(int)instrumentIN.mData.mStaffPlayerColor].r,
							mColors[(int)instrumentIN.mData.mStaffPlayerColor].g,
							mColors[(int)instrumentIN.mData.mStaffPlayerColor].b, 0.4f
						);
					}
					mEditorNotes[mCurrentEditorNote].gameObject.SetActive(true);
					mEditorNotes[mCurrentEditorNote].mBaseImage.color = tempColor;
					if (j % 7 == 6)
						mEditorNotes[mCurrentEditorNote].gameObject.GetComponentInChildren<TextMesh>().text = "7";
					else if (j % 7 == 0)
						mEditorNotes[mCurrentEditorNote].gameObject.GetComponentInChildren<TextMesh>().text = "R";
					IncreaseCurrentNoteCount(ref mCurrentEditorNote, mMaxPlayedNotes);
				}
			}
		}

		/// sets the selected notes for this instrument this measure.
		public void SetMeasure(Instrument instrumentIN)
		{
			ClearNotes(true, true);
			mCurrentPlacedEditorNote = 0;
			int[][] clips = instrumentIN.mClipNotes[mMeasureEditor.mCurrentMeasure.value];
			for (int i = 0; i < clips.Length; i++)
			{
				for (int j = 0; j < clips[i].Length; j++)
				{
					if (clips[i][j] != -1)
					{
						Vector2 pos = new Vector2(0, 0);
						pos.x = mBarLines[i].position.x;
						pos.y = mStaffLines[clips[i][j]].transform.position.y;
						mPlacedEditorNotes[mCurrentPlacedEditorNote].gameObject.SetActive(true);
						mPlacedEditorNotes[mCurrentPlacedEditorNote].transform.position = pos;
						mPlacedEditorNotes[mCurrentPlacedEditorNote].mBaseImage.color =
							mColors[(int)instrumentIN.mData.mStaffPlayerColor];
						IncreaseCurrentNoteCount(ref mCurrentPlacedEditorNote, mMaxPlayedNotes);
					}
				}
			}
		}

		private void IncreaseCurrentNoteCount(ref int valueIN, int max)
		{
			valueIN = valueIN + 1 < max ? valueIN + 1 : 0;
		}

		/// Sets a single Editor note in the staff player.
		public void SetSingleEditorNote(int timestep, int note, Instrument instrumentIN, List<StaffPlayerNote> editorNotesIN, int currentNote)
		{
			int index = mMeasureEditor.mKey.value;
			int scale = mMeasureEditor.mScale.value;

			int progressionRate = mMeasureEditor.mCurrentInstSet.GetProgressionRate(mMeasureEditor.mProgressionRate.value);
			int chordStep = (int)(timestep / progressionRate) % 4;

			int stepToTake = mMusicGenerator.ChordProgression[chordStep];
			for (int x = 0; x < note + stepToTake; x++)
			{
				int subindex = (x + (int)mMeasureEditor.mMode.value) % Instrument.mMusicScales[scale].Length;
				subindex = subindex % Instrument.mMusicScales[scale].Length;
				index += Instrument.mMusicScales[scale][subindex];
			}
			index += (Instrument.mOctave * (int)(note / Instrument.mMusicScales[scale].Length));
			index = index % MusicGenerator.mMaxInstrumentNotes;
			editorNotesIN[currentNote].gameObject.SetActive(true);
			editorNotesIN[currentNote].transform.position = new Vector2(mBarLines[timestep].position.x, mStaffLines[index].transform.position.y);
		}

		/// Shows the set editor notes:
		public void ShowSetEditorNotes(int[][] notesIN, int instrumentIndex)
		{
			List<Instrument> instruments = new List<Instrument>();
			if (mMusicGenerator.mState >= eGeneratorState.editorInitializing)
				instruments = MeasureEditor.Instance.mLoadedClip.mInstrumentSet.mInstruments;
			else
				instruments = mMusicGenerator.mInstrumentSet.mInstruments;

			for (int i = 0; i < mMeasureEditor.mCurrentInstSet.mTimeSignature.mStepsPerMeasure; i++)
			{
				for (int j = 0; j < notesIN[i].Length; j++)
				{
					if (notesIN[i][j] != -1)
					{
						Vector2 pos = new Vector2(0, 0);
						pos.x = mBarLines[i].position.x;
						pos.y = mStaffLines[notesIN[i][j]].transform.position.y;
						mPlacedEditorNotes[mCurrentPlacedEditorNote].gameObject.SetActive(true);
						mPlacedEditorNotes[mCurrentPlacedEditorNote].index = new Vector2(i, notesIN[i][j]);
						mPlacedEditorNotes[mCurrentPlacedEditorNote].transform.position = pos;
						mPlacedEditorNotes[mCurrentPlacedEditorNote].mBaseImage.color =
							mColors[(int)instruments[instrumentIndex].mData.mStaffPlayerColor];
						IncreaseCurrentNoteCount(ref mCurrentPlacedEditorNote, mMaxPlayedNotes);
					}
				}
			}
		}

		public void RemoveNote(EditorNote note)
		{
			Instrument instrument = mInstrumentPanel.mInstrument;
			if (instrument.RemoveClipNote((int)note.index.x, (int)note.index.y, mMeasureEditor.mCurrentMeasure.value))
			{
				note.transform.position = new Vector2(-10000, -10000);
				note.gameObject.SetActive(false);
				mMeasureEditor.UIToggleAllInstruments(true);
			}
		}
		/// if left/right mouse click are pressed, adds or removes an editor note, respectively.
		public void SetEditorNotes(Instrument instrumentIN)
		{

			if (Input.GetKeyDown("mouse 0"))
			{
				/// remove notes if needed
				EditorNote noteToRemove = mNoteToggler.GetEditorNoteToRemove();
				if (noteToRemove != null)
				{
					RemoveNote(noteToRemove);
					return;
				}

				mNoteToggler.CheckRaycast();
				int note = 0;
				int timestep = 0;

				Vector2 pos = new Vector2(0, 0);
				bool isOver = false;
				for (int i = 0; i < mHoverObjects.Count; i++)
				{
					if (mHoverObjects[i].isOver)
					{
						pos.y = mStaffLines[i].transform.position.y;
						note = i;
						isOver = true;
					}
				}

				if (isOver == false)
					return;

				float nearest = 100000;
				Vector2 mousePos = Input.mousePosition;
				for (int j = 0; j < mMeasureEditor.mCurrentInstSet.mTimeSignature.mStepsPerMeasure; j++)
				{
					Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, mBarLines[j].position);
					float dist = Mathf.Abs(mousePos.x - screenPos.x);
					if (dist < nearest)
					{
						nearest = dist;
						pos.x = mBarLines[j].position.x;
						timestep = j;
					}
				}

				if (instrumentIN.AddClipNote(timestep, note, mMeasureEditor.mCurrentMeasure.value))
				{
					mPlacedEditorNotes[mCurrentPlacedEditorNote].transform.position = pos;
					mPlacedEditorNotes[mCurrentPlacedEditorNote].mBaseImage.color = mColors[(int)instrumentIN.mData.mStaffPlayerColor];
					mPlacedEditorNotes[mCurrentPlacedEditorNote].index = new Vector2(timestep, note);
					IncreaseCurrentNoteCount(ref mCurrentPlacedEditorNote, mMaxPlayedNotes);
					mMeasureEditor.UIToggleAllInstruments();
				}
			}
		}
	}
}