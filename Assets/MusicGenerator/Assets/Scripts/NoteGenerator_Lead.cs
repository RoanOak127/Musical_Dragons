using System.Collections;
using System.Collections.Generic;

namespace ProcGenMusic
{
	/// <summary>
	/// This class generates lead notes for a measure
	/// </summary>
	public class NoteGenerator_Lead : NoteGenerator
	{
		///<summary> list of melodic notes we've already played (for determining ascend/descend)</summary>
		public List<int> mPlayedMelodicNotes = new List<int>();

		///<summary> for lead influence</summary>
		public const int mDescendingInfluence = -1;

		///<summary> for lead influence </summary>
		public const int mAscendingInfluence = 1;

		///<summary> Clears the stored melodic notes that have played.</summary>
		public override void ClearNotes() { mPlayedMelodicNotes.Clear(); }

		///<summary> Whether we're currently using the pentatonic scale</summary>
		private bool mIsStandardPentatonic = false;

		///<summary> Our standard pentatonic avoid notes for the major scale</summary>
		public static readonly int[] mMajorPentatonicAvoid = new int[] { 2, 5 };

		///<summary> Our standard pentatonic avoid notes for the minor scale</summary>
		public static readonly int[] mMinorPentatonicAvoid = new int[] { 0, 4 };

		///<summary> Our current avoid notes</summary>
		private int[] mAvoidNotes = new int[] {-1, -1 };

		///<summary> Whether the next selected note will have its ascension forced in its current direction</summary>
		private bool mForceAscenscion = false;

		/// <summary>
		/// Generates the next notes for this lead instrument.
		/// </summary>
		/// <returns></returns>
		public override int[] GenerateNotes()
		{
			mIsStandardPentatonic = mInstrument.mData.mIsPentatonic;

			//UnitySystemConsoleRedirector.Redirect();

			bool tritone = mInstrument.mCurrentProgressionStep < 0;

			if (IsPercussion())
			{
				return GetPercussionNotes();
			}
			else if (UnityEngine.Random.Range(0, 100) > mInstrument.mData.OddsOfUsingChordNotes && tritone == false)
			{
				if (UnityEngine.Random.Range(0, 100) < mInstrument.mData.OddsOfPlaying * mInstrument.mData.OddsOfPlayingMultiplier)
				{
					int nextNote = GetRawLeadNoteIndex();

					/// here we find the shortest rhythm step and make sure we're not playing something dischordant if it may be playing as well.
					int shortestTimestep = (int)mMusicGenerator.GetShortestRhythmTimestep();
					shortestTimestep = mMusicGenerator.mInstrumentSet.GetInverseProgressionRate(shortestTimestep);
					if (shortestTimestep == 1 || mMusicGenerator.mInstrumentSet.SixteenthStepsTaken % shortestTimestep == 0)
					{
						if (IsAvoidNote(nextNote))
						{
							nextNote = FixAvoidNote(nextNote);
						}
					}

					int note = AdjustRawLeadIndex(nextNote);
					AddSingleNote(note);
				}
				else
				{
					AddEmptyNotes();
				}

				return mNotes;
			}
			else
			{
				return mFallback();
			}
		}

		/// <summary>
		/// Gets the next melodic note.
		/// </summary>
		/// <returns></returns>
		private int GetRawLeadNoteIndex()
		{
			int[] scale = Instrument.mMusicScales[(int)mMusicGenerator.mGeneratorData.mScale];
			int octaveOffset = (scale.Length - 1) * (GetOctaveIndex());
			int noteOUT = UnityEngine.Random.Range(octaveOffset, (scale.Length - 1) + octaveOffset);
			int progressionstep = mInstrument.mCurrentProgressionStep < 0 ? mInstrument.mCurrentProgressionStep * -1 : mInstrument.mCurrentProgressionStep;

			if (mPlayedMelodicNotes.Count == 0)
			{
				mInstrument.mData.LeadInfluence = UnityEngine.Random.Range(0, 100) > 50 ? mAscendingInfluence : mDescendingInfluence;
			}
			else
			{
				int ultimateNote = mPlayedMelodicNotes[mPlayedMelodicNotes.Count - 1];
				noteOUT = UnityEngine.Random.Range(ultimateNote + mInstrument.mData.LeadInfluence, ultimateNote + ((int)mInstrument.mData.LeadMaxSteps * mInstrument.mData.LeadInfluence));
			}

			// here, we try to stay within range, and adjust the ascend/ descend influence accordingly:
			if (UnityEngine.Random.Range(0, 100) > mInstrument.mData.AscendDescendInfluence && mForceAscenscion == false)
			{
				mInstrument.mData.LeadInfluence *= -1;
			}

			mForceAscenscion = false;
			noteOUT = RangeCheck(scale, noteOUT, progressionstep);
			return noteOUT;
		}

		/// <summary>
		/// Ensures the note is within range
		/// </summary>
		/// <param name="scale"></param>
		/// <param name="noteOUT"></param>
		/// <param name="progressionstep"></param>
		/// <returns></returns>
		private int RangeCheck(int[] scale, int noteOUT, int progressionstep)
		{
			if (noteOUT + progressionstep >= (scale.Length * 3))
			{
				noteOUT = (scale.Length * 3) - progressionstep - 2;
				mInstrument.mData.LeadInfluence = mDescendingInfluence;
				mForceAscenscion = true;
			}
			else if (noteOUT + progressionstep < 0)
			{
				mInstrument.mData.LeadInfluence = mAscendingInfluence;
				mForceAscenscion = true;
				noteOUT = -progressionstep + 1;
			}

			return noteOUT;
		}

		/// <summary>
		/// Returns true if this lead note is a half step above a chord note:
		/// </summary>
		/// <param name="noteIN"></param>
		/// <returns></returns>
		private bool IsAvoidNote(int noteIN)
		{
			if (IsPentatonicAvoid(noteIN))
			{
				return true;
			}

			int note = MusicHelpers.SafeLoop(noteIN - 1, Instrument.mScaleLength);
			int progressionstep = mInstrument.mCurrentProgressionStep < 0 ? mInstrument.mCurrentProgressionStep * -1 : mInstrument.mCurrentProgressionStep;
			int scaleNote = MusicHelpers.SafeLoop(note + (int)mMusicGenerator.mGeneratorData.mMode + progressionstep, Instrument.mScaleLength);
			int[] scale = Instrument.mMusicScales[(int)mMusicGenerator.mGeneratorData.mScale];

			bool isHalfStep = scale[scaleNote] == Instrument.mHalfStep;

			bool isAboveChordNode = (note == Instrument.mSeventhChord[0] ||
				note == Instrument.mSeventhChord[1] ||
				note == Instrument.mSeventhChord[2] ||
				(note == Instrument.mSeventhChord[3] && mInstrument.mData.ChordSize == Instrument.mSeventhChord.Length));

			bool isSeventh = (mInstrument.mData.ChordSize != Instrument.mSeventhChord.Length && note == Instrument.mSeventhChord[3] - 1);

			return (isHalfStep && isAboveChordNode) || isSeventh;
		}

		/// <summary>
		/// Returns true if this note should be avoided in pentatonic scales.
		/// </summary>
		/// <param name="noteIN"></param>
		/// <returns></returns>
		private bool IsPentatonicAvoid(int noteIN)
		{
			int note = MusicHelpers.SafeLoop(noteIN - 1, Instrument.mScaleLength);
			int progressionstep = mInstrument.mCurrentProgressionStep < 0 ? mInstrument.mCurrentProgressionStep * -1 : mInstrument.mCurrentProgressionStep;

			// currently does not respect modal changes and avoids the interval instead. add mMusicGenerator.mGeneratorData.mMode to change with modes
			int scaleNote = MusicHelpers.SafeLoop(note + progressionstep, Instrument.mScaleLength);

			if (mIsStandardPentatonic)
			{
				if (mMusicGenerator.mGeneratorData.mScale == eScale.Major || mMusicGenerator.mGeneratorData.mScale == eScale.HarmonicMajor)
				{

					return scaleNote == mMajorPentatonicAvoid[0] || scaleNote == mMajorPentatonicAvoid[1];
				}
				else if (mMusicGenerator.mGeneratorData.mScale == eScale.NatMinor ||
					mMusicGenerator.mGeneratorData.mScale == eScale.HarmonicMinor ||
					mMusicGenerator.mGeneratorData.mScale == eScale.mMelodicMinor)
				{
					return scaleNote == mMinorPentatonicAvoid[0] || scaleNote == mMinorPentatonicAvoid[1];
				}
			}
			else
			{
				return (mAvoidNotes[0] > 0 && scaleNote == mAvoidNotes[0]) || (mAvoidNotes[1] > 0 && scaleNote == mAvoidNotes[1]);
			}
			return false;
		}

		/// <summary>
		/// Fixes an avoid note to (hopefully) not be dischordant:
		/// </summary>
		/// <param name="nextNote"></param>
		/// <returns></returns>
		private int FixPentatonicAvoidNote(int nextNote)
		{
			int adjustedNote = nextNote + mInstrument.mData.LeadInfluence;
			bool isAvoidNote = true;
			int maxAttempts = Instrument.mScaleLength;
			for (int i = 1; i < maxAttempts && isAvoidNote; i++)
			{
				adjustedNote = nextNote + (i * mInstrument.mData.LeadInfluence);
				if (IsPentatonicAvoid(adjustedNote) == false)
				{
					nextNote = adjustedNote;
					isAvoidNote = false;
				}
			}
			return adjustedNote;
		}

		/// <summary>
		/// Fixes an avoid note to (hopefully) not be dischordant:
		/// </summary>
		/// <param name="nextNote"></param>
		/// <returns></returns>
		private int FixAvoidNote(int nextNote)
		{
			int adjustedNote = nextNote + mInstrument.mData.LeadInfluence;
			bool isAvoidNote = true;
			int maxAttempts = Instrument.mScaleLength;
			for (int i = 1; i < maxAttempts && isAvoidNote; i++)
			{
				adjustedNote = nextNote + (i * mInstrument.mData.LeadInfluence);
				int progressionstep = mInstrument.mCurrentProgressionStep < 0 ? mInstrument.mCurrentProgressionStep * -1 : mInstrument.mCurrentProgressionStep;
				adjustedNote = RangeCheck(Instrument.mMusicScales[(int)mMusicGenerator.mGeneratorData.mScale], adjustedNote, progressionstep);
				if (IsAvoidNote(adjustedNote) == false)
				{
					nextNote = adjustedNote;
					isAvoidNote = false;
				}
			}
			return adjustedNote;
		}

		/// <summary>
		/// steps the note through the scale, adjusted for mode, key, progression step to find th
		/// actual note index instead of our raw steps.
		/// </summary>
		/// <param name="noteIN"></param>
		/// <returns></returns>
		private int AdjustRawLeadIndex(int noteIN)
		{
			int note = 0;
			int progressionstep = (mInstrument.mCurrentProgressionStep < 0) ? mInstrument.mCurrentProgressionStep * -1 : mInstrument.mCurrentProgressionStep;
			for (int j = 0; j < noteIN + progressionstep; j++)
			{
				int index = j + (int)mMusicGenerator.mGeneratorData.mMode;
				index = index % Instrument.mScaleLength;
				int testNote = note;
				testNote += Instrument.mMusicScales[(int)mMusicGenerator.mGeneratorData.mScale][index];
				int key = (int)mMusicGenerator.mGeneratorData.mKey;
				if (testNote + key >= 36)
				{
					mPlayedMelodicNotes.Add(j - 1 - progressionstep);
					note += key;
					mInstrument.mData.LeadInfluence = mDescendingInfluence;
					mForceAscenscion = true;
					return note;
				}
				note = testNote;
			}
			note += (int)mMusicGenerator.mGeneratorData.mKey;

			mPlayedMelodicNotes.Add(noteIN);
			return note;
		}
	}
}