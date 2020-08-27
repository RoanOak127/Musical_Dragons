using System.Collections;
using System.Collections.Generic;

namespace ProcGenMusic
{
	public abstract class NoteGenerator
	{
		///<summary> container for the notes for a single step</summary>
		protected int[] mNotes = new int[4] { 0, 0, 0, 0 };

		///<summary> Reference for our intstrument</summary>
		protected Instrument mInstrument = null;

		///<summary> Reference for our music generator</summary>
		protected MusicGenerator mMusicGenerator = null;

		///<summary> unplayed notes are -1.</summary>
		private const int mUnplayed = -1;

		///<summary> fallback function if the note fails a check (like, if a lead instrument plays a rhythm chord instead.)</summary>
		public delegate int[] Fallback(Fallback fallback = null);

		///<summary> Reference for our fallback.</summary>
		protected Fallback mFallback = null;

		/// <summary>
		/// Initializes this note genereator.
		/// </summary>
		/// <param name="instrument"></param>
		/// <param name="fallback"></param>
		public void Init(Instrument instrument, Fallback fallback)
		{
			mInstrument = instrument;
			mFallback = fallback;
			mMusicGenerator = mInstrument.mMusicGenerator;
		}

		/// <summary>
		/// adds a single note for this instrument
		/// </summary>
		/// <param name="noteIN"></param>
		/// <param name="addPattern"></param>
		protected void AddSingleNote(int noteIN, bool addPattern = false)
		{
			mNotes[0] = noteIN;
			mNotes[1] = (addPattern ? EmptyPatternedNote(1) : mUnplayed);
			mNotes[2] = (addPattern ? EmptyPatternedNote(2) : mUnplayed);
			mNotes[3] = (addPattern ? EmptyPatternedNote(3) : mUnplayed);
		}

		/// <summary>
		/// fills the current octaves and notes with non-played values.
		/// </summary>
		protected void AddEmptyNotes()
		{
			for (int i = 0; i < Instrument.mSeventhChord.Length; i++)
			{
				mNotes[i] = mUnplayed;
				mInstrument.mCurrentPatternNotes[i] = mUnplayed;
				mInstrument.mCurrentPatternOctave[i] = 0;
			}
		}

		/// <summary>
		/// Sets the empty patterned notes. Plays no regular notes:
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		protected int EmptyPatternedNote(int index)
		{
			mInstrument.mCurrentPatternNotes[index] = mUnplayed;
			mInstrument.mPatternOctaveOffset[(int)mInstrument.mCurrentPatternStep][index] = 0;
			return mUnplayed;
		}

		/// <summary>
		/// Returns the index of a random available octave index
		/// </summary>
		/// <returns></returns>
		public int GetOctaveIndex()
		{
			int octave = UnityEngine.Random.Range(0, mInstrument.mData.mOctavesToUse.Count);
			return mInstrument.mData.mOctavesToUse[octave];
		}

		/// <summary>
		/// Returns an octave
		/// </summary>
		/// <param name="indexIN"></param>
		/// <returns></returns>
		public int GetOctave(int indexIN = 0)
		{
			int octave = UnityEngine.Random.Range(0, mInstrument.mData.mOctavesToUse.Count);

			// add it to our octave pattern, if needs be.
			if (mInstrument.mData.mUsePattern && mInstrument.mbAreSettingPattern)
				mInstrument.mCurrentPatternOctave[indexIN] = mInstrument.mData.mOctavesToUse[octave];

			return mInstrument.mData.mOctavesToUse[octave] * Instrument.mOctave;
		}

		/// <summary>
		/// Gets a note from a chord.
		/// </summary>
		/// <param name="chordNote"></param>
		/// <param name="chordIndex"></param>
		/// <param name="octaveOffsetIN"></param>
		/// <returns></returns>
		protected int GetChordNote(int chordNote = 0, int chordIndex = 0, int octaveOffsetIN = -1)
		{
			int note = (int)mMusicGenerator.mGeneratorData.mKey;

			//tri-tone check.
			int progressionStep = (mInstrument.mCurrentProgressionStep < 0) ? mInstrument.mCurrentProgressionStep * -1 : mInstrument.mCurrentProgressionStep;

			//add octave offset:
			int newOctave = (octaveOffsetIN == -1) ? GetOctave(chordIndex) : octaveOffsetIN;
			note += (mInstrument.mbAreRepeatingPattern && mInstrument.mData.mUsePattern && mInstrument.mData.mSuccessionType != eSuccessionType.lead) ? mInstrument.mCurrentPatternOctave[chordIndex] * Instrument.mOctave : newOctave;

			//for melodies we don't want to keep playing the same note repeatedly
			if (IsRedundant(chordNote))
			{
				int extraStep = 2;
				chordNote = (chordNote != Instrument.mSeventhChord[(int)mInstrument.mData.ChordSize - 2]) ? chordNote + extraStep : 0;
			}

			mInstrument.mCurrentPatternNotes[chordIndex] = chordNote;

			note += GetChordOffset(progressionStep, (int)mMusicGenerator.mGeneratorData.mMode, chordNote);
			if (mInstrument.mCurrentProgressionStep < 0)
				note += Instrument.mTritoneStep;

			return note;
		}

		/// <summary>
		/// Returns whether this note is redundant
		/// </summary>
		/// <param name="noteIN"></param>
		/// <returns></returns>
		private bool IsRedundant(int noteIN)
		{
			if (mInstrument.mData.mSuccessionType == eSuccessionType.rhythm || mMusicGenerator.mInstrumentSet.SixteenthStepsTaken == 0)
				return false;

			if (noteIN == mInstrument.mPatternNoteOffset[mMusicGenerator.mInstrumentSet.SixteenthStepsTaken - 1][0])
				return (UnityEngine.Random.Range(0.0f, 100.0f) < mInstrument.mData.RedundancyAvoidance) ? true : false;

			return false;
		}

		/// <summary>
		/// Returns this chord offset for this note.
		/// </summary>
		/// <param name="rootOffset"></param>
		/// <param name="mode"></param>
		/// <param name="chordNote"></param>
		/// <returns></returns>
		private int GetChordOffset(int rootOffset, int mode, int chordNote = 0)
		{
			int noteOUT = 0;
			int[] scale = Instrument.mMusicScales[(int)mMusicGenerator.mGeneratorData.mScale];
			for (int i = 0; i < rootOffset + chordNote; i++)
			{
				int index = (i + mode) % scale.Length;
				noteOUT += scale[index];
			}
			return noteOUT;
		}

		/// <summary>
		/// Returns the repeating notes for this instrument.
		/// </summary>
		/// <returns></returns>
		protected int[] AddRepeatNotes()
		{
			for (int i = 0; i < mInstrument.mCurrentPatternNotes.Length; i++)
			{
				int note = mInstrument.mCurrentPatternNotes[i];
				mNotes[i] = (note != mUnplayed) ? GetChordNote(note, i) : mUnplayed;
			}
			return mNotes;
		}

		/// <summary>
		/// Returns whether this instrument is a percussion instrument.
		/// </summary>
		/// <returns></returns>
		protected bool IsPercussion()
		{
			return mMusicGenerator.AllClips[(int)mInstrument.InstrumentTypeIndex][0].Count == 1;
		}

		/// <summary>
		/// Returns notes for a percussion instrumnet.
		/// </summary>
		/// <returns></returns>
		protected int[] GetPercussionNotes()
		{
			if (mInstrument.mData.mSuccessionType == eSuccessionType.rhythm || UnityEngine.Random.Range(0, 100) <= mInstrument.mData.OddsOfPlaying)
			{
				mNotes[0] = 0;
				// roll odds for additional notes, and check for 7th.
				// It's fairly arbitrary since they're all the same note, but allows for
				// varying the number of beats the percussion will play.
				mNotes[1] = mInstrument.mData.StrumLength > 0 && UnityEngine.Random.Range(0, 100) < mInstrument.mData.OddsOfUsingChordNotes ? 0 : mUnplayed;
				mNotes[2] = mInstrument.mData.StrumLength > 0 && UnityEngine.Random.Range(0, 100) < mInstrument.mData.OddsOfUsingChordNotes ? 0 : mUnplayed;
				mNotes[3] = mInstrument.mData.StrumLength > 0 &&
					UnityEngine.Random.Range(0, 100) < mInstrument.mData.OddsOfUsingChordNotes &&
					mInstrument.mData.ChordSize == Instrument.mSeventhChord.Length ?
					0 : mUnplayed;
			}
			else
				AddEmptyNotes();

			return mNotes;
		}

		/// <summary>
		/// clears any saved notes for this instrumnet
		/// </summary>
		public abstract void ClearNotes();

		/// <summary>
		/// generates the next set of notes for this instrumnet.
		/// </summary>
		/// <returns></returns>
		public abstract int[] GenerateNotes();
	}
}