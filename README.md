# Unity AnimatorBrain

A simple yet effective Unity C# script designed to streamline interactions with the `Animator` component. It provides convenient methods for playing individual animations, cross-fading, managing layers, and executing sequential animation combos.

## Features

* **Direct Play:** Instantly play an animation state (`PlayAnimation`).
* **Cross-Fade:** Smoothly transition between animation states (`CrossFadeToAnimation`).
* **Multi-Layer Control:** Play different animations on different Animator layers simultaneously (`PlayMultiLayerAnimation`).
* **Animation Combos:** Define and play sequences of animations (`AnimationCombo`).
* **Looping Combos:** Optionally loop the entire animation combo sequence.
* **Stop Combos:** Halt the currently executing combo sequence (`StopCombo`).
* **Automatic Duration Calculation:** Attempts to calculate animation durations for combos (relies on state names matching clip names).
* **Self-Contained:** Requires only the `Animator` component on the same GameObject.

## Setup

1.  Copy the `AnimatorBrain.cs` script into your Unity project's `Assets` folder (e.g., within a `Scripts` subfolder).
2.  Select the GameObject in your scene that has the `Animator` component you want to control.
3.  Add the `AnimatorBrain` script as a component to that same GameObject (Component -> Add -> Search for "AnimatorBrain"). The script automatically requires an `Animator` component, so one will be added if it doesn't exist, but you should already have one configured.
4.  Ensure your `Animator` component has an `Animator Controller` assigned with the necessary animation states (e.g., "Idle", "Run", "Attack1", "Attack2").

## Usage

To use the `AnimatorBrain`, you first need a reference to it from another script.

**Example (Getting the reference):**

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public AnimatorBrain animatorBrain; // Assign in Inspector OR find automatically

    void Start()
    {
        // Option 1: Assign in Inspector (Drag the GameObject with AnimatorBrain onto the slot)

        // Option 2: Get component if PlayerController is on the same GameObject
        if (animatorBrain == null)
        {
            animatorBrain = GetComponent<AnimatorBrain>();
        }

        if (animatorBrain == null)
        {
            Debug.LogError("PlayerController could not find AnimatorBrain component!");
        }
    }

    void Update()
    {
        // Example usage within Update or other methods
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Play a single attack animation instantly
            // Assumes "Attack" state exists in the Animator Controller
            if (animatorBrain != null)
                animatorBrain.PlayAnimation("Attack");
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
             // Start a non-looping combo
             // Assumes "Attack1", "Attack2", "HeavyAttack" states exist
            if (animatorBrain != null)
                animatorBrain.AnimationCombo(false, "Attack1", "Attack2", "HeavyAttack");
                // Also you can add time breaks. Like "(Attack1, 1.0f) , ("Attack2, 0f));
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
             // Stop any currently playing combo
             if (animatorBrain != null)
                animatorBrain.StopCombo();
        }

         if (Input.GetKey(KeyCode.W))
        {
             // CrossFade to Run animation with a 0.2s transition, enable looping
             // Assumes a "Run" state and a "Loop" boolean parameter exist in the Animator Controller
            if (animatorBrain != null)
                animatorBrain.CrossFadeToAnimation("Run", 0.2f, true);
        }
         else if (Input.GetKeyUp(KeyCode.W)) // Example: Go back to Idle when W is released
        {
            // CrossFade to Idle animation, disable looping
            // Assumes an "Idle" state exists
            if (animatorBrain != null)
                 animatorBrain.CrossFadeToAnimation("Idle", 0.3f, false); // Typically Idle loops itself via state setting
        }
    }
}
