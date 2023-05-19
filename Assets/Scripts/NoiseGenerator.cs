using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator : MonoBehaviour
{
    ComputeBuffer _weightsBuffer; //this is the buffer that will hold the noise values from the GPU
    public ComputeShader NoiseShader;

    [SerializeField] float noiseScale = 0.08f;
    [SerializeField] float amplitude = 200;
    [SerializeField] float frequency = 0.004f;
    [SerializeField] int octaves = 6;
    [SerializeField, Range(0f, 1f)] float groundPercent = 0.2f;


    private void Awake() {
        CreateBuffers();
    }

    private void OnDestroy() {
        ReleaseBuffers();
    }

    public float[] GetNoise() {
        float[] noiseValues =
            new float[GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk];

        //Connect buffer to shader
        NoiseShader.SetBuffer(0, "_Weights", _weightsBuffer);

        //Set the shader variables
        NoiseShader.SetInt("_ChunkSize", GridMetrics.PointsPerChunk);
        NoiseShader.SetFloat("_NoiseScale", noiseScale);
        NoiseShader.SetFloat("_Amplitude", amplitude);
        NoiseShader.SetFloat("_Frequency", frequency);
        NoiseShader.SetInt("_Octaves", octaves);
        NoiseShader.SetFloat("_GroundPercent", groundPercent);

        //Execute
        NoiseShader.Dispatch(
            0, GridMetrics.PointsPerChunk / GridMetrics.NumThreads, GridMetrics.PointsPerChunk / GridMetrics.NumThreads, GridMetrics.PointsPerChunk / GridMetrics.NumThreads
        );

        //Read the data from the buffer into the array
        _weightsBuffer.GetData(noiseValues);

        return noiseValues;
    }
    
    void CreateBuffers() {
        _weightsBuffer = new ComputeBuffer(
            GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk * GridMetrics.PointsPerChunk, sizeof(float)
        );
    }

    void ReleaseBuffers() {
        _weightsBuffer.Release();
    }
}
