using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : MonoBehaviour
{
    public PlayerNinja myPlayer;

    [SerializeField] AudioClip hitClip;

    void Awake()
    {
        myPlayer = (PlayerNinja)FindObjectOfType(typeof(PlayerNinja));
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<ObjectInteractable>())
        {
            AudioSource.PlayClipAtPoint(hitClip, other.gameObject.transform.position);
            if (!other.gameObject.GetComponent<ObjectInteractable>().isObstacle)
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
}
