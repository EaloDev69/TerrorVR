using UnityEngine;

enum GhostState
{
    Idle, 
    Patrol,
    Sus,
    Chase,
    Lost
}
public class Fantasma : MonoBehaviour
{
    [SerializeField] GhostState estadoActual = GhostState.Idle;
    
}
