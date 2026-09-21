using UnityEngine;

public class DynamicObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool spawnActive = true;
    [SerializeField] private bool destroyOnSpawn = false;

    [Header("Existing Object Activation Alternative")]
    [SerializeField] private GameObject existingSceneObject;

    private GameObject spawnedInstance;

    public void SpawnObject()
    {
        if (existingSceneObject != null)
        {
            existingSceneObject.SetActive(true);
            if (spawnPoint != null)
            {
                existingSceneObject.transform.position = spawnPoint.position;
                existingSceneObject.transform.rotation = spawnPoint.rotation;
            }
            return;
        }

        if (prefabToSpawn != null && spawnPoint != null)
        {
            spawnedInstance = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
            spawnedInstance.SetActive(spawnActive);

            if (destroyOnSpawn)
            {
                Destroy(gameObject);
            }
        }
    }
}