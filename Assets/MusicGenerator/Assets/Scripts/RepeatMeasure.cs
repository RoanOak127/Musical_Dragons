using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// Repeating measure class. Handles logic for repeating the previous measure.
	/// </summary>
	public class RepeatMeasure : Measure
	{
		/// <summary>
		/// Plays the next sequence in the measure.
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
				set.mMusicGenerator.BarlineColorSet.Invoke(set.SixteenthStepsTaken, true);
				if (set.SixteenthStepsTaken % set.mTimeSignature.Half == 0)
					TakeStep(set, eTimestep.eighth);
				if (set.SixteenthStepsTaken % set.mTimeSignature.Quarter == 0)
					TakeStep(set, eTimestep.quarter);
				if (set.SixteenthStepsTaken % set.mTimeSignature.Eighth == 0)
					TakeStep(set, eTimestep.half);
				if (set.SixteenthStepsTaken % set.mTimeSignature.Sixteenth == 0)
				{
					TakeStep(set, eTimestep.whole);
					set.mMeasureStartTimer = 0.0f;
				}

				TakeStep(set, (int)eTimestep.sixteenth);
				set.SixteenthRepeatCount += 1;
				set.mSixteenthStepTimer = set.mBeatLength;
				set.SixteenthStepsTaken += 1;
			}
			else if (set.SixteenthStepsTaken == set.mTimeSignature.mStepsPerMeasure)
			{
				set.mMeasureStartTimer += Time.deltaTime;
				if (set.mMeasureStartTimer > set.mBeatLength)
					ResetMeasure(set);
			}
		}

		/// <summary>
		/// Resets the measure
		/// </summary>
		/// <param name="set"></param>
		/// <param name="SetThemeRepeat"></param>
		/// <param name="hardReset"></param>
		/// <param name="isRepeating"></param>
		public override void ResetMeasure(InstrumentSet set, Action SetThemeRepeat = null, bool hardReset = false, bool isRepeating = true)
		{
			ResetRepeatMeasure(set, SetThemeRepeat, hardReset, isRepeating);
		}

		/// <summary>
		/// Takes a single step through the measure:
		/// </summary>
		/// <param name="set"></param>
		/// <param name="timeStepIN"></param>
		/// <param name="stepsTaken"></param>
		public override void TakeStep(InstrumentSet set, eTimestep timeStepIN, int stepsTaken = 0)
		{
			bool usingTheme = set.mMusicGenerator.mGeneratorData.mThemeRepeatOptions == eThemeRepeatOptions.eUseTheme;
			bool repeatingMeasure = set.mMusicGenerator.mGeneratorData.mThemeRepeatOptions == eThemeRepeatOptions.eRepeat;
			for (int instIndex = 0; instIndex < set.mInstruments.Count; instIndex++)
			{
				Instrument instrument = set.mInstruments[instIndex];
				int instType = (int)instrument.InstrumentTypeIndex;

				if ((instrument.mData.mTimeStep == timeStepIN || set.mMusicGenerator.OnUIPlayerIsEditing()) && instrument.mData.mIsMuted == false)
				{
					if (instType >= set.mMusicGenerator.AllClips.Count)
						throw new ArgumentOutOfRangeException("Single clip instrument has not been loaded into the generator");

					int instrumentSubIndex = UnityEngine.Random.Range(0, set.mMusicGenerator.AllClips[instType].Count);
					if (set.mMusicGenerator.OnUIPlayerIsEditing())
					{
						for (int chordNote = 0; chordNote < instrument.mData.ChordSize; chordNote++)
							set.mMusicGenerator.RepeatNotePlayed.Invoke(new RepeatNoteArgs(instIndex, chordNote, set.SixteenthRepeatCount, instrumentSubIndex, set));
					}
					else if (usingTheme)
						PlayThemeNotes(set, instrument, instType, instrumentSubIndex, instIndex);
					else if (repeatingMeasure)
						PlayRepeatNotes(set, instrument, instIndex, instrumentSubIndex);
				}
			}
		}

		/// <summary>
		/// Plays the repeating notes for this timestep.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="instrument"></param>
		/// <param name="instIndex"></param>
		/// <param name="instSubIndex"></param>
		private void PlayRepeatNotes(InstrumentSet set, Instrument instrument, int instIndex, int instSubIndex)
		{
			for (int chordNote = 0; chordNote < instrument.mData.ChordSize; chordNote++)
			{
				if (instrument.mRepeatingNotes.Length > set.SixteenthRepeatCount && instrument.mRepeatingNotes[set.SixteenthRepeatCount][chordNote] != InstrumentSet.mUnplayed)
				{
					if (instrument.mData.StrumLength == 0.0f)
					{
						set.mMusicGenerator.PlayAudioClip(set, (int)instrument.InstrumentTypeIndex, instrument.mRepeatingNotes[set.SixteenthRepeatCount][chordNote], instrument.mData.Volume, instIndex);
						set.mMusicGenerator.UIStaffNotePlayed.Invoke(instrument.mRepeatingNotes[set.SixteenthRepeatCount][chordNote], (int)instrument.mData.mStaffPlayerColor);
					}
					else
					{
						int[] clip = instrument.mThemeNotes[set.SixteenthRepeatCount];
						set.Strum(clip, instIndex);
						break;
					}
				}
			}
		}

		/// <summary>
		/// Plays the theme notes for this repeat step.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="instrument"></param>
		/// <param name="instType"></param>
		/// <param name="instSubIndex"></param>
		/// <param name="instIndex"></param>
		private void PlayThemeNotes(InstrumentSet set, Instrument instrument, int instType, int instSubIndex, int instIndex)
		{
			for (int chordNote = 0; chordNote < instrument.mData.ChordSize; chordNote++)
			{
				int[][] notes = instrument.mThemeNotes;
				if (notes.Length > set.SixteenthRepeatCount &&
					notes[set.SixteenthRepeatCount].Length > chordNote &&
					notes[set.SixteenthRepeatCount][chordNote] != InstrumentSet.mUnplayed)
				{
					if (instrument.mData.StrumLength == 0.0f)
					{
						int note = notes[set.SixteenthRepeatCount][chordNote];
						set.mMusicGenerator.PlayAudioClip(set, instType, note, instrument.mData.Volume, instIndex);
						set.mMusicGenerator.UIStaffNotePlayed.Invoke(note, (int)instrument.mData.mStaffPlayerColor);
					}
					else
					{
						set.Strum(notes[set.mRepeatCount], instIndex);
						break;
					}
				}
			}
		}
	}
}