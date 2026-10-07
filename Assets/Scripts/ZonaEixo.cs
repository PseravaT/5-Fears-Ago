using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ZonaEixo : MonoBehaviour
{
    public Transform eixo;

    void Reset() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        var j = other.GetComponent<Jogador>();
        if (j != null) j.eixo = eixo;
    }
}