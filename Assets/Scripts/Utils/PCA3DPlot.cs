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
    public int limitPlots = 1000;

    [Header("System Mode")]
    public bool useParticlesByDefault = true; // por defecto usar partículas
    public KeyCode toggleKey = KeyCode.Space; // tecla para alternar

    private GameObject container;       // contenedor de esferas
    private ParticleSystem particleSys; // referencia al sistema de partículas
    private List<Vector4> points;       // cache de puntos
    private bool showingParticles;      // modo actual

    void Start()
    {
        if (csvFile == null)
        {
            Debug.LogError("Debes asignar un archivo CSV en el inspector.");
            return;
        }

        points = LoadCSV(csvFile.text);

    
                ShowAsObjects();
        /*if (useParticlesByDefault)
            ShowAsParticles();
        else
            ShowAsObjects();*/
    }

    void Update()
    {

                
        // cambiar a partículas
               // ClearObjects();
               //ShowAsParticles();
        /*if (Input.GetKeyDown(toggleKey))
        {
            if (showingParticles)
            {
                // cambiar a objetos
                ClearParticles();
                ShowAsObjects();
            }
            else
            {
                // cambiar a partículas
                ClearObjects();
                ShowAsParticles();
            }
        }*/
    }

    // ========================
    // Mostrar con partículas
    // ========================
    void ShowAsParticles()
    {
        if (particleSys == null)
        {
            GameObject go = new GameObject("PCA3DParticles");
            particleSys = go.AddComponent<ParticleSystem>();

            var main = particleSys.main;
            main.startSize = pointSize;
            main.maxParticles = points.Count;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = Mathf.Infinity;

            var emission = particleSys.emission;
            emission.enabled = false; 

            var renderer = particleSys.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = spherePrefab.GetComponent<MeshFilter>().sharedMesh;
            renderer.material = spherePrefab.GetComponent<Renderer>().sharedMaterial;
        }

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            Vector4 p = points[i];
            Vector3 pos = new Vector3(p.x, p.y, p.z) * scale;

            particles[i].position = pos;
            particles[i].startSize = pointSize;
            particles[i].startLifetime = 999999f;
            particles[i].remainingLifetime = 999999f;

            Color color = Color.HSVToRGB((p.w % 10) / 10f, 1f, 1f);
            particles[i].startColor = color;
        }

        particleSys.SetParticles(particles, particles.Length);
        particleSys.Play();

        showingParticles = true;
    }

    void ClearParticles()
    {
        if (particleSys != null)
        {
            Destroy(particleSys.gameObject);
            particleSys = null;
        }
    }

    // =====================
    // Mostrar con objetos
    // =====================
    void ShowAsObjects()
    {
        if (spherePrefab == null)
        {
            Debug.LogError("Debes asignar un prefab de esfera en el inspector.");
            return;
        }

        container = new GameObject("PCA3DObjects");

        foreach (var p in points)
        {
            Vector3 position = new Vector3(p.x, p.y, p.z) * scale;
            GameObject sphere = Instantiate(spherePrefab, position, Quaternion.identity, container.transform);
            sphere.transform.localScale = Vector3.one * pointSize;

            Renderer rend = sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                Color color = Color.HSVToRGB((p.w % 10) / 10f, 1f, 1f);
                rend.material.color = color;
            }
        }

        showingParticles = false;
    }

    void ClearObjects()
    {
        if (container != null)
        {
            Destroy(container);
            container = null;
        }
    }

    // ====================
    // Cargar CSV a lista
    // ====================
    List<Vector4> LoadCSV(string csvText)
    {
        List<Vector4> result = new List<Vector4>();

        using (StringReader reader = new StringReader(csvText))
        {
            string line;
            bool isHeader = true;
            int limitCount = 0;

            while ((line = reader.ReadLine()) != null && limitPlots > limitCount)
            {
                if (isHeader)
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

                result.Add(new Vector4(pca1, pca2, pca3, cluster));
                limitCount++;
            }
        }

        return result;
    }
}
