using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class LVL3Manager : MonoBehaviour
{
    public static LVL3Manager Instance; // static para que la variable sea de la clase en si, y no del objeto.

    // ---

    [SerializeField] CVType[] CV;
    [Space]
    [SerializeField] CVPalabra answerPrefab;

    [Header("Tiempo de juego")]
    [Space]
    [SerializeField] float gameTime;
    [SerializeField] float minCooldown;
    [SerializeField] float maxCooldown;

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

    public Dictionary<CVType.CVFields, string[]> CurrentCV { get; private set; }

    public float remainingTime { get; private set; }

    public event System.Action<bool> OnGameFinished = delegate { };


    // ---

    CVCanvasAnswer[] AnswersInScene;



    // Patron Singleton para que solo haya una instancia de este manager
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        AnswersInScene = FindObjectsOfType<CVCanvasAnswer>();
    }

    public void StartGame()
    {
        StartCoroutine(Game());
    }

    private IEnumerator Game()
    {
        var chosenCV = CV[Random.Range(0, CV.Length)];
        CurrentCV = CVToDictionary(chosenCV);

        yield return InstanciarPalabras(CurrentCV);
        yield return Timer();

        OnGameFinished(IsDone());
    }   


    private IEnumerator InstanciarPalabras(Dictionary<CVType.CVFields, string[]> dictionaryCV)
    {
        foreach (var words in dictionaryCV)
        {
            foreach (var word in words.Value)
            {
                yield return new WaitForSeconds(Random.Range(minCooldown, maxCooldown)); //Espera entre cada aparición
                float horizontalAngle = Random.Range(minHorizontalAngle, maxHorizontalAngle);
                float verticalAngle = Random.Range(minVerticalAngle, maxVerticalAngle);

                Quaternion rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0);
                Vector3 direction = rotation * Vector3.forward;

                float distance = Random.Range(minRange, maxRange);
                Vector3 position = transform.position + direction * distance;

                CVPalabra obj = Instantiate(answerPrefab, position, Quaternion.identity);

                // Asignar la palabra al objeto
                obj.SetText(word, words.Key);

                // Animación de escalado para que aparezca
                obj.PlayAppearAnimation(0.5f);
            }
        }
    }

    private IEnumerator Timer()
    {
        remainingTime = gameTime;

        while (remainingTime > 0f && !IsDone())
        {
            remainingTime -= Time.deltaTime;

            yield return null;
        }
    }

    bool IsDone()
    {
        return AnswersInScene.All(x => x.CheckPalabra());
    }

    Dictionary<CVType.CVFields, string[]> CVToDictionary(CVType cv)
    {
        Dictionary<CVType.CVFields, string[]> options = new();

        options.Add(CVType.CVFields.SobreMi, cv.SobreMi);
        options.Add(CVType.CVFields.Contacto, cv.Contacto);
        options.Add(CVType.CVFields.ExpLaboral, cv.ExpLaboral);
        options.Add(CVType.CVFields.Formaciones, cv.Formaciones);
        options.Add(CVType.CVFields.Cursos, cv.Cursos);

        return options;
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
