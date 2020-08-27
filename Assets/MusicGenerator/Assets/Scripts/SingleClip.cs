using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// A single Clip. Used for playing a specific set of instruments/notes. Create via the Measure Editor in the executable.
	/// </summary>
	public class SingleClip : MonoBehaviour
	{
		private float mTempo = 0.0f; ///< our tempo for this clip.

		public bool mIsPlaying { get; set; } ///< whether we're playing or not.
		public int mStaffPlayerColor { get; private set; } ///< used only in the measure editor UI.
		public float mSixteenthStepTimer { get; private set; }
		public int mSixteenthStepCount { get; private set; }
		public int mMeasureStepCount { get; private set; }
		public float mSixteenthMeasure { get; private set; }
		public InstrumentSet mInstrumentSet { get; private set; }
		public ClipMeasure mClipMeasure { get; private set; }
		public eClipState mState { get; private set; }
		public bool mIsRepeating { get; set; }
		public int mNumMeasures { get; private set; }

		void Awake()
		{
			mClipMeasure = new ClipMeasure();
			mIsPlaying = false;
			mStaffPlayerColor = 0;
			mSixteenthStepTimer = 0.0f;
			mSixteenthStepCount = 0;
			mMeasureStepCount = 0;
			mSixteenthMeasure = 0.0f;
			mInstrumentSet = null;
			mState = eClipState.Stop;
			mIsRepeating = false;
			mNumMeasures = 1;
		}

		/// <summary>
		/// async init.
		/// </summary>
		/// <param name="save"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public IEnumerator AsyncInit(ClipSave save, System.Action<bool> callback)
		{
			mInstrumentSet = new InstrumentSet();
			mInstrumentSet.Init();
			mTempo = save.mTempo;
			mNumMeasures = save.mNumberOfMeasures;
			mSixteenthMeasure = 60 / mTempo;
			mIsRepeating = save.mClipIsRepeating;

			mInstrumentSet.mData.Tempo = mTempo;
			mInstrumentSet.SetProgressionRate(save.mProgressionRate);
			mInstrumentSet.mData.RepeatMeasuresNum = mNumMeasures;
			bool isFinished = false;
			StartCoroutine(AsyncLoadInstruments(save, ((x) => { isFinished = x; })));
			yield return new WaitUntil(() => isFinished);
			callback(isFinished);
			yield return null;
		}

		/// <summary>
		/// non-async init.
		/// </summary>
		/// <param name="save"></param>
		public void Init(ClipSave save)
		{
			StartInitialization(save);
			LoadInstruments(save);
		}

		private void StartInitialization(ClipSave save)
		{
			mInstrumentSet = new InstrumentSet();
			mInstrumentSet.Init();
			mInstrumentSet.LoadData(new InstrumentSetData());
			mTempo = save.mTempo;
			mNumMeasures = save.mNumberOfMeasures;
			mSixteenthMeasure = 60 / mTempo;
			mIsRepeating = save.mClipIsRepeating;

			mInstrumentSet.mData.Tempo = mTempo;
			mInstrumentSet.SetProgressionRate(save.mProgressionRate);
			mInstrumentSet.mData.RepeatMeasuresNum = mNumMeasures;
		}

		/// <summary>
		/// Resets a clip
		/// </summary>
		public void ResetClip()
		{
			mClipMeasure.ResetMeasure(mInstrumentSet, null, true);
		}

		/// <summary>
		///  Set the clip state:
		/// </summary>
		/// <param name="stateIN"></param>
		public void SetState(eClipState stateIN)
		{
			mState = stateIN;
			switch (mState)
			{
				case eClipState.Pause:
					break;
				case eClipState.Play:
					mInstrumentSet.ResetRepeatCount();
					break;
				case eClipState.Stop:
					ResetClip();
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Clip update:
		/// </summary>
		void Update()
		{
			switch (mState)
			{
				case eClipState.Play:
					if (mInstrumentSet.mRepeatCount <= mNumMeasures)
						mClipMeasure.PlayMeasure(mInstrumentSet);
					else if (mIsRepeating)
					{
						ResetClip();
					}
					else
					{
						SetState(eClipState.Stop);
						ResetClip();
					}
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// load the clip:
		/// </summary>
		/// <param name="save"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public IEnumerator AsyncLoadInstruments(ClipSave save, System.Action<bool> callback)
		{
			for (int i = 0; i < save.mClipInstrumentSaves.Count; i++)
			{
				ClipInstrumentSave instrumentSave = save.mClipInstrumentSaves[i];
				mInstrumentSet.mInstruments.Add(new Instrument());
				mInstrumentSet.mInstruments[i].Init(i);

				Instrument instrument = mInstrumentSet.mInstruments[i];

				for (int x = 0; x < instrumentSave.mClipMeasures.Count; x++)
				{
					for (int y = 0; y < instrumentSave.mClipMeasures[x].timestep.Count; y++)
					{
						for (int z = 0; z < instrumentSave.mClipMeasures[x].timestep[y].notes.Count; z++)
							instrument.mClipNotes[x][y][z] = instrumentSave.mClipMeasures[x].timestep[y].notes[z];
					}
				}
				int index = 999;
				yield return StartCoroutine(MusicGenerator.Instance.AsyncLoadBaseClips(instrumentSave.mInstrumentType, ((x) => { index = x; })));
				//yield return new WaitUntil(() => index != 999);
				instrument.mData.InstrumentType = instrumentSave.mInstrumentType;
				instrument.mData.Volume = instrumentSave.mVolume;
				instrument.InstrumentTypeIndex = index;
				instrument.mData.mStaffPlayerColor = (eStaffPlayerColors)instrumentSave.mStaffPlayerColor;
				instrument.mData.mTimeStep = instrumentSave.mTimestep;
				instrument.mData.mSuccessionType = instrumentSave.mSuccessionType;
				instrument.mData.StereoPan = instrumentSave.mStereoPan;
			}
			callback(true);
			yield return null;
		}

		///load the clip:
		public void LoadInstruments(ClipSave save)
		{
			for (int i = 0; i < save.mClipInstrumentSaves.Count; i++)
			{
				ClipInstrumentSave instrumentSave = save.mClipInstrumentSaves[i];
				mInstrumentSet.mInstruments.Add(new Instrument());
				mInstrumentSet.mInstruments[i].Init(i);

				Instrument instrument = mInstrumentSet.mInstruments[i];

				for (int x = 0; x < instrumentSave.mClipMeasures.Count; x++)
				{
					for (int y = 0; y < instrumentSave.mClipMeasures[x].timestep.Count; y++)
					{
						for (int z = 0; z < instrumentSave.mClipMeasures[x].timestep[y].notes.Count; z++)
							instrument.mClipNotes[x][y][z] = instrumentSave.mClipMeasures[x].timestep[y].notes[z];
					}
				}
				instrument.mData.Volume = instrumentSave.mVolume;
				int index = MusicGenerator.Instance.LoadBaseClips(instrumentSave.mInstrumentType);
				instrument.mData.InstrumentType = instrumentSave.mInstrumentType;
				instrument.InstrumentTypeIndex = index;
				instrument.mData.mStaffPlayerColor = ((eStaffPlayerColors)instrumentSave.mStaffPlayerColor);
				instrument.mData.mTimeStep = instrumentSave.mTimestep;
				instrument.mData.mSuccessionType = (instrumentSave.mSuccessionType);
				instrument.mData.StereoPan = (instrumentSave.mStereoPan);
			}
		}
	}
}