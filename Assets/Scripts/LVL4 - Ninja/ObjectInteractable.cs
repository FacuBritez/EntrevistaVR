using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectInteractable : MonoBehaviour
{
    public Vector3 target;

    public float objectSpeed;
    [SerializeField] float rotationSpeed;
    public bool isObstacle;


    [SerializeField]
    GameObject NegativeMeshesParent;
    [SerializeField]
    GameObject PositiveMeshesParent;
    void Awake()
    {
        SetObjectType();
    }
    void Start()
    {
        Destroy(this.gameObject, 45f);
        transform.rotation = Quaternion.Euler(Random.Range(0,360), Random.Range(0,360), Random.Range(0,360));
    }

    void Update()
    {

        transform.position = Vector3.MoveTowards(this.transform.position, target, objectSpeed * Time.deltaTime);
        transform.Rotate(new Vector3(1, 1, 1) * rotationSpeed * Time.deltaTime);
        
        if (Vector3.Distance(this.transform.position, target) < 0.05)
        {
            Destroy(this.gameObject);
        }
    }

    void SetObjectType()
    {
        GameObject SpawnedMesh;
        if (Random.Range(0, 2) == 0)
        {
            //NEGATIVO
            isObstacle = true;
            SpawnedMesh = NegativeMeshesParent.transform.GetChild(Random.Range(0, NegativeMeshesParent.transform.childCount)).gameObject;
            SpawnedMesh.SetActive(true);
        }
        else
        {
            //POSITIVO
            isObstacle = false;
            SpawnedMesh = PositiveMeshesParent.transform.GetChild(Random.Range(0, PositiveMeshesParent.transform.childCount)).gameObject;
            SpawnedMesh.SetActive(true);
        }

    }

    
}
