using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	[Serializable]
	/// <summary>
	/// The set of instruments for a configuration. Handles the timing, playing, repeating and other settings for its instruments.
	/// For normal uses of the generator, you should not need to call any of the public functions in here, as they're handled by the
	/// MusicGenerator or the SingleClip logic.
	/// </summary>
	public class InstrumentSet
	{
		///<summary> anything less than this isn't a functional tempo. Edit at your own risk.</summary>
		public const float mMinTempo = 1.0f;

		///<summary> anything greater than this is likely to cause problems. Edit at your own risk.</summary>
		public const float mMaxTempo = 350.0f;

		///<summary> max number of steps per progression. Currently only support 4</summary>
		static readonly public int mMaxFullstepsTaken = 4;

		///<summary> repeat count for sixteenth steps</summary>
		private int mSixteenthRepeatCount = 0;
		///<summary> repeat count for sixteenth steps</summary>
		public int SixteenthRepeatCount { get { return mSixteenthRepeatCount; } set { mSixteenthRepeatCount = Mathf.Clamp(value, 0, Instrument.mStepsPerMeasure * mData.RepeatMeasuresNum); } }

		///<summary> number of 1/16 steps taken for current measure </summary>
		private int mSixteenthStepsTaken = 0;
		///<summary> number of 1/16 steps taken for current measure</summary>
		public int SixteenthStepsTaken { get { return mSixteenthStepsTaken; } set { mSixteenthStepsTaken = Mathf.Clamp(value, 0, Instrument.mStepsPerMeasure); } }

		///<summary> timer for single steps</summary>
		public float mSixteenthStepTimer = 0;

		///<summary> length of measures. Used for timing.set on start</summary>
		public float mBeatLength { get; private set; }

		///<summary> how many times we've repeated the measure.</summary>
		public int mRepeatCount = 0;

		///<summary> resets the repeat count;</summary>
		public void ResetRepeatCount() { mRepeatCount = 0; }

		///<summary> delay to balance out when we start a new measure</summary>
		public float mMeasureStartTimer = 0;

		///<summary> how many steps in the chord progression have been taken</summary>
		private int mProgressionStepsTaken = -1;
		///<summary> how many steps in the chord progression have been taken</summary>
		public int ProgressionStepsTaken { get { return mProgressionStepsTaken; } set { mProgressionStepsTaken = Mathf.Clamp(value, -1, InstrumentSet.mMaxFullstepsTaken); } }

		///<summary> resets the progression steps taken.</summary>
		public void ResetProgressionSteps() { ProgressionStepsTaken = -1; }

		///<summary> unplayed note</summary>
		public static readonly int mUnplayed = -1;

		///<summary> if using linear dynamic style, this is our current level of groups that are playing.</summary>
		public int mCurrentGroupLevel { get; private set; }

		[Tooltip("Our instrument set data.")]
		///<summary>Our instrument set data.</summary>
		public InstrumentSetData mData = null;

		[Tooltip("Reference to the music generator")]
		///<summary>Reference to the music generator</summary>
		public MusicGenerator mMusicGenerator = null;

		[Tooltip("Our time signature object.")]
		///<summary>Our time signature object.</summary>
		public TimeSignature mTimeSignature = new TimeSignature();

		[Tooltip("list of our current instruments")]
		///<summary>list of our current instruments</summary>
		public List<Instrument> mInstruments = new List<Instrument>();

		/// <summary>
		/// Initializes music set.
		/// </summary>
		public void Init()
		{
			mMusicGenerator = MusicGenerator.Instance;
			mBeatLength = 0;
			mCurrentGroupLevel = 0;
			mTimeSignature.Init();
		}

		/// <summary>
		/// Loads the instrument set data.
		/// </summary>
		/// <param name="data"></param>
		public void LoadData(InstrumentSetData data)
		{
			mData = data;
			mTimeSignature.SetTimeSignature(data.mTimeSignature);
			mRepeatCount = 0;
			UpdateTempo();
		}

		/// <summary>
		/// Sets the time signature data:
		/// </summary>
		/// <param name="signature"></param>
		public void SetTimeSignature(eTimeSignature signature)
		{
			if (mData == null)
				return;
			mData.mTimeSignature = signature;
			mTimeSignature.SetTimeSignature(mData.mTimeSignature);
		}

		/// <summary>
		/// Resets the instrument set values:
		/// </summary>
		public void Reset()
		{
			if (mMusicGenerator == null)
				return;

			mRepeatCount = 0;
			mCurrentGroupLevel = 0;

			ProgressionStepsTaken = -1;
			if (mMusicGenerator.mState == eGeneratorState.repeating)
				mMusicGenerator.SetState(eGeneratorState.playing);

			for (int i = 0; i < mInstruments.Count; i++)
				mInstruments[i].ResetInstrument();
		}

		/// <summary>
		/// Gets the inverse progression rate.
		/// </summary>
		/// <param name="valueIN"></param>
		/// <returns></returns>
		public int GetInverseProgressionRate(int valueIN)
		{
			valueIN = valueIN >= 0 && valueIN < mTimeSignature.mTimestepNumInverse.Length ? valueIN : 0;
			return mTimeSignature.mTimestepNumInverse[valueIN];
		}

		/// <summary>
		/// Returns the progression rate.
		/// </summary>
		/// <param name="valueIN"></param>
		/// <returns></returns>
		public int GetProgressionRate(int valueIN)
		{
			valueIN = valueIN >= 0 && valueIN < mTimeSignature.mTimestepNum.Length ? valueIN : 0;
			return mTimeSignature.mTimestepNum[valueIN];
		}

		/// <summary>
		/// Sets the progression rate.
		/// </summary>
		/// <param name="valueIN"></param>
		public void SetProgressionRate(int valueIN)
		{
			valueIN = valueIN > 0 && valueIN <= mTimeSignature.mStepsPerMeasure ? valueIN : mTimeSignature.mStepsPerMeasure;
			mData.mProgressionRate = (eProgressionRate)GetInverseProgressionRate(valueIN);
		}

		/// <summary>
		/// Updates the tempo.
		/// </summary>
		public void UpdateTempo()
		{
			int minute = 60;
			mBeatLength = minute / mData.Tempo; //beats per minute
		}

		/// <summary>
		/// strums a clip.
		/// </summary>
		/// <param name="clipIN"></param>
		/// <param name="instIndex"></param>
		public void Strum(int[] clipIN, int instIndex)
		{
			mMusicGenerator.StartCoroutine(StrumClip(clipIN, instIndex));
		}

		/// <summary>
		/// staggers the playClip() call:
		/// </summary>
		/// <param name="clipIN"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public IEnumerator StrumClip(int[] clipIN, int i)
		{
			if (mInstruments[i].mData.mSuccessionType == eSuccessionType.rhythm && mInstruments[i].mData.mArpeggio == false)
			{
				Array.Sort(clipIN);
			}

			float variation = UnityEngine.Random.Range(0, mInstruments[i].mData.StrumVariation);
			for (int j = 0; j < clipIN.Length; j++)
			{
				if (clipIN[j] != mUnplayed)
				{
					mMusicGenerator.PlayAudioClip(this, (int)mInstruments[i].InstrumentTypeIndex, clipIN[j], mInstruments[i].mData.Volume, i);
					mMusicGenerator.UIStaffNoteStrummed.Invoke(clipIN[j], (int)mInstruments[i].mData.mStaffPlayerColor);
					yield return new WaitForSeconds(mInstruments[i].mData.StrumLength + variation);
				}
			}
		}

		/// <summary>
		/// Selects which instrument groups will play next measure.
		/// </summary>
		public void SelectGroups()
		{
			eGroupRate rate = mMusicGenerator.mGeneratorData.mGroupRate;
			if (rate == eGroupRate.eEndOfMeasure ||
				(rate == eGroupRate.eEndOfProgression && ProgressionStepsTaken >= mMaxFullstepsTaken - 1))
			{
				/// Either randomly choose which groups play or:
				if (mMusicGenerator.mGeneratorData.mDynamicStyle == eDynamicStyle.Random)
				{
					for (int i = 0; i < mMusicGenerator.mGroupIsPlaying.Count; i++)
						mMusicGenerator.mGroupIsPlaying[i] = (UnityEngine.Random.Range(0, 100.0f) < mMusicGenerator.mGeneratorData.mGroupOdds[i]);
				}
				else //we ascend / descend through our levels.
				{
					int ascend = 1;
					int descend = -1;
					int numGroup = mMusicGenerator.mGeneratorData.mGroupOdds.Count;

					int change = UnityEngine.Random.Range(0, 100) < 50 ? ascend : descend;
					int PotentialLevel = change + mCurrentGroupLevel;

					if (PotentialLevel < 0)
						PotentialLevel = mCurrentGroupLevel;
					if (PotentialLevel >= mMusicGenerator.mGeneratorData.mGroupOdds.Count)
						PotentialLevel = 0;

					//roll to see if we can change.
					if (UnityEngine.Random.Range(0, 100.0f) > mMusicGenerator.mGeneratorData.mGroupOdds[PotentialLevel])
						PotentialLevel = mCurrentGroupLevel;

					mCurrentGroupLevel = PotentialLevel;
					for (int i = 0; i < numGroup; i++)
						mMusicGenerator.mGroupIsPlaying[i] = i <= mCurrentGroupLevel;
				}
			}
		}

		/// <summary>
		/// Sets all multipliers back to their base.
		/// </summary>
		public void ResetMultipliers()
		{
			for (int i = 0; i < mInstruments.Count; i++)
				mInstruments[i].mData.OddsOfPlayingMultiplier = Instrument.mOddsOfPlayingMultiplierBase;
		}
	}
}