using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ProcGenMusic
{
	//just a basic class for the ui slider to set values, update text, handle tooltip, etc:
	public class EffectsOption
	{
		private float mBaseValue = 0.0f;
		public Pair_String_Float mGeneratorValue;
		public Slider mSlider = null;
		public Text mOutput = null;
		public Button mResetButton = null;
		public delegate void UpdateAction(float value);
		public UpdateAction mUpdateAction;
		public EffectsOption(Pair_String_Float generatorValueIN, UpdateAction argUpdateAction, Tooltips tooltipsIN, Component cp, float baseIN)
		{
			mBaseValue = baseIN;
			mUpdateAction = argUpdateAction;
			mGeneratorValue = generatorValueIN;
			tooltipsIN.AddUIElement(ref mSlider, cp, mGeneratorValue.First);
			mResetButton = cp.GetComponentInChildren<Button>();
			mResetButton.onClick.AddListener(ResetSlider);
			Component[] components = cp.gameObject.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp2 in components)
			{
				if (cp2.name == generatorValueIN.First + "Output")
					mOutput = cp2.GetComponentInChildren<Text>();
			}
			Init();
		}

		public void Init()
		{
			mOutput.text = mGeneratorValue.Second.ToString();
			mSlider.value = mGeneratorValue.Second;
		}

		/// Sets the music generator value from the slider each tick.
		public void Update()
		{
			if (mGeneratorValue.Second != mSlider.value)
			{
				mGeneratorValue.Second = mSlider.value;
				mUpdateAction(mGeneratorValue.Second);
				mOutput.text = mSlider.value.ToString();
			}
		}

		/// Resets the value to the base value.
		public void ResetSlider()
		{
			mSlider.value = mBaseValue;
		}
	};

	/// UI Global effects pannel. Handles sliders, creation of tooltips, setting values, etc.
	public class GlobalEffectsPanel : HelperSingleton<GlobalEffectsPanel>
	{
		//reset values. We can't bypass the effect on the master group entirely 
		//via scripting (I don't think), so these are essentially minimizing the effect to nothing.
		static readonly private float mBaseCenterFrequency = 226.0f;
		static readonly private float mBaseOctaveRange = 3.78f;
		static readonly private float mBaseFrequencyGain = 1.63f;
		static readonly private float mBaseLowpassCutoffFreq = 22000.00f;
		static readonly private float mBaseLowpassResonance = 1.00f;
		static readonly private float mBaseHighpassCutoffFreq = 10.00f;
		static readonly private float mBaseHighpassResonance = 1.00f;
		static readonly private float mBaseEchoDelay = 13.0f;
		static readonly private float mBaseEchoDecay = .23f;
		static readonly private float mBaseEchoDry = 100.0f;
		static readonly private float mBaseEchoWet = 0.0f;
		static readonly private float mBaseNumEchoChannels = 0.00f;
		static readonly private float mBaseReverb = -10000.00f;
		static readonly private float mBaseRoomSize = -10000.00f;
		static readonly private float mBaseReverbDecay = 0.1f;

		private MusicGenerator mMusicGenerator = null;
		private Animator mAnimator = null;

		public EffectsOption mDistortion = null;
		public EffectsOption mCenterFrequency = null;
		public EffectsOption mOctaveRange = null;
		public EffectsOption mFrequencyGain = null;
		public EffectsOption mLowpassCutoffFreq = null;
		public EffectsOption mLowpassResonance = null;
		public EffectsOption mHighpassCutoffFreq = null;
		public EffectsOption mHighpassResonance = null;
		public EffectsOption mEchoDelay = null;
		public EffectsOption mEchoDecay = null;
		public EffectsOption mEchoDry = null;
		public EffectsOption mEchoWet = null;
		public EffectsOption mNumEchoChannels = null;
		public EffectsOption mReverb = null;
		public EffectsOption mRoomSize = null;
		public EffectsOption mReverbDecay = null;
		public List<EffectsOption> mOptions = new List<EffectsOption>();

		public void Init(MusicGenerator managerIN)
		{
			mMusicGenerator = managerIN;
			Tooltips tooltips = UIManager.Instance.mTooltips;
			mAnimator = GetComponentInParent<Animator>();
			mOptions.Clear();
			/// we create an EffectsOption for each slider, which will set its base value, tooltip, etc.
			Component[] components = this.GetComponentsInChildren(typeof(Transform), true);
			foreach (Component cp in components)
			{
				if (cp.name == "MasterDistortion")
					mOptions.Add(mDistortion = new EffectsOption(mMusicGenerator.mGeneratorData.mDistortion, (x) => mMusicGenerator.mGeneratorData.mDistortion.Second = x, tooltips, cp, 0.0f));
				if (cp.name == "MasterCenterFrequency")
					mOptions.Add(mCenterFrequency = new EffectsOption(mMusicGenerator.mGeneratorData.mCenterFreq, (x) => mMusicGenerator.mGeneratorData.mCenterFreq.Second = x, tooltips, cp, mBaseCenterFrequency));
				if (cp.name == "MasterOctaveRange")
					mOptions.Add(mOctaveRange = new EffectsOption(mMusicGenerator.mGeneratorData.mOctaveRange, (x) => mMusicGenerator.mGeneratorData.mOctaveRange.Second = x, tooltips, cp, mBaseOctaveRange));
				if (cp.name == "MasterFrequencyGain")
					mOptions.Add(mFrequencyGain = new EffectsOption(mMusicGenerator.mGeneratorData.mFreqGain, (x) => mMusicGenerator.mGeneratorData.mFreqGain.Second = x, tooltips, cp, mBaseFrequencyGain));
				if (cp.name == "MasterLowpassCutoffFreq")
					mOptions.Add(mLowpassCutoffFreq = new EffectsOption(mMusicGenerator.mGeneratorData.mLowpassCutoffFreq, (x) => mMusicGenerator.mGeneratorData.mLowpassCutoffFreq.Second = x, tooltips, cp, mBaseLowpassCutoffFreq));
				if (cp.name == "MasterLowpassResonance")
					mOptions.Add(mLowpassResonance = new EffectsOption(mMusicGenerator.mGeneratorData.mLowpassResonance, (x) => mMusicGenerator.mGeneratorData.mLowpassResonance.Second = x, tooltips, cp, mBaseLowpassResonance));
				if (cp.name == "MasterHighpassCutoffFreq")
					mOptions.Add(mHighpassCutoffFreq = new EffectsOption(mMusicGenerator.mGeneratorData.mHighpassCutoffFreq, (x) => mMusicGenerator.mGeneratorData.mHighpassCutoffFreq.Second = x, tooltips, cp, mBaseHighpassCutoffFreq));
				if (cp.name == "MasterHighpassResonance")
					mOptions.Add(mHighpassResonance = new EffectsOption(mMusicGenerator.mGeneratorData.mHighpassResonance, (x) => mMusicGenerator.mGeneratorData.mHighpassResonance.Second = x, tooltips, cp, mBaseHighpassResonance));
				if (cp.name == "MasterEchoDelay")
					mOptions.Add(mEchoDelay = new EffectsOption(mMusicGenerator.mGeneratorData.mEchoDelay, (x) => mMusicGenerator.mGeneratorData.mEchoDelay.Second = x, tooltips, cp, mBaseEchoDelay));
				if (cp.name == "MasterEchoDecay")
					mOptions.Add(mEchoDecay = new EffectsOption(mMusicGenerator.mGeneratorData.mEchoDecay, (x) => mMusicGenerator.mGeneratorData.mEchoDecay.Second = x, tooltips, cp, mBaseEchoDecay));
				if (cp.name == "MasterEchoDry")
					mOptions.Add(mEchoDry = new EffectsOption(mMusicGenerator.mGeneratorData.mEchoDry, (x) => mMusicGenerator.mGeneratorData.mEchoDry.Second = x, tooltips, cp, mBaseEchoDry));
				if (cp.name == "MasterEchoWet")
					mOptions.Add(mEchoWet = new EffectsOption(mMusicGenerator.mGeneratorData.mEchoWet, (x) => mMusicGenerator.mGeneratorData.mEchoWet.Second = x, tooltips, cp, mBaseEchoWet));
				if (cp.name == "MasterNumEchoChannels")
					mOptions.Add(mNumEchoChannels = new EffectsOption(mMusicGenerator.mGeneratorData.mNumEchoChannels, (x) => mMusicGenerator.mGeneratorData.mNumEchoChannels.Second = x, tooltips, cp, mBaseNumEchoChannels));
				if (cp.name == "MasterReverb")
					mOptions.Add(mReverb = new EffectsOption(mMusicGenerator.mGeneratorData.mReverb, (x) => mMusicGenerator.mGeneratorData.mReverb.Second = x, tooltips, cp, mBaseReverb));
				if (cp.name == "MasterRoomSize")
					mOptions.Add(mRoomSize = new EffectsOption(mMusicGenerator.mGeneratorData.mRoomSize, (x) => mMusicGenerator.mGeneratorData.mRoomSize.Second = x, tooltips, cp, mBaseRoomSize));
				if (cp.name == "MasterReverbDecay")
					mOptions.Add(mReverbDecay = new EffectsOption(mMusicGenerator.mGeneratorData.mReverbDecay, (x) => mMusicGenerator.mGeneratorData.mReverbDecay.Second = x, tooltips, cp, mBaseReverbDecay));
			}
			GetComponentInParent<CanvasGroup>().interactable = false;
			GetComponentInParent<CanvasGroup>().blocksRaycasts = false;
		}

		public void ForceSliderUpdate()
		{
			for (int i = 0; i < mOptions.Count; i++)
				mOptions[i].mSlider.value = mOptions[i].mGeneratorValue.Second;
		}

		void Update()
		{
			/// updates all our effects from the sliders:
			for (int i = 0; i < mOptions.Count; i++)
				mOptions[i].Update();
		}

		/// Toggles the effects panel animation:
		public void GlobalEffectsPanelToggle()
		{
			if (mAnimator.GetInteger("mState") == 0)
			{
				mAnimator.SetInteger("mState", 3);
				GetComponentInParent<CanvasGroup>().interactable = true;
				GetComponentInParent<CanvasGroup>().blocksRaycasts = true;
			}
			else
			{
				mAnimator.SetInteger("mState", 0);
				GetComponentInParent<CanvasGroup>().interactable = false;
				GetComponentInParent<CanvasGroup>().blocksRaycasts = false;
			}
		}
	}
}