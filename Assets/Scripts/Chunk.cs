using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public ComputeShader MarchingShader;
    public MeshFilter MeshFilter;
    public MeshCollider MeshCollider;
    Mesh _mesh;

    //buffers to transfer data between the CPU and the GPU
    ComputeBuffer _trianglesBuffer;
    ComputeBuffer _trianglesCountBuffer;
    ComputeBuffer _weightsBuffer;

    public NoiseGenerator NoiseGenerator;

    private void Awake() {
        CreateBuffers();
    }

    private void OnDestroy() {
        ReleaseBuffers();
    }

    struct Triangle {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public static int SizeOf => sizeof(float) * 3 * 3;
    }

    float[] _weights;

    void Start() {
        _weights = NoiseGenerator.GetNoise();
        //MeshFilter.sharedMesh = ConstructMesh();
        _mesh = new Mesh();
        UpdateMesh();
    }
    //Contruct the mesh from the data in the buffers computed in the shaders
    Mesh ConstructMesh() {
        int kernel = MarchingShader.FindKernel("March");

        MarchingShader.SetBuffer(kernel, "_Triangles", _trianglesBuffer);
        MarchingShader.SetBuffer(kernel, "_Weights", _weightsBuffer);

        MarchingShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk);
        MarchingShader.SetFloat("_IsoLevel", .5f);

        _weightsBuffer.SetData(_weights);
        _trianglesBuffer.SetCounterValue(0);

        MarchingShader.Dispatch(kernel, GridMetrics.PointsPerChunk / GridMetrics.NumThreads, GridMetrics.PointsPerChunk / GridMetrics.NumThreads, GridMetrics.PointsPerChunk / GridMetrics.NumThreads);

        Triangle[] triangles = new Triangle[ReadTriangleCount()];
        _trianglesBuffer.GetData(triangles);

        return CreateMeshFromTriangles(triangles);
    }
    //Change weight for the point in the chunk that is affected by the brush
    public void EditWeights(Vector3 hitPosition, float brushSize, bool add)
    {
        int kernel = MarchingShader.FindKernel("UpdateWeights");

        _weightsBuffer.SetData(_weights);
        MarchingShader.SetBuffer(kernel, "_Weights", _weightsBuffer);
        MarchingShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk);
        MarchingShader.SetVector("_HitPosition", hitPosition);
        MarchingShader.SetFloat("_BrushSize", brushSize);
        MarchingShader.SetFloat("_TerraformStrength", add ? 1f : -1f);

        MarchingShader.Dispatch(kernel, GridMetrics.PointsPerChunk / GridMetrics.NumThreads, GridMetrics.PointsPerChunk / GridMetrics.NumThreads, GridMetrics.PointsPerChunk / GridMetrics.NumThreads);
        _weightsBuffer.GetData(_weights);

        UpdateMesh();
    }

    int ReadTriangleCount() {
        int[] triCount = { 0 };
        ComputeBuffer.CopyCount(_trianglesBuffer, _trianglesCountBuffer, 0);
        _trianglesCountBuffer.GetData(triCount);
        return triCount[0];
    }

    Mesh CreateMeshFromTriangles(Triangle[] triangles) {
        Vector3[] verts = new Vector3[triangles.Length * 3];
        int[] tris = new int[triangles.Length * 3];

        for (int i = 0; i < triangles.Length; i++) {
            int startIndex = i * 3;

            verts[startIndex] = triangles[i].a;
            verts[startIndex + 1] = triangles[i].b;
            verts[startIndex + 2] = triangles[i].c;

            tris[startIndex] = startIndex;
            tris[startIndex + 1] = startIndex + 1;
            tris[startIndex + 2] = startIndex + 2;
        }
        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.RecalculateNormals();
        return _mesh;
    }
    void UpdateMesh()
    {
        Mesh mesh = ConstructMesh();
        MeshFilter.sharedMesh = mesh;
        MeshCollider.sharedMesh = mesh;
    }

    //private void OnDrawGizmos() {
    //    if (_weights == null || _weights.Length == 0) {
    //        return;
    //    }
    //    for (int x = 0; x < GridMetrics.PointsPerChunk; x++) {
    //        for (int y = 0; y < GridMetrics.PointsPerChunk; y++) {
    //            for (int z = 0; z < GridMetrics.PointsPerChunk; z++) {
    //                int index = x + GridMetrics.PointsPerChunk * (y + GridMetrics.PointsPerChunk * z);
    //                float noiseValue = _weights[index];
    //                Gizmos.color = Color.Lerp(Color.black, Color.white, noiseValue);
    //                Gizmos.DrawCube(new Vector3(x, y, z), Vector3.one * .2f);
    //            }
    //        }
    //    }
    //}

    void CreateBuffers() {
        _trianglesBuffer = new ComputeBuffer(5 * (GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk), Triangle.SizeOf, ComputeBufferType.Append);
        _trianglesCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        _weightsBuffer = new ComputeBuffer(GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk, sizeof(float));
    }

    void ReleaseBuffers() {
        _trianglesBuffer.Release();
        _trianglesCountBuffer.Release();
        _weightsBuffer.Release();
    }
}
