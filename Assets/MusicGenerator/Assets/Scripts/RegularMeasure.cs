using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// A regular, non-repeating measure.
	/// </summary>
	public class RegularMeasure : Measure
	{
		/// <summary>
		/// Plays through the next step in the measure.
		/// </summary>
		/// <param name="set"></param>
		public override void PlayMeasure(InstrumentSet set)
		{
			
			if (set.mMusicGenerator == null)
				return;
			set.UpdateTempo();
			set.mSixteenthStepTimer -= Time.deltaTime;
			if (set.mSixteenthStepTimer <= 0 && set.SixteenthStepsTaken < set.mTimeSignature.mStepsPerMeasure)
			{
				set.mMusicGenerator.BarlineColorSet.Invoke(set.SixteenthStepsTaken, false);

				if (set.SixteenthStepsTaken % (int)set.mData.mProgressionRate == set.mTimeSignature.Whole)
				{
					set.ProgressionStepsTaken += 1;
					set.ProgressionStepsTaken = set.ProgressionStepsTaken % set.mMusicGenerator.ChordProgression.Length;
					set.mMusicGenerator.CheckKeyChange();
				}
				if (set.SixteenthStepsTaken % set.mTimeSignature.Half == 0)
					TakeStep(set, eTimestep.eighth, set.ProgressionStepsTaken);
				if (set.SixteenthStepsTaken % set.mTimeSignature.Quarter == 0)
					TakeStep(set, eTimestep.quarter, set.ProgressionStepsTaken);
				if (set.SixteenthStepsTaken % set.mTimeSignature.Eighth == 0)
					TakeStep(set, eTimestep.half, set.ProgressionStepsTaken);
				if (set.SixteenthStepsTaken % set.mTimeSignature.Sixteenth == 0)
				{
					TakeStep(set, eTimestep.whole, set.ProgressionStepsTaken);
					set.mMeasureStartTimer = 0.0f;
				}

				TakeStep(set, eTimestep.sixteenth, set.ProgressionStepsTaken);

				set.mSixteenthStepTimer = set.mBeatLength;
				set.SixteenthStepsTaken += 1;
			}
			else if (set.SixteenthStepsTaken == set.mTimeSignature.mStepsPerMeasure)
			{
				set.mMeasureStartTimer += Time.deltaTime;

				if (set.mMeasureStartTimer > set.mBeatLength) //We don't actually want to reset until the next beat.
				{
					set.mMusicGenerator.GenerateNewProgression();
					ResetMeasure(set, set.mMusicGenerator.SetThemeRepeat);
				}
			}
		}

		/// <summary>
		/// Plays the next step in the measure.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="timeStepIN"></param>
		/// <param name="stepsTaken"></param>
		public override void TakeStep(InstrumentSet set, eTimestep timeStepIN, int stepsTaken = 0)
		{
			for (int instIndex = 0; instIndex < set.mInstruments.Count; instIndex++)
			{
				if (set.mInstruments[instIndex].mData.Group >= set.mMusicGenerator.mGeneratorData.mGroupOdds.Count || set.mData.mProgressionRate < 0)
					return;
				
				Instrument instrument = set.mInstruments[instIndex];
				bool groupIsPlaying = set.mMusicGenerator.mGroupIsPlaying[(int)instrument.mData.Group];

				if (instrument.mData.mTimeStep == timeStepIN && groupIsPlaying && instrument.mData.mIsMuted == false)
					PlayNotes(set, instrument, stepsTaken, instIndex);
			}
		}

		/// <summary>
		/// Exits a non-repeating measure, resetting values to be able to play the next:
		/// </summary>
		/// <param name="set"></param>
		/// <param name="SetThemeRepeat"></param>
		/// <param name="hardReset"></param>
		/// <param name="isRepeating"></param>
		public override void ResetMeasure(InstrumentSet set, Action SetThemeRepeat = null, bool hardReset = false, bool isRepeating = true)
		{
			ResetRegularMeasure(set, SetThemeRepeat, hardReset, isRepeating);
		}

		/// <summary>
		/// Plays the notes for this timestep
		/// </summary>
		/// <param name="set"></param>
		/// <param name="instrument"></param>
		/// <param name="stepsTaken"></param>
		/// <param name="instIndex"></param>
		private void PlayNotes(InstrumentSet set, Instrument instrument, int stepsTaken, int instIndex)
		{
			// we want to fill this whether we play it or not:
			int progressionStep = set.mMusicGenerator.ChordProgression[stepsTaken];
			int[] clip = instrument.GetProgressionNotes(progressionStep);
			if (instrument.mData.StrumLength == 0.0f || instrument.mData.mSuccessionType != eSuccessionType.rhythm)
			{
				for (int j = 0; j < clip.Length; j++)
				{
					if (clip[j] != InstrumentSet.mUnplayed) //we ignore -1
					{
						try
						{
							set.mMusicGenerator.PlayAudioClip(set, (int)instrument.InstrumentTypeIndex, clip[j], instrument.mData.Volume, instIndex);
							set.mMusicGenerator.UIStaffNotePlayed.Invoke(clip[j], (int)instrument.mData.mStaffPlayerColor);
						}
						catch (ArgumentOutOfRangeException e)
						{
							throw new ArgumentOutOfRangeException(e.Message);
						}
					}
				}
			}
			else
				set.Strum(clip, instIndex);
		}
	}
}