using System.Collections;
using System.Collections.Generic;

namespace ProcGenMusic
{
	/// <summary>
	/// This class generates notes for a rhythm instrument
	/// </summary>
	public class NoteGenerator_Rhythm : NoteGenerator
	{
		/// <summary>
		/// Generates notes for a rhythm instrument for a single step
		/// </summary>
		/// <returns></returns>
		public override int[] GenerateNotes()
		{
			if (mInstrument.mbAreRepeatingPattern && mInstrument.mData.mUsePattern)
				return AddRepeatNotes();
			else if (IsPercussion())
				return GetPercussionNotes();

			//because we generally want to play at least one note for rhythm
			//we start backward and just play the root chord note if other chord notes don't play.
			bool successfulNote = false;
			if (mInstrument.mData.mArpeggio && mInstrument.mData.StrumLength > 0.0f)
			{
				for (int i = 0; i < mInstrument.mData.ChordSize; i++)
					mNotes[i] = GetChordNote(mInstrument.mArpeggioPattern[i], i, 0);
			}
			else
			{
				for (int i = (int)mInstrument.mData.ChordSize - 1; i >= 0; i--)
				{
					if (UnityEngine.Random.Range(0, 100) <= mInstrument.mData.OddsOfUsingChordNotes || (i == 0 && successfulNote == false))
					{
						int chordNote = Instrument.mSeventhChord[i];
						mNotes[i] = GetChordNote(chordNote, i);
						successfulNote = true;
					}
					else
						mNotes[i] = EmptyPatternedNote(i);
				}
			}

			//add an empty 7th if we're not using them so we still have 4 notes.
			int fifthChord = Instrument.mTriadCount;
			if (mInstrument.mData.ChordSize == fifthChord)
				mNotes[3] = EmptyPatternedNote(fifthChord);

			return mNotes;
		}
		public override void ClearNotes() { }
	}
}