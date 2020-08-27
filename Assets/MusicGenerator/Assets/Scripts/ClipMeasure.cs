using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	public class ClipMeasure : Measure
	{
		/// <summary>
		/// Plays the next steps in the measure.
		/// </summary>
		/// <param name="set"></param>
		public override void PlayMeasure(InstrumentSet set)
		{
			set.UpdateTempo();

			if (set.mMusicGenerator == null)
				return;

			set.mSixteenthStepTimer -= Time.deltaTime;
			// we'll take a step when the timer hits zero, or it's the first step
			if (set.mSixteenthStepTimer <= 0 && set.SixteenthStepsTaken < set.mTimeSignature.mStepsPerMeasure)
			{
				if (set.SixteenthStepsTaken % (int)set.mData.mProgressionRate == 0)
					set.ProgressionStepsTaken += 1;
				if (set.ProgressionStepsTaken > InstrumentSet.mMaxFullstepsTaken - 1)
					set.ProgressionStepsTaken = -1;

				TakeStep(set, (int)eTimestep.sixteenth, set.SixteenthRepeatCount);
				set.SixteenthRepeatCount += 1;
				set.mSixteenthStepTimer = set.mBeatLength;

				set.SixteenthStepsTaken += 1;
			}
			// Reset once we've reached the end
			else if (set.SixteenthStepsTaken == set.mTimeSignature.mStepsPerMeasure)
			{
				set.mMeasureStartTimer += Time.deltaTime;
				if (set.mMeasureStartTimer > set.mBeatLength)
				{
					bool hardReset = false;
					ResetMeasure(set, set.mMusicGenerator.SetThemeRepeat, hardReset, true);
				}
			}
		}

		/// <summary>
		/// Exits our measure
		/// </summary>
		/// <param name="set"></param>
		/// <param name="SetThemeRepeat"></param>
		/// <param name="hardReset"></param>
		/// <param name="isRepeating"></param>
		public override void ResetMeasure(InstrumentSet set, Action SetThemeRepeat = null, bool hardReset = false, bool isRepeating = true)
		{
			ResetRepeatMeasure(set, SetThemeRepeat, hardReset, isRepeating);
		}

		/// Takes a measure step.
		public override void TakeStep(InstrumentSet set, eTimestep timeStepIN, int value = 0)
		{
			for (int i = 0; i < set.mInstruments.Count; i++)
			{
				if (set.mInstruments[i].mData.mIsMuted == false)
				{
					for (int j = 0; j < set.mInstruments[i].mData.ChordSize; j++)
					{
						int note = set.mInstruments[i].mClipNotes[set.mRepeatCount][value][j];
						if (note != InstrumentSet.mUnplayed)
						{
							/// set percussion to 0
							if (set.mInstruments[i].mData.InstrumentType.Contains("p_"))
								note = 0;

							set.mMusicGenerator.PlayAudioClip(set, (int)set.mInstruments[i].InstrumentTypeIndex, note, set.mInstruments[i].mData.Volume, i);
						}
					}
				}
			}
		}
	}
}