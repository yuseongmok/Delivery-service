using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RobberAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform targetPlayer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.areaMask = NavMesh.AllAreas;
    }

    public void SetTarget(Transform player)
    {
        targetPlayer = player;
    }

    private void Update()
    {
        if (targetPlayer != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(targetPlayer.position);
        }
    }
}