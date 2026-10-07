using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class Jogador : MonoBehaviour
{
    public float velocidade = 3f;
    public float gravidade = -9.81f;
    public float velocidadeGiro = 12f;

    [FormerlySerializedAs("cameraRef")]
    public Transform eixo;

    CharacterController cc;
    float velY;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        var kb = Keyboard.current;
        float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        float z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);

        Vector3 frente = eixo.forward; frente.y = 0; frente.Normalize();
        Vector3 direita = eixo.right;  direita.y = 0; direita.Normalize();

        Vector3 mov = (frente * z + direita * x).normalized;
        Vector3 dir = mov * velocidade;

        if (cc.isGrounded && velY < 0) velY = -1f;
        velY += gravidade * Time.deltaTime;
        dir.y = velY;

        cc.Move(dir * Time.deltaTime);

        if (mov != Vector3.zero)
        {
            Quaternion alvo = Quaternion.LookRotation(mov);
            transform.rotation = Quaternion.Slerp(transform.rotation, alvo, velocidadeGiro * Time.deltaTime);
        }
    }
}