using UnityEngine;

public class SacrificeAttackComponents : MonoBehaviour
{
    [SerializeField] private SacrificeAttackConfig config;
    [SerializeField] private GameObject explosionParticles;
    public SacrificeAttackConfig Config => config;
    public GameObject ExplosionParticles => explosionParticles;
}

public class ComboAttackComponents: MonoBehaviour
{

}
