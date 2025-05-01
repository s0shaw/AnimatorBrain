using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A component to simplify interactions with the Unity Animator component.
/// Provides methods for playing animations, cross-fading, handling layers,
/// and executing sequential animation combos with optional delays.
/// Requires an Animator component on the same GameObject.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorBrain : MonoBehaviour
{
    // Define a structure for combo steps for clarity
    private struct ComboStep
    {
        public string AnimName;
        public float DelayAfter; // Additional delay AFTER this animation finishes

        public ComboStep(string name, float delay)
        {
            AnimName = name;
            DelayAfter = Mathf.Max(0f, delay); // Ensure delay is not negative
        }
    }
    private Animator animator;
    // Queue now holds ComboStep structures
    private Queue<ComboStep> animationQueue = new Queue<ComboStep>();
    // List also holds ComboStep structures for looping
    private List<ComboStep> currentComboSequence = new List<ComboStep>();
    private bool isPlayingCombo = false;
    private bool loopCombo = false;
    private Coroutine comboCoroutine = null;

    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("AnimatorBrain: Animator component not found on this GameObject!", this);
        }
    }

    /// <summary>
    /// Plays the specified animation state immediately (no transition).
    /// Looping should be controlled within the Animator Controller's state settings.
    /// Can optionally set a 'Loop' bool parameter if it exists in the controller.
    /// </summary>
    /// <param name="animationName">The name of the animation state to play.</param>
    /// <param name="setLoopParameter">Whether to set the 'Loop' bool parameter in the Animator.</param>
    /// <param name="loopValue">The value to set the 'Loop' parameter to if setLoopParameter is true.</param>
    public void PlayAnimation(string animationName, bool setLoopParameter = false, bool loopValue = false)
    {
        if (!EnsureAnimator()) return;
        StopCombo(); // Stop any ongoing combo when a new animation is played directly
        animator.Play(animationName, 0); // Typically use base layer (0)
        if (setLoopParameter)
        {
            animator.SetBool("Loop", loopValue); // If you have a "Loop" parameter
        }
    }

    /// <summary>
    /// Smoothly transitions (cross-fades) to the specified animation state.
    /// </summary>
    /// <param name="animationName">The name of the animation state to transition to.</param>
    /// <param name="transitionDuration">The duration of the transition in seconds.</param>
    /// <param name="loop">Sets the value of the 'Loop' bool parameter in the Animator (if it exists).</param>
    public void CrossFadeToAnimation(string animationName, float transitionDuration, bool loop)
    {
        if (!EnsureAnimator()) return;
        StopCombo(); // Stop any ongoing combo when a new animation is cross-faded to
        animator.CrossFade(animationName, transitionDuration, 0); // Typically use base layer (0)
        animator.SetBool("Loop", loop); // If you have a "Loop" parameter
    }

    /// <summary>
    /// Plays different animations on multiple Animator layers simultaneously.
    /// </summary>
    /// <param name="animLayerPairs">Tuples containing the animation state name and layer index.</param>
    public void PlayMultiLayerAnimation(params (string animName, int layerIndex)[] animLayerPairs)
    {
        if (!EnsureAnimator()) return;
        StopCombo(); // Stop any ongoing combo when multi-layer animations are played
        foreach (var pair in animLayerPairs)
        {
            animator.Play(pair.animName, pair.layerIndex);
        }
    }

    /// <summary>
    /// Starts a simple sequence of animations (a combo) without extra delays between them.
    /// Stops any previously running combo before starting the new one.
    /// </summary>
    /// <param name="loop">If true, the entire combo sequence will repeat after finishing.</param>
    /// <param name="animations">The names of the animation states to play in sequence.</param>
    public void AnimationCombo(bool loop, params string[] animations)
    {
        // Convert the string array to an array of ComboStep with 0 delay
        var comboSteps = animations
            .Where(anim => !string.IsNullOrEmpty(anim))
            .Select(anim => new ComboStep(anim, 0f))
            .ToArray();

        StartComboInternal(loop, comboSteps);
    }

    /// <summary>
    /// Starts a sequence of animations (a combo) with optional specified delays *after* each animation.
    /// Stops any previously running combo before starting the new one.
    /// </summary>
    /// <param name="loop">If true, the entire combo sequence will repeat after finishing.</param>
    /// <param name="comboSteps">An array of tuples, each containing the animation state name and the delay (in seconds) to wait *after* that animation finishes.</param>
    public void AnimationCombo(bool loop, params (string animName, float delayAfter)[] comboSteps)
    {
        // Convert the tuple array to an array of ComboStep
         var steps = comboSteps
            .Where(step => !string.IsNullOrEmpty(step.animName))
            .Select(step => new ComboStep(step.animName, step.delayAfter))
            .ToArray();

        StartComboInternal(loop, steps);
    }

    // Internal method to handle the actual combo setup and start
    private void StartComboInternal(bool loop, params ComboStep[] steps)
    {
        if (!EnsureAnimator() || steps == null || steps.Length == 0) return;

        StopCombo(); // Clear previous combo state first

        this.loopCombo = loop;
        animationQueue.Clear();
        currentComboSequence.Clear();

        foreach (var step in steps)
        {
            animationQueue.Enqueue(step);
            currentComboSequence.Add(step); // Store for potential looping
        }

        if (animationQueue.Count > 0)
        {
            isPlayingCombo = true;
            PlayNextInAnimationCombo();
        }
        else
        {
            // If no valid steps were provided
            isPlayingCombo = false;
            loopCombo = false;
        }
    }


    /// <summary>
    /// Stops the currently playing animation combo, if any.
    /// </summary>
    public void StopCombo()
    {
        if (isPlayingCombo)
        {
             if (comboCoroutine != null)
             {
                 StopCoroutine(comboCoroutine);
                 comboCoroutine = null;
             }
             animationQueue.Clear();
             currentComboSequence.Clear();
             isPlayingCombo = false;
             loopCombo = false;
             Debug.Log("Animation combo stopped.");
        }
    }


    private void PlayNextInAnimationCombo()
    {
        if (!EnsureAnimator())
        {
            isPlayingCombo = false;
            return;
        }

        if (animationQueue.Count == 0)
        {
            if (loopCombo && currentComboSequence.Count > 0)
            {
                // Re-queue the combo sequence for looping
                foreach (var step in currentComboSequence)
                {
                    animationQueue.Enqueue(step);
                }
                // Continue to play the first animation of the re-queued sequence below...
            }
            else
            {
                // Combo finished
                isPlayingCombo = false;
                currentComboSequence.Clear();
                loopCombo = false;
                comboCoroutine = null;
                // Debug.Log("Animation combo finished.");
                return;
            }
        }

        if (animationQueue.Count > 0)
        {
            // Dequeue the next step (which includes animation name and delay)
            ComboStep nextStep = animationQueue.Dequeue();
            string nextAnim = nextStep.AnimName;
            float delayAfter = nextStep.DelayAfter;

            animator.Play(nextAnim, 0); // Play the animation
            Debug.Log($"Playing combo part: {nextAnim}, Delay After: {delayAfter}s");

            float clipDuration = CalculateAnimationDuration(nextAnim);

            // Total time to wait = animation's duration + specified extra delay
            float totalWaitTime = clipDuration + delayAfter;

            // Use EndOfFrame for very short/zero total wait times to avoid issues
            if (totalWaitTime <= 0.01f)
            {
                Debug.LogWarning($"AnimatorBrain: Total wait time for '{nextAnim}' + delay is near-zero ({totalWaitTime}). Playing next at end of frame.");
                comboCoroutine = StartCoroutine(PlayNextEndOfFrame());
            }
            else
            {
                // Wait for the total duration before playing the next animation
                comboCoroutine = StartCoroutine(WaitAndPlayNext(totalWaitTime));
            }
        }
         else
        {
            isPlayingCombo = false;
            currentComboSequence.Clear();
            loopCombo = false;
            comboCoroutine = null;
        }
    }

    private IEnumerator PlayNextEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        if (isPlayingCombo) // Check if combo is still active
        {
            PlayNextInAnimationCombo();
        }
    }

    private IEnumerator WaitAndPlayNext(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        comboCoroutine = null;
        if (isPlayingCombo) // Check if combo is still active
        {
             PlayNextInAnimationCombo();
        }
    }

    /// <summary>
    /// Calculates the duration of the AnimationClip associated with the given animation state name.
    /// Assumes the state name matches the AnimationClip name.
    /// </summary>
    /// <param name="animName">The name of the animation state.</param>
    /// <returns>The duration of the animation in seconds, or 0 if not found or invalid.</returns>
    private float CalculateAnimationDuration(string animName)
    {
        // Early exit if animator or its controller is missing
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;

        RuntimeAnimatorController ac = animator.runtimeAnimatorController;
        AnimationClip[] clips = ac.animationClips;

        if (clips == null || clips.Length == 0)
        {
            // This is less critical now as 0 duration is handled, but good to know.
            // Debug.LogWarning($"AnimatorBrain: No animation clips found in the Animator Controller '{ac.name}'. Cannot determine duration for '{animName}'.");
            return 0f;
        }

        foreach (AnimationClip clip in clips)
        {
            if (clip != null && clip.name == animName)
            {
                 // Account for the Animator's overall speed multiplier
                return (animator.speed > 0) ? clip.length / animator.speed : clip.length;
            }
        }

        Debug.LogWarning($"AnimatorBrain: Animation clip for state '{animName}' not found in Animator Controller '{ac.name}'. Returning 0 duration.");
        return 0f; // Clip not found
    }

    /// <summary>
    /// Helper method to ensure the animator reference is valid before using it.
    /// </summary>
    /// <returns>True if the animator is valid, false otherwise.</returns>
    private bool EnsureAnimator()
    {
        if (animator == null)
        {
            Debug.LogError("AnimatorBrain: Animator reference is missing!", this);
            return false;
        }
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("AnimatorBrain: Animator does not have a Runtime Animator Controller assigned.", this);
            return false;
        }
        return true;
    }
}