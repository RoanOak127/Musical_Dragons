using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// A measure is a set of 16th steps, this class handles the playing of them.
	/// </summary>
	public abstract class Measure
	{
		///<summary> Plays a step through measure.</summary> 
		public abstract void PlayMeasure(InstrumentSet set);

		///<summary>  Resets the measure</summary> 
		public abstract void ResetMeasure(InstrumentSet set, Action SetThemeRepeat = null, bool hardReset = false, bool isRepeating = true);

		///<summary>  Takes a single measure step.</summary> 
		public abstract void TakeStep(InstrumentSet set, eTimestep timeStepIN, int stepsTaken = 0);

		/// <summary>
		/// Resets a repeating measure.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="SetThemeRepeat"></param>
		/// <param name="hardReset"></param>
		/// <param name="isRepeating"></param>
		protected void ResetRepeatMeasure(InstrumentSet set, Action SetThemeRepeat = null, bool hardReset = false, bool isRepeating = true)
		{
			set.mMusicGenerator.RepeatedMeasureExited.Invoke(set.mMusicGenerator.mState);

			set.mRepeatCount += 1;

			set.mMeasureStartTimer = 0.0f;
			set.mSixteenthStepTimer = 0.0f;
			set.ResetMultipliers();
			set.SixteenthStepsTaken = 0;

			if (isRepeating == false)
				return;

			//if we've repeated all the measures set to repeat in their entirety, reset the step counts.
			bool isEditing = set.mMusicGenerator.OnUIPlayerIsEditing();
			int repeatNum = isEditing ? set.mData.RepeatMeasuresNum + 1 : set.mData.RepeatMeasuresNum * 2;
			if (set.mRepeatCount >= repeatNum || isEditing || hardReset)
			{
				if (isEditing == false || hardReset)
				{
					set.mRepeatCount = 0;
				}

				set.SixteenthRepeatCount = 0;
				for (int i = 0; i < set.mInstruments.Count; i++)
					set.mInstruments[i].ClearRepeatingNotes();

				if (set.mMusicGenerator.mState > eGeneratorState.stopped && set.mMusicGenerator.mState < eGeneratorState.editorInitializing)
					set.mMusicGenerator.SetState(eGeneratorState.playing);
			}
		}

		/// <summary>
		/// Resets a regular measure.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="SetThemeRepeat"></param>
		/// <param name="hardReset"></param>
		/// <param name="isRepeating"></param>
		protected void ResetRegularMeasure(InstrumentSet set, Action SetThemeRepeat = null, bool hardReset = false, bool isRepeating = true)
		{
			if (set.mMusicGenerator == null)
				throw new ArgumentNullException("music generator does not exist. Please ensure a game object with this class exists");

			set.mRepeatCount += 1;

			if (SetThemeRepeat != null)
				SetThemeRepeat();

			for (int i = 0; i < set.mInstruments.Count; i++)
			{
				set.mInstruments[i].ClearPatternNotes();
				set.mInstruments[i].ResetPatternStepsTaken();
				set.mInstruments[i].ClearPlayedLeadNotes();
				set.mInstruments[i].GenerateArpeggio();
			}
			set.SixteenthStepsTaken = 0;

			//select groups:
			set.SelectGroups();

			if (set.ProgressionStepsTaken >= InstrumentSet.mMaxFullstepsTaken - 1)
				set.ProgressionStepsTaken = -1;

			if (set.mMusicGenerator.mGeneratorData.mThemeRepeatOptions == eThemeRepeatOptions.eNone)
			{
				set.mRepeatCount = 0;
				for (int i = 0; i < set.mInstruments.Count; i++)
					set.mInstruments[i].ClearRepeatingNotes();
			}

			set.mMeasureStartTimer = 0.0f;
			set.mSixteenthStepTimer = 0.0f;

			set.ResetMultipliers();
		}
	}
}