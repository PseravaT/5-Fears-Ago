using UnityEngine;
using Unity.Cinemachine;
using System.Collections.Generic;
using System.Collections;

public class cameraChange : MonoBehaviour
{

    public Transform player;
    public CinemachineCamera cameraAtiva;

private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cameraAtiva.Priority = 1;
        }
    }
    
private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            cameraAtiva.Priority = 0;
        }
    }
}
