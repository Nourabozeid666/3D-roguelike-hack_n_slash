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

    PlayerHudData data;

    /// <param name="entity">Live player entity (may be null in a scene without a player).</param>
    /// <param name="floorGetter">Reads the current run floor each snapshot (RunController.CurrentFloor).</param>
    public RunPlayerHudSource(IEntity entity, Func<int> floorGetter)
    {
        this.entity = entity;
        this.floorGetter = floorGetter;
        this.data = Snapshot();
    }

    /// <summary>Subscribe to the health events and emit the current snapshot. Idempotent-safe for a
    /// fresh source; call Disable() before discarding (see PlayerUiBootstrap unbind flow).</summary>
    public void Enable()
    {
        if (entity == null) return;
        entity.OnDamageTaken += OnDamaged;
        entity.OnHealed += OnHealthChanged;
        entity.OnMaxHealthChanged += OnHealthChanged;
        entity.OnDied += OnDied;
    }

    public void Disable()
    {
        if (entity == null) return;
        entity.OnDamageTaken -= OnDamaged;
        entity.OnHealed -= OnHealthChanged;
        entity.OnMaxHealthChanged -= OnHealthChanged;
        entity.OnDied -= OnDied;
    }

    public PlayerHudData GetPlayerHud() => data;

    void OnDamaged(float damage, AttackEffectData effect) => Push();
    void OnHealthChanged(float value) => Push();
    void OnDied() => Push();

    PlayerHudData Snapshot() => new PlayerHudData(
        Mathf.RoundToInt(entity != null ? Mathf.Max(0f, entity.Health) : 0f),
        Mathf.RoundToInt(entity != null ? Mathf.Max(1f, entity.MaxHealth) : 1f),
        0,
        100,
        1,
        floorGetter != null ? Mathf.Max(1, floorGetter()) : 1);

    void Push()
    {
        PlayerHudData next = Snapshot();
        if (next.Equals(data)) return;
        data = next;
        Changed?.Invoke(data);
    }
}
