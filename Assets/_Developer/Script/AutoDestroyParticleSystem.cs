using UnityEngine;

public class AutoDestroyParticleSystem : MonoBehaviour
{
    private ParticleSystem[] particleSystems;

    void Start()
    {
        // Get ALL ParticleSystems (parent + children)
        particleSystems = GetComponentsInChildren<ParticleSystem>(includeInactive: false);

        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.loop = false;
        }
    }

    void Update()
    {
        bool allDead = true;

        foreach (var ps in particleSystems)
        {
            if (ps.IsAlive())
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            Destroy(gameObject);
        }
    }
}

