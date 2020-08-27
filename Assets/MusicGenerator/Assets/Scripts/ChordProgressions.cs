using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// Generates chord progressions
	/// </summary>
	public class ChordProgressions
	{
		public ChordProgressionData mData = null;

		///<summary> which steps belong to the tonic:</summary>
		static readonly private int[] mTonicChords = new int[] { 1, 3, 6 };

		///<summary> which steps belong to the subdominant:</summary>
		static readonly private int[] mSubdominantChords = new int[] { 4, 2 };

		///<summary> which steps belong to the dominant:</summary>
		static readonly private int[] mDominantChords = new int[] { 5, 7 };
		private int[] mCurrentChords = new int[] { };

		/// <summary> our current chord progression </summary> 
		private int[] mProgression = new int[4] { 1, 4, 4, 5 };

		///<summary> Cached array to reduce GC</summary>
		ListArrayInt temp = new ListArrayInt(10);

		/// <summary>Loads our progression data.</summary>
		public void LoadProgressionData(ChordProgressionData data)
		{
			mData = data;
		}

		/// <summary>
		/// Generates a new progression. 
		/// </summary>
		/// <param name="modeIN"></param>
		/// <param name="scaleIN"></param>
		/// <param name="keyChange"></param>
		/// <returns></returns>
		public int[] GenerateProgression(eMode modeIN, eScale scaleIN, int keyChange)
		{
			//here we decide which chord step we'll use based on tonal influences and whether we'll change keys:
			// this is a bit mangled, but it works :P
			for (int i = 0; i < MusicGenerator.mMaxFullstepsTaken; i++)
			{
				switch (i)
				{
					case 0:
						mCurrentChords = Random.Range(0, 100) < mData.TonicInfluence ? mTonicChords : mSubdominantChords;
						break;
					case 1:
						mCurrentChords = Random.Range(0, 100) < mData.SubdominantInfluence ? mSubdominantChords : mTonicChords;
						break;
					case 2:
						mCurrentChords = Random.Range(0, 100) < mData.SubdominantInfluence ? mSubdominantChords : mDominantChords;
						break;
					case 3:
						if (Random.Range(0, 100) < mData.DominantInfluence)
							mCurrentChords = mDominantChords;
						else if (Random.Range(0, 100) < mData.SubdominantInfluence)
							mCurrentChords = mSubdominantChords;
						else
							mCurrentChords = mTonicChords;
						break;
					default:
						break;
				}
				int tritone = (mCurrentChords == mDominantChords && Random.Range(0, 100) < mData.TritoneSubInfluence) ? -1 : 1;
				mProgression[i] = tritone * GetProgressionSteps(mCurrentChords, modeIN, scaleIN, keyChange);
			}
			return mProgression;
		}

		/// <summary>
		/// Gets the chord interval.
		/// </summary>
		/// <param name="chords"></param>
		/// <param name="modeIN"></param>
		/// <param name="isMajorScale"></param>
		/// <param name="keyChange"></param>
		/// <returns></returns>
		private int GetProgressionSteps(int[] chords, eMode modeIN, eScale isMajorScale, int keyChange)
		{
			temp.Clear();
			//create a new array of possible chord steps, excluding the steps we'd like to avoid:
			for (int i = 0; i < chords.Length; i++)
			{
				/// we're going to ignore excluded steps when changing keys, if it's not an avoid note.
				/// it's too likely that the note that's excluded is the only available note that's shared between
				/// the two keys for that chord type (like, if V is excluded, VII is never shared in major key ascending fifth step up)
				if ((keyChange != 0 && CheckKeyChangeAvoid(isMajorScale, keyChange, chords[i], modeIN)) ||
					mData.mExcludedProgSteps[chords[i] - 1] != true)
				{
					temp.Add(chords[i]);
				}
			}

			if (temp.Count == 0)
				Debug.Log("progression steps == 0");

			return temp[Random.Range(0, temp.Count)];
		}

		/// <summary>
		/// Checks for notes to avoid before a key change
		/// </summary>
		/// <param name="scaleIN"></param>
		/// <param name="keyChange"></param>
		/// <param name="chord"></param>
		/// <param name="modeIN"></param>
		/// <returns></returns>
		private bool CheckKeyChangeAvoid(eScale scaleIN, int keyChange, int chord, eMode modeIN)
		{

			// Musically, this could be more robust, but essentially checks to make sure a given chord will not sound
			// bad when changing keys. We change the key early in the generator, so, for example, we don't
			// want to play the 4th chord in the new key if we're descending, that chord is not shared
			// between the two keys. 
			// TODO: more intelligent key changes :P 
			int mode = (int)modeIN;

			//if we're not changing keys, there's nothing to avoid:
			if (keyChange == 0)return true;

			bool isNotAvoidNote = true;
			if (scaleIN == eScale.Major || scaleIN == eScale.HarmonicMajor)
			{
				if ((keyChange > 0 && chord == 7 - mode) ||
					(keyChange < 0 && chord == 4 - mode))
					isNotAvoidNote = false;
			}
			else if (scaleIN != 0)
			{
				if ((keyChange > 0 && chord == 2 - mode) ||
					(keyChange < 0 && chord == 6 - mode))
					isNotAvoidNote = false;
			}
			return isNotAvoidNote;
		}
	}
}