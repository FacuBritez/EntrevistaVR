using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;


public class LVL3Manager : MonoBehaviour
{
    public static LVL3Manager instance; // static para que la variable sea de la clase en si, y no del objeto.
    [SerializeField] CVType[] CV;
    [Space]
    [SerializeField] CVPalabra answerPrefab;
    [Space]
    [SerializeField] CVCanvas canvas;

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

    //---

    CVType currentCV;

    // ---



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
        currentCV = CV[Random.Range(0, CV.Length)];
        canvas.SetText(currentCV);

        yield return InstanciarPalabras(GetOptions(currentCV));
    }

    private IEnumerator InstanciarPalabras(KeyValuePair<string, CVType.CVFields>[] words)
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
            Vector3 position = transform.position + direction * distance;

            CVPalabra obj = Instantiate(answerPrefab, position, Quaternion.identity);

            // Asignar la palabra al objeto
            obj.SetText(words[i].Key, words[i].Value);

            // Animación de escalado para que aparezca
            obj.PlayAppearAnimation(0.5f);

        }
    }

    KeyValuePair<string, CVType.CVFields>[] GetOptions(CVType cv)
    {
        List<KeyValuePair<string, CVType.CVFields>> options = new();

        options.AddRange(StringsToValuePairs(cv.SobreMi, CVType.CVFields.SobreMi));
        options.AddRange(StringsToValuePairs(cv.ExpLaboral, CVType.CVFields.ExpLaboral));
        options.AddRange(StringsToValuePairs(cv.Formaciones, CVType.CVFields.Formaciones));
        options.AddRange(StringsToValuePairs(cv.Cursos, CVType.CVFields.Cursos));

        return options.ToArray();

    }

    KeyValuePair<string, CVType.CVFields>[] StringsToValuePairs(string[] input, CVType.CVFields field)
    {
        return input.Select(x => new KeyValuePair<string, CVType.CVFields>(x, field)).ToArray();    
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, minRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxRange);

        Gizmos.color = Color.yellow;
        Vector3 dir1 = Quaternion.Euler(minVerticalAngle, minHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir2 = Quaternion.Euler(minVerticalAngle, maxHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir3 = Quaternion.Euler(maxVerticalAngle, minHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir4 = Quaternion.Euler(maxVerticalAngle, maxHorizontalAngle, 0) * Vector3.forward;
        Vector3 dir5 = Quaternion.Euler(minVerticalAngle, 180, 0) * Vector3.forward;
        Vector3 dir6 = Quaternion.Euler(maxVerticalAngle, 180, 0) * Vector3.forward;

        Gizmos.DrawLine(transform.position, transform.position + dir1 * maxRange);
        Gizmos.DrawLine(transform.position, transform.position + dir2 * maxRange);
        Gizmos.DrawLine(transform.position, transform.position + dir3 * maxRange);
        Gizmos.DrawLine(transform.position, transform.position + dir4 * maxRange);
        Gizmos.DrawLine(transform.position, transform.position + dir5 * maxRange);
        Gizmos.DrawLine(transform.position, transform.position + dir6 * maxRange);
    }
}
