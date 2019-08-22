using CircularBuffer;
using Source;
using UnityEngine;
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshAudio : MonoBehaviour {  
    // Unity Inspector
    [Header("Mesh")]
    public int GridSize = 128;
    public float HeightScale = 128;
    public int RoadExtent = 16;
    public AnimationCurve HeightCurve;
    public AnimationCurve RoadCurve;
    
    [Header("Sampling")]
    public int UpdateInterval;
    public float LerpFactor;

    [Header("Rendering")] 
    public Material HeightMaterial;
    public Gradient HeightGradient;
    public Texture2D HeightTexture;
    
    // Circular Samples Buffer
    private CircularBuffer<float> _offsets;
    
    // Required components.
    private AudioSource _audioSource;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    
    // Work buffers.
    private Vector3[] _vertexBuffer;

    private float[] _samplePrev;
    private float[] _sampleNew;
    
    // Mesh instance.
    private Mesh _mesh;
    
    // Road min/max
    private int _roadMin, _roadMax;
    
    // Start is called before the first frame update
    private void Start() {
        // Reference required components.
        _audioSource = GetComponent<AudioSource>();
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        
        // Generate gradient texture.
        var heightTex = Utility.GenerateGradientTexture(HeightGradient, 128);
        
        // Set mesh renderer material.
        _meshRenderer.material = HeightMaterial;
        _meshRenderer.material.SetTexture("_MainTex", heightTex);
        _meshRenderer.material.SetFloat("_MaxHeight", HeightScale);
        
        // Calculate road min/max.
        _roadMin = (GridSize / 2) - RoadExtent;
        _roadMax = (GridSize / 2) + RoadExtent;
        
        _mesh = Utility.GenerateGrid(GridSize - 1, GridSize - 1, true);
        
        // Mark mesh as dynamic for realtime edits.
        _offsets = new CircularBuffer<float>(GridSize * GridSize);
        _vertexBuffer = _mesh.vertices;
        
        
        _samplePrev = new float[GridSize];
        _sampleNew = new float[GridSize];
        
        _meshFilter.sharedMesh = _mesh;

        HeightTexture = Utility.GenerateGradientTexture(HeightGradient, 128);

        for (var i = 0; i < _offsets.Capacity; i++) {
            _offsets.PushFront(0f);
        }
    }

    // Update is called once per frame
    private void Update() {
        var frame = 1;
        
        // Load latest sample into sample buffer.
        if (Time.frameCount % UpdateInterval == 0) {
            _audioSource.GetOutputData(_sampleNew, 1);
        }

        // Push sample buffer to offset buffer.
        for (var i = 0; i < _sampleNew.Length; i++) {
            var sample = Mathf.Lerp(_samplePrev[i], _sampleNew[i], Time.deltaTime * LerpFactor);
            
            // Calculate road min/max
            if (i > _roadMin && i < _roadMax) {
                sample *= RoadCurve.Evaluate((i - _roadMin) / (float)(_roadMax - _roadMin));
            }
            _offsets.PushFront(sample);
            _samplePrev[i] = sample;
        }
        
        for (var i = 0; i < _offsets.Size; i++) {
            var vertex = _vertexBuffer[i];
            vertex.y = _offsets[i] * HeightScale * HeightCurve.Evaluate((_offsets[i] + 0.5f));
            _vertexBuffer[i] = vertex;
        }

        _mesh.vertices = _vertexBuffer;
        _mesh.RecalculateNormals();

        frame = -frame;
    }
}
