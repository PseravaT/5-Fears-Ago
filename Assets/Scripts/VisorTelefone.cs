using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

public class VisorTelefone : MonoBehaviour
{
    public TMP_Text linha1;   // faixa de cima do visor
    public TMP_Text linha2;   // faixa de baixo (opcional)

    [Header("Repouso")]
    public string horaFixa = "03:07";    // vazio = hora real do PC
    public string dataFixa = "QUI 12/10"; // vazio = data real do PC

    [Header("Tempos (segundos)")]
    public float intervaloPisca = 0.5f;

    Coroutine rotina;

    void Start() => Repouso();

    void Parar()
    {
        if (rotina != null) StopCoroutine(rotina);
        rotina = null;
    }

    public void Repouso()
    {
        Parar();
        var pt = new CultureInfo("pt-BR");
        string hora = string.IsNullOrEmpty(horaFixa) ? System.DateTime.Now.ToString("HH:mm") : horaFixa;
        string data = string.IsNullOrEmpty(dataFixa) ? System.DateTime.Now.ToString("ddd dd/MM", pt).ToUpper() : dataFixa;
        Mostrar(hora, data);
    }

    public void Chamando(string texto = "DESCONHECIDO", string sub = "NOVA CHAMADA")
    {
        Parar();
        rotina = StartCoroutine(Piscar(texto, sub));
    }

    public void NovaMensagem()
    {
        Parar();
        Mostrar("NOVA MSG", "01");
    }

    public void Apagar()
    {
        Parar();
        Mostrar("", "");
    }

    IEnumerator Piscar(string a, string b)
    {
        while (true)
        {
            Mostrar(a, b);
            yield return new WaitForSeconds(intervaloPisca);
            Mostrar("", b);
            yield return new WaitForSeconds(intervaloPisca);
        }
    }

    void Mostrar(string a, string b)
    {
        if (linha1) linha1.text = a;
        if (linha2) linha2.text = b;
    }
}