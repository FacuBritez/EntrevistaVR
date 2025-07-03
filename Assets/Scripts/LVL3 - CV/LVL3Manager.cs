using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


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

    //---------

    CVType currentCV;



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
            Vector3 position = transform.position + direction * distance;

            CVPalabra obj = Instantiate(answerPrefab, position, Quaternion.identity);

            // Asignar la palabra al objeto
            obj.Text.text = words[i];

            // Animación de escalado para que aparezca
            yield return ScaleAnimationAppears(obj.transform, 0.5f);


        }
    }


    // Animación de escalado de un objeto al aparecer
    private IEnumerator ScaleAnimationAppears(Transform obj, float duration)
    {
        float time = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = obj.localScale;

        while (time < duration)
        {
            obj.localScale = Vector3.Lerp(startScale, endScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        obj.localScale = endScale;
    }

    // Animación de escalado de un objeto al desaparecer
    private IEnumerator ScaleAnimationDesappears(Transform obj, float duration)
    {
        float time = 0f;
        Vector3 startScale = obj.localScale;
        Vector3 endScale = Vector3.zero;

        while (time < duration)
        {
            obj.localScale = Vector3.Lerp(startScale, endScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        obj.localScale = endScale;
    }


    string[] GetOptions(CVType cv)
    {
        List<string> options = new();

        options.AddRange(cv.SobreMi.Palabras);
        options.AddRange(cv.ExpLaboral);
        options.AddRange(cv.Formaciones);
        options.AddRange(cv.Cursos);

        return options.ToArray();

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
