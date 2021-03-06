﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ProcGenMusic
{

	/// Just an array class we can treat a bit like a list, with an index of the last object added.
	/// Skipping the generic version so we can more easily serialize.
	[Serializable]
	public struct ListArrayInt
	{
		public ListArrayInt(int size)
		{
			Count = 0;
			mArray = new int[size];
		}

		public ListArrayInt(int[] contents)
		{
			Count = contents.Length;
			mArray = contents;
		}
		public bool Equals(ListArrayInt other) { return mArray == other.mArray; }
		public int this [int index] { get { return mArray[index]; } set { mArray[index] = value; } }
		public void Clear() { Count = 0; }
		public void Add(int a)
		{
			mArray[Count] = a;
			Count++;
		}

		public bool Contains(int value)
		{
			for (int i = 0; i < Count; i++)
			{
				if (mArray[i].Equals(value))
				{
					return true;
				}
			}
			return false;
		}

		[SerializeField]
		public int[] mArray;
		[SerializeField]
		public int Count;
	}

	[Serializable]
	public struct ListArrayFloat
	{
		public ListArrayFloat(int size)
		{
			Count = 0;
			mArray = new float[size];
		}

		public ListArrayFloat(float[] contents)
		{
			Count = contents.Length;
			mArray = contents;
		}
		public bool Equals(ListArrayFloat other) { return mArray == other.mArray; }
		public float this [int index] { get { return mArray[index]; } set { mArray[index] = value; } }
		public void Clear() { Count = 0; }
		public void Add(float a)
		{
			mArray[Count] = a;
			Count++;
		}

		public bool Contains(float value)
		{
			for (int i = 0; i < Count; i++)
			{
				if (mArray[i].Equals(value))
				{
					return true;
				}
			}
			return false;
		}
		public float[] mArray;
		public int Count { get; private set; }
	}

	[Serializable]
	public struct ListArrayBool
	{
		public ListArrayBool(int size)
		{
			Count = 0;
			mArray = new bool[size];
		}

		public ListArrayBool(bool[] contents)
		{
			Count = contents.Length;
			mArray = contents;
		}
		public bool Equals(ListArrayBool other) { return mArray == other.mArray; }
		public bool this [int index] { get { return mArray[index]; } set { mArray[index] = value; } }
		public void Clear() { Count = 0; }
		public void Add(bool a)
		{
			mArray[Count] = a;
			Count++;
		}

		public bool Contains(bool value)
		{
			for (int i = 0; i < Count; i++)
			{
				if (mArray[i].Equals(value))
				{
					return true;
				}
			}
			return false;
		}
		public bool[] mArray;
		public int Count { get; private set; }
	}

	[Serializable]
	public struct ListArray<T>
	{
		public ListArray(int size)
		{
			Count = 0;
			mArray = new T[size];
		}

		public ListArray(T[] contents)
		{
			Count = contents.Length;
			mArray = contents;
		}
		public bool Equals(ListArray<T> other) { return mArray == other.mArray; }
		public T this [int index] { get { return mArray[index]; } set { mArray[index] = value; } }
		public void Clear() { Count = 0; }
		public void Add(T a)
		{
			mArray[Count] = a;
			Count++;
		}

		public bool Contains(T value)
		{
			for (int i = 0; i < Count; i++)
			{
				if (mArray[i].Equals(value))
				{
					return true;
				}
			}
			return false;
		}
		public T[] mArray;
		public int Count { get; private set; }
	}

	/// just a Pair class, no real functionality other than being a container :P.
	[Serializable]
	public struct Pair_String_Float
	{
		//public Pair_String_Float(){}
		public Pair_String_Float(string first, float second)
		{
			this.First = first;
			this.Second = second;
		}

		[SerializeField]
		public String First;

		[SerializeField]
		public float Second;
	}

	/// Static class with some helper functions for the music generator.
	public static class MusicHelpers
	{
		/// returns the generator directory.
		/// using in lieu of resources or the streamingAssets directory as asset bundles need to be loaded
		/// and I'd prefer not to require the user to move assets into streamingAssets directory after installation.
		/// provided the musicGenerator folder isn't buried super deep into the application.datapath, this should be fairly quick
		/// and only runs once on awake or exporting assets.
		public static string GetMusicGeneratorPath()
		{
			string[] directories = System.IO.Directory.GetDirectories(Application.dataPath, "MusicGenerator", System.IO.SearchOption.AllDirectories);
			for (int i = 0; i < directories.Length; i++)
			{
				if (directories[i].Contains("MusicGenerator"))
					return directories[i];
			}
			return "";
		}

#if !UNITY_EDITOR && UNITY_ANDROID
		public static IEnumerator GetUWR(string argPath, System.Action<UnityWebRequest> callback, bool searchPersistentData = true)
		{
			string path = Application.streamingAssetsPath + argPath;
			var uwr = UnityWebRequest.Get(path);
			yield return uwr.SendWebRequest();
			if (searchPersistentData && string.IsNullOrEmpty(uwr.error) == false)
			{
				path = "file://" + Application.persistentDataPath + argPath;
				uwr = UnityWebRequest.Get(path);
				yield return uwr.SendWebRequest();
				if (string.IsNullOrEmpty(uwr.error) == false)
				{
					Debug.Log("Can't read file at " + path);
				}
			}
			callback(uwr);
			yield return null;
		}
#else
		public static string GetFilePath(string argPath)
		{
			string path = Application.streamingAssetsPath + argPath;
			if (File.Exists(path) == false)
			{
				path = Application.persistentDataPath + argPath;
				if (File.Exists(path) == false)
				{
					Debug.Log("Can't read file at " + path);
				}
			}
			return path;
		}
#endif

		/// returns value within 0 and max, looping, rather than clamping.
		public static int SafeLoop(int noteIN, int max)
		{
			return noteIN < 0 ? max + noteIN : noteIN % max;
		}

		/// Returns true if we're finished writing all of the files of this configuration to file.
		public static bool CheckConfigWriteComplete(string FileCurrentlyWriting)
		{
			bool isLocked = false;
			string[] files = System.IO.Directory.GetFiles(Application.persistentDataPath + "/InstrumentSaves/" + FileCurrentlyWriting);
			System.IO.FileInfo generatorInfo = new System.IO.FileInfo(Application.persistentDataPath + "/InstrumentSaves/" + FileCurrentlyWriting + "/generator.txt");
			if (IsFileLocked(generatorInfo))
				isLocked = true;
			for (int i = 1; i < files.Length; i++)
			{
				string fileName = Application.persistentDataPath + "/InstrumentSaves/" + FileCurrentlyWriting + "/instruments" + (i - 1).ToString() + ".txt";
				System.IO.FileInfo instrumentInfo = new System.IO.FileInfo(fileName);
				if (IsFileLocked(instrumentInfo))
					isLocked = true;
			}
			if (!isLocked)
				return true;

			return false;
		}

		/// Returns true if we're finished writing this clip to file.
		public static bool CheckClipwriteComplete(string FileCurrentlyWriting)
		{
			string path = "";
#if !UNITY_EDITOR && UNITY_ANDROID
			path = Application.persistentDataPath + "/MusicGenerator/InstrumentClips/" + FileCurrentlyWriting + ".txt";
#else
			path = Application.persistentDataPath + "/InstrumentClips/" + FileCurrentlyWriting + ".txt";
#endif //!UNITY_EDITOR && UNITY_ANDROID

			System.IO.FileInfo generatorInfo = new System.IO.FileInfo(path);
			if (IsFileLocked(generatorInfo))
				return false;

			return true;
		}

		/// Returns whether this file is locked.
		public static bool IsFileLocked(System.IO.FileInfo file)
		{
			System.IO.FileStream stream = null;

			try
			{
				stream = file.Open(System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
			}
			catch (System.IO.IOException)
			{
				return true;
			}
			finally
			{
				if (stream != null)
					stream.Close();
			}

			return false;
		}
	}

	//TO NOTE: not thread safe. But, this is created by the generator in awake and never destroyed. If accessed any time after awake is called should be fine.
	//please, let it wake itself up and don't try to access MusicGenerator.Instance in awake() :P
	//To ensure only a single instance of this class.
	//Used only by the MusicGenerator and UI. 
	//MusicGenerator creation is slower as sound assets are loaded on creation. Create one at game start and never destroy.
	public class HelperSingleton<T> : MonoBehaviour where T : Component
	{
		private static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<T>();
					if (instance == null)
					{
						GameObject obj = new GameObject();
						obj.name = typeof(T).Name;
						instance = obj.AddComponent<T>();
					}
				}
				return instance;
			}
		}

		public virtual void Awake()
		{
			if (instance == null)
			{
				instance = this as T;
				if (transform == transform.root)
					DontDestroyOnLoad(this.gameObject);
			}
			else
				Destroy(gameObject);
		}
	}

	////////////////////////////////////
	/// event args:
	////////////////////////////////////

	/// Clip loaded event:
	public class ClipLoadedArgs : EventArgs
	{
		public ClipLoadedArgs(ClipSave clipConfig)
		{
			mTempo = clipConfig.mTempo;
			mKey = (int)clipConfig.mKey;
			mScale = (int)clipConfig.mScale;
			mMode = (int)clipConfig.mMode;
		}
		public float mTempo = 100.0f;
		public int mKey = 0;
		public int mScale = 0;
		public int mMode = 0;
	}

	/// Repeat note args:
	public class RepeatNoteArgs : EventArgs
	{
		public RepeatNoteArgs(int indexAIN, int indexBIN, int repeatingCountIN, int instrumentSubIndexIN, InstrumentSet setIN)
		{
			indexA = indexAIN;
			indexB = indexBIN;
			repeatingCount = repeatingCountIN;
			instrumentSubIndex = instrumentSubIndexIN;
			instrumentSet = setIN;
		}
		public int indexA = 0;
		public int indexB = 0;
		public int repeatingCount = 0;
		public int instrumentSubIndex;
		public InstrumentSet instrumentSet = null;
	}

	//InstrumentSet set, int clipIndexX, int clipIndexY, float volumeIN, int instrumentIndex
	public class NotePlayedArgs : EventArgs
	{
		public NotePlayedArgs(InstrumentSet argSet, int argClipIndexX, int argClipIndexY, float argVolumeIN, int argInstrumentIndex)
		{
			set = argSet;
			clipIndexX = argClipIndexX;
			clipIndexY = argClipIndexY;
			volume = argVolumeIN;
			instrumentIndex = argInstrumentIndex;
		}
		public InstrumentSet set = null;
		public int clipIndexX = 0;
		public int clipIndexY = 0;
		public float volume = 0;
		public int instrumentIndex = 0;
	}
	//////////////////////////////
	/// Save classes:
	//////////////////////////////
	[Serializable]
	public class TooltipEntry
	{
		public string[] mTooltips = new string[2] { "", "" };
		public TooltipEntry(string name, string value) { mTooltips[0] = name; mTooltips[1] = value; }
	}

	[Serializable]
	public class TooltipSave
	{
		public List<TooltipEntry> mTooltips = new List<TooltipEntry>();
	}

	///Deprecated. Use MusicGeneratorData
	[Serializable]
	public class GeneratorSave
	{
		public float mStateTimer = 0.0f;
		public float mStartDelay = 1.0f;
		public float mMasterVolume = 1.0f;
		public float mVolFadeRate = 2.0f;
		public eMode mMode = eMode.Ionian;
		public eThemeRepeatOptions mThemeRepeatOptions = eThemeRepeatOptions.eNone;
		public int mKeySteps = 0;
		public float mKeyChangeAscendDescend = 50.0f;
		public float mSetThemeOdds = 10.0f;
		public float mPlayThemeOdds = 90.0f;
		public eScale mScale = 0;
		public float mProgressionChangeOdds = 25.0f;
		public eKey mKey = 0;
		public float mKeyChangeOdds = 0.0f;
		public List<float> mGroupOdds = new List<float>() { 100.0f, 100.0f, 100.0f, 100.0f };
		public uint mGroupRate = 0;
		public float mTempo = 0.0f;
		public int mRepeatMeasuresNum = 2;
		public List<bool> mExcludedProgSteps = new List<bool>() { false, false, false, false, false, false, false };
		public float mTonicInfluence = 50.0f;
		public float mSubdominantInfluence = 50.0f;
		public float mDominantInfluence = 50.0f;
		public float mTritoneSubInfluence = 50.0f;
		public int mProgressionRate = 8;
		public eDynamicStyle mDynamicStyle = eDynamicStyle.Linear;
		public eTimeSignature mTimeSignature = eTimeSignature.FourFour;

		public float mDistortion = 0.0f;
		public float mCenterFreq = 0.0f;
		public float mOctaveRange = 0.0f;
		public float mFreqGain = 0.0f;
		public float mLowpassCutoffFreq = 0.0f;
		public float mLowpassResonance = 0.0f;
		public float mHighpassCutoffFreq = 0.0f;
		public float mHighpassResonance = 0.0f;
		public float mEchoDelay = 0.0f;
		public float mEchoDecay = 0.0f;
		public float mEchoDry = 0.0f;
		public float mEchoWet = 0.0f;
		public float mNumEchoChannels = 4;
		public float mRever = 0.0f;
		public float mRoomSize = 0.0f;
		public float mReverbDecay = 0.0f;

		public float[] mAudioSourceVolume = new float[10] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
	}

	///Deprecated Use InstrumentData
	[Serializable]
	public class InstrumentSave
	{
		public uint mChordSize = 3;
		public uint mStaffPlayerColor = (int)eStaffPlayerColors.Red;
		public eSuccessionType mSuccessionType = eSuccessionType.melody;
		public string mInstrumentType = "ViolinShort";
		public List<int> mOctavesToUse = new List<int> { 0, 1, 2 };
		public float mStereoPan = 0;
		public uint mOddsOfPlaying = 50;
		public uint mPreviousOdds = 100;
		public float mVolume = 0.5f;
		public float mOctaveOdds = 20;
		static public float mOddsOfPlayingMultiplierBase = 1.0f;
		public float mOddsOfPlayingMultiplierMax = 1.5f;
		public float mOddsOfPlayingMultiplier = 1.0f;
		public uint mGroup = 0;
		public bool mIsSolo = false;
		public bool mIsMuted = false;
		public eTimestep mTimeStep = eTimestep.quarter;
		public float mOddsOfUsingChord = 50.0f;
		public float mOddsOfUsingChordNotes = 50.0f;
		public float mStrumLength = 0.00f;
		public float mStrumVariation = 0.0f;
		public float mRedundancyAvoidance = 75.0f;
		public float mRoomSize = 0.0f;
		public float mReverb = 0.0f;
		public float mEcho = 0.0f;
		public float mEchoDelay = 0.0f;
		public float mEchoDecay = 0.0f;
		public float mFlanger = 0.0f;
		public float mDistortion = 0.0f;
		public float mChorus = 0.0f;
		public uint mUsePattern = 0;
		public uint mPatternlength = 4;
		public uint mPatternRelease = 4;
		public float mRedundancyOdds = 50.0f;
		public uint mLeadMaxSteps = 3;
		public float AscendDescendInfluence = 75.0f;
		public float mAudioSourceVolume = 0.0f;
	}

	[Serializable]
	public class ClipNotesTimeStep
	{
		public List<int> notes = new List<int>();
	}

	[Serializable]
	public class ClipNotesMeasure
	{
		public List<ClipNotesTimeStep> timestep = new List<ClipNotesTimeStep>();
	}

	[Serializable]
	public class ClipSave
	{
		public List<ClipInstrumentSave> mClipInstrumentSaves = new List<ClipInstrumentSave>();

		public float mTempo;
		public int mProgressionRate;
		public int mNumberOfMeasures;
		public eKey mKey;
		public eMode mMode;
		public eScale mScale;
		public bool mClipIsRepeating;
	}

	[Serializable]
	public class ClipInstrumentSave
	{
		public string mInstrumentType;
		public float mVolume;
		public int mStaffPlayerColor;
		public eTimestep mTimestep;
		public eSuccessionType mSuccessionType;
		public float mStereoPan;
		public List<ClipNotesMeasure> mClipMeasures = new List<ClipNotesMeasure>();
	}

	/// <summary>
	/// Redirects writes to System.Console to Unity3D's Debug.Log.
	/// </summary>
	/// <author>
	/// Jackson Dunstan, http://jacksondunstan.com/articles/2986
	/// </author>
	public static class UnitySystemConsoleRedirector
	{
		private class UnityTextWriter : TextWriter
		{
			private StringBuilder buffer = new StringBuilder();

			public override void Flush()
			{
				Debug.Log(buffer.ToString());
				buffer.Length = 0;
			}

			public override void Write(string value)
			{
				buffer.Append(value);
				if (value != null)
				{
					var len = value.Length;
					if (len > 0)
					{
						var lastChar = value[len - 1];
						if (lastChar == '\n')
						{
							Flush();
						}
					}
				}
			}

			public override void Write(char value)
			{
				buffer.Append(value);
				if (value == '\n')
				{
					Flush();
				}
			}

			public override void Write(char[] value, int index, int count)
			{
				Write(new string(value, index, count));
			}

			public override Encoding Encoding
			{
				get { return Encoding.Default; }
			}
		}

		public static void Redirect()
		{
			Console.SetOut(new UnityTextWriter());
		}
	}

	public enum eStaffPlayerColors {Brown, Blue, Red, LightBlue, Black, White, Pink, Green, Orange }

	/// Available time signatures.
	[Serializable]
	public enum eTimeSignature { FourFour, ThreeFour, FiveFour }

	/// State of the clip.
	public enum eClipState { Play, Pause, Stop }

	/// The type of succession. melody, rhythm or lead
	[Serializable]
	public enum eSuccessionType { melody, rhythm, lead }

	/// Musical key
	[Serializable]
	public enum eKey { C, CSharp, D, DSharp, E, F, FSharp, G, GSharp, A, ASharp, B }

	/// Music Generator repeat options
	[Serializable]
	public enum eThemeRepeatOptions { eNone, eUseTheme, eRepeat }

	/// Music Generator timesteps
	[Serializable]
	public enum eTimestep { sixteenth, eighth, quarter, half, whole }

	/// progression rate:
	[Serializable]
	public enum eProgressionRate { sixteen = 16, eight = 8, four = 4, two = 2, one = 1 }

	/// Rate at which we roll for new musical groups of instruments
	[Serializable]
	public enum eGroupRate { eEndOfMeasure, eEndOfProgression }

	/// State of the music generator
	[Serializable]
	public enum eGeneratorState { loading, initializing, ready, stopped, playing, repeating, paused, editorInitializing, editing, editorPlaying, editorPaused, editorStopped }

	/// Music generator volume states
	[Serializable]
	public enum eVolumeState { idle, fadedOutIdle, fadingIn, fadingOut }

	/// Music Generator modes
	[Serializable]
	public enum eMode { Ionian, Dorian, Phrygian, Lydian, Mixolydian, Aoelean, Locrian }

	/// Music Generator scales.
	[Serializable]
	public enum eScale { Major, NatMinor, mMelodicMinor, HarmonicMinor, HarmonicMajor }

	/// Style of dynamics: linear, Random. The way in which we choose which groups to play.
	[Serializable]
	public enum eDynamicStyle { Linear, Random }
}