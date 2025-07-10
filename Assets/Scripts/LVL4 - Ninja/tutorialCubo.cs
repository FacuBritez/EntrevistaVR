using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tutorialCubo : MonoBehaviour
{

    public bool isCuboBueno;


    [SerializeField]
    GameObject CuboBueno;
    [SerializeField]
    ObjectSpawner spawner;
    [SerializeField]
    Timer timer;
    [SerializeField]
    AudioSource musicSource;
    [SerializeField]
    GameObject canvasTexto;

    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// </summary>
    void Awake()
    {
        musicSource.Stop();
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<SuctionGun>() && isCuboBueno)
        {
            spawner.enabled = true;
            timer.enabled = true;
            musicSource.Play();

            canvasTexto.SetActive(true);
            Destroy(this.gameObject);
        }
        else if (other.gameObject.GetComponent<Sword>() && !isCuboBueno)
        {
            CuboBueno.SetActive(true);
            Destroy(this.gameObject);
        }

        
    }
}
