using UnityEngine;

namespace Scripts {
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class MeshAudio : MonoBehaviour {  
        // Unity Inspector
        [Header("Mesh")]
        public int SampleCount = 128;
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
    
        // Circular Samples Buffer
        private CircularBuffer<float> _offsets;
    
        // Required components.
        private AudioSource _audioSource;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
    
        // Work buffers.
        private Vector3[] _vertexBuffer;
        
        // Sample data buffers.
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
        
            // Calculate road min/max row indices.
            // This is so we can apply the road curve to the vertices.
            _roadMin = (SampleCount / 2) - RoadExtent;
            _roadMax = (SampleCount / 2) + RoadExtent;
            
            // Generate the grid mesh.
            // We pass in Count - 1 for the dimensions as the number of vertices per side is the dimension + 1.
            // We do this because we want the number of waveform samples and vertices per row to be equal.
            // Yes, this does technically mean the grid ends up being 1 unit short of what one may expect.
            _mesh = Utility.GenerateGrid(SampleCount - 1, SampleCount - 1, true);
        
            // Initialize the offset buffer.
            // The capacity is SampleCount ^ 2 or the total number of vertices.
            _offsets = new CircularBuffer<float>(SampleCount * SampleCount);
        
            // Get vertex data from the grid mesh.
            _vertexBuffer = _mesh.vertices;
        
            // Initialize sample buffers.
            // We use two buffers so that we can perform some interpolation on the data.
            // Most audio sources will be a bit too chaotic to form decent terrain otherwise.
            _samplePrev = new float[SampleCount];
            _sampleNew = new float[SampleCount];
        
            // Assign the mesh instance.
            _meshFilter.sharedMesh = _mesh;
        
            // Prime the offset buffer to avoid alignment issues.
            // If we try to iterate a non-full buffer, we will only apply offsets to some of the mesh.
            // This seems to cause alignment issues and the easiest solution I found was to just prime the buffer.
            for (var i = 0; i < _offsets.Capacity; i++) {
                _offsets.PushFront(0f);
            }
        }

        // Update is called once per frame
        private void Update() {       
            // Load latest sample data into the new sample buffer.
            // Using modulus here allows us to sample every Nth frame.
            // Depending on your sample count, this can be an expensive operation.
            // This also helps smooth the data a bit more as interpolation still runs between sample updates.
            if (Time.frameCount % UpdateInterval == 0) {
                _audioSource.GetOutputData(_sampleNew, 1);
            }

            // Push modified sample data into the offset buffer.
            for (var i = 0; i < _sampleNew.Length; i++) {
                // Interpolate between the previous and incoming sample data.
                var sample = Mathf.Lerp(_samplePrev[i], _sampleNew[i], Time.deltaTime * LerpFactor);
            
                // Apply the road curve.
                if (i > _roadMin && i < _roadMax) {
                    sample *= RoadCurve.Evaluate((i - _roadMin) / (float)(_roadMax - _roadMin));
                }
            
                // Push the new sample to the offset buffer.
                _offsets.PushFront(sample);
            
                // Update the previous sample data for the next frame.
                _samplePrev[i] = sample;
            }
        
            // Apply all offsets to the mesh vertices.
            for (var i = 0; i < _offsets.Size; i++) {
                var vertex = _vertexBuffer[i];
                
                // Sample data appears to be within [-0.5, 0.5].
                // Shifting it up by 0.5 gives us a 0-1 value to use with our curve.
                // I could be wrong here, I haven't really validated this beyond logging a few random samples.
                vertex.y = _offsets[i] * HeightScale * HeightCurve.Evaluate((_offsets[i] + 0.5f));
                _vertexBuffer[i] = vertex;
            }
        
            // Update vertex changes in the mesh.
            _mesh.vertices = _vertexBuffer;
        
            // Recalculate normals for proper lighting and shading.
            // While we could cheat a bit for a plane, a complex mesh is a bit more difficult.
            // Best to just let Unity handle normals here.
            _mesh.RecalculateNormals();
        }
    }
}
