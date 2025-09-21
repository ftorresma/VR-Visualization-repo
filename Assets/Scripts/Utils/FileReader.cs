using System;
using UnityEngine;

public class FileReader : MonoBehaviour
{
    public TextAsset csvFile;  // Asigna tu CSV en el inspector

    void Start()
    {
        if (csvFile == null)
        {
            Debug.LogError("No se asignó el CSV en el inspector.");
            return;
        }

        // Leer todo el texto
        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        Debug.Log($"El CSV tiene {lines.Length} filas");

        // Mostrar las primeras 5 filas en consola
        for (int i = 0; i < Mathf.Min(5, lines.Length); i++)
        {
            Debug.Log($"Fila {i}: {lines[i]}");
        }
    }
}
