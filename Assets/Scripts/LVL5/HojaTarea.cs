using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HojaTarea : MonoBehaviour
{

    bool salio = false;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!salio)
        {
            transform.position += Vector3.up * Time.deltaTime * 0.1f;
        }
        if (transform.position.y >= 0.9f)
        {
            salio = true;
        }
    }


}
