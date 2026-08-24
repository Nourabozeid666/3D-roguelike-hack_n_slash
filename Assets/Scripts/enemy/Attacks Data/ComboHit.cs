using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ComboHit
{
    [SerializeField] private AnimationClip clip; // Drag & drop your .anim clip here!
    [SerializeField] private float damage;       // Damage for this swing
    // Automatically gets the clip name's hash for the Animator
    public int AnimationHash => clip != null ? Animator.StringToHash(clip.name) : 0;
    // Automatically gets the exact duration of the animation clip in seconds
    public float Duration => clip != null ? clip.length : 0f;
    public float Damage => damage;
}

// if i wanted to make multiple combos
[System.Serializable]
public class ComboSequence
{
    [SerializeField] private string sequenceName;
    [SerializeField] private List<ComboHit> hits;
    public IReadOnlyList<ComboHit> Hits => hits;
}