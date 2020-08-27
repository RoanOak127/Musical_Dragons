using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProcGenMusic
{
	[Serializable]
	public class InstrumentSetData
	{
		[SerializeField]
		///<summary> version number of this save.</summary>
		private float mVersion = 0.0f;

		[Tooltip("our time signature.")]
		///<summary> Our time signature</summary>
		public eTimeSignature mTimeSignature = eTimeSignature.FourFour;

		[Tooltip("how quickly we step through our chord progression")]
		///<summary> how quickly we step through our chord progression</summary>
		public eProgressionRate mProgressionRate = eProgressionRate.eight;

		[Tooltip("current tempo")]
		[Range(1, 350)]
		[SerializeField]
		///<summary>current tempo</summary>
		private float mTempo = 100.0f;
		public float Tempo { get { return mTempo; } set { mTempo = Mathf.Clamp(value, InstrumentSet.mMinTempo, InstrumentSet.mMaxTempo); } }

		[Tooltip("number measure we'll repeat, if we're repeating measures")]
		[Range(1, 4)]
		[SerializeField]
		///<summary>number measure we'll repeat, if we're repeating measures</summary>
		private int mRepeatMeasuresNum = 1;
		public int RepeatMeasuresNum { get { return mRepeatMeasuresNum; } set { mRepeatMeasuresNum = Mathf.Clamp(value, 1, 4); } }

		/// <summary>
		/// Saves our instrument set data
		/// </summary>
		/// <param name="pathIN"></param>
		/// <param name="data"></param>
		public static void SaveData(string pathIN, InstrumentSetData data)
		{
			data.mVersion = MusicGenerator.Version;
			pathIN = pathIN + "/InstrumentSetData.txt";
			string save = JsonUtility.ToJson(data);
			File.WriteAllText(pathIN, save);
			UnityEngine.Debug.Log("file successfully written to " + pathIN);
		}

		private static InstrumentSetData UpdateVersion(string pathIN, InstrumentSetData save)
		{
			if (save == null || save.mVersion == 0.0f)
			{
				string generatorPath = MusicFileConfig.GetConfigDirectory(pathIN) + "/generator.txt";
				/// we need to grab these from the generatorSave as the variables belonged to that in the last version
				if (File.Exists(generatorPath))
				{
					GeneratorSave generatorSave = JsonUtility.FromJson<GeneratorSave>(File.ReadAllText(generatorPath));
					save = new InstrumentSetData();
					save.Tempo = generatorSave.mTempo;
					save.RepeatMeasuresNum = generatorSave.mRepeatMeasuresNum;
					save.mProgressionRate = (eProgressionRate)generatorSave.mProgressionRate;
					save.mTimeSignature = generatorSave.mTimeSignature;
				}
			}
			return save;
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		/// <summary>
		/// Loads our instrument set data
		/// </summary>
		/// <param name="pathIN"></param>
		/// <returns></returns>
		public static IEnumerator LoadData(string pathIN, System.Action<InstrumentSetData> callback)
		{
			string data = null;
			yield return MusicHelpers.GetUWR(MusicFileConfig.mSavesPath + "/" + pathIN + "/InstrumentSetData.txt", (x) => { data = x.downloadHandler.text; });

			InstrumentSetData saveOUT = JsonUtility.FromJson<InstrumentSetData>(data);
			// Version check and update.
			if (saveOUT.mVersion == 0.0f)
			{
				string generatorData = null;
				yield return MusicHelpers.GetUWR("/MusicGenerator/InstrumentSaves/" + pathIN + "/generator.txt", (x) => { generatorData = x.downloadHandler.text; });
				GeneratorSave generatorSave = JsonUtility.FromJson<GeneratorSave>(generatorData);

				/// we need to grab these from the generatorSave as the variables belonged to that in the last version
				saveOUT = new InstrumentSetData();
				saveOUT.Tempo = generatorSave.mTempo;
				saveOUT.RepeatMeasuresNum = generatorSave.mRepeatMeasuresNum;
				saveOUT.mProgressionRate = (eProgressionRate)generatorSave.mProgressionRate;
				saveOUT.mTimeSignature = generatorSave.mTimeSignature;
			}
			callback(saveOUT);
			yield return null;
		}
#else 
		/// <summary>
		/// Loads our instrument set data
		/// </summary>
		/// <param name="pathIN"></param>
		/// <returns></returns>
		public static InstrumentSetData LoadData(string pathIN)
		{
			string data = "";
			string instrumentSetDataPath = MusicFileConfig.GetConfigDirectory(pathIN) + "/InstrumentSetData.txt";
			if (File.Exists(instrumentSetDataPath))
			{
				data = File.ReadAllText(instrumentSetDataPath);
			}
			else
			{
				throw new ArgumentNullException("Instrument set configuration does not exist at " + pathIN);
			}
			InstrumentSetData saveOUT = JsonUtility.FromJson<InstrumentSetData>(data);
			if (saveOUT == null || saveOUT.mVersion != MusicGenerator.Version)
				return UpdateVersion(pathIN, saveOUT);

			return saveOUT;
		}
#endif
	}
}