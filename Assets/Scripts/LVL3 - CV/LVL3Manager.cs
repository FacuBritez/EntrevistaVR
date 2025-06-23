using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class LVL3Manager : MonoBehaviour
{
    public static LVL3Manager instance; // static para que la variable sea de la clase en si, y no del objeto.
    [Space]
    [SerializeField] GameObject answerPrefab;

    [Header("Rango de aparición de las palabras")]
    [Space]
    [SerializeField] float minRange;
    [SerializeField] float maxRange;

    [Header("Ángulos de aparición de las palabras")]
    [Space]
    [SerializeField] float minHorizontalAngle = 100f;
    [SerializeField] float maxHorizontalAngle = 260f;
    [SerializeField] float minVerticalAngle = -25f;
    [SerializeField] float maxVerticalAngle = -10f;

    [Header("Variables temporales para el desarrollo")]
    [Space]
    // Para pruebas, reemplazar InstanciarPalabras(words[seccion]) por amount
    [SerializeField] int amount;
    private string[][] words = new string[][]
    {
        new string[] { "Palabra1", "Palabra2", "Palabra3" },
        new string[] { "Palabra4", "Palabra5", "Palabra6" },
        new string[] { "Palabra7", "Palabra8", "Palabra9" }
    };
    
    

    // Patron Singleton para que solo haya una instancia de este manager
    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(Game());
    }

    private IEnumerator Game()
    {
        int seccion = 0; // Sección actual del juego
        StartCoroutine(InstanciarPalabras(words[seccion]));
        yield return null;
    }

    private IEnumerator InstanciarPalabras(string[] words)
    {
        // Espera antes de empezar a instaciar las palabras
        yield return new WaitForSeconds(1f);
        for (int i = 0; i < words.Length; i++)
        {
            yield return new WaitForSeconds(0.5f); //Espera entre cada aparición
            float horizontalAngle = Random.Range(minHorizontalAngle, maxHorizontalAngle);
            float verticalAngle = Random.Range(minVerticalAngle, maxVerticalAngle);

            Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
            Vector3 direction = rotation * Vector3.forward;

            float distance = Random.Range(minRange, maxRange);
            Vector3 position = direction * distance;

            GameObject obj = Instantiate(answerPrefab, position, Quaternion.identity);
            obj.transform.localScale = Vector3.zero; // Escalado inicial a 0

            // Animación de escalado para que aparezca
            StartCoroutine(ScaleAnimation(obj.transform, Vector3.one, 0.5f));
        }
    }


    // Animación de escalado de un objeto
    // Sirve tanto para que aparezca como para que desaparezca
    private IEnumerator ScaleAnimation(Transform obj, Vector3 targetScale, float duration)
    {
        float time = 0f;
        Vector3 initialScale = obj.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            obj.localScale = Vector3.Lerp(initialScale, targetScale, time / duration);
            yield return null;
        }
        obj.localScale = targetScale;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Vector3.zero, minRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Vector3.zero, maxRange);

        Gizmos.color = Color.yellow;
        Vector3 dir1 = Quaternion.Euler(minVerticalAngle, minHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir2 = Quaternion.Euler(minVerticalAngle, maxHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir3 = Quaternion.Euler(maxVerticalAngle, minHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir4 = Quaternion.Euler(maxVerticalAngle, maxHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir5 = Quaternion.Euler(minVerticalAngle, 180, 0) * Vector3.forward;
        Vector3 dir6 = Quaternion.Euler(maxVerticalAngle, 180, 0) * Vector3.forward;

        Gizmos.DrawLine(Vector3.zero, dir1 * maxRange);
        Gizmos.DrawLine(Vector3.zero, dir2 * maxRange);
        Gizmos.DrawLine(Vector3.zero, dir3 * maxRange);
        Gizmos.DrawLine(Vector3.zero, dir4 * maxRange);
        Gizmos.DrawLine(Vector3.zero, dir5 * maxRange);
        Gizmos.DrawLine(Vector3.zero, dir6 * maxRange);
    }
}
