using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hace crecer un radio de incendio forestal y consume (encoge y destruye) los árboles
/// que quedan dentro de ese radio, instanciando fuego/humo en cada árbol quemado.
/// </summary>
public class SimuladorIncendioVR : MonoBehaviour
{
    [Header("Incendio")]
    [SerializeField] private float temperaturaActual = 22f;
    [SerializeField] private float velocidadPropagacion = 1.5f;
    [SerializeField] private float intervaloEscaneo = 0.5f;

    [Header("Radios (metros de mundo)")]
    [SerializeField] private float radioMortal = 0.06f;
    [SerializeField] private float radioMaximoIncendio = 0.5f;
    [SerializeField] private float radioInicial = 0.02f;
    [SerializeField] private float crecimientoRadioPorSegundo = 0.03f;

    [Header("Peligro por proximidad")]
    [SerializeField] private bool usarPeligroPorProximidad = true;

    [Header("Referencias de cámara")]
    public Transform jugadorVR;

    [Header("Efectos")]
    public ParticleSystem particulasHumoOrigen;
    public GameObject prefabFuegoArbol;
    public GameObject prefabHumoArbol;

    [Header("Eventos")]
    public UnityEvent OnJugadorEnPeligro;
    public UnityEvent OnIncendioContenido;

    private float radioIncendioActual;
    private bool interactuado = false;
    private readonly HashSet<Collider> arbolesEnProceso = new HashSet<Collider>();
    private Coroutine propagacionCoroutine;
    private Coroutine escaneoCoroutine;

    void Start()
    {
        radioIncendioActual = radioInicial;

        if (prefabFuegoArbol != null)
            Instantiate(prefabFuegoArbol, transform.position, Quaternion.identity, transform);
        if (particulasHumoOrigen != null)
            particulasHumoOrigen.Play();

        propagacionCoroutine = StartCoroutine(BuclePropagacion());
        escaneoCoroutine = StartCoroutine(BucleEscaneoArboles());
    }

    void Update()
    {
        if (!usarPeligroPorProximidad || jugadorVR == null) return;

        float distancia = Vector3.Distance(jugadorVR.position, transform.position);
        if (distancia <= radioMortal)
        {
            interactuado = true;
            OnJugadorEnPeligro?.Invoke();
        }
    }

    IEnumerator BuclePropagacion()
    {
        while (radioIncendioActual < radioMaximoIncendio)
        {
            float multiplicador = interactuado ? 6f : 2f;
            temperaturaActual += velocidadPropagacion * Time.deltaTime * multiplicador;
            radioIncendioActual += crecimientoRadioPorSegundo * Time.deltaTime;
            yield return null;
        }

        OnIncendioContenido?.Invoke();
    }

    IEnumerator BucleEscaneoArboles()
    {
        var espera = new WaitForSeconds(intervaloEscaneo);
        while (radioIncendioActual < radioMaximoIncendio)
        {
            Collider[] objetosCercanos = Physics.OverlapSphere(transform.position, radioIncendioActual);
            foreach (Collider obj in objetosCercanos)
            {
                if (arbolesEnProceso.Contains(obj)) continue;

                if (obj.name.Contains("tree") || obj.name.Contains("Tree"))
                {
                    arbolesEnProceso.Add(obj);
                    StartCoroutine(QuemarseArbol(obj));
                }
            }
            yield return espera;
        }
    }

    IEnumerator QuemarseArbol(Collider arbolCollider)
    {
        Transform arbol = arbolCollider.transform;
        Vector3 escalaOriginal = arbol.localScale;
        Vector3 posicionArbol = arbol.position;
        Transform padreComun = arbol.parent;

        GameObject fuegoInstancia = null;
        if (prefabFuegoArbol != null)
        {
            fuegoInstancia = Instantiate(prefabFuegoArbol, posicionArbol, Quaternion.identity);
            if (padreComun != null) fuegoInstancia.transform.SetParent(padreComun, true);
        }
        if (prefabHumoArbol != null)
        {
            GameObject humoInstancia = Instantiate(prefabHumoArbol, posicionArbol, Quaternion.identity,
                        fuegoInstancia != null ? fuegoInstancia.transform : null);
            if (fuegoInstancia == null && padreComun != null) humoInstancia.transform.SetParent(padreComun, true);
        }

        float tiempo = 0f;
        while (tiempo < 3f && arbol != null)
        {
            arbol.localScale = Vector3.Lerp(escalaOriginal, Vector3.zero, tiempo / 3f);
            tiempo += Time.deltaTime;
            yield return null;
        }

        if (arbol != null) Destroy(arbol.gameObject);
        if (fuegoInstancia != null) Destroy(fuegoInstancia, 4f);
    }

    void OnDestroy()
    {
        if (propagacionCoroutine != null) StopCoroutine(propagacionCoroutine);
        if (escaneoCoroutine != null) StopCoroutine(escaneoCoroutine);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioIncendioActual);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioMortal);
    }
}
