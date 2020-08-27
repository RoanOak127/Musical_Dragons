using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProcGenMusic
{
	[Serializable]
	/// <summary>
	/// Our data for an instrument
	/// </summary>
	public class InstrumentData
	{
		/// version number of this save.
		[SerializeField]
		private float mVersion = 0.1f;

		[Tooltip("Whether this chord will play a triad or a 7th (tetrad). See: https://en.wikipedia.org/wiki/Chord_(music)#Number_of_notes")]
		[Range(Instrument.mTriadCount, Instrument.mTriadCount + 1)]
		[SerializeField]
		private int mChordSize = 3;
		public int ChordSize { get { return mChordSize; } set { mChordSize = Mathf.Clamp(value, Instrument.mTriadCount, Instrument.mTriadCount + 1); } }

		[Tooltip("The color of the m staff player notes used by this instrument:")]
		public eStaffPlayerColors mStaffPlayerColor = eStaffPlayerColors.Red;

		[Tooltip("Melody, rhythm or lead. See: https://en.wikipedia.org/wiki/Lead_instrument#Melody_and_harmony")]
		public eSuccessionType mSuccessionType = eSuccessionType.melody;

		[Tooltip("Name of this instrument type")]
		[SerializeField]
		private string mInstrumentType = "harp";
		public string InstrumentType
		{
			get { return mInstrumentType.ToLower(); }
			set
			{
				value = value.ToLower();
				bool validType = false;
				List<string> instrumentTypes = MusicGenerator.Instance.LoadedInstrumentNames;
				if (instrumentTypes.Count <= 0)
					return;

				for (int i = 0; i < instrumentTypes.Count; i++)
				{
					if (value == instrumentTypes[i])
						validType = true;
				}
				mInstrumentType = validType ? value : instrumentTypes[0];
			}
		}

		[Tooltip("Which octaves will be used, keep within 0 through 2. See: https://en.wikipedia.org/wiki/Octave")]
		[Range(0, 2)]
		public ListArrayInt mOctavesToUse = new ListArrayInt() { mArray = new int[] { 0, 1, 2 } };

		[Tooltip("How clips will pan in the audioSource. See: https://docs.unity3d.com/ScriptReference/AudioSource-panStereo.html")]
		[Range(-1, 1)][SerializeField]
		public float mStereoPan = 0;
		public float StereoPan { get { return mStereoPan; } set { mStereoPan = Mathf.Clamp(value, -1, 1); } }

		[Tooltip("Odds this note will play, each timestep. 0-100")]
		[Range(0, 100)][SerializeField]
		private int mOddsOfPlaying = 50;
		public int OddsOfPlaying { get { return mOddsOfPlaying; } set { mOddsOfPlaying = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("Saves odds when moved to chorus, for recall 0-100")]
		[Range(0, 100)][SerializeField]
		private int mPreviousOdds = 100;
		public int PreviousOdds { get { return mPreviousOdds; } set { mPreviousOdds = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("Instrument volume 0 -1;")]
		[Range(0, 1)][SerializeField]
		private float mVolume = 0.5f;
		public float Volume { get { return mVolume; } set { mVolume = Mathf.Clamp(value, 0, 1); } }

		[Tooltip("Volume of this instrument's audio source.")]
		[Range(-80, 20)][SerializeField]
		private float mAudioSourceVolume = 0.0f;
		public float AudioSourceVolume { get { return mAudioSourceVolume; } set { mAudioSourceVolume = Mathf.Clamp(value, -80, 20); } }

		[Tooltip("Odds an octave note will be used: 0 through 100.")]
		[Range(0, 100)][SerializeField]
		private float mOctaveOdds = 20;
		public float OctaveOdds { get { return mOctaveOdds; } set { mOctaveOdds = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("The higher end of the multiplier. Odds of playing are multiplied against this on a successful roll")]
		[Range(1, 10)][SerializeField]
		private float mOddsOfPlayingMultiplierMax = 1.5f;
		public float OddsOfPlayingMultiplierMax { get { return mOddsOfPlayingMultiplierMax; } set { mOddsOfPlayingMultiplierMax = Mathf.Clamp(value, 1, 10); } }

		[Tooltip("The currently used multiplier.")]
		[SerializeField]
		private float mOddsOfPlayingMultiplier = 1.0f;
		public float OddsOfPlayingMultiplier { get { return mOddsOfPlayingMultiplier; } set { mOddsOfPlayingMultiplier = value; } }

		[Tooltip("Which instrument group this instrument belongs to. See: https://en.wikipedia.org/wiki/Dynamics_(music)")]
		[Range(0, 3)][SerializeField]
		private int mGroup = 0;
		public int Group { get { return mGroup; } set { mGroup = Mathf.Clamp(value, 0, 3); } }

		[Tooltip("Whether the instrument is solo:")]
		public bool mIsSolo = false;

		[Tooltip("Whether this instrument uses the pentatonic scale (Lead Only)")]
		public bool mIsPentatonic = false;

		[Tooltip("Our notes to avoid for this lead instrument")]
		[Range(0, 6)]
		public int[] mLeadAvoidNotes = new int[2] {-1, -1 };

		[Tooltip("Whether the uses an arpeggio style (if a melody):")]
		public bool mArpeggio = false;

		[Tooltip("Whether instrument is muted")]
		public bool mIsMuted = false;

		[Tooltip("Currently used timestep")]
		public eTimestep mTimeStep = eTimestep.quarter;

		[Tooltip("Odds each note of the chord will play")]
		[Range(0, 100)][SerializeField]
		private float mOddsOfUsingChordNotes = 50.0f;
		public float OddsOfUsingChordNotes { get { return mOddsOfUsingChordNotes; } set { mOddsOfUsingChordNotes = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("Variation between different strums.")]
		[Range(0, 1)][SerializeField]
		private float mStrumVariation = 0.0f;
		public float StrumVariation { get { return mStrumVariation; } set { mStrumVariation = Mathf.Clamp(value, 0, 1); } }

		[Tooltip("If we have a redundant melodic note, odds we pick another")]
		[Range(0, 100)][SerializeField]
		private float mRedundancyAvoidance = 100.0f;
		public float RedundancyAvoidance { get { return mRedundancyAvoidance; } set { mRedundancyAvoidance = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("Delay between notes for strum effect, in seconds")]
		[Range(0.0f, 1.0f)][SerializeField]
		private float mStrumLength = 0.00f;
		public float StrumLength { get { return mStrumLength; } set { mStrumLength = Mathf.Clamp(value, 0.0f, 1.0f); } }

		///effects values///
		[Range(-10000, 0)][SerializeField]
		private float mRoomSize = -10000.0f;
		public float RoomSize { get { return mRoomSize; } set { mRoomSize = Mathf.Clamp(value, -10000, 10000); } }

		[Range(10000, 2000)][SerializeField]
		private float mReverb = -2000.0f;
		public float Reverb { get { return mReverb; } set { mReverb = Mathf.Clamp(value, -10000, 2000); } }

		[Range(0, 1)][SerializeField]
		private float mEcho = 0.0f;
		public float Echo { get { return mEcho; } set { mEcho = Mathf.Clamp(value, 0, 1); } }

		[Range(100, 1000)][SerializeField]
		private float mEchoDelay = 0.0f;
		public float EchoDelay { get { return mEchoDelay; } set { mEchoDelay = Mathf.Clamp(value, 100, 1000); } }

		[Range(0, .9f)][SerializeField]
		private float mEchoDecay = 0.0f;
		public float EchoDecay { get { return mEchoDecay; } set { mEchoDecay = Mathf.Clamp(value, 0, 0.9f); } }

		[Range(0, 100)][SerializeField]
		private float mFlanger = 0.0f;
		public float Flanger { get { return mFlanger; } set { mFlanger = Mathf.Clamp(value, 0, 100); } }

		[Range(0, 1)][SerializeField]
		private float mDistortion = 0.0f;
		public float Distortion { get { return mDistortion; } set { mDistortion = Mathf.Clamp(value, 0, 1); } }

		[Range(0, 1)][SerializeField]
		private float mChorus = 0.0f;
		public float Chorus { get { return mChorus; } set { mChorus = Mathf.Clamp(value, 0, 1); } }

		///pattern variables:
		[Tooltip("Whether we'll use a pattern for this instrument")]
		public bool mUsePattern = false;

		[Tooltip("Length of our pattern:")]
		[Range(0, 8)][SerializeField]
		private int mPatternlength = 4;
		public int PatternLength { get { return mPatternlength; } set { mPatternlength = Mathf.Clamp(value, 0, 8); } }

		[Tooltip("At which point we stop using the pattern")]
		[Range(0, 8)][SerializeField]
		private int mPatternRelease = 4;
		public int PatternRelease { get { return mPatternRelease; } set { mPatternRelease = Mathf.Clamp(value, 0, 8); } }

		[Tooltip("Odds we ever play the same note twice. No UI controller for this. Set manually.")]
		[Range(0, 100)][SerializeField]
		private float mRedundancyOdds = 50.0f;
		public float RedundancyOdds { get { return mRedundancyOdds; } set { mRedundancyOdds = Mathf.Clamp(value, 1, 100); } }

		[Tooltip("How many scale steps a melody can take at once")]
		[Range(1, 7)][SerializeField]
		private int mLeadMaxSteps = 3;
		public int LeadMaxSteps { get { return mLeadMaxSteps; } set { mLeadMaxSteps = Mathf.Clamp(value, 1, 7); } }

		[Tooltip("Likelihood of melody continuing to ascend/descend")]
		[Range(0, 100)][SerializeField]
		private float mAscendDescendInfluence = 75.0f;
		public float AscendDescendInfluence { get { return mAscendDescendInfluence; } set { mAscendDescendInfluence = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("How much we tend to ascend/descend ")]
		[Range(-1, 1)][SerializeField]
		private int mLeadInfluence = NoteGenerator_Lead.mAscendingInfluence;
		public int LeadInfluence { get { return mLeadInfluence; } set { value = value == 0 ? 1 : value; mLeadInfluence = Mathf.Clamp(value, -1, 1); } }

		/// <summary>
		/// Saves to json
		/// </summary>
		/// <param name="pathIN"></param>
		/// <param name="data"></param>
		public static void SaveData(string pathIN, InstrumentData data)
		{
			data.mVersion = MusicGenerator.Version;
			string save = JsonUtility.ToJson(data);
			File.WriteAllText(pathIN, save);
		}

		/// <summary>
		/// Updates the save version to match the music generator's version
		/// </summary>
		/// <param name="data"></param>
		/// <param name="pathIN"></param>
		/// <param name="saveOUT"></param>
		/// <returns></returns>
		private static InstrumentData UpdateVersion(string data, string pathIN, InstrumentData saveOUT)
		{
			InstrumentSave instrumentSave = JsonUtility.FromJson<InstrumentSave>(data);

			if (saveOUT != null)
			{
				if (saveOUT.mVersion == 0.0f)
				{
					saveOUT.mStaffPlayerColor = (eStaffPlayerColors)instrumentSave.mStaffPlayerColor;
				}
				if (saveOUT.mVersion == 1.1f)
				{
					for (int i = 0; i < instrumentSave.mOctavesToUse.Count; i++)
					{
						saveOUT.mOctavesToUse.Add(instrumentSave.mOctavesToUse[i]);
					}
					saveOUT.mIsPentatonic = false;
					saveOUT.mLeadAvoidNotes = new int[2] {-1, -1 };
				}
			}
			return saveOUT;
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		/// <summary>
		/// loads from json
		/// </summary>
		/// <param name="pathIN"></param>
		/// <returns></returns>
		public static void LoadData(string data, string fileName, System.Action<InstrumentData> callback)
		{
			InstrumentData saveOUT = null;
			saveOUT = JsonUtility.FromJson<InstrumentData>(data);
			saveOUT.mInstrumentType = saveOUT.mInstrumentType.ToLower();
			if (saveOUT.mVersion != MusicGenerator.Version)
			{
				InstrumentSave instrumentSave = JsonUtility.FromJson<InstrumentSave>(data);

				if (saveOUT != null)
				{
					if (saveOUT.mVersion == 0.0f)
					{
						saveOUT.mStaffPlayerColor = (eStaffPlayerColors)instrumentSave.mStaffPlayerColor;
					}
					if (saveOUT.mVersion == 1.1f)
					{
						for (int i = 0; i < instrumentSave.mOctavesToUse.Count; i++)
						{
							saveOUT.mOctavesToUse.Add(instrumentSave.mOctavesToUse[i]);
						}
						saveOUT.mIsPentatonic = false;
						saveOUT.mLeadAvoidNotes = new int[2] {-1, -1 };
					}
				}

				InstrumentData.SaveData(fileName, saveOUT);
			}
			callback(saveOUT);
		}

#else
		/// loads from json
		public static InstrumentData LoadData(string pathIN, string fileName)
		{
			string configPath = MusicFileConfig.GetConfigDirectory(pathIN) + fileName;
			string data = "";
			if (File.Exists(configPath))
				data = File.ReadAllText(configPath);
			else
			{
				throw new ArgumentNullException("Instrument configuration does not exist at " + configPath);
			}

			InstrumentData saveOUT = JsonUtility.FromJson<InstrumentData>(data);
			saveOUT.mInstrumentType = saveOUT.mInstrumentType.ToLower();
			if (saveOUT.mVersion != MusicGenerator.Version)
			{
				return UpdateVersion(data, pathIN + fileName, saveOUT);
			}

			return saveOUT;
		}
#endif //!UNITY_EDITOR && UNITY_ANDROID

	}
}