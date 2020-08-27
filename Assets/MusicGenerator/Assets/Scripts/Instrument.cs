using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	///  Instrument Class. Handles selecting notes to play and other instrument settings.
	/// </summary>
	[System.Serializable]
	public class Instrument
	{
		[Tooltip("The data for this instrument")]
		///<summary> our data</summary>
		public InstrumentData mData = null;

		///<summary> index of MusicGenerator.mInstruments()</summary>
		private int mInstrumentIndex = 0;

		///<summary> index of MusicGenerator.mInstruments()</summary>
		public int InstrumentIndex { get { return mInstrumentIndex; } set { mInstrumentIndex = value < MusicGenerator.mMaxInstruments ? value : 0; } }

		///<summary> how many notes in an octave.</summary>
		public const int mOctave = 12;

		///<summary> scale steps in a seventh chord</summary>
		static readonly public int[] mSeventhChord = new int[] { 0, 2, 4, 6 };

		///<summary> scale steps in a pentatonic sevent chord</summary>
		static readonly public int[] mPentSeventhChord = new int[] { 0, 2, 3, 5 };

		///<summary> number of notes in a triad :P</summary>
		public const int mTriadCount = 3;

		///<summary> just a base of 1.</summary>
		public const float mOddsOfPlayingMultiplierBase = 1;

		///<summary> number of steps per measure (this is the max. when using 5/4 signature. it could be as low as 12.)</summary>
		public const int mStepsPerMeasure = 20;

		///<summary> value for an 'unused' note </summary>
		private const int mUnplayed = -1;

		///<summary>steps between notes. Used in scales:</summary>
		public const int mHalfStep = 1;

		///<summary>steps between notes. Used in scales:</summary>
		public const int mFullStep = 2;

		///<summary>steps between notes. Used in scales:</summary>
		public const int mFullPlusHalf = 3;

		///<summary>for exotic scales. Currently not implemented</summary>
		public const int mDoubleStep = 4;

		///<summary>amount to adjust tritone chords/ notes.</summary>
		public const int mTritoneStep = 5;

		///<summary> when created, we flag ourselves to generate a new theme/repeat, as this may have happened mid-measure.</summary>
		public bool mNeedsTheme { get; private set; }

		// our scales: TO NOTE: Melodic minor is both ascending and descending, which isn't super accurate for classical theory, generally. But, it was causing issue so
		// now just uses the scale in both ascend/descending melodies. It's on the wishlist, but is problematic for a few reasons.
		///<summary> The scales steps in melodic minor</summary>
		static readonly public int[] mMelodicMinor = new int[] { mFullStep, mHalfStep, mFullStep, mFullStep, mFullStep, mFullStep, mHalfStep };

		///<summary> The scales steps in natural minor</summary>
		static readonly public int[] mNaturalMinor = new int[] { mFullStep, mHalfStep, mFullStep, mFullStep, mHalfStep, mFullStep, mFullStep };

		///<summary> The scales steps in harmonic minor</summary>
		static readonly private int[] mHarmonicMinor = new int[] { mFullStep, mHalfStep, mFullStep, mFullStep, mHalfStep, mFullPlusHalf, mHalfStep };

		///<summary> The scales steps in the Major scale</summary>
		static readonly public int[] mMajorScale = new int[] { mFullStep, mFullStep, mHalfStep, mFullStep, mFullStep, mFullStep, mHalfStep };

		///<summary> The scales steps in the Harmonic Major scale</summary>
		static readonly private int[] mHarmonicMajor = new int[] { mFullStep, mFullStep, mHalfStep, mFullStep, mHalfStep, mFullPlusHalf, mHalfStep };

		///<summary> A container of our scale-steps</summary>
		static readonly public int[][] mMusicScales = new int[][] { mMajorScale, mNaturalMinor, mMelodicMinor, mHarmonicMinor, mHarmonicMajor, mMelodicMinor };

		///<summary> length of our scales. Currently all 7 notes.</summary>
		public const int mScaleLength = 7;

		///<summary> array of our chord offsets to use in a pattern</summary>
		public int[][] mPatternNoteOffset = new int[mStepsPerMeasure][];

		///<summary> array of our octave offsets to use in a pattern</summary>
		public int[][] mPatternOctaveOffset = new int[mStepsPerMeasure][];

		///<summary> which steps we're using.</summary>
		public int mPatternstepsTaken { get; private set; }

		///<summary> Resets the number of pattern steps taken (this is handled internally by the instrument set and shouldn't need to be manually changed.)</summary>
		public void ResetPatternStepsTaken() { mPatternstepsTaken = 0; }

		//Variables used by the music generator:

		///<summary> array of notes played last measure</summary>
		public int[][] mRepeatingNotes = new int[64][];

		///<summary> notes of saved theme measure</summary>
		public int[][] mThemeNotes = new int[64][];

		///<summary> measure; timestep ; note.  Info for playing a clip.</summary>
		public int[][][] mClipNotes = new int[4][][];

		///<summary> Reference to our music generator</summary>
		public MusicGenerator mMusicGenerator { get; private set; }

		///<summary> whether we're repeating the pattern this iteration</summary>
		public bool mbAreRepeatingPattern { get; private set; }

		///<summary> whether we're setting the pattern this iteration</summary>
		public bool mbAreSettingPattern { get; private set; }

		///<summary> which index of the pattern we're playing this iteration</summary>
		public int mCurrentPatternStep { get; private set; }

		///<summary> array of current pattern notes for this step:</summary>
		public int[] mCurrentPatternNotes = new int[4] {-1, -1, -1, -1 };

		///<summary> array of current pattern octave offsets for this step:</summary>
		public int[] mCurrentPatternOctave = new int[4] { 0, 0, 0, 0 };

		///<summary> what our current step of the chord progression is.</summary>
		public int mCurrentProgressionStep = 0;

		///<summary> pattern for our arpeggio.</summary>
		public int[] mArpeggioPattern = new int[4] { 0, 2, 4, -1 };

		///<summary> pattern for our Triad.</summary>
		private int[] mTriadPattern = new int[3] { 0, 2, 4 };

		///<summary> pattern for our seventh.</summary>
		private int[] mSeventhPattern = new int[4] { 0, 2, 4, 6 };

		public static System.Random sRandom = new System.Random();
		/// <summary>
		///  Generates an arpeggio pattern for this measure:
		/// </summary>
		public void GenerateArpeggio()
		{
			int[] notes = mData.ChordSize == mTriadCount ? mTriadPattern : mSeventhPattern;
			for (int i = 0; i < notes.Length; i++)
			{
				int rand = sRandom.Next(i + 1);
				int temp = notes[i];
				notes[i] = notes[rand];
				mArpeggioPattern[i] = notes[rand];
				notes[rand] = temp;
				mArpeggioPattern[rand] = temp;
			}
			if (mData.ChordSize == mTriadCount)
				mArpeggioPattern[3] = -1;
		}

		[Tooltip("Index for the type of instrument. beware setting manually")]
		///<summary>"Index for the type of instrument. beware setting manually"</summary>
		private int mInstrumentTypeIndex = 0;
		///<summary>"Index for the type of instrument. beware setting manually"</summary>
		public int InstrumentTypeIndex { get { return mInstrumentTypeIndex; } set { mInstrumentTypeIndex = value < MusicGenerator.Instance.AllClips.Count ? value : 0; } }

		///<summary> our progression notes to be played</summary>
		private int[] mProgressionNotes = new int[] {-1, -1, -1, -1 };

		///<summary> our note generators. </summary>
		public NoteGenerator[] mNoteGenerators = new NoteGenerator[] { new NoteGenerator_Melody(), new NoteGenerator_Rhythm(), new NoteGenerator_Lead() };

		/// <summary>
		///  Instrument initialization
		/// </summary>
		/// <param name="index"></param>
		public void Init(int index)
		{
			mNeedsTheme = true;
			mbAreRepeatingPattern = false;
			mbAreSettingPattern = false;
			mCurrentPatternStep = 0;
			mData = new InstrumentData();
			InstrumentIndex = index;
			mPatternstepsTaken = 0;

			mMusicGenerator = MusicGenerator.Instance;
			LoadClipNotes();
			InitRepeatingAndThemeNotes();
			ClearClipNotes();
			InitPatternNotes();
			SetupNoteGenerators();
		}

		/// <summary>
		/// Returns an array of ints coresponding to the notes to play
		/// </summary>
		/// <param name="progressionStep"></param>
		/// <returns></returns>
		public int[] GetProgressionNotes(int progressionStep)
		{
			SetupNoteGeneration(progressionStep);
			SelectNotes();
			CheckValidity();
			SetRepeatNotes();
			SetMultiplier();
			return mProgressionNotes;
		}

		/// <summary>
		///  Resets the instrument.
		/// </summary>
		public void ResetInstrument()
		{
			ClearThemeNotes();
			ClearPlayedLeadNotes();
			ClearPatternNotes();
		}

		/// <summary>
		/// Clears our played lead notes.
		/// </summary>
		public void ClearPlayedLeadNotes()
		{
			mNoteGenerators[(int)eSuccessionType.lead].ClearNotes();
		}

		/// <summary>
		/// Sets the pattern variables. Mostly for readability in other functions :\
		/// </summary>
		/// <param name="progressionStep"></param>
		private void SetupNoteGeneration(int progressionStep)
		{
			InstrumentSet set = mMusicGenerator.mInstrumentSet;
			int invProgRate = set.GetInverseProgressionRate((int)mData.mTimeStep);
			int progRate = set.GetProgressionRate((int)mData.mTimeStep);

			// chord progressions are set in their sensible way: I-IV-V for example starting on 1. 
			// it's easier to leave like that as it's readable (from a music perspective, anyhow) and adjust here, rather than 0 based:
			mCurrentProgressionStep = progressionStep - ((progressionStep < 0) ? -1 : 1);

			mPatternstepsTaken = (int)(set.SixteenthStepsTaken / invProgRate);
			mCurrentPatternStep = mPatternstepsTaken % mData.PatternLength;

			mbAreRepeatingPattern = (mPatternstepsTaken >= mData.PatternLength && mPatternstepsTaken < progRate - mData.PatternRelease);
			mbAreSettingPattern = (mPatternstepsTaken < mData.PatternLength);

			if (mCurrentPatternStep < mPatternNoteOffset.Length - 1)
			{
				mCurrentPatternNotes = mPatternNoteOffset[(int)mCurrentPatternStep];
				mCurrentPatternOctave = mPatternOctaveOffset[(int)mCurrentPatternStep];
			}
		}

		/// <summary>
		/// Sets up our note generators.
		/// </summary>
		private void SetupNoteGenerators()
		{
			NoteGenerator.Fallback melodicFallback = x => mNoteGenerators[(int)eSuccessionType.rhythm].GenerateNotes();
			NoteGenerator.Fallback leadFallback = x => mNoteGenerators[(int)eSuccessionType.melody].GenerateNotes();
			mNoteGenerators[(int)eSuccessionType.lead].Init(this, leadFallback);
			mNoteGenerators[(int)eSuccessionType.melody].Init(this, melodicFallback);
			mNoteGenerators[(int)eSuccessionType.rhythm].Init(this, null);
		}

		/// <summary>
		/// Sets our notes to repeat.
		/// </summary>
		private void SetRepeatNotes()
		{
			InstrumentSet set = mMusicGenerator.mInstrumentSet;
			int count = set.SixteenthStepsTaken + (set.mRepeatCount * set.mTimeSignature.mStepsPerMeasure);
			for (int i = 0; i < mProgressionNotes.Length; i++)
				mRepeatingNotes[count][i] = mProgressionNotes[i];
		}

		/// <summary>
		///  Sets our array of notes to play, based on rhythm/leading and other variables:
		/// </summary>
		private void SelectNotes()
		{
			mProgressionNotes = mNoteGenerators[(int)mData.mSuccessionType].GenerateNotes();
		}

		/// <summary>
		/// Checks for out of range notes in our list and forces it back within range.
		/// </summary>
		private void CheckValidity()
		{
			if (mProgressionNotes.Length != mSeventhChord.Length)
				throw new Exception("We haven't fully filled our note array. Something has gone wrong.");

			for (int i = 0; i < mProgressionNotes.Length; i++)
			{
				int note = mProgressionNotes[i];
				int clipArraySize = MusicGenerator.mMaxInstrumentNotes;

				if (note == mUnplayed || (note < clipArraySize && note >= mUnplayed))
					continue;

				if (note < 0)
					note = MusicHelpers.SafeLoop(note, mOctave);
				else if (note >= clipArraySize)
				{
					note = MusicHelpers.SafeLoop(note, mOctave);
					note += (mData.mUsePattern && mbAreRepeatingPattern) ? mCurrentPatternOctave[i] * mOctave : 2 * mOctave;
				}

				/// if somehow this is still out of range, we've utterly broken things...
				if (note < 0 || note > clipArraySize)
				{
					Debug.Log("something's gone wrong note is out of range.");
					note = 0;
				}

				mProgressionNotes[i] = note;
			}
		}

		/// <summary>
		/// Sets the theme notes from the repeating list.
		/// </summary>
		public void SetThemeNotes()
		{
			mNeedsTheme = false;
			for (int x = 0; x < mRepeatingNotes.Length; x++)
				for (int y = 0; y < mRepeatingNotes[x].Length; y++)
					mThemeNotes[x][y] = mRepeatingNotes[x][y];
		}

		/// <summary>
		/// sets our multiplier for the next played note:
		/// </summary>
		private void SetMultiplier()
		{
			mData.OddsOfPlayingMultiplier = mOddsOfPlayingMultiplierBase;
			for (int i = 0; i < mProgressionNotes.Length; i++)
			{
				if (mProgressionNotes[i] != mUnplayed)
					mData.OddsOfPlayingMultiplier = mData.OddsOfPlayingMultiplierMax;
			}
		}

		/// <summary>
		/// Just initializes our clip notes to unplayed.
		/// </summary>
		private void LoadClipNotes()
		{
			int numMeasures = 4;
			for (int x = 0; x < numMeasures; x++)
			{
				mClipNotes[x] = new int[mStepsPerMeasure][];
				for (int y = 0; y < mClipNotes[x].Length; y++)
				{
					mClipNotes[x][y] = new int[MusicGenerator.mMaxInstrumentNotes];
					for (int z = 0; z < mClipNotes[x][y].Length; z++)
						mClipNotes[x][y][z] = mUnplayed;
				}

			}
		}

		/// <summary>
		/// Initializes our repeat/theme notes arrays.
		/// </summary>
		private void InitRepeatingAndThemeNotes()
		{
			for (int x = 0; x < mRepeatingNotes.Length; x++)
			{
				mRepeatingNotes[x] = new int[mSeventhChord.Length];
				mThemeNotes[x] = new int[mSeventhChord.Length];
				for (int y = 0; y < mSeventhChord.Length; y++)
				{
					mRepeatingNotes[x][y] = -1;
					mThemeNotes[x][y] = -1;
				}
			}
		}

		/// <summary>
		/// Clears the repeating note array.
		/// </summary>
		public void ClearRepeatingNotes()
		{
			for (int x = 0; x < mRepeatingNotes.Length; x++)
				for (int y = 0; y < mSeventhChord.Length; y++)
					mRepeatingNotes[x][y] = -1;
		}

		/// <summary>
		/// Clears the theme note array.
		/// </summary>
		public void ClearThemeNotes()
		{
			mNeedsTheme = true;
			for (int x = 0; x < mThemeNotes.Length; x++)
				for (int y = 0; y < mThemeNotes[x].Length; y++)
					mThemeNotes[x][y] = -1;
		}

		/// <summary>
		/// Clears the pattern notes.
		/// </summary>
		public void ClearPatternNotes()
		{
			for (int i = 0; i < mStepsPerMeasure; i++)
			{
				for (int j = 0; j < mSeventhChord.Length; j++)
				{
					mPatternNoteOffset[i][j] = mUnplayed;
					mPatternOctaveOffset[i][j] = 0;
				}
			}
		}

		/// <summary>
		/// Initializes our pattern notes.
		/// </summary>
		private void InitPatternNotes()
		{
			for (int i = 0; i < mStepsPerMeasure; i++)
			{
				mPatternOctaveOffset[i] = new int[mSeventhChord.Length];
				mPatternNoteOffset[i] = new int[mSeventhChord.Length];
				for (int j = 0; j < mSeventhChord.Length; j++)
				{
					mPatternNoteOffset[i][j] = mUnplayed;
					mPatternOctaveOffset[i][j] = 0;
				}
			}
		}

		// -----------------------------------------------------------
		// clip management: This is only used the the UI version of the player to set clip notes:
		// should probably be moved out of here, or stored elsewhere.
		// -----------------------------------------------------------

		/// <summary>
		/// Adds the clip note.
		/// </summary>
		/// <param name="timestep"></param>
		/// <param name="note"></param>
		/// <param name="measure"></param>
		/// <returns></returns>
		public bool AddClipNote(int timestep, int note, int measure = 0)
		{
			for (int i = 0; i < mClipNotes[measure][timestep].Length; i++)
			{
				if (mClipNotes[measure][timestep][i] == mUnplayed)
				{
					mClipNotes[measure][timestep][i] = note;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Removes the clip note.
		/// </summary>
		/// <param name="timestep"></param>
		/// <param name="note"></param>
		/// <param name="measure"></param>
		/// <returns></returns>
		public bool RemoveClipNote(int timestep, int note, int measure = 0)
		{
			for (int i = 0; i < mClipNotes[measure][timestep].Length; i++)
			{
				if (mClipNotes[measure][timestep][i] == note)
				{
					mClipNotes[measure][timestep][i] = mUnplayed;
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Clears the clip notes.
		/// </summary>
		public void ClearClipNotes()
		{
			for (int x = 0; x < mClipNotes.Length; x++)
				for (int i = 0; i < mClipNotes[x].Length; i++)
					for (int j = 0; j < mClipNotes[x][i].Length; j++)
						mClipNotes[x][i][j] = mUnplayed;
		}

		/////////////////////////////////////////
		/// Save / Load functions.
		/////////////////////////////////////////

		/// <summary>
		/// loads and sets values from save file.
		/// </summary>
		/// <param name="data"></param>
		public void LoadInstrument(InstrumentData data)
		{
			mData = data;
			string stringIndex = InstrumentIndex.ToString();
			mMusicGenerator.mMixer.SetFloat("Volume" + stringIndex, data.AudioSourceVolume);
			mMusicGenerator.mMixer.SetFloat("Reverb" + stringIndex, mData.Reverb);
			mMusicGenerator.mMixer.SetFloat("RoomSize" + stringIndex, mData.RoomSize);
			mMusicGenerator.mMixer.SetFloat("Chorus" + stringIndex, mData.Chorus);
			mMusicGenerator.mMixer.SetFloat("Flange" + stringIndex, mData.Flanger);
			mMusicGenerator.mMixer.SetFloat("Distortion" + stringIndex, mData.Distortion);
			mMusicGenerator.mMixer.SetFloat("Echo" + stringIndex, mData.Echo);
			mMusicGenerator.mMixer.SetFloat("EchoDelay" + stringIndex, mData.EchoDelay);
			mMusicGenerator.mMixer.SetFloat("EchoDecay" + stringIndex, mData.EchoDecay);
		}
	}
}