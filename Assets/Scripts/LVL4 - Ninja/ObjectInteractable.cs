using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInteractable : MonoBehaviour
{
    public Vector3 target;

    public float objectSpeed;
    public bool isObstacle;

    [SerializeField]
    GameObject[] NegativeMeshes;
    [SerializeField]
    GameObject[] PositiveMeshes;
    void Awake()
    {
        SetObjectType();
    }
    void Start()
    {
        Destroy(this.gameObject, 45f);
    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(this.transform.position, target, objectSpeed);

    }

    void SetObjectType()
    {
        GameObject SpawnedMesh;
        if (Random.Range(0, 2) == 0)
        {
            isObstacle = true;
            SpawnedMesh = Instantiate(NegativeMeshes[Random.Range(0, NegativeMeshes.Length)],this.transform);
        }
        else
        {
            isObstacle = false;
            SpawnedMesh = Instantiate(PositiveMeshes[Random.Range(0, PositiveMeshes.Length)],this.transform);
        }

    }

    
}
