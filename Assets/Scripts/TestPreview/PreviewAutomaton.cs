using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class PreviewAutomaton : MonoBehaviour
{
    public RawImage image;
    public RenderTexture mapDisplay;
    public ComputeShader CAShader;
    public ComputeShader projectorShader;
    private const int textureSize = 128;

    public enum CorrectionMode { On, Off };
    public enum Mode { CPU_Discrete, CPU_Continuous, GPU };
    public int chunkSize;

    [Space(10)]
    [Header("Automaton Configuration")]
    public Mode mode;
    public CorrectionMode correction;
    public int seed;
    public float P;
    public bool E;
    [Range(0, 8)] public float B;
    [Range(0, 8)] public float D;
    public int ITERATIONS;
    [Range(0,1)] public List<float>rulesVector = new List<float>();

    private Dictionary<Vector2Int, bool[,]> chunks = new Dictionary<Vector2Int, bool[,]>();

    [Space(10)]
    [Header("Display Settings")]
    [Range(0f, 1f)] public float scrollSensitivity;
    [Range(0f, 1f)] public float zoomSensitivity;

    public Vector2 mapOffset = new Vector2(-0.5f, -0.5f);
    public float zoomLevel = 1f;

    public void RandomizeRules()
    {
        rulesVector.Clear();
        for (int i = 0; i < 8; i++)
        {
            rulesVector.Add(UnityEngine.Random.value);
        }
    }

    public void ApplyRules()
    {
        chunks.Clear();
        LoadChunks();
        Draw();
    }

    public void LoadChunks()
    {
        float tilesPerTexLen = textureSize / zoomLevel;
        Vector2 floatChunkCoor = mapOffset * tilesPerTexLen / chunkSize;
        Vector2Int downLeftChunkPos = new Vector2Int(Round(floatChunkCoor.x), Round(floatChunkCoor.y));
        int chunksPerTexLen = Mathf.CeilToInt(tilesPerTexLen / chunkSize) + 1;
        for (int i = 0; i < chunksPerTexLen; i++)
        {
            for (int j = 0; j < chunksPerTexLen; j++)
            {
                GenerateChunk(downLeftChunkPos.x + i, downLeftChunkPos.y + j);
            }
        }
    }

    public void Scroll(Vector2 direction)
    {
        mapOffset += scrollSensitivity * direction;
    }
    
    public void Zoom(bool sign)
    {
        zoomLevel += sign ? zoomSensitivity : -zoomSensitivity;
        if (zoomLevel < 0.001f)
            zoomLevel = 0.001f;
    }

    public void _Reset()
    {
        mapOffset = Vector2.one * -0.5f;
        zoomLevel = 1;
    }

    public void Draw()
    {
        mapDisplay = new RenderTexture(textureSize, textureSize, 1);
        image.texture = mapDisplay;
        mapDisplay.enableRandomWrite = true;
        ComputeBuffer mapBuffer = new ComputeBuffer(chunkSize * chunkSize, sizeof(int));

        foreach (KeyValuePair<Vector2Int, bool[,]> element in chunks)
        {
            Vector2 chunkPos = WorldToDisplayPosition(element.Key);
            if (IsChunkOnDisplay(chunkPos))
                DispatchShader(chunkPos, mapBuffer, element.Value);
        }
        mapBuffer.Dispose();
    }



    private void GenerateChunk(int chunkX, int chunkY)
    {
        bool[,] tiles = GetChunk(chunkX, chunkY);
        if (tiles == null)
        {
            tiles = SimulateTilesGeneration(chunkX, chunkY);
            //tiles = Test(chunkX, chunkY);
            chunks[new Vector2Int(chunkX, chunkY)] = tiles;
        }
    }

    private int Round(float value)
    {
        //return value > 0 ? Mathf.CeilToInt(value) : Mathf.FloorToInt(value);
        return Mathf.FloorToInt(value);
    }

    private bool[,] GetChunk(int chunkX, int chunkY)
    {
        Vector2Int key = new Vector2Int(chunkX, chunkY);
        if (chunks.ContainsKey(key))
            return chunks[key];
        else
            return null;
    }

    private void DispatchShader(Vector2 chunkPos, ComputeBuffer mapBuffer, bool[,] map)
    {
        int kernelIndex = projectorShader.FindKernel("CSMain");
        int[,] blittableMap = new int[chunkSize, chunkSize];
        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                blittableMap[i, j] = map[i, j] ? 1 : 0;
            }
        }
        mapBuffer.SetData(blittableMap);
        projectorShader.SetTexture(kernelIndex, "Result", mapDisplay);
        projectorShader.SetBuffer(kernelIndex, "chunkMap", mapBuffer);
        projectorShader.SetVector("offset", chunkPos);
        projectorShader.SetFloat("indexToUV", zoomLevel / mapDisplay.width);
        projectorShader.SetInt("textureSize", textureSize);
        projectorShader.Dispatch(kernelIndex, 4, 4, 1);
    }

    private bool IsChunkOnDisplay(Vector2 chunkDisplayPosition)
    {
        Bounds displayBound = new Bounds(Vector2.one * 0.5f, Vector2.one);
        Vector2 chunkDim = new Vector2(chunkSize * zoomLevel / mapDisplay.width, chunkSize * zoomLevel / mapDisplay.height);
        Bounds chunkBound = new Bounds(chunkDisplayPosition + (chunkDim / 2), chunkDim); 
        return displayBound.Intersects(chunkBound);
    }

    private Vector2 WorldToDisplayPosition(Vector2Int chunkPosition)
    {
        Vector2 displayPos = Vector2.zero;
        displayPos.x += (chunkPosition.x - mapOffset.x) * chunkSize * zoomLevel / mapDisplay.width;
        displayPos.y += (chunkPosition.y - mapOffset.y) * chunkSize * zoomLevel / mapDisplay.height;
        //displayPos -= mapOffset;
        return displayPos;
    }


    #region Generation Code
    private float GetValue(bool l, bool uL, bool u, bool uR, bool r, bool dR, bool d, bool dL)
    {
        if (mode == Mode.CPU_Discrete)
        {
            return Convert.ToInt32(l) + Convert.ToInt32(uL) + Convert.ToInt32(u) + Convert.ToInt32(uR) + 
                Convert.ToInt32(r) + Convert.ToInt32(dR) + Convert.ToInt32(d) + Convert.ToInt32(dL);
        }
        else if (mode == Mode.CPU_Continuous)
        {
            float value = (l ? rulesVector[0] : 0) + (uL ? rulesVector[1] : 0) + (u ? rulesVector[2] : 0) + (uR ? rulesVector[3] : 0)
                + (r ? rulesVector[4] : 0) + (dR ? rulesVector[5] : 0) + (d ? rulesVector[6] : 0) + (dL ? rulesVector[7] : 0);
            return value / rulesVector.Sum();
        }
        else
        {
            Debug.Log("Mode is invalid for Previewing chunk");
            return 0;
        }
    }

    private bool[,] SimulateTilesGeneration(int chunkX, int chunkY)
    {
        bool[,] initialMainTiles = GetInitialMap(chunkX, chunkY);
        bool[,] initialUpTiles = GetInitialMap(chunkX, chunkY + 1);
        bool[,] initialDownTiles = GetInitialMap(chunkX, chunkY - 1);
        bool[,] initialLeftTiles = GetInitialMap(chunkX - 1, chunkY);
        bool[,] initialRightTiles = GetInitialMap(chunkX + 1, chunkY);

        bool[,] iterativeMainTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeUpTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeDownTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeLeftTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeRightTiles = new bool[chunkSize, chunkSize];
        bool[,] temp;

        if (ITERATIONS == 0)
            return initialMainTiles;

        float _D, _B;
        if (mode == Mode.CPU_Discrete)
        {
            _D = D;
            _B = B;
        }
        else
        {
            _D = (float)(D / 8f);
            _B = (float)(B / 8f);
        }

        for (int i = 0; i < ITERATIONS; i++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    float t = 0;
                    bool l, uL, u, uR, r, dR, d, dL;

                    if (y == 0)
                    {
                        dR = x == chunkSize - 1 ? initialRightTiles[0, chunkSize - 1] : initialMainTiles[x + 1, chunkSize - 1];
                        d = initialMainTiles[x, chunkSize - 1];
                        dL = x == 0 ? initialLeftTiles[chunkSize - 1, chunkSize - 1] : initialMainTiles[x - 1, chunkSize - 1];
                    }
                    else
                    {
                        dR = (x == chunkSize - 1) || y == 0 ? E : initialUpTiles[x + 1, y - 1];
                        d = y == 0 ? E : initialUpTiles[x, y - 1];
                        dL = y == 0 || x == 0 ? E : initialUpTiles[x - 1, y - 1];
                    }

                    l = x == 0 ? E : initialUpTiles[x - 1, y];
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialUpTiles[x - 1, y + 1];
                    u = (y == chunkSize - 1) ? E : initialUpTiles[x, y + 1];
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialUpTiles[x + 1, y + 1];
                    r = (x == chunkSize - 1) ? E : initialUpTiles[x + 1, y];

                    t = GetValue(l, uL, u, uR, r, dR, d, dL);

                    if (initialUpTiles[x, y])
                    {
                        iterativeUpTiles[x, y] = !(t < _D);
                    }
                    else
                    {
                        iterativeUpTiles[x, y] = t > _B;
                    }
                }
            }

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    float t = 0;
                    bool l, uL, u, uR, r, dR, d, dL;

                    if (x == chunkSize - 1)
                    {
                        uR = y == chunkSize - 1 ? initialUpTiles[0, 0] : initialMainTiles[0, y + 1];
                        r = initialMainTiles[0, y];
                        dR = y == 0 ? initialDownTiles[0, chunkSize - 1] : initialMainTiles[0, y - 1];
                    }
                    else
                    {
                        uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialLeftTiles[x + 1, y + 1];
                        r = (x == chunkSize - 1) ? E : initialLeftTiles[x + 1, y];
                        dR = (x == chunkSize - 1) || y == 0 ? E : initialLeftTiles[x + 1, y - 1];
                    }

                    l = x == 0 ? E : initialLeftTiles[x - 1, y];
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialLeftTiles[x - 1, y + 1];
                    u = (y == chunkSize - 1) ? E : initialLeftTiles[x, y + 1];
                    d = y == 0 ? E : initialLeftTiles[x, y - 1];
                    dL = y == 0 || x == 0 ? E : initialLeftTiles[x - 1, y - 1];

                    t = GetValue(l, uL, u, uR, r, dR, d, dL);

                    if (initialLeftTiles[x, y])
                    {
                        iterativeLeftTiles[x, y] = !(t < _D);
                    }
                    else
                    {
                        iterativeLeftTiles[x, y] = t > _B;
                    }
                }
            }

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    float t = 0;
                    bool l, uL, u, uR, r, dR, d, dL;

                    if (x == 0 && y == 0)
                    {
                        dL = E;
                        l = initialLeftTiles[chunkSize - 1, y];
                        uL = initialLeftTiles[chunkSize - 1, y + 1];

                        d = initialDownTiles[x, chunkSize - 1];
                        dR = initialDownTiles[x + 1, chunkSize - 1];

                        u = initialMainTiles[x, y + 1];
                        uR = initialMainTiles[x + 1, y + 1];
                        r = initialMainTiles[x + 1, y];
                    }
                    else if (x == chunkSize - 1 && y == chunkSize - 1)
                    {
                        uR = E;
                        r = initialRightTiles[0, y];
                        dR = initialRightTiles[0, y - 1];

                        u = initialUpTiles[x, 0];
                        uL = initialUpTiles[x - 1, 0];

                        l = initialMainTiles[x - 1, y];
                        d = initialMainTiles[x, y - 1];
                        dL = initialMainTiles[x - 1, y - 1];
                    }
                    else if (x == 0 && y == chunkSize - 1)
                    {
                        uL = E;
                        l = initialLeftTiles[chunkSize - 1, y];
                        dL = initialLeftTiles[chunkSize - 1, y - 1];

                        u = initialUpTiles[x, 0];
                        uR = initialUpTiles[x + 1, 0];

                        r = initialMainTiles[x + 1, y];
                        dR = initialMainTiles[x + 1, y - 1];
                        d = initialMainTiles[x, y - 1];
                    }
                    else if (x == chunkSize - 1 && y == 0)
                    {
                        dR = E;
                        r = initialRightTiles[0, y];
                        uR = initialRightTiles[0, y + 1];

                        d = initialDownTiles[x, chunkSize - 1];
                        dL = initialDownTiles[x - 1, chunkSize - 1];

                        l = initialMainTiles[x - 1, y];
                        uL = initialMainTiles[x - 1, y + 1];
                        u = initialMainTiles[x, y + 1];
                    }
                    else if (x == 0)
                    {
                        l = initialLeftTiles[chunkSize - 1, y];
                        uL = initialLeftTiles[chunkSize - 1, y + 1];
                        dL = initialLeftTiles[chunkSize - 1, y + 1];

                        u = initialMainTiles[x, y + 1];
                        uR = initialMainTiles[x + 1, y + 1];
                        r = initialMainTiles[x + 1, y];
                        dR = initialMainTiles[x + 1, y - 1];
                        d = initialMainTiles[x, y - 1];
                    }
                    else if (y == 0)
                    {
                        d = initialDownTiles[x, chunkSize - 1];
                        dR = initialDownTiles[x + 1, chunkSize - 1];
                        dL = initialDownTiles[x - 1, chunkSize - 1];

                        l = initialMainTiles[x - 1, y];
                        uL = initialMainTiles[x - 1, y + 1];
                        u = initialMainTiles[x, y + 1];
                        uR = initialMainTiles[x + 1, y + 1];
                        r = initialMainTiles[x + 1, y];
                    }
                    else if (x == chunkSize - 1)
                    {
                        r = initialRightTiles[0, y];
                        dR = initialRightTiles[0, y - 1];
                        uR = initialRightTiles[0, y + 1];

                        l = initialMainTiles[x - 1, y];
                        uL = initialMainTiles[x - 1, y + 1];
                        u = initialMainTiles[x, y + 1];
                        d = initialMainTiles[x, y - 1];
                        dL = initialMainTiles[x - 1, y - 1];
                    }
                    else if (y == chunkSize - 1)
                    {
                        u = initialUpTiles[x, 0];
                        uL = initialUpTiles[x - 1, 0];
                        uR = initialUpTiles[x + 1, 0];

                        l = initialMainTiles[x - 1, y];
                        r = initialMainTiles[x + 1, y];
                        dR = initialMainTiles[x + 1, y - 1];
                        d = initialMainTiles[x, y - 1];
                        dL = initialMainTiles[x - 1, y - 1];
                    }
                    else
                    {
                        l = initialMainTiles[x - 1, y];
                        uL = initialMainTiles[x - 1, y + 1];
                        u = initialMainTiles[x, y + 1];
                        uR = initialMainTiles[x + 1, y + 1];
                        r = initialMainTiles[x + 1, y];
                        dR = initialMainTiles[x + 1, y - 1];
                        d = initialMainTiles[x, y - 1];
                        dL = initialMainTiles[x - 1, y - 1];
                    }

                    t = GetValue(l, uL, u, uR, r, dR, d, dL);

                    if (initialMainTiles[x, y])
                    {
                        iterativeMainTiles[x, y] = !(t < _D);
                    }
                    else
                    {
                        iterativeMainTiles[x, y] = t > _B;
                    }
                }
            }

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    float t = 0;
                    bool l, uL, u, uR, r, dR, d, dL;

                    if (x == 0)
                    {
                        l = initialMainTiles[chunkSize - 1, y];
                        uL = y == chunkSize - 1 ? initialUpTiles[chunkSize - 1, 0] : initialMainTiles[chunkSize - 1, y + 1];
                        dL = y == 0 ? initialDownTiles[chunkSize - 1, chunkSize - 1] : initialMainTiles[chunkSize - 1, y - 1];
                    }
                    else
                    {
                        l = x == 0 ? E : initialRightTiles[x - 1, y];
                        uL = x == 0 || (y == chunkSize - 1) ? E : initialRightTiles[x - 1, y + 1];
                        dL = y == 0 || x == 0 ? E : initialRightTiles[x - 1, y - 1];
                    }

                    u = (y == chunkSize - 1) ? E : initialRightTiles[x, y + 1];
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialRightTiles[x + 1, y + 1];
                    r = (x == chunkSize - 1) ? E : initialRightTiles[x + 1, y];
                    dR = (x == chunkSize - 1) || y == 0 ? E : initialRightTiles[x + 1, y - 1];
                    d = y == 0 ? E : initialRightTiles[x, y - 1];

                    t = GetValue(l, uL, u, uR, r, dR, d, dL);

                    if (initialRightTiles[x, y])
                    {
                        iterativeRightTiles[x, y] = !(t < _D);
                    }
                    else
                    {
                        iterativeRightTiles[x, y] = t > _B;
                    }
                }
            }

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    float t = 0;
                    bool l, uL, u, uR, r, dR, d, dL;

                    if (y == chunkSize - 1)
                    {
                        uL = x == 0 ? initialLeftTiles[chunkSize - 1, 0] : initialMainTiles[x - 1, 0];
                        u = initialMainTiles[x, 0];
                        uR = x == chunkSize - 1 ? initialRightTiles[0, 0] : initialMainTiles[x + 1, 0];
                    }
                    else
                    {
                        uL = x == 0 || (y == chunkSize - 1) ? E : initialDownTiles[x - 1, y + 1];
                        u = (y == chunkSize - 1) ? E : initialDownTiles[x, y + 1];
                        uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialDownTiles[x + 1, y + 1];
                    }

                    l = x == 0 ? E : initialDownTiles[x - 1, y];
                    r = (x == chunkSize - 1) ? E : initialDownTiles[x + 1, y];
                    dR = (x == chunkSize - 1) || y == 0 ? E : initialDownTiles[x + 1, y - 1];
                    d = y == 0 ? E : initialDownTiles[x, y - 1];
                    dL = y == 0 || x == 0 ? E : initialDownTiles[x - 1, y - 1];

                    t = GetValue(l, uL, u, uR, r, dR, d, dL);

                    if (initialDownTiles[x, y])
                    {
                        iterativeDownTiles[x, y] = !(t < _D);
                    }
                    else
                    {
                        iterativeDownTiles[x, y] = t > _B;
                    }
                }
            }

            temp = initialUpTiles;
            initialUpTiles = iterativeUpTiles;
            iterativeUpTiles = temp;

            temp = initialLeftTiles;
            initialLeftTiles = iterativeLeftTiles;
            iterativeLeftTiles = temp;

            temp = initialMainTiles;
            initialMainTiles = iterativeMainTiles;
            iterativeMainTiles = temp;

            temp = initialRightTiles;
            initialRightTiles = iterativeRightTiles;
            iterativeRightTiles = temp;

            temp = initialDownTiles;
            initialDownTiles = iterativeDownTiles;
            iterativeDownTiles = temp;
        }

        if (correction == CorrectionMode.On)
            initialMainTiles = ChunkCorrection(initialMainTiles, GetChunkSeed(chunkX, chunkY));
        return initialMainTiles;
    }

    private bool[,] Test(int chunkX, int chunkY)
    {
        bool[,] initialMainTiles = GetInitialMap(chunkX, chunkY);
        bool[,] initialUpTiles = GetInitialMap(chunkX, chunkY + 1);
        bool[,] initialDownTiles = GetInitialMap(chunkX, chunkY - 1);
        bool[,] initialLeftTiles = GetInitialMap(chunkX - 1, chunkY);
        bool[,] initialRightTiles = GetInitialMap(chunkX + 1, chunkY);

        bool[,] iterativeMainTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeUpTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeDownTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeLeftTiles = new bool[chunkSize, chunkSize];
        bool[,] iterativeRightTiles = new bool[chunkSize, chunkSize];
        bool[,] temp;

        if (ITERATIONS == 0)
            return initialMainTiles;

        float _D, _B;
        if (mode == Mode.CPU_Discrete)
        {
            _D = D;
            _B = B;
        }
        else
        {
            _D = (float)(D / 8f);
            _B = (float)(B / 8f);
        }

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                float t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (y == 0)
                {
                    dR = x == chunkSize - 1 ? initialRightTiles[0, chunkSize - 1] : initialMainTiles[x + 1, chunkSize - 1];
                    d = initialMainTiles[x, chunkSize - 1];
                    dL = x == 0 ? initialLeftTiles[chunkSize - 1, chunkSize - 1] : initialMainTiles[x - 1, chunkSize - 1];
                }
                else
                {
                    dR = (x == chunkSize - 1) || y == 0 ? E : initialUpTiles[x + 1, y - 1];
                    d = y == 0 ? E : initialUpTiles[x, y - 1];
                    dL = y == 0 || x == 0 ? E : initialUpTiles[x - 1, y - 1];
                }

                l = x == 0 ? E : initialUpTiles[x - 1, y];
                uL = x == 0 || (y == chunkSize - 1) ? E : initialUpTiles[x - 1, y + 1];
                u = (y == chunkSize - 1) ? E : initialUpTiles[x, y + 1];
                uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialUpTiles[x + 1, y + 1];
                r = (x == chunkSize - 1) ? E : initialUpTiles[x + 1, y];

                t = GetValue(l, uL, u, uR, r, dR, d, dL);

                if (initialUpTiles[x, y])
                {
                    iterativeUpTiles[x, y] = !(t < _D);
                }
                else
                {
                    iterativeUpTiles[x, y] = t > _B;
                }
            }
        }

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                float t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (x == chunkSize - 1)
                {
                    uR = y == chunkSize - 1 ? initialUpTiles[0, 0] : initialMainTiles[0, y + 1];
                    r = initialMainTiles[0, y];
                    dR = y == 0 ? initialDownTiles[0, chunkSize - 1] : initialMainTiles[0, y - 1];
                }
                else
                {
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialLeftTiles[x + 1, y + 1];
                    r = (x == chunkSize - 1) ? E : initialLeftTiles[x + 1, y];
                    dR = (x == chunkSize - 1) || y == 0 ? E : initialLeftTiles[x + 1, y - 1];
                }

                l = x == 0 ? E : initialLeftTiles[x - 1, y];
                uL = x == 0 || (y == chunkSize - 1) ? E : initialLeftTiles[x - 1, y + 1];
                u = (y == chunkSize - 1) ? E : initialLeftTiles[x, y + 1];
                d = y == 0 ? E : initialLeftTiles[x, y - 1];
                dL = y == 0 || x == 0 ? E : initialLeftTiles[x - 1, y - 1];

                t = GetValue(l, uL, u, uR, r, dR, d, dL);

                if (initialLeftTiles[x, y])
                {
                    iterativeLeftTiles[x, y] = !(t < _D);
                }
                else
                {
                    iterativeLeftTiles[x, y] = t > _B;
                }
            }
        }

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                float t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (x == 0 && y == 0)
                {
                    dL = E;
                    l = initialLeftTiles[chunkSize - 1, y];
                    uL = initialLeftTiles[chunkSize - 1, y + 1];

                    d = initialDownTiles[x, chunkSize - 1];
                    dR = initialDownTiles[x + 1, chunkSize - 1];

                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                }
                else if (x == chunkSize - 1 && y == chunkSize - 1)
                {
                    uR = E;
                    r = initialRightTiles[0, y];
                    dR = initialRightTiles[0, y - 1];

                    u = initialUpTiles[x, 0];
                    uL = initialUpTiles[x - 1, 0];

                    l = initialMainTiles[x - 1, y];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }
                else if (x == 0 && y == chunkSize - 1)
                {
                    uL = E;
                    l = initialLeftTiles[chunkSize - 1, y];
                    dL = initialLeftTiles[chunkSize - 1, y - 1];

                    u = initialUpTiles[x, 0];
                    uR = initialUpTiles[x + 1, 0];

                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                }
                else if (x == chunkSize - 1 && y == 0)
                {
                    dR = E;
                    r = initialRightTiles[0, y];
                    uR = initialRightTiles[0, y + 1];

                    d = initialDownTiles[x, chunkSize - 1];
                    dL = initialDownTiles[x - 1, chunkSize - 1];

                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                }
                else if (x == 0)
                {
                    l = initialLeftTiles[chunkSize - 1, y];
                    uL = initialLeftTiles[chunkSize - 1, y + 1];
                    dL = initialLeftTiles[chunkSize - 1, y + 1];

                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                }
                else if (y == 0)
                {
                    d = initialDownTiles[x, chunkSize - 1];
                    dR = initialDownTiles[x + 1, chunkSize - 1];
                    dL = initialDownTiles[x - 1, chunkSize - 1];

                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                }
                else if (x == chunkSize - 1)
                {
                    r = initialRightTiles[0, y];
                    dR = initialRightTiles[0, y - 1];
                    uR = initialRightTiles[0, y + 1];

                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }
                else if (y == chunkSize - 1)
                {
                    u = initialUpTiles[x, 0];
                    uL = initialUpTiles[x - 1, 0];
                    uR = initialUpTiles[x + 1, 0];

                    l = initialMainTiles[x - 1, y];
                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }
                else
                {
                    l = initialMainTiles[x - 1, y];
                    uL = initialMainTiles[x - 1, y + 1];
                    u = initialMainTiles[x, y + 1];
                    uR = initialMainTiles[x + 1, y + 1];
                    r = initialMainTiles[x + 1, y];
                    dR = initialMainTiles[x + 1, y - 1];
                    d = initialMainTiles[x, y - 1];
                    dL = initialMainTiles[x - 1, y - 1];
                }

                t = GetValue(l, uL, u, uR, r, dR, d, dL);

                if (initialMainTiles[x, y])
                {
                    iterativeMainTiles[x, y] = !(t < _D);
                }
                else
                {
                    iterativeMainTiles[x, y] = t > _B;
                }
            }
        }

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                float t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (x == 0)
                {
                    l = initialMainTiles[chunkSize - 1, y];
                    uL = y == chunkSize - 1 ? initialUpTiles[chunkSize - 1, 0] : initialMainTiles[chunkSize - 1, y + 1];
                    dL = y == 0 ? initialDownTiles[chunkSize - 1, chunkSize - 1] : initialMainTiles[chunkSize - 1, y - 1];
                }
                else
                {
                    l = x == 0 ? E : initialRightTiles[x - 1, y];
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialRightTiles[x - 1, y + 1];
                    dL = y == 0 || x == 0 ? E : initialRightTiles[x - 1, y - 1];
                }

                u = (y == chunkSize - 1) ? E : initialRightTiles[x, y + 1];
                uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialRightTiles[x + 1, y + 1];
                r = (x == chunkSize - 1) ? E : initialRightTiles[x + 1, y];
                dR = (x == chunkSize - 1) || y == 0 ? E : initialRightTiles[x + 1, y - 1];
                d = y == 0 ? E : initialRightTiles[x, y - 1];

                t = GetValue(l, uL, u, uR, r, dR, d, dL);

                if (initialRightTiles[x, y])
                {
                    iterativeRightTiles[x, y] = !(t < _D);
                }
                else
                {
                    iterativeRightTiles[x, y] = t > _B;
                }
            }
        }

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                float t = 0;
                bool l, uL, u, uR, r, dR, d, dL;

                if (y == chunkSize - 1)
                {
                    uL = x == 0 ? initialLeftTiles[chunkSize - 1, 0] : initialMainTiles[x - 1, 0];
                    u = initialMainTiles[x, 0];
                    uR = x == chunkSize - 1 ? initialRightTiles[0, 0] : initialMainTiles[x + 1, 0];
                }
                else
                {
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialDownTiles[x - 1, y + 1];
                    u = (y == chunkSize - 1) ? E : initialDownTiles[x, y + 1];
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialDownTiles[x + 1, y + 1];
                }

                l = x == 0 ? E : initialDownTiles[x - 1, y];
                r = (x == chunkSize - 1) ? E : initialDownTiles[x + 1, y];
                dR = (x == chunkSize - 1) || y == 0 ? E : initialDownTiles[x + 1, y - 1];
                d = y == 0 ? E : initialDownTiles[x, y - 1];
                dL = y == 0 || x == 0 ? E : initialDownTiles[x - 1, y - 1];

                t = GetValue(l, uL, u, uR, r, dR, d, dL);

                if (initialDownTiles[x, y])
                {
                    iterativeDownTiles[x, y] = !(t < _D);
                }
                else
                {
                    iterativeDownTiles[x, y] = t > _B;
                }
            }
        }

        temp = initialUpTiles;
        initialUpTiles = iterativeUpTiles;
        iterativeUpTiles = temp;

        temp = initialLeftTiles;
        initialLeftTiles = iterativeLeftTiles;
        iterativeLeftTiles = temp;

        temp = initialMainTiles;
        initialMainTiles = iterativeMainTiles;
        iterativeMainTiles = temp;

        temp = initialRightTiles;
        initialRightTiles = iterativeRightTiles;
        iterativeRightTiles = temp;

        temp = initialDownTiles;
        initialDownTiles = iterativeDownTiles;
        iterativeDownTiles = temp;

        for (int i = 0; i < ITERATIONS - 1; i++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    float t = 0;
                    bool l, uL, u, uR, r, dR, d, dL;

                    l = x == 0 ? E : initialMainTiles[x - 1, y];
                    uL = x == 0 || (y == chunkSize - 1) ? E : initialMainTiles[x - 1, y + 1];
                    u = (y == chunkSize - 1) ? E : initialMainTiles[x, y + 1];
                    uR = (x == chunkSize - 1) || (y == chunkSize - 1) ? E : initialMainTiles[x + 1, y + 1];
                    r = (x == chunkSize - 1) ? E : initialMainTiles[x + 1, y];
                    dR = (x == chunkSize - 1) || (y == 0) ? E : initialMainTiles[x + 1, y - 1];
                    d = (y == 0) ? E : initialMainTiles[x, y - 1];
                    dL = (x == 0) || (y == 0) ? E : initialMainTiles[x - 1, y - 1];

                    t = GetValue(l, uL, u, uR, r, dR, d, dL);

                    if (initialMainTiles[x, y])
                    {
                        iterativeMainTiles[x, y] = !(t < _D);
                    }
                    else
                    {
                        iterativeMainTiles[x, y] = t > _B;
                    }
                }
            }

            temp = initialMainTiles;
            initialMainTiles = iterativeMainTiles;
            iterativeMainTiles = temp;
        }

        if (correction == CorrectionMode.On)
            initialMainTiles = ChunkCorrection(initialMainTiles, GetChunkSeed(chunkX, chunkY));
        return initialMainTiles;
    }


    private int GetChunkSeed(int chunkX, int chunkY)
    {
        System.Random prng = new System.Random(seed);
        prng = new System.Random(prng.Next(-100000, 100000) + chunkX);
        int chunkSeed = prng.Next(-100000, 100000) + chunkY;

        return chunkSeed;
    }

    private bool[,] GetInitialMap(int chunkX, int chunkY)
    {
        System.Random prng = new System.Random(GetChunkSeed(chunkX, chunkY));

        bool[,] initialTiles = new bool[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = prng.Next(-100000, 100000) < P;
            }
        }

        return initialTiles;
    }

    private bool[,] GenerateTilesOnGPU(int currentChunkX, int currentChunkY)
    {
        System.Random prng = new System.Random(seed);
        prng = new System.Random(prng.Next(-100000, 100000) + currentChunkX);
        int chunkSeed = prng.Next(-100000, 100000) + currentChunkY;
        prng = new System.Random(chunkSeed);

        float[,] initialTiles = new float[chunkSize, chunkSize];

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                initialTiles[x, y] = (prng.Next(0, 100000) / 100000f) < P ? 1 : 0;
            }
        }

        ComputeBuffer prev = new ComputeBuffer(chunkSize * chunkSize, sizeof(float), ComputeBufferType.Default);
        ComputeBuffer curr = new ComputeBuffer(chunkSize * chunkSize, sizeof(float), ComputeBufferType.Default);

        CAShader.SetInt("E", E ? 1 : 0);
        CAShader.SetInt("B", (int)B);
        CAShader.SetInt("D", (int)D);
        CAShader.SetInt("chunkSize", chunkSize);

        int kernelHandle = CAShader.FindKernel("CSMain");
        
        prev.SetData(initialTiles);
        for (int i = 0; i < ITERATIONS; i++)
        {
            if (i % 2 == 0)
            {
                CAShader.SetBuffer(kernelHandle, "initialTiles", prev);
                CAShader.SetBuffer(kernelHandle, "initialTiles", curr);
            }
            else
            {
                CAShader.SetBuffer(kernelHandle, "initialTiles", curr);
                CAShader.SetBuffer(kernelHandle, "initialTiles", prev);
            }
            CAShader.Dispatch(kernelHandle, 1, 1, 1);    
        }
        if (ITERATIONS % 2 == 0)
        {
            prev.GetData(initialTiles);
        }
        else
        {
            curr.GetData(initialTiles);
        }
        curr.Dispose();
        prev.Dispose();

        bool[,] answer = new bool[chunkSize, chunkSize];
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                answer[x, y] = initialTiles[x, y] == 1f;
            }
        }
        return answer;
    }

    private bool[,] ChunkCorrection(bool[,] chunkMap, int chunkSeed)
    {
        List<int> fillIds = new List<int>();
        fillIds.Add(-1);

        int[,] fillMap = new int[chunkSize, chunkSize];
        int[,] blockMap = new int[chunkSize / 3, chunkSize / 3];

        List<TestFiller> fillers = new List<TestFiller>();
        for (int i = 0; i < 3; i++)
            fillers.AddRange(FloodFillPartition(chunkMap, fillMap, blockMap, fillIds, i));

        for (int i = 1; i < 3; i++)
        {
            int yStart = (int)(chunkSize * (i / 3f));
            for (int x = 0; x < chunkSize; x++)
            {
                int index1 = fillMap[x, yStart];
                int index2 = fillMap[x, yStart - 1];
                if (fillIds[index1] != fillIds[index2] && fillIds[index1] != -1 && fillIds[index2] != -1)
                {
                    TestFiller fillerB = fillers.Find((filler) => filler.fillerIndex == index2);
                    //TestFiller fillerB = fillers.Find((filler) => fillerIds[filler.fillerIndex] == fillerIds[index2]);
                    int colorToReplace = fillIds[index2];
                    int colorToReplaceWith = fillIds[index1];
                    for (int j = 0; j < fillIds.Count; j++)
                    {
                        if (fillIds[j] == colorToReplace)
                            fillIds[j] = colorToReplaceWith;
                    }
                    fillers.Remove(fillerB);
                }
            }
        }

        List<int> duplicates = new List<int>();
        for (int i = 0; i < fillers.Count; i++)
        {
            if (duplicates.Contains(fillIds[fillers[i].fillerIndex]))
                fillers.Remove(fillers[i--]);
            else
                duplicates.Add(fillIds[fillers[i].fillerIndex]);
        }

        if (fillers.Count == 1)
            return chunkMap;
        return ConnectAutomataPaths(fillers, fillIds, blockMap, fillMap, chunkMap, chunkSeed);
    }

    #region Connector Code
    private List<TestFiller> FloodFillPartition(in bool[,] chunkMap, int[,] fillMap, int[,] blockMap, List<int> fillIds, int partitionID)
    {
        int yStart = (int)(chunkSize * (partitionID / 3f));
        int yEnd = (int)(chunkSize * ((partitionID + 1f) / 3f));

        List<TestFiller> fillers = new List<TestFiller>();

        for (int y = yStart; y < yEnd; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                if (!chunkMap[x, y])
                    continue;
                if (fillMap[x, y] != 0)
                    continue;

                int index = fillIds.Count;
                fillIds.Add(index);
                fillMap[x, y] = index;
                TestFiller filler = new TestFiller(index, yStart, yEnd, x, y);
                fillers.Add(filler);

                while (filler.Step(chunkMap, fillMap, blockMap)) ;
            }
        }

        return fillers;
    }
    private class TestFiller
    {
        public readonly int fillerIndex;

        private Queue<Vector2> toExplore;
        private int yStart, yEnd;
        private int currX, currY;

        public TestFiller(int index, int yStart, int yEnd, int currX, int currY)
        {

            fillerIndex = index;
            this.yStart = yStart;
            this.yEnd = yEnd;
            this.currX = currX;
            this.currY = currY;

            toExplore = new Queue<Vector2>();
            toExplore.Enqueue(new Vector2(currX, currY));
        }
        public Vector2 GetPoint()
        {
            return new Vector2(currX, currY);
        }
        public bool Step(in bool[,] tileMap, int[,] fillMap, int[,] blockMap)
        {
            if (toExplore.Count == 0)
            {
                toExplore = null;
                return false;
            }

            int size = tileMap.GetLength(0);
            Vector2 floorPos = toExplore.Dequeue();
            currX = (int)floorPos.x;
            currY = (int)floorPos.y;

            blockMap[currX / 3, currY / 3] = fillerIndex;

            if (currX + 1 < size && tileMap[currX + 1, currY] && (fillMap[currX + 1, currY] != fillerIndex))
            {
                fillMap[currX + 1, currY] = fillerIndex;
                toExplore.Enqueue(new Vector2(currX + 1, currY));
            }

            if (currX - 1 >= 0 && tileMap[currX - 1, currY] && (fillMap[currX - 1, currY] != fillerIndex))
            {
                fillMap[currX - 1, currY] = fillerIndex;
                toExplore.Enqueue(new Vector2(currX - 1, currY));
            }

            if ((currY + 1) < yEnd && tileMap[currX, currY + 1] && (fillMap[currX, currY + 1] != fillerIndex))
            {
                fillMap[currX, currY + 1] = fillerIndex;
                toExplore.Enqueue(new Vector2(currX, currY + 1));
            }

            if ((currY - 1) >= yStart && tileMap[currX, currY - 1] && (fillMap[currX, currY - 1] != fillerIndex))
            {
                fillMap[currX, currY - 1] = fillerIndex;
                toExplore.Enqueue(new Vector2(currX, currY - 1));
            }

            return true;
        }
    }
    private int ManhattanDistance(Vector2 a, Vector2 b)
    {
        return (int)Mathf.Abs(a.x - b.x) + (int)Mathf.Abs(a.y - b.y);
    }
    private Vector2 AStar(int startFillId, List<int> fillIds, int[,] blockMap, Vector2 start, Vector2 target)
    {
        if (fillIds[blockMap[(int)start.x, (int)start.y]] != startFillId)
        {
            //start block already contains a tile that belongs to a different fill
            return start;
        }
        Vector2 closestPoint = start;
        int minDistance = int.MaxValue;

        PriorityQueue<Node> openNodes = new PriorityQueue<Node>();
        openNodes.Enqueue(new Node(start, ManhattanDistance(start, target)));

        int currX, currY, currDist, size = blockMap.GetLength(0);
        bool[,] marks = new bool[size, size];
        Vector2 currPos;
        Node currNode;
        while (openNodes.Size() != 0)
        {
            currNode = openNodes.Dequeue();
            currDist = ManhattanDistance(currNode.pos, target);
            closestPoint = currDist < minDistance ? currNode.pos : closestPoint;
            minDistance = Mathf.Min(currDist, minDistance);

            currX = (int)currNode.pos.x;
            currY = (int)currNode.pos.y;

            if (currX + 1 < size && fillIds[blockMap[currX + 1, currY]] == startFillId && !marks[currX + 1, currY])
            {
                currPos.x = currX + 1;
                currPos.y = currY;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX + 1, currY] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
            if (currX - 1 >= 0 && fillIds[blockMap[currX - 1, currY]] == startFillId && !marks[currX - 1, currY])
            {
                currPos.x = currX - 1;
                currPos.y = currY;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX - 1, currY] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
            if (currY + 1 < size && fillIds[blockMap[currX, currY + 1]] == startFillId && !marks[currX, currY + 1])
            {
                currPos.x = currX;
                currPos.y = currY + 1;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX, currY + 1] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
            if (currY - 1 >= 0 && fillIds[blockMap[currX, currY - 1]] == startFillId && !marks[currX, currY - 1])
            {
                currPos.x = currX;
                currPos.y = currY - 1;
                currDist = ManhattanDistance(currPos, target);
                if (currDist < 2 * minDistance)
                {
                    marks[currX, currY - 1] = true;
                    openNodes.Enqueue(new Node(currPos, currDist));
                }
            }
        }

        return closestPoint;
    }
    private bool[,] ConnectAutomataPaths(List<TestFiller> fillers, List<int> fillIds, int[,] blockMap, int[,] fillMap, bool[,] chunkMap, int chunkSeed)
    {
        if (fillers.Count < 2)
        {
            return chunkMap;
        }
        TestFiller baseFiller = fillers[0];

        Vector2 baseBlockPos = new Vector2((int)baseFiller.GetPoint().x / 3, (int)baseFiller.GetPoint().y / 3);
        int baseFillerId = fillIds[baseFiller.fillerIndex];

        while (fillers.Count != 1)
        {
            Vector2 startPoint = new Vector2((int)fillers[1].GetPoint().x / 3, (int)fillers[1].GetPoint().y / 3);
            Vector2 newBasePoint = AStar(fillIds[baseFillerId], fillIds, blockMap, baseBlockPos, startPoint);
            Vector2 newStartPoint = AStar(fillIds[fillers[1].fillerIndex], fillIds, blockMap, startPoint, newBasePoint);
            List<int> toBeMerged = RandomDirectedWalk(baseFiller.fillerIndex, fillers[1].fillerIndex, ref newBasePoint, ref newStartPoint, fillIds, chunkMap, fillMap, chunkSeed);

            for (int i = 0; i < toBeMerged.Count; i++)
            {
                int mergingId = toBeMerged[0];
                fillIds.ConvertAll((id) => id = (id == mergingId ? baseFillerId : id));
                fillers.Remove(fillers.Find((filler) => fillIds[filler.fillerIndex] == mergingId));
                toBeMerged.RemoveAt(0);
            }
        }

        return chunkMap;
    }
    private List<int> RandomDirectedWalk(int baseFillerIndex, int startFillerIndex, ref Vector2 basePos, ref Vector2 targetPos, List<int> fillIds, bool[,] chunkMap, int[,] fillMap, int chunkSeed)
    {
        int chunkSize = chunkMap.GetLength(0);
        List<int> toBeMerged = new List<int>();
        toBeMerged.Add(fillIds[startFillerIndex]);

        Vector2 baseTile = basePos * 3;
        bool baseTileFound = false;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (fillIds[fillMap[(int)baseTile.x + x, (int)baseTile.y + y]] == fillIds[baseFillerIndex])
                {
                    baseTile.x += x;
                    baseTile.y += y;
                    baseTileFound = true;
                    break;
                }
            }
            if (baseTileFound)
            {
                break;
            }
        }
        Vector2 currTile = targetPos * 3;
        bool targetTileFound = false;
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                if (fillIds[fillMap[(int)currTile.x + x, (int)currTile.y + y]] == fillIds[startFillerIndex])
                {
                    currTile.x += x;
                    currTile.y += y;
                    targetTileFound = true;
                    break;
                }
            }
            if (targetTileFound)
            {
                break;
            }
        }

        System.Random prng = new System.Random(chunkSeed);
        while (fillIds[fillMap[(int)currTile.x, (int)currTile.y]] != fillIds[baseFillerIndex])
        {
            Vector2 currToBase;
            //Move to the closest edge inside the targetFill
            while (true)
            {
                currToBase = baseTile - currTile;

                if (currToBase.sqrMagnitude == 0)
                    break;
                if (Mathf.Abs(currToBase.x) >= Mathf.Abs(currToBase.y))
                {
                    if (currToBase.x > 0 && currTile.x + 1 < chunkSize && chunkMap[(int)currTile.x + 1, (int)currTile.y])
                        currTile.x++;
                    else if (currToBase.x < 0 && currTile.x - 1 >= 0 && chunkMap[(int)currTile.x - 1, (int)currTile.y])
                        currTile.x--;
                    else if (currToBase.y > 0 && currTile.y + 1 < chunkSize && chunkMap[(int)currTile.x, (int)currTile.y + 1])
                        currTile.y++;
                    else if (currToBase.y < 0 && currTile.y - 1 >= 0 && chunkMap[(int)currTile.x, (int)currTile.y - 1])
                        currTile.y--;
                    else
                        break;
                }
                else
                {
                    if (currToBase.y > 0 && currTile.y + 1 < chunkSize && chunkMap[(int)currTile.x, (int)currTile.y + 1])
                        currTile.y++;
                    else if (currToBase.y < 0 && currTile.y - 1 >= 0 && chunkMap[(int)currTile.x, (int)currTile.y - 1])
                        currTile.y--;
                    else if (currToBase.x > 0 && currTile.x + 1 < chunkSize && chunkMap[(int)currTile.x + 1, (int)currTile.y])
                        currTile.x++;
                    else if (currToBase.x < 0 && currTile.x - 1 >= 0 && chunkMap[(int)currTile.x - 1, (int)currTile.y])
                        currTile.x--;
                    else
                        break;
                }

                int currFillId = fillIds[fillMap[(int)currTile.x, (int)currTile.y]];
                if (currFillId == fillIds[baseFillerIndex])
                    return toBeMerged;
                if (currFillId != fillIds[startFillerIndex] && currFillId != fillIds[baseFillerIndex] && !toBeMerged.Contains(currFillId))
                    toBeMerged.Add(currFillId);
            }

            if (currToBase.sqrMagnitude == 0)
                break;

            //Break walls
            float dirAngle = Mathf.Atan2(currToBase.y, currToBase.x);
            float zigzagChance = Mathf.Abs(Mathf.Cos(2 * dirAngle * Mathf.Rad2Deg));

            float value = prng.Next(0, 100000) / 100000f;
            float zzValue = prng.Next(0, 100000) / 100000f;
            if (value <= 0.5f)
            {
                if (zzValue <= zigzagChance)
                {
                    if (zzValue <= zigzagChance / 2f && currTile.y + 1 < chunkSize)
                    {
                        if (!chunkMap[(int)currTile.x, (int)currTile.y + 1])
                            fillMap[(int)currTile.x, (int)currTile.y + 1] = startFillerIndex;
                        chunkMap[(int)currTile.x, (int)currTile.y + 1] = true;
                    }
                    else if (currTile.y - 1 >= 0)
                    {
                        if (!chunkMap[(int)currTile.x, (int)currTile.y - 1])
                            fillMap[(int)currTile.x, (int)currTile.y - 1] = startFillerIndex;
                        chunkMap[(int)currTile.x, (int)currTile.y - 1] = true;
                    }
                }
                if (currToBase.x > 0 && currTile.x + 1 < chunkSize)
                {
                    currTile.x++;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
                else if (currTile.x - 1 >= 0)
                {
                    currTile.x--;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
            }
            else
            {
                if (zzValue <= zigzagChance)
                {
                    if (zzValue <= zigzagChance / 2f && currTile.x + 1 < chunkSize)
                    {
                        if (!chunkMap[(int)currTile.x + 1, (int)currTile.y])
                            fillMap[(int)currTile.x + 1, (int)currTile.y] = startFillerIndex;
                        chunkMap[(int)currTile.x + 1, (int)currTile.y] = true;
                    }
                    else if (currTile.x - 1 >= 0)
                    {
                        if (!chunkMap[(int)currTile.x - 1, (int)currTile.y])
                            fillMap[(int)currTile.x - 1, (int)currTile.y] = startFillerIndex;
                        chunkMap[(int)currTile.x - 1, (int)currTile.y] = true;
                    }
                }
                if (currToBase.y > 0 && currTile.y + 1 < chunkSize)
                {
                    currTile.y++;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
                else if (currTile.y - 1 >= 0)
                {
                    currTile.y--;
                    if (!chunkMap[(int)currTile.x, (int)currTile.y])
                        fillMap[(int)currTile.x, (int)currTile.y] = startFillerIndex;
                    chunkMap[(int)currTile.x, (int)currTile.y] = true;
                }
            }
        }

        return toBeMerged;
    }
    private class Node : IComparable
    {
        public Vector2 pos;
        public int distance;

        public Node(Vector2 pos, int distance)
        {
            this.pos = pos;
            this.distance = distance;
        }

        public int CompareTo(object obj)
        {
            int objDist = ((Node)obj).distance;
            if (objDist > distance)
                return -1;
            if (objDist < distance)
                return 1;
            return 0;
        }
    }
    private class PriorityQueue<T> where T : IComparable
    {
        List<T> queue;

        public PriorityQueue()
        {
            queue = new List<T>();
        }

        public void Enqueue(T item)
        {
            queue.Add(item);
            int childIndex = queue.Count - 1;

            while (childIndex > 0)
            {
                int parentIndex = (childIndex - 1) / 2;

                if (queue[childIndex].CompareTo(queue[parentIndex]) >= 0)
                {
                    break;
                }

                T temp = queue[childIndex];
                queue[childIndex] = queue[parentIndex];
                queue[parentIndex] = temp;

                childIndex = parentIndex;
            }
        }

        public T Dequeue()
        {
            int lastIndex = queue.Count - 1;
            T frontItem = queue[0];
            queue[0] = queue[lastIndex];
            queue.RemoveAt(lastIndex);
            lastIndex--;
            int parentIndex = 0;

            while (true)
            {
                int childIndex = parentIndex * 2 + 1;

                if (childIndex > lastIndex)
                {
                    break;
                }

                int rightChild = childIndex + 1;
                if (rightChild <= lastIndex && queue[rightChild].CompareTo(queue[childIndex]) < 0)
                {
                    childIndex = rightChild;
                }
                if (queue[parentIndex].CompareTo(queue[childIndex]) <= 0)
                {
                    break;
                }

                T temp = queue[parentIndex];
                queue[parentIndex] = queue[childIndex];
                queue[childIndex] = temp;

                parentIndex = childIndex;
            }
            return frontItem;
        }

        public int Size()
        {
            return queue.Count;
        }
    }
    #endregion
    #endregion
}
