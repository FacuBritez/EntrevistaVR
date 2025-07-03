using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{

    [Header("Spawn Limits")]

    [SerializeField]
    Transform LeftDownCorner;
    [SerializeField]
    Transform RightUpCorner;

    [Header("Object References")]

    [SerializeField]
    GameObject ObjectPrefab;
    [SerializeField]
    Vector3 CameraPosition;
    [SerializeField]
    float cameraHeightMultiplier;




    [Header("Game Difficulty Settings")]
    //Dato x = Velocidad, Dato y = Frequencia
    public int currentStage;

    [Tooltip("x = Frequencia, y = VelocidadMinima, z = VelocidadMaxima, w = ComboParaSubirDeStage")]
    public Vector4[] StageSettingsArray;


    float timeAux;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        timeAux += Time.deltaTime;
        if (timeAux > StageSettingsArray[currentStage].x)
        {
            SpawnObject(ObjectPrefab);
            timeAux = 0;
        }
    }

    void SpawnObject(GameObject prefab)
    {
        CameraPosition = Camera.main.transform.position;

        float xRandom = Random.Range(LeftDownCorner.position.x, RightUpCorner.position.x);
        float yRandom = Random.Range(LeftDownCorner.position.y, RightUpCorner.position.y);

        GameObject newObject = Instantiate(prefab, new Vector3(xRandom, yRandom, transform.position.z), Quaternion.identity);
        LoadData(newObject.GetComponent<ObjectInteractable>());
    }

    void LoadData(ObjectInteractable spawnedObject)
    {
        spawnedObject.target = new Vector3(CameraPosition.x, CameraPosition.y * cameraHeightMultiplier, CameraPosition.z);
        spawnedObject.objectSpeed = GetLevelSpeed();
    }

    float GetLevelSpeed()
    {
        float speed;
        switch (currentStage)
        {
            case 0:
                speed = Random.Range(StageSettingsArray[currentStage].y, StageSettingsArray[currentStage].z) / 100;
                break;
            case 1:
                speed = Random.Range(StageSettingsArray[currentStage].y, StageSettingsArray[currentStage].z) / 100;
                break;
            case 2:
                speed = Random.Range(StageSettingsArray[currentStage].y, StageSettingsArray[currentStage].z) / 100;
                break;
            case 3:
                speed = Random.Range(StageSettingsArray[currentStage].y, StageSettingsArray[currentStage].z) / 100;
                break;
            default:
                print("Stage does not exist.");
                speed = 200;
                break;
        }

        return speed;
    }

    public void StageUp()
    {
        if (currentStage != 3)
        {
            currentStage++;
        }
    }
    public void StageDown()
    {
        if (currentStage != 0)
        {
            currentStage--;
        }
    }

    public void Hit(int Combo)
    {
        if (Combo >= StageSettingsArray[currentStage].w)
        {
            StageUp();
        }
    }
}
