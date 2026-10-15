using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections.Generic;
using System.Collections;

public class walking : MonoBehaviour
{
    private CharacterController controller;

    public float speed = 5.0f;
    public float turnSpeed = 180f;

    public InputAction movement;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        // Removemos o movement.Enable() daqui!
    }

    // O Unity chama isso sempre que o script for LIGADO (no início do jogo e ao sair do relógio)
    void OnEnable()
    {
        movement.Enable();
    }

    // O Unity chama isso sempre que o script for DESLIGADO (ao entrar no relógio)
    void OnDisable()
    {
        movement.Disable();
    }

    void Update()
    {
        Vector2 inputValor = movement.ReadValue<Vector2>();

        transform.Rotate(0, inputValor.x * turnSpeed * Time.deltaTime, 0);
        
        Vector3 movDir = transform.forward * inputValor.y * speed;

        controller.Move(movDir * Time.deltaTime - Vector3.up * 0.1f);
    }
}