using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace ProcGenMusic
{
	/// <summary>
	/// This class handles loading and saving of config files.
	/// </summary>
	public class MusicFileConfig : MonoBehaviour
	{

#if !UNITY_EDITOR && UNITY_ANDROID
		public static string mClipsPath { get { return "/MusicGenerator/InstrumentClips"; } }
		public string mPersistentClipsPath { get { return Application.persistentDataPath + mClipsPath; } }
		public string mStreamingClipsPath { get { return Application.streamingAssetsPath + mClipsPath; } }

		public static string mSavesPath { get { return "/MusicGenerator/InstrumentSaves"; } }
		public string mPersistentSavesPath { get { return Application.persistentDataPath + mSavesPath; } }
		public string mStreamingSavesPath { get { return Application.streamingAssetsPath + mSavesPath; } }
#else
		public static string mClipsPath { get { return "/InstrumentClips"; } }
		public string mPersistentClipsPath { get { return Application.persistentDataPath + mClipsPath; } }
		public string mStreamingClipsPath { get { return Application.streamingAssetsPath + "/MusicGenerator" + mClipsPath; } }

		public static string mSavesPath { get { return "/InstrumentSaves"; } }
		public string mPersistentSavesPath { get { return Application.persistentDataPath + mSavesPath; } }
		public string mStreamingSavesPath { get { return Application.streamingAssetsPath + "/MusicGenerator" + mSavesPath; } }
#endif //!UNITY_EDITOR && UNITY_ANDROID

		/// Path to the tooltips
		private const string TOOLTIPS_PATH = "/MusicGenerator/tooltips.txt";

		/// Path to the presets save file
		private string mPresetsSavesPath = Application.streamingAssetsPath + "/MusicGenerator/InstrumentSaves/presets.txt";

		/// Path to the clips presets file
		private string mPresetsClipsPath = Application.streamingAssetsPath + "/MusicGenerator/InstrumentClips/presets.txt";

		/// <summary>
		/// Returns the Persistent saves directory
		/// </summary>
		/// <param name="argDirectory"></param>
		/// <returns></returns>
		public static string GetPersistentSaveDirectory(string argDirectory)
		{
			string saveDir = Application.persistentDataPath + mSavesPath + "/" + argDirectory;

			if (Directory.Exists(saveDir) == false)
			{
				Directory.CreateDirectory(saveDir);
			}

			return saveDir;
		}

		/// <summary>
		/// Exports the names of the config presets for mobile loading
		/// </summary>
		/// <param name="argPresets"></param>
		public void ExportMobilePresets(List<string> argPresets)
		{
			string presetNames = string.Join(",", argPresets.ToArray());
			File.WriteAllText(mPresetsSavesPath, presetNames);
		}

		/// <summary>
		/// Exports the names of the clip presets for mobile loading
		/// </summary>
		/// <param name="argPresets"></param>
		public void ExportMobileClipPresets(string argPresets)
		{
			File.WriteAllText(mPresetsClipsPath, argPresets);
		}

		/// <summary>
		/// saves a global configuration: instruments, instrument and global settings.
		/// </summary>
		/// <param name="argFileName"></param>
		public static void SaveConfiguration(string argFileName)
		{
			string directory = GetPersistentSaveDirectory(argFileName);
			MusicGeneratorData.SaveData(directory, MusicGenerator.Instance.mGeneratorData);
			InstrumentSetData.SaveData(directory, MusicGenerator.Instance.mInstrumentSet.mData);
			ChordProgressionData.SaveData(directory, MusicGenerator.Instance.mChordProgressions.mData);
			SaveInstrumentData(directory);
			UnityEngine.Debug.Log(directory + " was successfully written to file");
		}

		/// <summary>
		/// Saves the instrument data
		/// </summary>
		/// <param name="argDirectory"></param>
		private static void SaveInstrumentData(string argDirectory)
		{
			for (int i = 0; i < MusicGenerator.mMaxInstruments; i++)
			{
				string fileName = argDirectory + "/instruments" + i + ".txt";
				if (i < MusicGenerator.Instance.mInstrumentSet.mInstruments.Count)
				{
					Instrument instrument = MusicGenerator.Instance.mInstrumentSet.mInstruments[i];
					InstrumentData.SaveData(fileName, instrument.mData);
				}
				else if (File.Exists(fileName))
				{
					/// If the user has deleted an instrument since the last save, 
					/// we need to delete that file.
					File.Delete(fileName);
				}
			}
		}

		/// <summary>
		/// saves the tooltips.
		/// </summary>
		/// <param name="argFileName"></param>
		/// <param name="argTooltipSave"></param>
		public static void SaveTooltips(string argFileName, TooltipSave argTooltipSave)
		{
			string fileName = Application.streamingAssetsPath + TOOLTIPS_PATH;

			string serializedString = JsonUtility.ToJson(argTooltipSave);
			File.WriteAllText(fileName, serializedString);
			UnityEngine.Debug.Log("tooltips saved");
		}

		/// <summary>
		/// loads the tooltips:
		/// </summary>
		/// <returns></returns>
		public static TooltipSave LoadTooltips()
		{
			string fileName = Application.streamingAssetsPath + TOOLTIPS_PATH;
			string tooltipsString = File.ReadAllText(fileName);

			TooltipSave saveOUT = JsonUtility.FromJson<TooltipSave>(tooltipsString);

			if (saveOUT == null)
			{
				throw new ArgumentNullException("tooltip file was not sucessfully loaded");
			}

			return saveOUT;
		}

		/// <summary>
		/// saves a clip configuration: 
		/// </summary>
		/// <param name="argFileName"></param>
		/// <param name="argSerializedString"></param>
		public void SaveClipConfiguration(string argFileName, string argSerializedString)
		{
			if (Directory.Exists(mPersistentClipsPath) == false)
			{
				Directory.CreateDirectory(mPersistentClipsPath);
			}

			File.WriteAllText(mPersistentClipsPath + "/" + argFileName + ".txt", argSerializedString);
			UnityEngine.Debug.Log(argFileName + " saved");
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		/// <summary>
		/// Async Loads a global configuration: instruments, global settings, etc.
		/// </summary>
		/// <param name="folderIN"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public IEnumerator AsyncLoadConfig(string argDirectory, eGeneratorState argContinueState)
		{
			yield return StartCoroutine(LoadAndroidConfig(argDirectory, argContinueState, true));
		}

		/// <summary>
		/// Loads a global configuration: instruments, global settings, etc.
		/// </summary>
		/// <param name="folderIN"></param>
		public IEnumerator LoadConfig(string argDirectory, eGeneratorState argContinueState)
		{
			yield return StartCoroutine(LoadAndroidConfig(argDirectory, argContinueState));
		}

		/// <summary>
		/// Loads a global configuration: instruments, global settings, etc.
		/// </summary>
		/// <param name="folderIN"></param>
		public IEnumerator LoadAndroidConfig(string argDirectory, eGeneratorState argContinueState, bool argAsync = false)
		{
			MusicGenerator.Instance.ClearInstruments(MusicGenerator.Instance.mInstrumentSet);
			MusicGenerator.Instance.SetState(eGeneratorState.initializing);

			yield return StartCoroutine(LoadInstrumentSetData(argDirectory));
			yield return StartCoroutine(LoadGeneratorData(argDirectory));
			yield return StartCoroutine(LoadChordProgressionData(argDirectory));
			yield return StartCoroutine(LoadInstrumentsData(argDirectory, argAsync));
			SaveConfiguration(argDirectory);
			MusicGenerator.Instance.SetState(argContinueState);
			
			yield return null;
		}
		
		/// <summary>
		/// Loads the data for an instrument.
		/// </summary>
		/// <param name="resourceDirectory"></param>
		private IEnumerator LoadInstrumentsData(string argDirectory, bool async = false)
		{
			UnitySystemConsoleRedirector.Redirect();
			InstrumentSet set = MusicGenerator.Instance.mInstrumentSet;

			for (int i = 0; i < MusicGenerator.mMaxInstruments; i++)
			{
				string path = "/instruments" + i.ToString() + ".txt";

				string data = null;
				yield return MusicHelpers.GetUWR(mSavesPath + "/" + argDirectory + path, (x) => { data = x.downloadHandler.text; });

				if (string.IsNullOrEmpty(data))
				{
					yield break;
				}

				InstrumentData instrumentData = null;
				InstrumentData.LoadData(data, argDirectory, (x) => { instrumentData = x; });
				MusicGenerator.Instance.AddInstrument(set);
				set.mInstruments[set.mInstruments.Count - 1].LoadInstrument(instrumentData);
				int index = 0;
				if(async)
				{
					yield return StartCoroutine(MusicGenerator.Instance.AsyncLoadBaseClips(set.mInstruments[set.mInstruments.Count - 1].mData.InstrumentType, ((x) => { index = x; })));
				}
				else
				{
					index = MusicGenerator.Instance.LoadBaseClips(set.mInstruments[set.mInstruments.Count - 1].mData.InstrumentType);
				}
				set.mInstruments[set.mInstruments.Count - 1].InstrumentTypeIndex = index;
			}
			yield return null;
		}

		/// <summary>
		/// Loads data for chord progressions
		/// </summary>
		/// <param name="folderIN"></param>
		private IEnumerator LoadChordProgressionData(string argDirectory)
		{
			ChordProgressionData progressionData = null;

			yield return StartCoroutine(ChordProgressionData.LoadData(argDirectory, (x) => { progressionData = x; }));

			if (progressionData == null)
			{
				throw new Exception(argDirectory + " chord progression data failed to load");
			}
			else
			{
				MusicGenerator.Instance.mChordProgressions.LoadProgressionData(progressionData);
			}

			yield return null;
		}

		/// <summary>
		/// Loads data for the instrument set.
		/// </summary>
		/// <param name="folderIN"></param>
		private IEnumerator LoadInstrumentSetData(string argDirectory)
		{
			InstrumentSetData setData = null;

			yield return StartCoroutine(InstrumentSetData.LoadData(argDirectory, (x) => { setData = x; }));
			MusicGenerator.Instance.mInstrumentSet.LoadData(setData);
			yield return null;
		}

		/// <summary>
		/// Loads data for the Music Generator.
		/// </summary>
		/// <param name="folderIN"></param>
		private IEnumerator LoadGeneratorData(string argDirectory)
		{
			MusicGeneratorData data = null;
			yield return StartCoroutine(MusicGeneratorData.LoadData(argDirectory, (x) => { data = x; }));
			MusicGenerator.Instance.LoadGeneratorSave(data);
			yield return null;
		}

		/// <summary>
		/// loads the tooltips:
		/// </summary>
		/// <returns></returns>
		public static IEnumerator LoadTooltips(System.Action<TooltipSave> callback)
		{
			string tooltipsString = "";
			yield return MusicHelpers.GetUWR(TOOLTIPS_PATH, (x) => { tooltipsString = x.downloadHandler.text; }, false);

			TooltipSave saveOUT = JsonUtility.FromJson<TooltipSave>(tooltipsString);
			if (saveOUT == null)
			{
				throw new ArgumentNullException("tooltip file was not sucessfully loaded");
			}

			callback(saveOUT);
			yield return null;
		}

		/// <summary>
		/// Loads a clip configuration.
		/// </summary>
		/// <param name="fileNameIN"></param>
		/// <returns></returns>
		public IEnumerator LoadClipConfigurations(string argFileName, System.Action<ClipSave> argCallback)
		{
			string clipSaveString = "";
			yield return StartCoroutine(MusicHelpers.GetUWR(mClipsPath + "/" + argFileName, (x) => { clipSaveString = x.downloadHandler.text; }));
			ClipSave clipSave = JsonUtility.FromJson<ClipSave>(clipSaveString);

			if (clipSave == null)
				throw new ArgumentNullException("clipSave was null");

			argCallback(clipSave);
			yield return null;
		}

		/// <summary>
		/// Returns the data directory for this filename.
		/// </summary>
		/// <param name="folderIN"></param>
		/// <returns></returns>
		public static string GetConfigDirectory(string argDirectory)
		{
			string resourceDirectory = "";
			if (Directory.Exists(Application.persistentDataPath + mSavesPath + "/" + argDirectory))
			{
				resourceDirectory = Application.persistentDataPath + mSavesPath + "/" + argDirectory;
			}
			else if (Directory.Exists(Application.streamingAssetsPath + mSavesPath + "/" + argDirectory))
			{
				resourceDirectory = Application.streamingAssetsPath + mSavesPath + "/" + argDirectory;
			}
			else
			{
				throw new NullReferenceException("Configuration for " + argDirectory + " does not exist.");

			}
			return resourceDirectory;
		}
#else
		/// Loads a global configuration: instruments, global settings, etc.
		public void LoadConfig(string argDirectory, eGeneratorState argContinueState)
		{
			MusicGenerator.Instance.ClearInstruments(MusicGenerator.Instance.mInstrumentSet);

			LoadInstrumentSetData(argDirectory);
			LoadGeneratorData(argDirectory);
			LoadChordProgressionData(argDirectory);
			LoadInstrumentsData(argDirectory);
			SaveConfiguration(argDirectory);
			MusicGenerator.Instance.SetState(argContinueState);
		}

		/// <summary>
		/// async Loads a global configuration: instruments, global settings, etc.
		/// </summary>
		/// <param name="argDirectory"></param>
		/// <param name="argContinueState"></param>
		/// <returns></returns>
		public IEnumerator AsyncLoadConfig(string argDirectory, eGeneratorState argContinueState)
		{
			MusicGenerator.Instance.ClearInstruments(MusicGenerator.Instance.mInstrumentSet);

			LoadInstrumentSetData(argDirectory);
			yield return null;
			LoadGeneratorData(argDirectory);
			yield return null;
			LoadChordProgressionData(argDirectory);
			yield return null;
			yield return StartCoroutine(AsyncLoadInstrumentsData(argDirectory));
			MusicGenerator.Instance.ResetPlayer();
			SaveConfiguration(argDirectory);
			MusicGenerator.Instance.SetState(argContinueState);
			yield return null;
		}

		/// <summary>
		/// Loads a clip configuration.
		/// </summary>
		/// <param name="argFileName"></param>
		/// <returns></returns>
		public ClipSave LoadClipConfigurations(string argFileName)
		{
			string clipSaveString = File.Exists(mPersistentClipsPath + "/" + argFileName) ?
				clipSaveString = File.ReadAllText(mPersistentClipsPath + "/" + argFileName) :
				clipSaveString = File.ReadAllText(mStreamingClipsPath + "/" + argFileName);

			if (string.IsNullOrEmpty(clipSaveString))
			{
				throw new ArgumentNullException("clipSave");
			}

			ClipSave clipSave = JsonUtility.FromJson<ClipSave>(clipSaveString);

			if (clipSave == null)
			{
				throw new ArgumentNullException("clipSave");
			}

			return clipSave;
		}

		/// <summary>
		/// Async loads the instrument data.
		/// </summary>
		/// <param name="argResourceDirectory"></param>
		/// <returns></returns>
		public IEnumerator AsyncLoadInstrumentsData(string argResourceDirectory)
		{
			///Load the instruments:
			InstrumentSet set = MusicGenerator.Instance.mInstrumentSet;
			for (int i = 0; i < MusicGenerator.mMaxInstruments; i++)
			{
				string path = MusicFileConfig.GetConfigDirectory(argResourceDirectory) + "/instruments" + i.ToString() + ".txt";
				if (File.Exists(path))
				{
					InstrumentData instrumentData = InstrumentData.LoadData(argResourceDirectory, "/instruments" + i.ToString() + ".txt");
					if (instrumentData == null)
					{
						yield break;
					}
					MusicGenerator.Instance.AddInstrument(set);
					yield return null;
					set.mInstruments[set.mInstruments.Count - 1].LoadInstrument(instrumentData);
					int index = 999;
					yield return StartCoroutine(MusicGenerator.Instance.AsyncLoadBaseClips(set.mInstruments[set.mInstruments.Count - 1].mData.InstrumentType, ((x) => { index = x; })));
					set.mInstruments[set.mInstruments.Count - 1].InstrumentTypeIndex = index;
				}
			}
			yield return null;
		}

		/// <summary>
		/// Loads data for chord progressions
		/// </summary>
		/// <param name="folderIN"></param>
		private static void LoadChordProgressionData(string folderIN)
		{
			ChordProgressionData progressionData = ChordProgressionData.LoadData(folderIN);
			if (progressionData == null)
			{
				throw new Exception(folderIN + " chord progression data failed to load");
			}
			else
			{
				MusicGenerator.Instance.mChordProgressions.LoadProgressionData(progressionData);
			}
		}

		/// <summary>
		/// Loads data for the instrument set.
		/// </summary>
		/// <param name="folderIN"></param>
		private static void LoadInstrumentSetData(string folderIN)
		{
			InstrumentSetData setData = InstrumentSetData.LoadData(folderIN);
			MusicGenerator.Instance.mInstrumentSet.LoadData(setData);
		}

		/// <summary>
		/// Returns the data directory for this filename.
		/// </summary>
		/// <param name="folderIN"></param>
		/// <returns></returns>
		public static string GetConfigDirectory(string folderIN)
		{
			string resourceDirectory = "";
			if (Directory.Exists(Application.persistentDataPath + "/InstrumentSaves/" + folderIN))
			{
				resourceDirectory = Path.Combine(Application.persistentDataPath, "InstrumentSaves");
				resourceDirectory = Path.Combine(resourceDirectory, folderIN);
			}
			else if (Directory.Exists(Application.streamingAssetsPath + "/MusicGenerator/InstrumentSaves/" + folderIN))
			{
				resourceDirectory = Path.Combine(Application.streamingAssetsPath, "MusicGenerator");
				resourceDirectory = Path.Combine(resourceDirectory, "InstrumentSaves");
				resourceDirectory = Path.Combine(resourceDirectory, folderIN);
			}
			else
			{
				throw new NullReferenceException("Configuration for " + folderIN + " does not exist.");
			}

			return resourceDirectory;
		}

		/// <summary>
		/// Loads data for the Music Generator.
		/// </summary>
		/// <param name="folderIN"></param>
		private static void LoadGeneratorData(string folderIN)
		{
			MusicGeneratorData data = MusicGeneratorData.LoadData(folderIN);
			if (data == null)
			{
				throw new ArgumentNullException("Generator configuration does not exist at " + folderIN);
			}
			MusicGenerator.Instance.LoadGeneratorSave(data);
		}

		/// <summary>
		/// Loads the data for an instrument.
		/// </summary>
		/// <param name="folderIN"></param>
		private static void LoadInstrumentsData(string folderIN)
		{
			InstrumentSet set = MusicGenerator.Instance.mInstrumentSet;
			string configDir = GetConfigDirectory(folderIN);
			for (int i = 0; i < MusicGenerator.mMaxInstruments; i++)
			{
				string path = configDir + "/instruments" + i.ToString() + ".txt";
				if (File.Exists(path))
				{
					InstrumentData instrumentData = InstrumentData.LoadData(folderIN, "/instruments" + i.ToString() + ".txt");
					MusicGenerator.Instance.AddInstrument(set);
					set.mInstruments[set.mInstruments.Count - 1].LoadInstrument(instrumentData);
					int index = MusicGenerator.Instance.LoadBaseClips(set.mInstruments[set.mInstruments.Count - 1].mData.InstrumentType);
					set.mInstruments[set.mInstruments.Count - 1].InstrumentTypeIndex = index;
				}
			}
		}
#endif // !UNITY_EDITOR && UNITY_ANDROID
	}
}