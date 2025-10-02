using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public enum RenderMode
{
    Particles,
    Objects,
    Circles
}

public class PCA3DPlot : MonoBehaviour
{
    [Header("CSV Settings")]
    public TextAsset csvFile;

    [Header("Plot Settings")]
    public GameObject spherePrefab;
    public float pointSize = 0.2f;
    public float scale = 5f;
    public int limitPlots = 1000;

    [Header("Circle Billboard Settings")]
    public Material circleMaterial; // Shader "Flat/CircleBillboard"
    public Mesh quadMesh; // Asignar el mesh Quad de Unity

    [Header("System Mode")]
    public KeyCode toggleKey = KeyCode.Space;
    public RenderMode startMode = RenderMode.Objects;

    private GameObject container;
    private ParticleSystem particleSys;
    private List<Vector4> points;
    private List<Matrix4x4[]> matrixBatches;
    private const int batchSize = 1023;
    private RenderMode currentMode;
    private HashSet<int> hiddenIndices = new HashSet<int>();

    void Start()
    {
        if (csvFile == null)
        {
            Debug.LogError("Debes asignar un archivo CSV en el inspector.");
            return;
        }

        if (circleMaterial != null)
            circleMaterial.enableInstancing = true;

        points = LoadCSV(csvFile.text);

        currentMode = startMode;
        switch (startMode)
        {
            case RenderMode.Particles: ShowAsParticles(); break;
            case RenderMode.Objects: ShowAsObjects(); break;
            case RenderMode.Circles: ShowAsCircles(); break;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ClearAll();
            currentMode = (RenderMode)(((int)currentMode + 1) % 3);

            switch (currentMode)
            {
                case RenderMode.Particles:
                    ShowAsParticles();
                    Debug.Log("Modo: Partículas");
                    break;
                case RenderMode.Objects:
                    ShowAsObjects();
                    Debug.Log("Modo: Objetos");
                    break;
                case RenderMode.Circles:
                    ShowAsCircles();
                    Debug.Log("Modo: Círculos (Shader)");
                    break;
            }
        }

        if (currentMode == RenderMode.Circles && drawBatches != null && drawBatches.Count > 0)
        {
            DrawMeshInstances(circleMaterial, quadMesh);
            UpdateInteractive();
        }
    }

    // ======================== PARTICLES ========================
    void ShowAsParticles()
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

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            Vector4 p = points[i];
            Vector3 pos = new Vector3(p.x, p.y, p.z) * scale;

            particles[i].position = pos;
            particles[i].startSize = pointSize;
            particles[i].startLifetime = float.MaxValue;

            Color color = Color.HSVToRGB((p.w % 10) / 10f, 1f, 1f);
            particles[i].startColor = color;
        }

        particleSys.SetParticles(particles, particles.Length);
        particleSys.Play();

        currentMode = RenderMode.Particles;
    }

    void ClearParticles()
    {
        if (particleSys != null)
        {
            Destroy(particleSys.gameObject);
            particleSys = null;
        }
    }

    // ===================== OBJECTS =====================
    void ShowAsObjects()
    {
        container = new GameObject("PCA3DObjects");

        foreach (var p in points)
        {
            Vector3 pos = new Vector3(p.x, p.y, p.z) * scale;
            GameObject sphere = Instantiate(spherePrefab, pos, Quaternion.identity, container.transform);
            sphere.transform.localScale = Vector3.one * pointSize;

            Renderer rend = sphere.GetComponent<Renderer>();
            if (rend != null)
            {
                Color color = Color.HSVToRGB((p.w % 10) / 10f, 1f, 1f);
                rend.material.color = color;
            }
        }

        currentMode = RenderMode.Objects;
    }

    void ClearObjects()
    {
        if (container != null)
        {
            Destroy(container);
            container = null;
        }
    }

    // ===================== CIRCLES (Shader) =====================
    void ShowAsCircles()
    {
        if (circleMaterial == null || quadMesh == null)
        {
            Debug.LogError("Asigna el material con shader 'Flat/CircleBillboard' y el Mesh Quad.");
            return;
        }

        circleMaterial.enableInstancing = true;
        circleMaterial.SetFloat("_PointSize", pointSize * 0.5f);
        circleMaterial.SetFloat("_DistanceScale", 1f / Mathf.Max(0.001f, scale));

        hiddenIndices.Clear();
        PrepareDrawBatches();
        currentMode = RenderMode.Circles;
    }

    void DrawMeshInstances(Material mat, Mesh mesh)
    {
        if (drawBatches == null || drawBatches.Count == 0) return;

        if (!mat.enableInstancing) mat.enableInstancing = true;

        for (int i = 0; i < drawBatches.Count; i++)
        {
            var batch = drawBatches[i];
            Graphics.DrawMeshInstanced(mesh, 0, mat, batch.matrices, batch.matrices.Length, batch.props);
        }
    }

    struct DrawBatch
    {
        public Matrix4x4[] matrices;
        public MaterialPropertyBlock props;
    }

    List<DrawBatch> drawBatches = new List<DrawBatch>();

    void PrepareDrawBatches()
    {
        drawBatches.Clear();

        int total = points.Count;
        int processed = 0;

        while (processed < total)
        {
            int count = Mathf.Min(batchSize, total - processed);
            Matrix4x4[] matrices = new Matrix4x4[count];
            Vector4[] colorArray = new Vector4[count];
            int validCount = 0;

            for (int i = 0; i < count; i++)
            {
                int idx = processed + i;
                if (hiddenIndices.Contains(idx))
                    continue;

                Vector4 p = points[idx];
                Vector3 pos = new Vector3(p.x, p.y, p.z) * scale;
                matrices[validCount] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                Color c = Color.HSVToRGB((p.w % 10) / 10f, 1f, 1f);
                colorArray[validCount] = new Vector4(c.r, c.g, c.b, c.a);
                validCount++;
            }

            if (validCount > 0)
            {
                Matrix4x4[] mTrim = new Matrix4x4[validCount];
                Vector4[] cTrim = new Vector4[validCount];
                System.Array.Copy(matrices, 0, mTrim, 0, validCount);
                System.Array.Copy(colorArray, 0, cTrim, 0, validCount);

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetVectorArray("_InstanceColor", cTrim);

                DrawBatch db = new DrawBatch { matrices = mTrim, props = block };
                drawBatches.Add(db);
            }

            processed += count;
        }
    }

    // ===================== LIMPIEZA =====================
    void ClearCircles()
    {
        matrixBatches = null;
        drawBatches.Clear();
    }

    void ClearAll()
    {
        ClearParticles();
        ClearObjects();
        ClearCircles();
    }

    // ===================== LECTURA CSV =====================
    List<Vector4> LoadCSV(string csvText)
    {
        List<Vector4> result = new List<Vector4>();

        using (StringReader reader = new StringReader(csvText))
        {
            string line;
            bool isHeader = true;
            int limitCount = 0;

            while ((line = reader.ReadLine()) != null && limitCount < limitPlots)
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

    [SerializeField] private Transform proximityTarget;
    [SerializeField] private float interactDistance = 2.0f;

    private Dictionary<int, GameObject> activeObjects = new Dictionary<int, GameObject>();

    void UpdateInteractive()
    {
        if (proximityTarget == null) return;

        Vector3 targetPos = proximityTarget.position;
        int prevHiddenCount = hiddenIndices.Count;
        hiddenIndices.Clear();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 worldPos = new Vector3(points[i].x, points[i].y, points[i].z) * scale;
            float dist = Vector3.Distance(targetPos, worldPos);

            if (dist < interactDistance)
            {
                hiddenIndices.Add(i);
                if (!activeObjects.ContainsKey(i))
                {
                    GameObject obj = Instantiate(spherePrefab, worldPos, Quaternion.identity);
                    obj.transform.localScale = Vector3.one * pointSize;
                    Renderer rend = obj.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        Color c = Color.HSVToRGB((points[i].w % 10) / 10f, 1f, 1f);
                        rend.material.color = c;
                    }
                    activeObjects[i] = obj;
                }
            }
            else
            {
                if (activeObjects.ContainsKey(i))
                {
                    Destroy(activeObjects[i]);
                    activeObjects.Remove(i);
                }
            }
        }

        if (hiddenIndices.Count != prevHiddenCount)
            PrepareDrawBatches();
    }
}
