using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum GhostState
{
    Idle,   // inactivo: antes de Activar() o despues de atrapar al jugador
    Patrol, // Ronda
    Sus,    // Sospecha
    Chase,  // Persecucion
    Lost    // Perdida
}

// El fantasma atraviesa paredes: se mueve por transform y sus colliders son trigger.
// Solo se ve cuando la camara esta activa (lo controla Camara.cs con la capa del fantasma).
// Nunca se destruye ni se desactiva por culpa de la linterna o la camara.
[RequireComponent(typeof(Rigidbody))]
public class Fantasma : MonoBehaviour
{
    const string TagFantasma = "Ghost";

    [Header("Referencias")]
    [SerializeField] Transform jugador;          // cabeza del jugador; si queda vacio usa Camera.main
    [SerializeField] Transform[] puntosRuta;     // un punto por habitacion
    [SerializeField] string nombreCapa = "Fantasma";

    [Header("Estado")]
    [SerializeField] GhostState estadoActual = GhostState.Idle;
    [SerializeField] bool activarAlIniciar = true;

    [Header("Velocidades (m/s)")]
    [SerializeField] float velocidadRonda = 1.2f;
    [SerializeField] float velocidadSospecha = 2f;
    [SerializeField] float velocidadPersecucion = 3.5f;   // algo menor que el jugador corriendo

    [Header("Ronda")]
    [SerializeField] Vector2 esperaEnPunto = new Vector2(3f, 8f);   // min y max de segundos

    [Header("Sospecha")]
    [SerializeField] float radioSospecha = 8f;
    [SerializeField, Tooltip("Segundos minimos en Sospecha antes de poder pasar a Persecucion (aviso al jugador)")]
    float avisoMinimo = 3f;
    [SerializeField, Tooltip("Pasado el aviso, si el jugador esta a esta distancia empieza la persecucion")]
    float radioPersecucion = 5f;
    [SerializeField] float esperaAlLlegar = 3f;        // tiempo que espera en el punto sospechoso
    [SerializeField] float enfriamientoSospecha = 5f;  // evita entrar en Sospecha en bucle
    [SerializeField] float ventanaTrasLlave = 10f;     // sin Sospecha tras recoger una llave

    [Header("Persecucion y perdida")]
    [SerializeField] float distanciaCaptura = 0.6f;
    [SerializeField] float distanciaPerdida = 15f;
    [SerializeField] float tiempoPerdida = 6f;         // segundos lejos para perder el rastro
    [SerializeField] float duracionPerdida = 2f;       // pausa antes de volver a Ronda
    [SerializeField] Vector2 calmaTrasPersecucion = new Vector2(30f, 45f);

    [Header("Aparicion")]
    [SerializeField] float distanciaMinimaAparicion = 10f;   // nunca aparece frente al jugador

    [Header("Eventos")]
    [SerializeField] UnityEvent alAtraparJugador;   // aqui conectas pantalla negra, sonido y respawn

    public GhostState Estado => estadoActual;
    public event System.Action<GhostState> EstadoCambio;   // para audio, luces y estatica de la camara
    public float DistanciaAlJugador =>
        jugador != null ? DistanciaPlano(transform.position, jugador.position) : float.MaxValue;

    readonly List<int> bolsa = new List<int>();
    int ultimoIndice = -1;
    Vector3 destino;
    Vector3 puntoSospecha;
    bool esperando;
    bool llegoAlPunto;
    float temporizador;
    float tiempoEnSospecha;
    float tiempoLejos;
    float bloqueoSospechaHasta;

    void Awake()
    {
        if (jugador == null && Camera.main != null) jugador = Camera.main.transform;
        if (puntosRuta == null || puntosRuta.Length == 0)
            Debug.LogWarning("Fantasma: no hay puntosRuta asignados", this);

        Configurar();
    }

    void Start()
    {
        if (activarAlIniciar) Activar();
    }

    // Tag, capa y fisica. Sus colliders son trigger para atravesar todo sin empujar nada.
    void Configurar()
    {
        gameObject.tag = TagFantasma;

        int capa = LayerMask.NameToLayer(nombreCapa);
        if (capa >= 0) AplicarCapa(transform, capa);
        else Debug.LogWarning($"Fantasma: no existe la capa '{nombreCapa}'", this);

        var rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        foreach (var c in GetComponentsInChildren<Collider>(true)) c.isTrigger = true;
    }

    static void AplicarCapa(Transform t, int capa)
    {
        t.gameObject.layer = capa;
        foreach (Transform hijo in t) AplicarCapa(hijo, capa);
    }

    // ---------- API publica ----------

    // Lo enciende lejos del jugador y empieza la Ronda
    public void Activar()
    {
        int idx = ElegirIndiceAparicion();
        if (idx >= 0)
        {
            transform.position = puntosRuta[idx].position;
            ultimoIndice = idx;
        }

        bolsa.Clear();
        EntrarRonda();
    }

    public void Desactivar() => CambiarEstado(GhostState.Idle);

    // Puertas, correr, etc. Lo llaman otros scripts con la posicion del ruido
    public void OirRuido(Vector3 punto)
    {
        if (estadoActual != GhostState.Patrol && estadoActual != GhostState.Sus) return;
        if (Time.time < bloqueoSospechaHasta) return;
        EntrarSospecha(punto);
    }

    // Llamar al recoger una llave: abre una ventana segura sin Sospecha
    public void NotificarLlaveRecogida()
    {
        bloqueoSospechaHasta = Mathf.Max(bloqueoSospechaHasta, Time.time + ventanaTrasLlave);
    }

    // ---------- Maquina de estados ----------

    void Update()
    {
        if (jugador == null)
        {
            if (Camera.main == null) return;
            jugador = Camera.main.transform;
        }

        switch (estadoActual)
        {
            case GhostState.Patrol: ActualizarRonda(); break;
            case GhostState.Sus:    ActualizarSospecha(); break;
            case GhostState.Chase:  ActualizarPersecucion(); break;
            case GhostState.Lost:   ActualizarPerdida(); break;
        }
    }

    void CambiarEstado(GhostState nuevo)
    {
        if (estadoActual == nuevo) return;
        estadoActual = nuevo;
        EstadoCambio?.Invoke(nuevo);
    }

    // Ronda: va de habitacion en habitacion y se detiene en cada una
    void EntrarRonda()
    {
        CambiarEstado(GhostState.Patrol);
        esperando = false;
        ElegirSiguientePunto();
    }

    void ActualizarRonda()
    {
        if (DetectaJugadorCerca()) return;

        if (esperando)
        {
            temporizador -= Time.deltaTime;
            if (temporizador <= 0f)
            {
                esperando = false;
                ElegirSiguientePunto();
            }
            return;
        }

        if (Mover(destino, velocidadRonda))
        {
            esperando = true;
            temporizador = Random.Range(esperaEnPunto.x, esperaEnPunto.y);
        }
    }

    bool DetectaJugadorCerca()
    {
        if (Time.time < bloqueoSospechaHasta) return false;
        if (DistanciaAlJugador > radioSospecha) return false;

        EntrarSospecha(jugador.position);
        return true;
    }

    // Sospecha: va al punto donde estaba el jugador o donde sono el ruido
    void EntrarSospecha(Vector3 punto)
    {
        puntoSospecha = Plano(punto);
        llegoAlPunto = false;

        if (estadoActual == GhostState.Sus) return;   // ya estaba: solo cambia el punto

        tiempoEnSospecha = 0f;
        CambiarEstado(GhostState.Sus);
    }

    void ActualizarSospecha()
    {
        tiempoEnSospecha += Time.deltaTime;

        // Nunca hay persecucion sin al menos avisoMinimo segundos de aviso
        if (tiempoEnSospecha >= avisoMinimo && DistanciaAlJugador <= radioPersecucion)
        {
            EntrarPersecucion();
            return;
        }

        if (!llegoAlPunto)
        {
            llegoAlPunto = Mover(puntoSospecha, velocidadSospecha);
            if (llegoAlPunto) temporizador = esperaAlLlegar;
            return;
        }

        temporizador -= Time.deltaTime;
        if (temporizador <= 0f)
        {
            bloqueoSospechaHasta = Mathf.Max(bloqueoSospechaHasta, Time.time + enfriamientoSospecha);
            EntrarRonda();
        }
    }

    // Persecucion: directo al jugador, atravesando paredes
    void EntrarPersecucion()
    {
        tiempoLejos = 0f;
        CambiarEstado(GhostState.Chase);
    }

    void ActualizarPersecucion()
    {
        Mover(Plano(jugador.position), velocidadPersecucion);

        float d = DistanciaAlJugador;
        if (d <= distanciaCaptura)
        {
            Capturar();
            return;
        }

        if (d >= distanciaPerdida)
        {
            tiempoLejos += Time.deltaTime;
            if (tiempoLejos >= tiempoPerdida) EntrarPerdida();
        }
        else tiempoLejos = 0f;
    }

    void Capturar()
    {
        CambiarEstado(GhostState.Idle);
        alAtraparJugador?.Invoke();
    }

    // Perdida: pierde el rastro, se queda quieto un momento y vuelve a la Ronda con calma
    void EntrarPerdida()
    {
        temporizador = duracionPerdida;
        CambiarEstado(GhostState.Lost);
    }

    void ActualizarPerdida()
    {
        temporizador -= Time.deltaTime;
        if (temporizador > 0f) return;

        bloqueoSospechaHasta = Time.time + Random.Range(calmaTrasPersecucion.x, calmaTrasPersecucion.y);
        EntrarRonda();
    }

    // ---------- Rutas y movimiento ----------

    // Orden semi-aleatorio: recorre todos los puntos en orden barajado, sin repetir el ultimo
    void ElegirSiguientePunto()
    {
        if (puntosRuta == null || puntosRuta.Length == 0)
        {
            destino = transform.position;   // sin ruta: se queda en su sitio
            return;
        }

        if (bolsa.Count == 0) RellenarBolsa();

        int idx = bolsa[bolsa.Count - 1];
        bolsa.RemoveAt(bolsa.Count - 1);
        ultimoIndice = idx;
        destino = puntosRuta[idx].position;
    }

    void RellenarBolsa()
    {
        for (int i = 0; i < puntosRuta.Length; i++) bolsa.Add(i);

        for (int i = bolsa.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (bolsa[i], bolsa[j]) = (bolsa[j], bolsa[i]);
        }

        int ultimo = bolsa.Count - 1;
        if (bolsa.Count > 1 && bolsa[ultimo] == ultimoIndice)
            (bolsa[ultimo], bolsa[0]) = (bolsa[0], bolsa[ultimo]);
    }

    int ElegirIndiceAparicion()
    {
        if (puntosRuta == null || puntosRuta.Length == 0) return -1;

        var lejanos = new List<int>();
        int masLejano = 0;
        float mayor = -1f;

        for (int i = 0; i < puntosRuta.Length; i++)
        {
            float d = jugador != null
                ? DistanciaPlano(puntosRuta[i].position, jugador.position)
                : float.MaxValue;

            if (d >= distanciaMinimaAparicion) lejanos.Add(i);
            if (d > mayor) { mayor = d; masLejano = i; }
        }

        return lejanos.Count > 0 ? lejanos[Random.Range(0, lejanos.Count)] : masLejano;
    }

    // Mueve en linea recta, sin colisiones. Devuelve true al llegar.
    bool Mover(Vector3 objetivo, float velocidad)
    {
        transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidad * Time.deltaTime);

        Vector3 dir = objetivo - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            Quaternion giro = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, giro, 5f * Time.deltaTime);
        }

        return (transform.position - objetivo).sqrMagnitude < 0.0004f;
    }

    // Proyecta un punto a la altura actual del fantasma (casa de una sola planta)
    Vector3 Plano(Vector3 p)
    {
        p.y = transform.position.y;
        return p;
    }

    static float DistanciaPlano(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    // Ayuda para el tuning: muestra los radios al seleccionar el fantasma
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioSospecha);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, radioPersecucion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaPerdida);
    }
}
