using UnityEngine;

public class StaggerSeverity
{
    public enum Severity
    {
        None,
        Light,
        Medium,
        Heavy
    }

    public enum StaggerTier { Normal, Heavy, Power }
    public enum PoiseTier { Normal, Heavy, Power, Uninterruptible }

    public static Severity GetStaggerSeverity(PoiseTier currentPoise, StaggerTier staggerTier)
    {
        // Can Stagger if current poise is less than or equal the stagger tier's required poise
        if (currentPoise == PoiseTier.Uninterruptible)
        {
            return Severity.None;
        }
        else if (staggerTier == StaggerTier.Power)
        {
            return Severity.Heavy; 
        } else if (staggerTier == StaggerTier.Heavy && currentPoise != PoiseTier.Power)
        {
            return Severity.Medium;
        }
        else if (staggerTier == StaggerTier.Normal && currentPoise != PoiseTier.Heavy && currentPoise != PoiseTier.Power)
        {
            return Severity.Light;
        }
        else
        {
            return Severity.Light;
        }
    }

    public static float GetStaggerDuration(Severity severity)
    {
        // TODO: MAY NEED FINE TUNING
        switch (severity)
        {
            case Severity.None:
                return 0f;
            case Severity.Light:
                return 0.25f; // Light stagger duration
            case Severity.Medium:
                return 0.5f; // Medium stagger duration
            case Severity.Heavy:
                return 0.75f; // Heavy stagger duration
            default:
                return 0f;
        }
    }
}
