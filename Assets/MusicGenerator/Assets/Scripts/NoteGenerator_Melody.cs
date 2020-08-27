using System.Collections;
using System.Collections.Generic;

namespace ProcGenMusic
{
	/// <summary>
	/// This class generates notes for a melodic instrument
	/// </summary>
	public class NoteGenerator_Melody : NoteGenerator
	{
		/// <summary>
		/// Generates notes for a step
		/// </summary>
		/// <returns></returns>
		public override int[] GenerateNotes()
		{
			if (mInstrument.mbAreRepeatingPattern && mInstrument.mData.mUsePattern)
				return AddRepeatNotes();
			else if (IsPercussion())
				return GetPercussionNotes();
			else if (UnityEngine.Random.Range(0, 100) < mInstrument.mData.OddsOfPlaying * mInstrument.mData.OddsOfPlayingMultiplier)
			{
				if (UnityEngine.Random.Range(0, 100) > mInstrument.mData.OddsOfUsingChordNotes)
				{
					int note = UnityEngine.Random.Range(0, (int)mInstrument.mData.ChordSize);
					AddSingleNote(GetChordNote(Instrument.mSeventhChord[note], 0), true);
				}
				else
					return mFallback();
			}
			else
				AddEmptyNotes();

			return mNotes;
		}
		public override void ClearNotes() { }
	}
}