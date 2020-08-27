using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// data for our chord progressions
	/// </summary>
	public class ChordProgressionData
	{
		///<summary> Version number of this save</summary>
		[SerializeField]
		private float mVersion = 0.0f;

		[Tooltip("which steps of our current scale are excluded from our chord progression. see:https://en.wikipedia.org/wiki/Chord_progression.")]
		[SerializeField]
		///<summary>which steps of our current scale are excluded from our chord progression. see:https://en.wikipedia.org/wiki/Chord_progression.</summary>
		public bool[] mExcludedProgSteps = new bool[] { false, false, false, false, false, false, false };

		[Tooltip("influence of the liklihood of playing a tonic chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Tonic_(music)")]
		[Range(1, 100)]
		[SerializeField]
		///<summary>influence of the liklihood of playing a tonic chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Tonic_(music)</summary>
		private float mTonicInfluence = 50.0f;
		///<summary>influence of the liklihood of playing a tonic chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Tonic_(music)</summary>
		public float TonicInfluence { get { return mTonicInfluence; } set { mTonicInfluence = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("influence of the liklihood of playing a subdominant chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Subdominant")]
		[Range(0, 100)]
		[SerializeField]
		///<summary>influence of the liklihood of playing a subdominant chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Subdominant</summary>
		private float mSubdominantInfluence = 50.0f;
		///<summary>influence of the liklihood of playing a subdominant chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Subdominant</summary>
		public float SubdominantInfluence { get { return mSubdominantInfluence; } set { mSubdominantInfluence = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("influence of the liklihood of playing a dominant chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Dominant_(music)")]
		[Range(0, 100)]
		[SerializeField]
		///<summary>influence of the liklihood of playing a dominant chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Dominant_(music)")]</summary>
		private float mDominantInfluence = 50.0f;
		///<summary>influence of the liklihood of playing a dominant chord in our progression. This isn't straight-odds, and is finessed a little. see:  https://en.wikipedia.org/wiki/Dominant_(music)")]</summary>
		public float DominantInfluence { get { return mDominantInfluence; } set { mDominantInfluence = Mathf.Clamp(value, 0, 100); } }

		[Tooltip("odds of our dominant chord being replaced by a flat-5 substitution. see: https://en.wikipedia.org/wiki/Tritone_substitution")]
		[Range(0, 100)]
		[SerializeField]
		///<summary>odds of our dominant chord being replaced by a flat-5 substitution. see: https://en.wikipedia.org/wiki/Tritone_substitution</summary>
		private float mTritoneSubInfluence = 50.0f;
		///<summary>odds of our dominant chord being replaced by a flat-5 substitution. see: https://en.wikipedia.org/wiki/Tritone_substitution</summary>
		public float TritoneSubInfluence { get { return mTritoneSubInfluence; } set { mTritoneSubInfluence = Mathf.Clamp(value, 0, 100); } }

		/// <summary>
		///  Saves chord progression data to JSON
		/// </summary>
		/// <param name="argPath"></param>
		/// <param name="argData"></param>
		public static void SaveData(string argPath, ChordProgressionData argData)
		{
			argData.mVersion = MusicGenerator.Version;
			string save = JsonUtility.ToJson(argData);
			File.WriteAllText(argPath + "/ChordProgressionData.txt", save);
		}

		/// <summary>
		/// Updates save type and variables for new generator versions.
		/// </summary>
		/// <param name="pathIN"></param>
		/// <param name="saveOUT"></param>
		/// <returns></returns>
		private static ChordProgressionData UpdateVersion(string pathIN, ChordProgressionData saveOUT)
		{
			if (saveOUT == null || saveOUT.mVersion == 0.0f)
			{
				string generatorPath = MusicFileConfig.GetConfigDirectory(pathIN) + "/generator.txt";
				/// we need to grab these from the generatorSave as the variables belonged to that in the last version
				if (File.Exists(generatorPath))
				{
					GeneratorSave generatorSave = JsonUtility.FromJson<GeneratorSave>(File.ReadAllText(generatorPath));
					saveOUT = new ChordProgressionData();
					saveOUT.DominantInfluence = generatorSave.mDominantInfluence;
					saveOUT.mExcludedProgSteps = generatorSave.mExcludedProgSteps.ToArray();
					saveOUT.SubdominantInfluence = generatorSave.mSubdominantInfluence;
					saveOUT.TonicInfluence = generatorSave.mTonicInfluence;
					saveOUT.TritoneSubInfluence = generatorSave.mTritoneSubInfluence;
				}
			}
			return saveOUT;
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		/// <summary>
		///  Loads chord progression data from JSON
		/// </summary>
		/// <param name="pathIN"></param>
		/// <returns></returns>
		public static IEnumerator LoadData(string pathIN, System.Action<ChordProgressionData> callback)
		{
			string data = null;
			yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentSaves/" + pathIN + "/ChordProgressionData.txt", (x) => { data = x.downloadHandler.text; });
			ChordProgressionData saveOUT = JsonUtility.FromJson<ChordProgressionData>(data);

			if (saveOUT.mVersion == 0.0f)
			{
				string generatorData = null;
				yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentSaves/" + pathIN + "/generator.txt", (x) => { generatorData = x.downloadHandler.text; });
				GeneratorSave generatorSave = JsonUtility.FromJson<GeneratorSave>(generatorData);
				/// we need to grab these from the generatorSave as the variables belonged to that in the last version
				saveOUT = new ChordProgressionData();
				saveOUT.DominantInfluence = generatorSave.mDominantInfluence;
				saveOUT.mExcludedProgSteps = generatorSave.mExcludedProgSteps.ToArray();
				saveOUT.SubdominantInfluence = generatorSave.mSubdominantInfluence;
				saveOUT.TonicInfluence = generatorSave.mTonicInfluence;
				saveOUT.TritoneSubInfluence = generatorSave.mTritoneSubInfluence;
			}

			callback(saveOUT);
			yield return null;
		}
#else
		/// loads from json
		public static ChordProgressionData LoadData(string pathIN)
		{
			string data = "";
			string chordProgressionDataPath = MusicFileConfig.GetConfigDirectory(pathIN) + "/ChordProgressionData.txt";
			if (File.Exists(chordProgressionDataPath))
				data = File.ReadAllText(chordProgressionDataPath);

			ChordProgressionData saveOUT = JsonUtility.FromJson<ChordProgressionData>(data);
			if (saveOUT == null || saveOUT.mVersion != MusicGenerator.Version)
				return UpdateVersion(pathIN, saveOUT);

			return saveOUT;
		}
#endif

	}
}