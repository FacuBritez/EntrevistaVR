using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuctionGun : MonoBehaviour
{

    public PlayerNinja myPlayer;

    void Awake()
    {
        myPlayer = (PlayerNinja)FindObjectOfType(typeof(PlayerNinja));
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<ObjectInteractable>())
        {

            if (other.gameObject.GetComponent<ObjectInteractable>().isObstacle)
            {
                Debug.Log("Error, la pifie");
                Destroy(other.gameObject);
                myPlayer.UpdateValues(true, other.gameObject.GetComponent<ObjectInteractable>().isObstacle);
            }
            else
            {
                Debug.Log("Succionado, agarrado, correcto");
                Destroy(other.gameObject);
                myPlayer.UpdateValues(false, other.gameObject.GetComponent<ObjectInteractable>().isObstacle);
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
