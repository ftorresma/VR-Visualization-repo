using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class PCA3DPlot : MonoBehaviour
{
    [Header("CSV Settings")]
    public TextAsset csvFile;

    [Header("Plot Settings")]
    public GameObject spherePrefab;
    public float pointSize = 0.2f;
    public float scale = 5f;

    private GameObject container;

    void Start()
    {
        if (csvFile == null)
        {
            Debug.LogError("Debes asignar un archivo CSV en el inspector.");
            return;
        }

        if (spherePrefab == null)
        {
            Debug.LogError("Debes asignar un prefab de esfera en el inspector.");
            return;
        }

        //Crear contenedor
        container = new GameObject("PCA3DPoints");

        //Parsear CSV
        List<Vector4> points = LoadCSV(csvFile.text);

        //Instanciar esferas
        foreach (var p in points)
        {
            Vector3 position = new Vector3(p.x, p.y, p.z) * scale;
            GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity, container.transform);

            sphere.transform.localScale = Vector3.one * pointSize;

            //Color por cluster
            Renderer rend = sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                Color color = Color.HSVToRGB((p.w % 10) / 10f, 1f, 1f);
                rend.material.color = color;
            }

            //por aqui meter la interaccion con las esferas (prximamente)
        }
    }

    List<Vector4> LoadCSV(string csvText)
    {
        List<Vector4> points = new List<Vector4>();

        using (StringReader reader = new StringReader(csvText))
        {
            string line;
            bool isHeader = true;

            while ((line = reader.ReadLine()) != null)
            {
                if (isHeader) //saltar primera fila
                {
                    isHeader = false;
                    continue;
                }

                string[] values = line.Split(',');

                if (values.Length < 5)
                    continue;

                float pca1 = float.Parse(values[1], CultureInfo.InvariantCulture);
                float pca2 = float.Parse(values[2], CultureInfo.InvariantCulture);
                float pca3 = float.Parse(values[3], CultureInfo.InvariantCulture);
                int cluster = int.Parse(values[4]);

                points.Add(new Vector4(pca1, pca2, pca3, cluster));
            }
        }

        return points;
    }
}
