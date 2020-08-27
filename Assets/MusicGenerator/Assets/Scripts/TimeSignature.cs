using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// Manages the time signature for a measure
	/// </summary>
	[Serializable]
	public class TimeSignature
	{
		///<summary> number of steps per measure</summary>
		public int mStepsPerMeasure { get; private set; }

		///<summary> Notes per timestep</summary>
		public int[] mTimestepNum { get; private set; }

		///<summary> inverted notes per timestep</summary>
		public int[] mTimestepNumInverse { get; private set; }

		///<summary> sixteenth note;</summary>
		private int mSixteenth = 16;
		///<summary> sixteenth note;</summary>
		public int Sixteenth { get { return mSixteenth; } private set { mSixteenth = value; } }

		///<summary> eighth note;</summary>
		private int mEighth = 8;
		///<summary> eighth note;</summary>
		public int Eighth { get { return mEighth; } private set { mEighth = value; } }

		///<summary> quarter note;</summary>
		private int mQuarter = 4;
		///<summary> quarter note;</summary>
		public int Quarter { get { return mQuarter; } private set { mQuarter = value; } }

		///<summary> half note;</summary>
		private int mHalf = 2;
		///<summary> half note;</summary>
		public int Half { get { return mHalf; } private set { mHalf = value; } }

		///<summary> whole note;</summary>
		private int mWhole = 0;
		///<summary> whole note;</summary>
		public int Whole { get { return mWhole; } private set { mWhole = value; } }

		///<summary>our currently set time signature.</summary>
		private eTimeSignature mSignature = eTimeSignature.FourFour;
		///<summary>our currently set time signature.</summary>
		public eTimeSignature Signature { get { return mSignature; } set { SetTimeSignature(value); } }

		/// <summary>
		/// Initializes the time signature
		/// </summary>
		public void Init()
		{
			mStepsPerMeasure = 16;
			mTimestepNum = new int[] { 16, 8, 4, 2, 1 };
			mTimestepNumInverse = new int[] { 1, 2, 4, 8, 16 };
		}

		/// <summary>
		/// Sets our time signature and adjusts values.
		/// </summary>
		/// <param name="signature"></param>
		public void SetTimeSignature(eTimeSignature signature)
		{
			mSignature = signature;
			// Apologies for all the magic numbers. This is a bit of a hacky approach.
			// trying to shoehorn everything to the same system.
			switch (mSignature)
			{
				case eTimeSignature.FourFour:
					{
						mStepsPerMeasure = 16;
						mTimestepNum = new int[] { 16, 8, 4, 2, 1 };
						mTimestepNumInverse = new int[] { 1, 2, 4, 8, 16 };
						Sixteenth = 16;
						Eighth = 8;
						Quarter = 4;
						Half = 2;
						Whole = 0;
						break;
					}
				case eTimeSignature.ThreeFour:
					{
						mStepsPerMeasure = 12;
						mTimestepNum = new int[] { 12, 6, 3, 3, 1 };
						mTimestepNumInverse = new int[] { 1, 3, 3, 6, 12 };
						Sixteenth = 12;
						Eighth = 6;
						Quarter = 3;
						Half = 3;
						Whole = 0;
						break;
					}
				case eTimeSignature.FiveFour:
					{
						mStepsPerMeasure = 20;
						mTimestepNum = new int[] { 20, 10, 5, 5, 1 };
						mTimestepNumInverse = new int[] { 1, 5, 5, 10, 20 };
						Sixteenth = 20;
						Eighth = 10;
						Quarter = 5;
						Half = 5;
						Whole = 0;
						break;
					}
			}

			MusicGenerator.Instance.ResetPlayer();
		}
	}
}