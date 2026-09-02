using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class ParticlePool : MonoBehaviour
{
    // public static ParticlePool Instance;
    // public ObjectPool<GameObject> particlePool;
    // [SerializeField] GameObject particlePrefab;
    // public int maxParticles = 50;
    // GameObject[] prewarmedParticles;

    // // Start is called once before the first execution of Update after the MonoBehaviour is created
    // void Awake()
    // {
    //     Instance = this;
    //     if(Instance != null && Instance != this)
    //     {
    //         Destroy(gameObject);
    //     }
    //     else
    //     {
    //         Instance = this;
    //     }
    //     particlePool = new ObjectPool<GameObject>(
    //         createFunc: CreateParticle,
    //         actionOnGet: particle => GetParticle(particle),
    //         actionOnRelease: particle => ReleaseParticle(particle, 1f),
    //         actionOnDestroy: particle => Destroy(particle),
    //         maxSize: maxParticles
    //     );

    //     prewarmedParticles = new GameObject[maxParticles];
    //     for(int i = 0; i < maxParticles; i++) prewarmedParticles[i] = particlePool.Get();
    //     for(int i = 0; i < maxParticles; i++) particlePool.Release(prewarmedParticles[i]);
    // }

    // void GetParticle(GameObject particle)
    // {
    //     particle.SetActive(true);
    // }

    // void ReleaseParticle(GameObject particle, float delay)
    // {
    //     //StartCoroutine(ReleaseAfterDelay(particle, delay));
    // }

    // IEnumerator ReleaseAfterDelay(GameObject particle, float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     particle.SetActive(false);
    //     particlePool.Release(particle);
    // }

    // GameObject CreateParticle()
    // {
    //     GameObject temp = Instantiate(particlePrefab);
    //     temp.transform.SetParent(transform);
    //     temp.gameObject.SetActive(false);
    //     return temp;
    // }

    // public void SpawnParticle(Vector3 pos, Quaternion rot)
    // {
    //     GameObject particle = particlePool.Get();
    //     particle.transform.position = pos;
    //     particle.transform.rotation = rot;
    // }

    // public void DieParticle(GameObject obj)
    // {
    //     particlePool.Release(obj);
    // }
}
