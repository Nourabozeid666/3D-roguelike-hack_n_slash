using System;
using UnityEngine;

/// <summary>
/// Real HUD data source for a live run. Implements IPlayerHudSource — the same seam MockPlayerHudSource
/// sits behind — by reading the LIVE player health (PlayerController -&gt; IEntity) and the run floor
/// (RunController) instead of a moving mock. It subscribes to the entity's health events
/// (damage / heal / max-health / death) and re-raises Changed so the HUD re-renders in real time;
/// nothing polls. The presenter binds it exactly as it binds the mock, so swapping sources never
/// touches presenter or controller logic.
///
/// Deliberately Unity + IEntity dependent (IEntity is the teammate-owned shared interface), so it
/// lives OUTSIDE the dotnet harness — same precedent as PlayerUiBootstrap. Owned by RunBootstrap's
/// production path (entering the game scene from the menu) and injected via PlayerUiBootstrap.
/// </summary>
public class RunPlayerHudSource : IPlayerHudSource
{
    public event Action<PlayerHudData> Changed;

    readonly IEntity entity;
    readonly Func<int> floorGetter;
    ProgressionSystem progression;

    PlayerHudData data;

    /// <param name="entity">Live player entity (may be null in a scene without a player).</param>
    /// <param name="floorGetter">Reads the current run floor each snapshot (RunController.CurrentFloor).</param>
    /// <param name="progression">Live XP/level source (may be null in scenes without progression;
    /// attach later via SetProgression).</param>
    public RunPlayerHudSource(IEntity entity, Func<int> floorGetter, ProgressionSystem progression = null)
    {
        this.entity = entity;
        this.floorGetter = floorGetter;
        this.progression = progression;
        this.data = Snapshot();
    }

    /// <summary>Subscribe to the health and XP/level events and emit the current snapshot.
    /// Idempotent-safe for a fresh source; call Disable() before discarding (see PlayerUiBootstrap
    /// unbind flow).</summary>
    public void Enable()
    {
        SubscribeEntity();
        SubscribeProgression();
    }

    public void Disable()
    {
        UnsubscribeEntity();
        UnsubscribeProgression();
    }

    /// <summary>Attach (or swap) the live progression source and re-snapshot immediately. No-op when
    /// already attached; used when the progression host is created after this source.</summary>
    public void SetProgression(ProgressionSystem value)
    {
        if (progression == value) return;
        UnsubscribeProgression();
        progression = value;
        SubscribeProgression();
        Push();
    }

    /// <summary>Re-snapshot and push (e.g. a save restore set XP/level outside the event flow).</summary>
    public void Refresh() => Push();

    public PlayerHudData GetPlayerHud() => data;

    void SubscribeEntity()
    {
        if (entity == null) return;
        entity.OnDamageTaken += OnDamaged;
        entity.OnHealed += OnHealthChanged;
        entity.OnMaxHealthChanged += OnHealthChanged;
        entity.OnDied += OnDied;
    }

    void UnsubscribeEntity()
    {
        if (entity == null) return;
        entity.OnDamageTaken -= OnDamaged;
        entity.OnHealed -= OnHealthChanged;
        entity.OnMaxHealthChanged -= OnHealthChanged;
        entity.OnDied -= OnDied;
    }

    void SubscribeProgression()
    {
        if (progression == null) return;
        progression.OnXpChanged += OnProgressionChanged;
        progression.OnLevelUp += OnLevelUpChanged;
    }

    void UnsubscribeProgression()
    {
        if (progression == null) return;
        progression.OnXpChanged -= OnProgressionChanged;
        progression.OnLevelUp -= OnLevelUpChanged;
    }

    void OnDamaged(float damage, AttackEffectData effect) => Push();
    void OnHealthChanged(float value) => Push();
    void OnDied() => Push();
    void OnProgressionChanged(int currentXp, int xpRequired, int level) => Push();
    void OnLevelUpChanged(int newLevel, int pendingUpgrades) => Push();

    PlayerHudData Snapshot() => new PlayerHudData(
        Mathf.RoundToInt(entity != null ? Mathf.Max(0f, entity.Health) : 0f),
        Mathf.RoundToInt(entity != null ? Mathf.Max(1f, entity.MaxHealth) : 1f),
        progression != null ? Mathf.Max(0, progression.CurrentXp) : 0,
        progression != null ? Mathf.Max(1, progression.XpRequired) : 100,
        progression != null ? Mathf.Max(1, progression.CurrentLevel) : 1,
        floorGetter != null ? Mathf.Max(1, floorGetter()) : 1);

    void Push()
    {
        PlayerHudData next = Snapshot();
        if (next.Equals(data)) return;
        data = next;
        Changed?.Invoke(data);
    }
}
