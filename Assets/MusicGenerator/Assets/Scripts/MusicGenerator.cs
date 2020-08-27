using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ProcGenMusic
{
	/// <summary>
	/// Music Generator:
	/// See the included setup documentation file.
	/// Handles the state logic and other top level functions for the entire player. Loading assets, etc.
	/// </summary>
	/// <typeparam name="MusicGenerator"></typeparam>
	public class MusicGenerator : HelperSingleton<MusicGenerator>
	{
		///<summary> Generator Version</summary>
		public const float Version = 1.2f;

		[SerializeField]
		private Camera mMainCamera;

		///<summary> which platform we're on, this is detected in Awake().</summary>
		public string mPlatform { get; private set; }

		///<summary> Music Generator state</summary>
		public eGeneratorState mState { get; private set; }

		///<summary> Our generator data</summary>
		public MusicGeneratorData mGeneratorData = null;

		///<summary> whether we're fading in or out</summary>
		public eVolumeState mVolumeState { get; private set; }

		///<summary> Just stored strings for the mixer's volume.</summary>
		public static readonly string[] mVolNames = new string[] { "Volume0", "Volume1", "Volume2", "Volume3", "Volume4", "Volume5", "Volume6", "Volume7", "Volume8", "Volume9" };

		///<summary> ref to our instrument set</summary>
		public InstrumentSet mInstrumentSet = null;

		///<summary> Our repeating measure logic</summary>
		[SerializeField]
		private Measure mRepeatingMeasure = null;

		///<summary> Our regular measure logic</summary>
		[SerializeField]
		private Measure mRegularMeasure = null;

		///<summary> Sets the instrument set;</summary>
		public void SetInstrumentSet(InstrumentSet setIN) { if (setIN != null)mInstrumentSet = setIN; mInstrumentSet.Init(); }

		///<summary> setter for fade rate. This must be positive value.</summary>
		public void SetVolFadeRate(float value) { mGeneratorData.mVolFadeRate = Math.Abs(value); }

		///<summary> based on decibels. Needs to match vol slider on ui (if using ui). Edit at your own risk :)</summary>
		public const float mMaxVolume = 15;

		///<summary> based on decibels. Needs to match vol slider</summary> on ui (if using ui). Edit at your own risk :)
		public const float mMinVolume = -100.0f;

		///<summary> max number of notes per instrument</summary>
		public const int mMaxInstrumentNotes = 36;

		///<summary> number of notes in an octave</summary>
		public const int mOctave = 12;

		///<summary> number of audio sources we'll start with.</summary>
		public const float mNumStartingAudioSources = 10;

		///<summary> default config loaded on start.</summary>
		[SerializeField]
		public string mDefaultConfig = "AAADefault";

		///<summary> max number of steps per chord progression. Currently only support 4</summary>
		public const int mMaxFullstepsTaken = 4;

		///<summary> is frankly how many will fit at the moment...Only made 10 mixer groups. In theory there's no hard limit if you want to add mixer groups and expose their variables.</summary>
		static readonly public int mMaxInstruments = 10;

		///<summary> our currently played chord progression</summary>
		private int[] mChordProgression;
		///<summary> our currently played chord progression</summary>
		public int[] ChordProgression
		{
			get { return mChordProgression; }
			set
			{
				if (value.Length == 4)
				{
					mChordProgression = value;
				}
			}
		}

		///<summary> whether we'll change key next measure</summary>
		private bool mKeyChangeNextMeasure = false;

		/// whether this group is currently playing</summary>
		public ListArrayBool mGroupIsPlaying = new ListArrayBool(new bool[] { true, false, false, false });

		///<summary> chord progression logic</summary>
		public ChordProgressions mChordProgressions { get; private set; }

		///<summary> list of audio sources :P</summary>
		private List<AudioSource> mAudioSources = new List<AudioSource>();

		///<summary> our audio mixer</summary>
		public AudioMixer mMixer { get; private set; }

		///<summary> Reference to MusicFileConfig</summary>
		public MusicFileConfig mMusicFileConfig { get; private set; }

		///<summary> loaded instrument paths for the current configuration</summary>
		private List<string> mLoadedInstrumentNames = new List<string>();
		///<summary> loaded instrument paths for the current configuration</summary>
		public List<string> LoadedInstrumentNames { get { return mLoadedInstrumentNames; } private set { } }

		///<summary> Set in the editor. Possible instrument paths</summary>
		[SerializeField]
		private List<string> mBaseInstrumentPaths = new List<string>();
		///<summary> Set in the editor. Possible instrument paths</summary>
		public List<string> BaseInstrumentPaths { get { return mBaseInstrumentPaths; } }

		///<summary> multidimensional list of clips. top index is instrument</summary>
		private List<List<List<AudioClip>>> mAllClips = new List<List<List<AudioClip>>>();
		///<summary> multidimensional list of clips. top index is instrument</summary>
		public List<List<List<AudioClip>>> AllClips { get { return mAllClips; } }

		///<summary> keeps the asset bundles from being unloaded which causes an FMOD error if you try to edit the
		/// audio mixer in the editor while it's playing. Leave false unless you're using the audio mixer live in unity'd editor.</summary>
		[SerializeField]
		private bool mEnableLiveMixerEditing = false;

		///<summary> Whether we'll use async loading. Cannot set while initializing already.</summary>
		[SerializeField]
		private bool mUseAsyncLoading = false;
		///<summary> Whether we'll use async loading. Cannot set while initializing already.</summary>
		public bool UseAsyncLoading
		{
			get { return mUseAsyncLoading; }
			set { mUseAsyncLoading = mState != eGeneratorState.initializing ? value : mUseAsyncLoading; }
		}

		////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////
		/// public functions:
		////////////////////////////////////////////////////////////

		/// <summary>
		/// Loads a new configuration (song) to play.
		/// </summary>
		/// /// <param name="configIN"></param>
		/// <param name="continueState"></param>
		public void LoadConfiguration(string configIN, eGeneratorState continueState = eGeneratorState.ready)
		{
			if (mState == eGeneratorState.initializing)
			{
				return;
			}

			if (mUseAsyncLoading)
			{
				StartCoroutine(AsyncLoadConfiguration(configIN, continueState));
			}
			else
			{
				NonAsyncLoadConfiguration(configIN, continueState);
			}
		}

		/// <summary>
		/// Fades out the music before async loading a new configuration and fading back in.
		/// </summary>
		/// <param name="configIN"></param>
		/// <returns></returns>
		public IEnumerator FadeLoadConfiguration(string configIN)
		{
			mGeneratorData.mStateTimer = 0.0f;
			VolumeFadeOut();
			float maxWaitTime = 10.0f;
			yield return new WaitUntil(() => mVolumeState == eVolumeState.fadedOutIdle || mGeneratorData.mStateTimer > maxWaitTime);

			Stop();
			mVolumeState = eVolumeState.idle;

			Debug.Log("Fade load configuration finished");
			yield return StartCoroutine(mMusicFileConfig.AsyncLoadConfig(configIN, eGeneratorState.playing));
		}

		/// <summary>
		/// returns mInstrumentSet.instruments
		/// </summary>
		/// <returns></returns>
		public List<Instrument> GetInstruments()
		{
			return mInstrumentSet.mInstruments;
		}

		/// <summary>
		/// pauses the main music generator:
		/// </summary>
		public void Pause()
		{
			PauseGenerator.Invoke();
			if (mState != eGeneratorState.initializing)
			{
				SetState((mState < eGeneratorState.editing) ? eGeneratorState.paused : eGeneratorState.editorPaused);
			}
		}

		/// <summary>
		/// plays the main music generator:
		/// </summary>
		public void Play()
		{
			PlayGenerator.Invoke();

			if (mState != eGeneratorState.initializing)
			{
				SetState((mState < eGeneratorState.editing) ? eGeneratorState.playing : eGeneratorState.editorPlaying);
			}
		}

		/// <summary>
		/// stops the main music generator:
		/// </summary>
		public void Stop()
		{
			StopGenerator.Invoke();
			if (mState != eGeneratorState.initializing)
			{
				SetState((mState < eGeneratorState.editing) ? eGeneratorState.stopped : eGeneratorState.editorStopped);
			}
		}

		/// <summary>
		/// Set the music generator state:
		/// </summary>
		/// <param name="stateIN"></param>
		public void SetState(eGeneratorState stateIN)
		{
			mGeneratorData.mStateTimer = 0.0f;
			mState = stateIN;

			StateSet.Invoke(mState);

			switch (mState)
			{
				case eGeneratorState.stopped:
				case eGeneratorState.editorStopped:
					{
						ResetPlayer();
						break;
					}
				case eGeneratorState.initializing:
					break;
				case eGeneratorState.ready:
					Ready.Invoke();
					ChordProgression = mChordProgressions.GenerateProgression(mGeneratorData.mMode, mGeneratorData.mScale, 0);
					break;
				case eGeneratorState.playing:
					ChordProgression = mChordProgressions.GenerateProgression(mGeneratorData.mMode, mGeneratorData.mScale, 0);
					break;
				case eGeneratorState.repeating:
				case eGeneratorState.paused:
				case eGeneratorState.editing:
				case eGeneratorState.editorPaused:
				case eGeneratorState.editorPlaying:
				default:
					break;
			}
		}

		/// <summary>
		/// fades volume out
		/// </summary>
		public void VolumeFadeOut()
		{
			mVolumeState = eVolumeState.fadingOut;
		}

		/// <summary>
		/// fades volume in
		/// </summary>
		public void VolumeFadeIn()
		{
			mVolumeState = eVolumeState.fadingIn;
		}

		/// <summary>
		/// Sets the audio source volume for mAudioSource[indexIN]. This is different from the instrument volume which controls the clips
		/// played by an instrument, in that it controls the attenuation of the audioSource itself. If everything
		/// feels too quiet, this may be a good place to increase volume.
		/// </summary>
		/// <param name="indexIN"></param>
		/// <param name="volumeIN"></param>
		/// <param name="set"></param>
		public void SetAudioSourceVolume(int indexIN, float volumeIN, InstrumentSet set = null)
		{
			if (set == null)
			{
				set = mInstrumentSet;
			}
			mMixer.SetFloat("Volume" + indexIN.ToString(), volumeIN);
			set.mInstruments[indexIN].mData.AudioSourceVolume = volumeIN;
		}

		/// <summary>
		/// plays an audio clip:
		/// Look for an available clip that's not playing anything, creates a new one if necessary
		/// resets its properties  (volume, pan, etc) to match our new clip.
		/// </summary>
		/// <param name="set"></param>
		/// <param name="clipIndexX"></param>
		/// <param name="clipIndexY"></param>
		/// <param name="volumeIN"></param>
		/// <param name="instrumentIndex"></param>
		public void PlayAudioClip(InstrumentSet set, int clipIndexX, int clipIndexY, float volumeIN, int instrumentIndex)
		{
			NotePlayedArg.set = set;
			NotePlayedArg.clipIndexX = clipIndexX;
			NotePlayedArg.clipIndexY = clipIndexY;
			NotePlayedArg.volume = volumeIN;
			NotePlayedArg.instrumentIndex = instrumentIndex;

			// Override for external use and return true for MIDI, etc to surpress playing the note here
			if (OnNotePlayed(NotePlayedArg) == false)
			{
				return;
			}

			int instrumentSubIndex = UnityEngine.Random.Range(0, AllClips[clipIndexX].Count);
			AudioClip clip = AllClips[clipIndexX][instrumentSubIndex][clipIndexY];

			bool foundAvailable = false;
			for (int i = 0; i < mAudioSources.Count; i++)
			{
				if (mAudioSources[i].isPlaying == false)
				{
					mAudioSources[i].panStereo = set.mInstruments[instrumentIndex].mData.mStereoPan;
					mAudioSources[i].loop = false;
					mAudioSources[i].outputAudioMixerGroup = mMixer.FindMatchingGroups(instrumentIndex.ToString())[0];
					mAudioSources[i].volume = volumeIN;
					mAudioSources[i].clip = clip;
					mAudioSources[i].Play();
					foundAvailable = true;
					return;
				}
			}

			if (foundAvailable == false) //make a new audio souce.
			{
				mAudioSources.Add(mMainCamera.gameObject.AddComponent<AudioSource>());
				AudioSource audioSource = mAudioSources[mAudioSources.Count - 1];
				audioSource.panStereo = set.mInstruments[instrumentIndex].mData.mStereoPan;
				audioSource.outputAudioMixerGroup = mMixer.FindMatchingGroups(instrumentIndex.ToString())[0];
				audioSource.volume = volumeIN;
				audioSource.loop = false;
				audioSource.clip = clip;
				//audioSource.spatialBlend = 0;
				audioSource.Play();
			}
		}

		/// <summary>
		/// resets all player settings.
		/// reset player is called on things like loading new configurations, loading new levels, etc.
		/// sets all timing values and other settings back to the start 
		/// </summary>
		public void ResetPlayer()
		{
			if (mState <= eGeneratorState.initializing || mChordProgressions == null)
			{
				return;
			}

			mInstrumentSet.Reset();
			mRegularMeasure.ResetMeasure(mInstrumentSet, null, true);
			mRepeatingMeasure.ResetMeasure(mInstrumentSet, null, true);
			for (int i = 0; i < 4; i++)
			{
				mGroupIsPlaying[i] = (i == 0) ? true : false;
			}

			if (mState < eGeneratorState.editing)
			{
				ChordProgression = mChordProgressions.GenerateProgression(mGeneratorData.mMode, mGeneratorData.mScale, mGeneratorData.mKeySteps);
			}

			NormalMeasureExited.Invoke();
			PlayerReset.Invoke();
			mInstrumentSet.ResetRepeatCount();
			mInstrumentSet.ResetProgressionSteps();
		}

		/// <summary>
		/// Adds an instrument to our list. sets its instrument index
		/// </summary>
		/// <param name="setIN"></param>
		public void AddInstrument(InstrumentSet setIN)
		{
			setIN.mInstruments.Add(new Instrument());
			setIN.mInstruments[setIN.mInstruments.Count - 1].Init(setIN.mInstruments.Count - 1);
		}

		/// <summary>
		/// Deletes all instruments:
		/// </summary>
		/// <param name="setIN"></param>
		public void ClearInstruments(InstrumentSet setIN)
		{
			InstrumentsCleared.Invoke();
			if (setIN.mInstruments.Count == 0)
			{
				return;
			}

			for (int i = (int)setIN.mInstruments.Count - 1; i >= 0; i--)
			{
				RemoveInstrument((int)setIN.mInstruments[i].InstrumentIndex, setIN);
			}
			setIN.mInstruments.Clear();
		}

		/// <summary>
		/// Removes the instrument from our list. Fixes instrument indices.
		/// </summary>
		/// <param name="indexIN"></param>
		/// <param name="set"></param>
		public void RemoveInstrument(int indexIN, InstrumentSet set)
		{
			int typeIndex = (int)set.mInstruments[indexIN].InstrumentTypeIndex;
			set.mInstruments[indexIN] = null;
			set.mInstruments.RemoveAt(indexIN);

			for (int i = 0; i < set.mInstruments.Count; i++)
			{
				set.mInstruments[(int)i].InstrumentIndex = i;
			}

			RemoveBaseClip(typeIndex, set);
		}

		/// <summary>
		/// Removes a base clip if there are no instruments using it.
		/// </summary>
		/// <param name="typeIndex"></param>
		/// <param name="set"></param>
		public void RemoveBaseClip(int typeIndex, InstrumentSet set)
		{
			if (typeIndex < 0)
			{
				return;
			}

			bool isUsed = false;

			for (int i = 0; i < set.mInstruments.Count; i++)
			{
				if (set.mInstruments[i].InstrumentTypeIndex == typeIndex)
				{
					isUsed = true;
				}
			}

			if (!isUsed && AllClips.Count > typeIndex && mLoadedInstrumentNames.Count > typeIndex)
			{
				AllClips.RemoveAt(typeIndex);
				mLoadedInstrumentNames.RemoveAt(typeIndex);
			}
			CleanUpInstrumentTypeIndices(set);
		}

		/// <summary>
		/// Re-fixes the instrument types if something's been removed. There's probably a better way to do this.
		/// </summary>
		/// <param name="set"></param>
		public void CleanUpInstrumentTypeIndices(InstrumentSet set)
		{
			for (int i = 0; i < set.mInstruments.Count; i++)
			{
				set.mInstruments[i].InstrumentTypeIndex = LoadedInstrumentNames.IndexOf(set.mInstruments[i].mData.InstrumentType);
			}
		}

		/// <summary>
		/// Sets the volume.
		/// </summary>
		/// <param name="volIN"></param>
		public void SetVolume(float volIN)
		{
			if (mVolumeState == eVolumeState.idle)
			{
				mGeneratorData.mMasterVolume = volIN;
				UpdateEffectValue(MusicGeneratorData.mMasterVolName, mGeneratorData.mMasterVolume);
			}
		}

		/// Sets a global effect on the mixer. Can also edit manually on the master channel of the 'GeneratorMixer' in your scene.
		/// PLEASE USE WITH CAUTION!!! :) There's no idiot proofing on these values' effect on your speakers, game, pet's ears, your ears, etc. You could wake an Old One for all I know :P
		/// You can use the executable UI included with the asset to reset any of these to their defaults (click the reset button next to the slider)
		/// the mixer's current effects names and unity's min/max ranges :
		/// See: The Pair_String_Float variables above for settings these:
		/// "MasterDistortion"  0 : 1
		/// "MasterCenterFrequency" 20 : 22000
		/// "MasterOctaveRange" .2 : 5
		/// "MasterRoomSize" -10000 : 0
		///	"MasterReverbDecay" .1 : 5
		///	"MasterFrequencyGain" .05 : 3
		/// "MasterLowpassCutoffFreq" 10 : 22000
		/// "MasterLowpassResonance" 1 : 10
		/// "MasterHighpassCutoffFreq" 10 : 22000
		/// "MasterHighpassResonance" 1 : 10
		/// "MasterEchoDelay" 1 : 5000
		/// "MasterEchoDecay" .01 : 1
		/// "MasterEchoDry" 0 : 100
		/// "MasterEchoWet" 0 : 100
		/// "MasterNumEchoChannels" 0 : 16
		/// "MasterReverb" -10000 : 2000
		/// <summary>
		/// Sets a global effect on the mixer.
		/// </summary>
		/// <param name="valueIN"></param>
		public void SetGlobalEffect(Pair_String_Float valueIN)
		{
			mMixer.SetFloat(valueIN.First, valueIN.Second);
		}

		/// <summary>
		/// Returns the shortest rhythm timestep
		/// </summary>
		/// <returns></returns>
		public eTimestep GetShortestRhythmTimestep()
		{
			List<Instrument> instruments = mInstrumentSet.mInstruments;
			eTimestep shortestTime = eTimestep.whole;
			for (int i = 0; i < instruments.Count; i++)
			{
				if (instruments[i].mData.mSuccessionType != eSuccessionType.lead)
				{
					shortestTime = instruments[i].mData.mTimeStep < shortestTime ? instruments[i].mData.mTimeStep : shortestTime;
				}
			}
			return shortestTime;
		}

		////////////////////////////////////////////////////////////
		/// private utility functions:  Edit at your own risk :)
		////////////////////////////////////////////////////////////
		public override void Awake()
		{
			base.Awake();
			mMainCamera = Camera.main;

			SceneManager.sceneLoaded += OnSceneLoaded;

			GetPlatform();

			ChordProgression = new int[4] { 1, 4, 4, 5 }; //< sorry for magic numbers. This is a fairly basic chord progression
			mChordProgressions = new ChordProgressions();

			DontDestroyOnLoad(this.gameObject);

			mMusicFileConfig = gameObject.AddComponent<MusicFileConfig>();
			mGeneratorData = new MusicGeneratorData();

			//initialization of privately set values:
			mState = eGeneratorState.loading;
			mVolumeState = eVolumeState.idle;

			if (mInstrumentSet == null)
				SetInstrumentSet(new InstrumentSet());
			if (mRegularMeasure == null)
				mRegularMeasure = new RegularMeasure();
			if (mRepeatingMeasure == null)
				mRepeatingMeasure = new RepeatMeasure();

			SetInstrumentSet(mInstrumentSet);

			///load our mixer:
			string mixerPath = Application.streamingAssetsPath + "/MusicGenerator" + mPlatform + "/generatormixer";
			AssetBundle loadedAssetBundle = null;
			loadedAssetBundle = AssetBundle.LoadFromFile(mixerPath);

			if (loadedAssetBundle != null)
			{
				LoadAudioMixerFromBundle(loadedAssetBundle);
				return;
			}
		}

		/// <summary>
		/// Sets our platform string
		/// </summary>
		private void GetPlatform()
		{
			mPlatform = "/Windows";
			if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
				mPlatform = "/Linux";
			else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
				mPlatform = "/Mac";
			else if (Application.platform == RuntimePlatform.Android)
				mPlatform = "/Android";
			else if (Application.platform == RuntimePlatform.IPhonePlayer)
				mPlatform = "/IOS";
		}

		/// <summary>
		/// Loads the audio mixer from an asset bundle
		/// </summary>
		/// <param name="loadedAssetBundle"></param>
		private void LoadAudioMixerFromBundle(AssetBundle loadedAssetBundle)
		{
			mMixer = loadedAssetBundle.LoadAsset<AudioMixer>("GeneratorMixer");
			if (mMixer == null)
			{
				throw new System.ArgumentNullException("GeneratorMixer base file was not sucessfully loaded");
			}

			loadedAssetBundle.Unload(false);
		}

		///Loads an instrument set configuration:
		public void NonAsyncLoadConfiguration(string configIN, eGeneratorState continueState = eGeneratorState.ready)
		{
			if (mState > eGeneratorState.initializing && mChordProgressions != null)
			{
				Stop();
			}

			ResetPlayer();
			if (mMusicFileConfig != null)
			{
#if !UNITY_EDITOR && UNITY_ANDROID
				StartCoroutine(mMusicFileConfig.LoadConfig(configIN, continueState));
#else
				mMusicFileConfig.LoadConfig(configIN, continueState);
#endif //!UNITY_EDITOR && UNITY_ANDROID
			}
			else
			{
				throw new ArgumentNullException("configuration class doesn't exist or was not loaded yes");
			}
		}

		/// <summary>
		/// Async Loads an instrument set configuration:
		/// </summary>
		/// <param name="configIN"></param>
		/// <param name="continueState"></param>
		/// <returns></returns>
		public IEnumerator AsyncLoadConfiguration(string configIN, eGeneratorState continueState = eGeneratorState.ready)
		{
			ResetPlayer();
			SetState(eGeneratorState.initializing);
			yield return StartCoroutine(mMusicFileConfig.AsyncLoadConfig(configIN, continueState));
		}

		/// <summary>
		/// On scene load
		/// </summary>
		/// <param name="scene"></param>
		/// <param name="mode"></param>
		void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			mAudioSources.Clear();

			// These are generated as needed, but we'll certainly need some on start
			// this just keeps it from trying to add a dozen audio sources after things have already started playing.
			for (int i = 0; i < mNumStartingAudioSources; i++)
			{
				mAudioSources.Add(mMainCamera.gameObject.AddComponent<AudioSource>());
			}

			ResetPlayer();
		}

#if !UNITY_EDITOR && UNITY_ANDROID	

		/// <summary>
		/// Loads the base clips for android
		/// </summary>
		/// <param name="instrumentType"></param>
		/// <returns></returns>
		public int LoadBaseClips(string instrumentType)
		{
			instrumentType = instrumentType.ToLower();

			if (mLoadedInstrumentNames.Contains(instrumentType))
			{
				return mLoadedInstrumentNames.IndexOf(instrumentType);
			}

			mLoadedInstrumentNames.Add(instrumentType);

			InstrumentPrefabList subList = LoadInstrumentPrefabList(instrumentType + "_1");

			// Here we test for sub folder for the instrument and load any additional variations:
			if (subList != null)
			{
				AllClips.Add(new List<List<AudioClip>>());
				LoadAudioSourcesFromPrefabList(subList);
				for (int i = 2; i < mMaxInstruments + 1; i++)
				{
					InstrumentPrefabList list = LoadInstrumentPrefabList(instrumentType + "_" + i.ToString());

					if (list != null)
					{
						LoadAudioSourcesFromPrefabList(list);
					}
					else return AllClips.Count - 1;
				}
				return AllClips.Count - 1;
			}
			else // otherwise load normal instrument without sub folders.
			{
				AllClips.Add(new List<List<AudioClip>>());
				InstrumentPrefabList list = LoadInstrumentPrefabList(instrumentType);
				if (list == null)
				{
					throw new ArgumentNullException("prefab list for " + instrumentType + " does not exist 2");
				}
				LoadAudioSourcesFromPrefabList(list);
				return AllClips.Count - 1;
			}
		}

		/// <summary>
		/// non-async. Loads our audio clip from its asset bundle.
		/// </summary>
		/// <param name="pathName"></param>
		/// <returns></returns>
		private InstrumentPrefabList LoadInstrumentPrefabList(string pathName)
		{
			string path = null;
			path = Path.Combine(Application.streamingAssetsPath + "/MusicGenerator" + mPlatform, pathName);
			var myLoadedAssetBundle = AssetBundle.LoadFromFile(path);

			if (myLoadedAssetBundle != null)
			{
				GameObject go = myLoadedAssetBundle.LoadAsset<GameObject>(pathName);
				if (mEnableLiveMixerEditing == false)
				{
					myLoadedAssetBundle.Unload(false);
				}
				return go.GetComponent<InstrumentPrefabList>();
			}
			else
			{
				UnityEngine.Debug.Log("asset bundle for " + pathName + " could not be loaded or does not exist.");
				return null;
			}
		}

		/// <summary>
		/// async Loads our audio clip from its asset bundle.
		/// this is intentially really slow, so as to interfere as little as possible with the framerate.
		/// </summary>
		/// <param name="pathName"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		private IEnumerator AsyncLoadInstrumentPrefabList(string pathName, System.Action<InstrumentPrefabList> callback)
		{
			string path = null;
			path = Path.Combine(Application.streamingAssetsPath + "/MusicGenerator" + mPlatform, pathName);

			AssetBundle myLoadedAssetBundle = null;

			var bundleRequest = AssetBundle.LoadFromFileAsync(path);
			yield return bundleRequest;

			myLoadedAssetBundle = bundleRequest.assetBundle;
			if (myLoadedAssetBundle != null)
			{
				var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>(pathName);
				yield return new WaitUntil(() => assetLoadRequest.isDone);
				GameObject go = assetLoadRequest.asset as GameObject;

				if (mEnableLiveMixerEditing == false)
				{
					myLoadedAssetBundle.Unload(false);
				}
				callback(go.GetComponent<InstrumentPrefabList>());
				yield return null;
			}
			else
			{
				// for android we don't want to throw an exception, as this may have been just an attmept to load a subpath
				yield return null;
			}
		}

		/// <summary>
		/// Asynchronously Loads the instrument clips from the asset bundles into our mAllClips array
		/// </summary>
		/// <param name="instrumentType"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public IEnumerator AsyncLoadBaseClips(string instrumentType, System.Action<int> callback)
		{
			instrumentType = instrumentType.ToLower();

			// just return the correct index if we've already loaded these.
			if (mLoadedInstrumentNames.Contains(instrumentType))
			{
				callback(mLoadedInstrumentNames.IndexOf(instrumentType));
				yield break;
			}

			mLoadedInstrumentNames.Add(instrumentType);

			InstrumentPrefabList subList = null;
			yield return StartCoroutine(AsyncLoadInstrumentPrefabList(instrumentType + "_1", (x) => { subList = x; }));

			// Here we test for sub folder for the instrument and load any additional variations:
			if (subList != null)
			{
				AllClips.Add(new List<List<AudioClip>>());
				LoadAudioSourcesFromPrefabList(subList);

				for (int i = 2; i < mMaxInstruments + 1; i++)
				{
					InstrumentPrefabList list = null;
					yield return StartCoroutine(AsyncLoadInstrumentPrefabList(instrumentType + "_" + i.ToString(), (x) => { list = x; }));

					if (list != null)
					{
						LoadAudioSourcesFromPrefabList(list);
					}
					else
					{
						callback(AllClips.Count - 1);
						yield break;
					}
				}
				callback(AllClips.Count - 1);
				yield break;
			}
			else //load normal instrument without sub folders.
			{
				AllClips.Add(new List<List<AudioClip>>());
				InstrumentPrefabList list = null;
				yield return StartCoroutine(AsyncLoadInstrumentPrefabList(instrumentType, (x) => { list = x; }));

				if (list == null)
				{
					throw new ArgumentNullException("file at " + instrumentType + " does not exist");
				}

				LoadAudioSourcesFromPrefabList(list);

				callback(AllClips.Count - 1);
				yield break;
			}
		}

#else
		/// non-async. Loads the instrument clips from file into our mAllClips array
		/// Use if you need / want to preload clips when loading other assets, instead of on the fly.
		public int LoadBaseClips(string instrumentType)
		{
			instrumentType = instrumentType.ToLower();
			if (mLoadedInstrumentNames.Contains(instrumentType))
			{
				return mLoadedInstrumentNames.IndexOf(instrumentType);
			}

			mLoadedInstrumentNames.Add(instrumentType);

			string path = Application.streamingAssetsPath + "/MusicGenerator" + mPlatform + "/" + instrumentType + "_1";

			// Here we test for sub folder for the instrument and load any additional variations:
			if (File.Exists(path))
			{
				AllClips.Add(new List<List<AudioClip>>());
				for (int i = 1; i < mMaxInstruments + 1; i++)
				{
					if (File.Exists(Application.streamingAssetsPath + "/MusicGenerator" + mPlatform + "/" + instrumentType + "_" + i.ToString()))
					{
						InstrumentPrefabList list = LoadInstrumentPrefabList(instrumentType + "_" + i.ToString());

						if (list == null)
						{
							throw new ArgumentNullException("prefab list for " + instrumentType + " does not exist");
						}

						LoadAudioSourcesFromPrefabList(list);
					}
					else break;
				}
				return AllClips.Count - 1;
			}
			else // otherwise load normal instrument without sub folders.
			{
				AllClips.Add(new List<List<AudioClip>>());
				InstrumentPrefabList list = LoadInstrumentPrefabList(instrumentType);
				if (list == null)
				{
					throw new ArgumentNullException("prefab list for " + instrumentType + " does not exist");
				}

				LoadAudioSourcesFromPrefabList(list);
				return AllClips.Count - 1;
			}
		}

		/// <summary>
		/// non-async. Loads our audio clip from its asset bundle.
		/// </summary>
		/// <param name="pathName"></param>
		/// <returns></returns>
		private InstrumentPrefabList LoadInstrumentPrefabList(string pathName)
		{
			string path = null;
			path = Path.Combine(Application.streamingAssetsPath + "/MusicGenerator" + mPlatform, pathName);
			var myLoadedAssetBundle = AssetBundle.LoadFromFile(path);

			if (myLoadedAssetBundle != null)
			{
				GameObject go = myLoadedAssetBundle.LoadAsset<GameObject>(pathName);
				if (mEnableLiveMixerEditing == false)
				{
					myLoadedAssetBundle.Unload(false);
				}
				return go.GetComponent<InstrumentPrefabList>();
			}
			else
			{
				UnityEngine.Debug.Log("asset bundle for " + pathName + " could not be loaded or does not exist.");
				return null;
			}
		}

		/// <summary>
		/// Asynchronously Loads the instrument clips from the asset bundles into our mAllClips array
		/// </summary>
		/// <param name="instrumentType"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		public IEnumerator AsyncLoadBaseClips(string instrumentType, System.Action<int> callback)
		{
			instrumentType = instrumentType.ToLower();

			// just return the correct index if we've already loaded these.
			if (mLoadedInstrumentNames.Contains(instrumentType))
			{
				callback(mLoadedInstrumentNames.IndexOf(instrumentType));
				yield break;
			}

			mLoadedInstrumentNames.Add(instrumentType);

			string path = null;
			bool pathExists = false;

			path = Application.streamingAssetsPath + "/MusicGenerator" + mPlatform + "/" + instrumentType + "_1";
			pathExists = File.Exists(path);

			// Check for instrument sub-types.
			if (pathExists)
			{
				AllClips.Add(new List<List<AudioClip>>());
				yield return null;
				for (int i = 1; i < mMaxInstruments + 1; i++)
				{
					string instrumentName = instrumentType + "_" + i.ToString();
					string subPath = Application.streamingAssetsPath + "/MusicGenerator" + mPlatform + "/" + instrumentName;

					if (File.Exists(subPath))
					{
						InstrumentPrefabList list = null;

						yield return StartCoroutine(AsyncLoadInstrumentPrefabList(subPath, instrumentName, ((x) => { list = x; })));
						LoadAudioSourcesFromPrefabList(list);
					}
					else break;
				}
				callback(AllClips.Count - 1);
				yield return null;
			}
			else //load normal instrument without sub folders.
			{
				AllClips.Add(new List<List<AudioClip>>());
				InstrumentPrefabList list = null;
				string instrumentPath = Application.streamingAssetsPath + "/MusicGenerator" + mPlatform + "/" + instrumentType;

				if (File.Exists(instrumentPath) == false)
				{
					throw new ArgumentNullException("file at " + path + " does not exist");
				}
				yield return StartCoroutine(AsyncLoadInstrumentPrefabList(instrumentPath, instrumentType, ((x) => { list = x; })));

				LoadAudioSourcesFromPrefabList(list);

				callback(AllClips.Count - 1);
				yield return null;
			}
		}

		/// <summary>
		/// async Loads our audio clip from its asset bundle.
		/// this is intentially really slow, so as to interfere as little as possible with the framerate.
		/// </summary>
		/// <param name="pathName"></param>
		/// <param name="callback"></param>
		/// <returns></returns>
		private IEnumerator AsyncLoadInstrumentPrefabList(string pathName, string objectName, System.Action<InstrumentPrefabList> callback)
		{
			AssetBundle myLoadedAssetBundle = null;

			var bundleRequest = AssetBundle.LoadFromFileAsync(pathName);
			yield return bundleRequest;

			myLoadedAssetBundle = bundleRequest.assetBundle;
			if (myLoadedAssetBundle != null)
			{
				var assetLoadRequest = myLoadedAssetBundle.LoadAssetAsync<GameObject>(objectName);
				yield return new WaitUntil(() => assetLoadRequest.isDone);
				GameObject go = assetLoadRequest.asset as GameObject;

				if (mEnableLiveMixerEditing == false)
				{
					myLoadedAssetBundle.Unload(false);
				}
				callback(go.GetComponent<InstrumentPrefabList>());
				yield return null;
			}
			else
			{
				throw new ArgumentNullException("asset bundle for " + objectName + " could not be loaded or does not exist.");
			}
		}
#endif //!UNITY_EDITOR && UNITY_ANDROID

		/// <summary>
		/// Loads audioSource clips from the prefab list
		/// </summary>
		/// <param name="list"></param>
		private void LoadAudioSourcesFromPrefabList(InstrumentPrefabList list)
		{
			AllClips[AllClips.Count - 1].Add(new List<AudioClip>());
			int index = AllClips[AllClips.Count - 1].Count - 1;
			for (int x = 0; x < list.mAudioSources.Length; x++) //musician who named them started at 1, just haven't fixed. :P
			{
				if (list.mAudioSources[x] != null)
				{
					AllClips[AllClips.Count - 1][index].Add(list.mAudioSources[x].clip);
				}
			}
		}

		/// <summary>
		/// Loads the initial configuration.
		/// </summary>
		void Start()
		{
			Started.Invoke();

			if (!OnHasVisiblePlayer()) //without the UI, we load manually, as the UI panel is not going to do it, and we don't need ui initialized.
			{
				if (mUseAsyncLoading)
				{
					StartCoroutine(AsyncLoadConfiguration(mDefaultConfig));
				}
				else
				{
					LoadConfiguration(mDefaultConfig);
				}
			}
		}

		/// <summary>
		/// Generates new chord progression:
		/// </summary>
		private void GenNewChordProgression()
		{
			ChordProgression = mChordProgressions.GenerateProgression(mGeneratorData.mMode, mGeneratorData.mScale, mGeneratorData.mKeySteps);
			ProgressionGenerated.Invoke();
		}

		/// <summary>
		/// State update:
		/// </summary>
		/// <returns></returns>
		void Update() { UpdateState(Time.deltaTime); }
		private void UpdateState(float deltaT)
		{
			mGeneratorData.mStateTimer += deltaT;

			if (mState != eGeneratorState.initializing &&
				mState != eGeneratorState.editorInitializing &&
				mState != eGeneratorState.loading)
			{
				UpdateLiveSettings();
			}

			// just idiot-proofing. I don't want blame for anyone speakers :P
			// Feel free to adjust min/max values to your needs.
			mGeneratorData.mMasterVolume = mGeneratorData.mMasterVolume <= mMaxVolume ? mGeneratorData.mMasterVolume : mMaxVolume;
			mGeneratorData.mMasterVolume = mGeneratorData.mMasterVolume >= mMinVolume ? mGeneratorData.mMasterVolume : mMinVolume;

			switch (mState)
			{
				case eGeneratorState.ready:
					break;
				case eGeneratorState.playing:
					{
						mRegularMeasure.PlayMeasure(mInstrumentSet);
						break;
					}
				case eGeneratorState.repeating:
					{
						mRepeatingMeasure.PlayMeasure(mInstrumentSet);
						break;
					}
				case eGeneratorState.editorPlaying:
					{
						PlayMeasureEditorClip();
						break;
					}
					// nothing to do:
				case eGeneratorState.initializing:
				case eGeneratorState.paused:
				case eGeneratorState.stopped:
					// these are handled only by the ui measure editor
				case eGeneratorState.editing:
				case eGeneratorState.editorPaused:
				case eGeneratorState.editorStopped:
				default:
					break;
			}

			// handles the volume fade in / fade out:
			switch (mVolumeState)
			{
				case eVolumeState.fadingIn:
				case eVolumeState.fadingOut:
					FadeVolume(deltaT);
					break;
				case eVolumeState.fadedOutIdle:
				case eVolumeState.idle:
				default:
					break;
			}
			StateUpdated.Invoke(mState);
		}

		/// <summary>
		/// fades the volume:
		/// </summary>
		/// <param name="deltaT"></param>
		private void FadeVolume(float deltaT)
		{
			float currentVol;
			mMixer.GetFloat("MasterVol", out currentVol);

			switch (mVolumeState)
			{
				case eVolumeState.fadingIn:
					{
						if (currentVol <= mGeneratorData.mMasterVolume - (mGeneratorData.mVolFadeRate * deltaT))
						{
							currentVol += mGeneratorData.mVolFadeRate * deltaT;
						}
						else
						{
							currentVol = mGeneratorData.mMasterVolume;
							mVolumeState = eVolumeState.idle;
						}
						break;
					}
				case eVolumeState.fadingOut:
					{
						if (currentVol > mMinVolume + (mGeneratorData.mVolFadeRate * deltaT))
							currentVol -= mGeneratorData.mVolFadeRate * deltaT;
						else
						{
							currentVol = mMinVolume;
							mVolumeState = eVolumeState.fadedOutIdle;
						}
						break;
					}
				default:
					break;
			}

			mMixer.SetFloat("MasterVol", currentVol);

			VolumeFaded.Invoke(currentVol);
		}

		/// <summary>
		/// handles necessary updates when music is not stopped or paused.
		/// </summary>
		private void UpdateLiveSettings()
		{
			UpdateEffects();
		}

		/// <summary>
		/// Updates mixer effects.
		/// </summary>
		private void UpdateEffects()
		{
			if (mVolumeState == eVolumeState.idle)
			{
				SetVolume(mGeneratorData.mMasterVolume);
			}

			UpdateEffectValue(mGeneratorData.mDistortion.First, mGeneratorData.mDistortion.Second);
			UpdateEffectValue(mGeneratorData.mCenterFreq.First, mGeneratorData.mCenterFreq.Second);
			UpdateEffectValue(mGeneratorData.mOctaveRange.First, mGeneratorData.mOctaveRange.Second);
			UpdateEffectValue(mGeneratorData.mFreqGain.First, mGeneratorData.mFreqGain.Second);
			UpdateEffectValue(mGeneratorData.mLowpassCutoffFreq.First, mGeneratorData.mLowpassCutoffFreq.Second);
			UpdateEffectValue(mGeneratorData.mLowpassResonance.First, mGeneratorData.mLowpassResonance.Second);
			UpdateEffectValue(mGeneratorData.mHighpassCutoffFreq.First, mGeneratorData.mHighpassCutoffFreq.Second);
			UpdateEffectValue(mGeneratorData.mHighpassResonance.First, mGeneratorData.mHighpassResonance.Second);
			UpdateEffectValue(mGeneratorData.mEchoDelay.First, mGeneratorData.mEchoDelay.Second);
			UpdateEffectValue(mGeneratorData.mEchoDecay.First, mGeneratorData.mEchoDecay.Second);
			UpdateEffectValue(mGeneratorData.mEchoDry.First, mGeneratorData.mEchoDry.Second);
			UpdateEffectValue(mGeneratorData.mEchoWet.First, mGeneratorData.mEchoWet.Second);
			UpdateEffectValue(mGeneratorData.mNumEchoChannels.First, mGeneratorData.mNumEchoChannels.Second);
			UpdateEffectValue(mGeneratorData.mReverb.First, mGeneratorData.mReverb.Second);
			UpdateEffectValue(mGeneratorData.mRoomSize.First, mGeneratorData.mRoomSize.Second);
			UpdateEffectValue(mGeneratorData.mReverbDecay.First, mGeneratorData.mReverbDecay.Second);

			for (int i = 0; i < mInstrumentSet.mInstruments.Count; i++)
			{
				UpdateEffectValue(mVolNames[i], mInstrumentSet.mInstruments[i].mData.AudioSourceVolume);
			}
		}

		/// <summary>
		/// Updates our effects values
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		private void UpdateEffectValue(string first, float second)
		{
			float check;
			mMixer.GetFloat(first, out check);

			if (check != second)
			{
				mMixer.SetFloat(first, second);
			}
		}

		/// <summary>
		/// Checks for a keychange and starts setup if needed.
		/// </summary>
		public void CheckKeyChange()
		{
			if (mInstrumentSet.ProgressionStepsTaken == 0)
			{
				KeyChangeSetup();
			}
		}

		/// <summary>
		/// Generates a new chord progression
		/// </summary>
		public void GenerateNewProgression()
		{
			if (mInstrumentSet.ProgressionStepsTaken >= mMaxFullstepsTaken - 1)
			{
				SetKeyChange();

				if (UnityEngine.Random.Range(0, 100) < mGeneratorData.mProgressionChangeOdds || mKeyChangeNextMeasure)
				{
					ChordProgression = mChordProgressions.GenerateProgression(mGeneratorData.mMode, mGeneratorData.mScale, mGeneratorData.mKeySteps);
				}
			}
		}

		/// <summary>
		/// Sets theme / repeat variables.
		/// </summary>
		public void SetThemeRepeat()
		{
			if (mInstrumentSet.mRepeatCount >= mInstrumentSet.mData.RepeatMeasuresNum)
			{
				if (mGeneratorData.mThemeRepeatOptions > (int)eThemeRepeatOptions.eNone 
					&& mState == eGeneratorState.playing 
					&& mInstrumentSet.mRepeatCount >= mInstrumentSet.mData.RepeatMeasuresNum)
				{
					SetState(eGeneratorState.repeating);
				}

				// set our theme notes if we're going to use them.
				bool newInstrumentDetected = false;
				for (int i = 0; i < mInstrumentSet.mInstruments.Count; i++)
				{
					if (mInstrumentSet.mInstruments[i].mNeedsTheme)
					{
						newInstrumentDetected = true;
					}
				}
				if (UnityEngine.Random.Range(0, 100.0f) < mGeneratorData.mSetThemeOdds || newInstrumentDetected)
				{
					if (mInstrumentSet.mRepeatCount >= mInstrumentSet.mData.RepeatMeasuresNum)
					{
						for (int i = 0; i < mInstrumentSet.mInstruments.Count; i++)
						{
							mInstrumentSet.mInstruments[i].SetThemeNotes();
						}
					}
				}
			}
			NormalMeasureExited.Invoke();
		}

		/// <summary>
		/// sets up whether we'll change keys next measure: 
		/// </summary>
		private void KeyChangeSetup()
		{
			if (mKeyChangeNextMeasure)
			{
				mGeneratorData.mKey += mGeneratorData.mKeySteps;
				mGeneratorData.mKey = (eKey)MusicHelpers.SafeLoop((int)mGeneratorData.mKey, mOctave);

				mKeyChangeNextMeasure = false;
				KeyChanged.Invoke((int)mGeneratorData.mKey);
			}
		}

		/// <summary>
		/// changes the key for the current instrument set:
		/// alters the current chord progression to allow a smooth transition to 
		/// the new key
		/// </summary>
		private void SetKeyChange()
		{
			if (UnityEngine.Random.Range(0.0f, 100.0f) < mGeneratorData.mKeyChangeOdds)
			{
				mKeyChangeNextMeasure = true;
				mGeneratorData.mKeySteps = (UnityEngine.Random.Range(0, 100) < mGeneratorData.mKeyChangeAscendDescend) ? -Instrument.mScaleLength : Instrument.mScaleLength;
			}
			else
			{
				mGeneratorData.mKeySteps = 0;
			}
		}

		/// <summary>
		/// UI measure editor version only (for normal use, play clips through the singleClip state functions: )
		/// </summary>
		private void PlayMeasureEditorClip()
		{
			EditorClipPlayed.Invoke();
		}

		////////////////////////////////////////////////////////////
		////////////////////////////////////////////////////////////
		/// Events: Most of these are for the UI and can be safely ignored, unless you need to subscribe for your app uses.
		/// feel free to subscribe to them or call them, if your use of the generator requires knowing any of these events.
		////////////////////////////////////////////////////////////
		///<summary> On manager start event:</summary>
		public UnityEvent Started = new UnityEvent();

		///<summary> Music generator is fully initialized and ready</summary>
		public UnityEvent Ready = new UnityEvent();

		///<summary> Event for Generator state set:</summary>
		public class StateEvent : UnityEvent<eGeneratorState> { }
		public StateEvent StateSet = new StateEvent();

		///<summary> Event for state Update()</summary>
		public StateEvent StateUpdated = new StateEvent();

		public delegate bool HasVisiblePlayerEventHandler(object source, EventArgs args);
		///<summary> Event Handler for UI manager detection:
		/// This is a bit hacky. Please don't listen/return anything for this.
		/// It's used to detect UI states without being coupled to them.</summary>
		public event HasVisiblePlayerEventHandler HasVisiblePlayer;
		protected virtual bool OnHasVisiblePlayer()
		{
			if (HasVisiblePlayer != null)
				return HasVisiblePlayer(this, EventArgs.Empty);
			return false;
		}

		public delegate bool NotePlayedEventHandler(object source, NotePlayedArgs args);
		///<summary>Event handler for played notes. If using MIDI or some other player, override
		/// this and return false to surpress the playing of the music generator audio clip.</summary>
		public event NotePlayedEventHandler NotePlayed;
		public NotePlayedArgs NotePlayedArg = new NotePlayedArgs(null, 0, 0, 0, 0);
		protected virtual bool OnNotePlayed(NotePlayedArgs args)
		{
			if (NotePlayed != null)
				return NotePlayed(this, args);
			return true;
		}

		public class FloatEvent : UnityEvent<float> { };
		///<summary> Event Handler for fading volume</summary>
		public FloatEvent VolumeFaded = new FloatEvent();

		///<summary> Event Handler for Generates a chord progression:</summary>
		public UnityEvent ProgressionGenerated = new UnityEvent();

		///<summary> Event handler for clear instruments:</summary>
		public UnityEvent InstrumentsCleared = new UnityEvent();

		///<summary> Event Handler for exiting normal measure</summary>
		public UnityEvent NormalMeasureExited = new UnityEvent();

		///<summary> Event Handler for impending key change:</summary>
		public class IntEvent : UnityEvent<int> { }
		public IntEvent KeyChanged = new IntEvent();

		///<summary> Event for exiting the repeating measure.</summary>
		public StateEvent RepeatedMeasureExited = new StateEvent();

		///<summary> Set barline color event</summary>
		public class BarlineEvent : UnityEvent<int, bool> { }
		public BarlineEvent BarlineColorSet = new BarlineEvent();

		///<summary> Editor clip played event:</summary>
		public UnityEvent EditorClipPlayed = new UnityEvent();

		public delegate bool UIPlayerIsEditingEventHandler(object source, EventArgs args);
		///<summary>Event Handler for a the measure editor opening This is a bit hacky. Please don't listen/return anything here. It's used to detect UI states without being coupled to them.</summary>
		public event UIPlayerIsEditingEventHandler UIPlayerIsEditing;
		public bool OnUIPlayerIsEditing()
		{
			if (UIPlayerIsEditing != null)
				return UIPlayerIsEditing(this, EventArgs.Empty);

			return false;
		}

		///<summary> Events for repeating notes:
		public class RepeatNoteEvent : UnityEvent<RepeatNoteArgs> { }
		public RepeatNoteEvent RepeatNotePlayed = new RepeatNoteEvent();

		public class IntIntEvent : UnityEvent<int, int> { }
		///<summary> Event for staff player:</summary>
		public IntIntEvent UIStaffNotePlayed = new IntIntEvent();

		///<summary> Event for staff player strummed:</summary>
		public IntIntEvent UIStaffNoteStrummed = new IntIntEvent();

		public class ClipEvent : UnityEvent<ClipSave> { }
		///<summary> Event Single clip being loaded:</summary>
		public ClipEvent ClipLoaded = new ClipEvent();

		///<summary> Event for player being reset:</summary>
		public UnityEvent PlayerReset = new UnityEvent();

		///<summary> Event for play()</summary>
		public UnityEvent PlayGenerator = new UnityEvent();

		///<summary> Event for stop()</summary>
		public UnityEvent StopGenerator = new UnityEvent();

		///<summary> Event for pause()</summary>
		public UnityEvent PauseGenerator = new UnityEvent();

		/////////////////////////////////
		/// Save / Load /////////////////
		/////////////////////////////////

		/// <summary>
		/// Sets variables from save file.
		/// </summary>
		/// <param name="data"></param>
		public void LoadGeneratorSave(MusicGeneratorData data)
		{
			mGeneratorData = data;
			/// best to update our mixer now. it will get updated anyway, but the UI needs it updated sooner.
			SetGlobalEffect(mGeneratorData.mDistortion);
			SetGlobalEffect(mGeneratorData.mCenterFreq);
			SetGlobalEffect(mGeneratorData.mOctaveRange);
			SetGlobalEffect(mGeneratorData.mFreqGain);
			SetGlobalEffect(mGeneratorData.mLowpassCutoffFreq);
			SetGlobalEffect(mGeneratorData.mLowpassResonance);
			SetGlobalEffect(mGeneratorData.mHighpassCutoffFreq);
			SetGlobalEffect(mGeneratorData.mHighpassResonance);
			SetGlobalEffect(mGeneratorData.mEchoDelay);
			SetGlobalEffect(mGeneratorData.mEchoDecay);
			SetGlobalEffect(mGeneratorData.mEchoDry);
			SetGlobalEffect(mGeneratorData.mEchoWet);
			SetGlobalEffect(mGeneratorData.mNumEchoChannels);
			SetGlobalEffect(mGeneratorData.mReverb);
			SetGlobalEffect(mGeneratorData.mRoomSize);
			SetGlobalEffect(mGeneratorData.mReverbDecay);
		}
	}
}