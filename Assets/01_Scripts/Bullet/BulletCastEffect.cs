using UnityEngine;

public static class BulletCastEffect
{
    /// 발사 위치에 시각 효과를 재생하고 설정된 시간이 지나면 제거한다. 생성한 효과를 반환한다. (없으면 null)
    public static ParticleSystem Play(Transform muzzle, ParticleSystem prefab, float scale, float lifetime)
    {
        if (prefab == null || muzzle == null) return null;

        ParticleSystem effect = UnityEngine.Object.Instantiate(prefab, muzzle, false);
        effect.transform.localPosition = Vector3.zero;
        effect.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        effect.transform.localScale *= scale;

        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.stopAction = ParticleSystemStopAction.None;
            var trails = particles.trails;
            trails.worldSpace = false;
        }

        effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
        UnityEngine.Object.Destroy(effect.gameObject, lifetime);
        return effect;
    }

    /// 월드 적중 위치에서 효과를 재생하고 완료 후 제거한다.
    public static void PlayAt(Vector2 position, Vector2 direction, ParticleSystem prefab, float scale, float lifetime)
    {
        if (prefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle) * Quaternion.Euler(0f, 90f, 0f);
        ParticleSystem effect = UnityEngine.Object.Instantiate(prefab, position, rotation);
        effect.transform.localScale *= scale;
        foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = particles.main;
            main.stopAction = ParticleSystemStopAction.None;
        }
        effect.gameObject.SetActive(true);
        effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        effect.Play(true);
        UnityEngine.Object.Destroy(effect.gameObject, lifetime);
    }

}
