using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LVL3Manager : MonoBehaviour
{
    public static LVL3Manager instance;
    [SerializeField] GameObject answerPrefab;

    [Space]
    [SerializeField] float minRange, maxRange;

    [SerializeField] int amount;

    // Start is called before the first frame update
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start()
    {
        StartCoroutine(Game());
    }

    private IEnumerator Game()
    {
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < amount; i++)
        {
            Vector3 position;
            do
            {
                position = Random.onUnitSphere * Random.Range(minRange, maxRange);
                position.y *= 0.25f;
            }
            while (Vector3.Dot(Vector3.forward, position) > 0.5f);
            Instantiate(answerPrefab, position, Quaternion.identity);
        }

    }


    //Se ejecuta solo en desarrollo, para desarrollador
    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(Vector3.zero, minRange);
        Gizmos.DrawWireSphere(Vector3.zero, maxRange);
    }
}
