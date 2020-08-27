using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// the data for our base generator class.
	/// </summary>
	public class MusicGeneratorData
	{
		/// version number of this save.
		[SerializeField]
		private float mVersion = 0.0f;

		[Tooltip("State timer for our generator class.")]
		public float mStateTimer = 0.0f;

		[Tooltip("Master volume control")]
		[Range(-100, 15)]
		public float mMasterVolume = 1.0f;
		public static string mMasterVolName = "MasterVol";

		[Tooltip("The rate at which we'll fade in/out")]
		[Range(0, 20)]
		public float mVolFadeRate = 2.0f;

		[Tooltip("Our mode. see: https://en.wikipedia.org/wiki/Mode_(music)")]
		public eMode mMode = eMode.Ionian;

		[Tooltip("Whether our player will repeat refrains, use a theme, or neither.")]
		public eThemeRepeatOptions mThemeRepeatOptions = eThemeRepeatOptions.eNone;

		[Tooltip("Storage for whether our key change will ascend/descend through the circle of fifths.")]
		[Range(-7, 7)]
		public int mKeySteps = 0;

		[Tooltip("Odds our key change will ascend/descend through the circle of fifths.")]
		[Range(0, 100)]
		public float mKeyChangeAscendDescend = 50.0f;

		[Tooltip("Odds a new theme will be selected")]
		[Range(0, 100)]
		public float mSetThemeOdds = 10.0f;

		[Tooltip("Odds the theme will play")]
		[Range(0, 100)]
		public float mPlayThemeOdds = 90.0f;

		[Tooltip("Our scale. see: https://en.wikipedia.org/wiki/Scale_(music)")]
		public eScale mScale = eScale.Major;

		[Tooltip("Odds we choose a new progression")]
		[Range(0, 100)]
		public float mProgressionChangeOdds = 25.0f;

		[Tooltip("Our key. see: https://en.wikipedia.org/wiki/Key_(music)")]
		public eKey mKey = 0;

		[Tooltip("Odds we'll change key.")]
		[Range(0, 100)]
		public float mKeyChangeOdds = 0.0f;

		[Tooltip("Odds of each group being played. see included documentation")]
		[Range(0, 100)]
		public ListArrayFloat mGroupOdds = new ListArrayFloat(new float[] { 100.0f, 100.0f, 100.0f, 100.0f });

		[Tooltip("Whether we choose groups at the end of a measure or the end of a chord progression")]
		public eGroupRate mGroupRate = eGroupRate.eEndOfMeasure;

		[Tooltip(" selecting groups, whether they are chosen randomly, or crescendo linearly")]
		public eDynamicStyle mDynamicStyle = eDynamicStyle.Linear;

		[Tooltip("our time signature. see: https://en.wikipedia.org/wiki/Time_signature")]
		public eTimeSignature mTimeSignature = eTimeSignature.FourFour;

		/// Effects settings: These defaults are more or less unity's base values:
		public Pair_String_Float mDistortion = new Pair_String_Float("MasterDistortion", 0);
		public Pair_String_Float mCenterFreq = new Pair_String_Float("MasterCenterFrequency", 0);
		public Pair_String_Float mOctaveRange = new Pair_String_Float("MasterOctaveRange", 0);
		public Pair_String_Float mFreqGain = new Pair_String_Float("MasterFrequencyGain", 0);
		public Pair_String_Float mLowpassCutoffFreq = new Pair_String_Float("MasterLowpassCutoffFreq", 0);
		public Pair_String_Float mLowpassResonance = new Pair_String_Float("MasterLowpassResonance", 0);
		public Pair_String_Float mHighpassCutoffFreq = new Pair_String_Float("MasterHighpassCutoffFreq", 0);
		public Pair_String_Float mHighpassResonance = new Pair_String_Float("MasterHighpassResonance", 0);
		public Pair_String_Float mEchoDelay = new Pair_String_Float("MasterEchoDelay", 0);
		public Pair_String_Float mEchoDecay = new Pair_String_Float("MasterEchoDecay", 0);
		public Pair_String_Float mEchoDry = new Pair_String_Float("MasterEchoDry", 0);
		public Pair_String_Float mEchoWet = new Pair_String_Float("MasterEchoWet", 0);
		public Pair_String_Float mNumEchoChannels = new Pair_String_Float("MasterNumEchoChannels", 0);
		public Pair_String_Float mReverb = new Pair_String_Float("MasterReverb", 0);
		public Pair_String_Float mRoomSize = new Pair_String_Float("MasterRoomSize", 0);
		public Pair_String_Float mReverbDecay = new Pair_String_Float("MasterReverbDecay", 0);

		/// <summary>
		/// Saves our data
		/// </summary>
		/// <param name="pathIN"></param>
		/// <param name="data"></param>
		public static void SaveData(string pathIN, MusicGeneratorData data)
		{
			data.mVersion = MusicGenerator.Version;
			string save = JsonUtility.ToJson(data);
			File.WriteAllText(pathIN + "/generator.txt", save);
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		/// <summary>
		/// Loads our data
		/// </summary>
		/// <param name="pathIN"></param>
		/// <returns></returns>
		public static IEnumerator LoadData(string pathIN, System.Action<MusicGeneratorData> callback)
		{
			string data = null;
			yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentSaves/" + pathIN + "/generator.txt", (x) => { data = x.downloadHandler.text; });
			MusicGeneratorData generatorData = JsonUtility.FromJson<MusicGeneratorData>(data);

			// Version check and update.
			if (generatorData.mVersion != MusicGenerator.Version)
			{
				ChordProgressionData chordSave = new ChordProgressionData();
				string persistentDir = MusicFileConfig.GetPersistentSaveDirectory(pathIN);

				// apply the needed changes for version 1.1. was null before.
				if (generatorData.mVersion == 0.0f)
				{
					string generatorSaveData = null;
					yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentSaves/" + pathIN + "/generator.txt", (x) => { generatorSaveData = x.downloadHandler.text; });
					GeneratorSave generatorSave = JsonUtility.FromJson<GeneratorSave>(generatorSaveData);

					generatorData.mDistortion = new Pair_String_Float("MasterDistortion", generatorSave.mDistortion);
					generatorData.mCenterFreq = new Pair_String_Float("MasterCenterFrequency", generatorSave.mCenterFreq);
					generatorData.mOctaveRange = new Pair_String_Float("MasterOctaveRange", generatorSave.mOctaveRange);
					generatorData.mFreqGain = new Pair_String_Float("MasterFrequencyGain", generatorSave.mFreqGain);
					generatorData.mLowpassCutoffFreq = new Pair_String_Float("MasterLowpassCutoffFreq", generatorSave.mLowpassCutoffFreq);
					generatorData.mLowpassResonance = new Pair_String_Float("MasterLowpassResonance", generatorSave.mLowpassResonance);
					generatorData.mHighpassCutoffFreq = new Pair_String_Float("MasterHighpassCutoffFreq", generatorSave.mHighpassCutoffFreq);
					generatorData.mHighpassResonance = new Pair_String_Float("MasterHighpassResonance", generatorSave.mHighpassResonance);
					generatorData.mEchoDelay = new Pair_String_Float("MasterEchoDelay", generatorSave.mEchoDelay);
					generatorData.mEchoDecay = new Pair_String_Float("MasterEchoDecay", generatorSave.mEchoDecay);
					generatorData.mEchoDry = new Pair_String_Float("MasterEchoDry", generatorSave.mEchoDry);
					generatorData.mEchoWet = new Pair_String_Float("MasterEchoWet", generatorSave.mEchoWet);
					generatorData.mNumEchoChannels = new Pair_String_Float("MasterNumEchoChannels", generatorSave.mNumEchoChannels);
					generatorData.mReverb = new Pair_String_Float("MasterReverb", generatorSave.mRever);
					generatorData.mRoomSize = new Pair_String_Float("MasterRoomSize", generatorSave.mRoomSize);
					generatorData.mReverbDecay = new Pair_String_Float("MasterReverbDecay", generatorSave.mReverbDecay);

					generatorData.mGroupRate = (eGroupRate)generatorSave.mGroupRate;

					// We also need to create a chord progression data object:
					chordSave.mExcludedProgSteps = generatorSave.mExcludedProgSteps.ToArray();
					chordSave.SubdominantInfluence = generatorSave.mSubdominantInfluence;
					chordSave.DominantInfluence = generatorSave.mDominantInfluence;
					chordSave.TonicInfluence = generatorSave.mTonicInfluence;
					chordSave.TritoneSubInfluence = generatorSave.mTritoneSubInfluence;
				}
				else if (generatorData.mVersion == 1.1f)
				{
					string generatorSaveData = null;
					yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentSaves/" + pathIN + "/generator.txt", (x) => { generatorSaveData = x.downloadHandler.text; });
					GeneratorSave generatorSave = JsonUtility.FromJson<GeneratorSave>(generatorSaveData);

					generatorData.mGroupOdds.Clear();
					for (int i = 0; i < generatorSave.mGroupOdds.Count; i++)
					{
						generatorData.mGroupOdds.Add(generatorSave.mGroupOdds[i]);
					}
				}
			}

			callback(generatorData);
			yield return null;
		}
#else
		/// <summary>
		/// Loads our generator data
		/// </summary>
		/// <param name="pathIN"></param>
		/// <returns></returns>
		public static MusicGeneratorData LoadData(string pathIN)
		{
			string data = "";
			string generatorPath = null;
			generatorPath = MusicFileConfig.GetConfigDirectory(pathIN) + "/generator.txt";

			if (File.Exists(generatorPath))
				data = File.ReadAllText(generatorPath);
			else
			{
				throw new ArgumentNullException("Generator configuration does not exist at " + pathIN);
			}
			MusicGeneratorData saveOUT = JsonUtility.FromJson<MusicGeneratorData>(data);
			if (saveOUT.mVersion != MusicGenerator.Version)
			{
				return UpdateVersion(data, pathIN, saveOUT);
			}

			return saveOUT;
		}

		private static MusicGeneratorData UpdateVersion(string data, string pathIN, MusicGeneratorData save)
		{
			GeneratorSave generatorSave = JsonUtility.FromJson<GeneratorSave>(data);
			ChordProgressionData chordSave = new ChordProgressionData();

			/// apply the needed changes for version 1.1. was null before.
			if (save.mVersion == 0.0f)
			{
				save.mDistortion = new Pair_String_Float("MasterDistortion", generatorSave.mDistortion);
				save.mCenterFreq = new Pair_String_Float("MasterCenterFrequency", generatorSave.mCenterFreq);
				save.mOctaveRange = new Pair_String_Float("MasterOctaveRange", generatorSave.mOctaveRange);
				save.mFreqGain = new Pair_String_Float("MasterFrequencyGain", generatorSave.mFreqGain);
				save.mLowpassCutoffFreq = new Pair_String_Float("MasterLowpassCutoffFreq", generatorSave.mLowpassCutoffFreq);
				save.mLowpassResonance = new Pair_String_Float("MasterLowpassResonance", generatorSave.mLowpassResonance);
				save.mHighpassCutoffFreq = new Pair_String_Float("MasterHighpassCutoffFreq", generatorSave.mHighpassCutoffFreq);
				save.mHighpassResonance = new Pair_String_Float("MasterHighpassResonance", generatorSave.mHighpassResonance);
				save.mEchoDelay = new Pair_String_Float("MasterEchoDelay", generatorSave.mEchoDelay);
				save.mEchoDecay = new Pair_String_Float("MasterEchoDecay", generatorSave.mEchoDecay);
				save.mEchoDry = new Pair_String_Float("MasterEchoDry", generatorSave.mEchoDry);
				save.mEchoWet = new Pair_String_Float("MasterEchoWet", generatorSave.mEchoWet);
				save.mNumEchoChannels = new Pair_String_Float("MasterNumEchoChannels", generatorSave.mNumEchoChannels);
				save.mReverb = new Pair_String_Float("MasterReverb", generatorSave.mRever);
				save.mRoomSize = new Pair_String_Float("MasterRoomSize", generatorSave.mRoomSize);
				save.mReverbDecay = new Pair_String_Float("MasterReverbDecay", generatorSave.mReverbDecay);

				save.mGroupRate = (eGroupRate)generatorSave.mGroupRate;

				/// We also need to create a chord progression data object:
				chordSave.mExcludedProgSteps = generatorSave.mExcludedProgSteps.ToArray();
				chordSave.SubdominantInfluence = generatorSave.mSubdominantInfluence;
				chordSave.DominantInfluence = generatorSave.mDominantInfluence;
				chordSave.TonicInfluence = generatorSave.mTonicInfluence;
				chordSave.TritoneSubInfluence = generatorSave.mTritoneSubInfluence;
			}

			return save;
		}
#endif
	}
}