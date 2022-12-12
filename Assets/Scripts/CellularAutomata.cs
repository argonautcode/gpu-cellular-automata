using System;
using UnityEngine;

public class CellularAutomata : MonoBehaviour
{
    public int viewWidth;
    public int viewHeight;
    public float zoom = 1.0f;
    public int frameInterval = 1;

    [Space]
    public int cellSize;
    public float[] targetColor1 = new float[3];
    public float[] targetColor2 = new float[3];
    public float[] cellRules = new float[18];

    [Space]
    public Texture input;

    [Space]
    public RenderTexture bufferA;
    public RenderTexture bufferB;
    public RenderTexture heat;
    public RenderTexture display;

    [Space]
    public ComputeShader automata;
    public ComputeShader aesthetic;

    private int frameCount;
    private int kernelAutomata;
    private int kernelFancy;
    private int kernelHeat;
    private float[] color1 = new float[3];
    private float[] color2 = new float[3];

    private float[] padRules()
    {
        float[] paddedRules = new float[9 * 4];
        for (int i = 0; i < 9; i++)
        {
            paddedRules[i * 4] = cellRules[i];
            paddedRules[i * 4 + 1] = cellRules[9 + i];
        }
        return paddedRules;
    }

    void Start()
    {
        kernelAutomata = automata.FindKernel("LifeGame");
        kernelFancy = aesthetic.FindKernel("FancyGrid");
        kernelHeat = aesthetic.FindKernel("HeatTracker");

        bufferA = new RenderTexture(input.width, input.height, 24);
        bufferA.enableRandomWrite = true;
        bufferA.wrapMode = TextureWrapMode.Repeat;
        bufferA.filterMode = FilterMode.Point;
        bufferA.useMipMap = false;
        bufferA.Create();

        bufferB = new RenderTexture(input.width, input.height, 24);
        bufferB.enableRandomWrite = true;
        bufferB.wrapMode = TextureWrapMode.Repeat;
        bufferB.filterMode = FilterMode.Point;
        bufferB.useMipMap = false;
        bufferB.Create();

        heat = new RenderTexture(input.width, input.height, 24);
        heat.enableRandomWrite = true;
        heat.wrapMode = TextureWrapMode.Repeat;
        heat.filterMode = FilterMode.Point;
        heat.useMipMap = false;
        heat.Create();

        display = new RenderTexture(input.width * cellSize, input.height * cellSize, 24);
        display.enableRandomWrite = true;
        display.Create();

        Graphics.Blit(input, bufferA);
        Graphics.Blit(input, heat);

        Array.Copy(targetColor1, color1, targetColor1.Length);
        Array.Copy(targetColor2, color2, targetColor2.Length);

        automata.SetFloat("Width", input.width);
        automata.SetFloat("Height", input.height);
        automata.SetFloats("Rules", padRules());
        aesthetic.SetFloat("CellSize", cellSize);
        aesthetic.SetFloats("Color1", color1);
        aesthetic.SetFloats("Color2", color2);

        frameCount = 0;
    }

    void Update()
    {
        if (Math.Abs(frameCount % frameInterval) == 0)
        {
            automata.SetTexture(kernelAutomata, "Input", bufferA);
            automata.SetTexture(kernelAutomata, "Output", bufferB);
            automata.SetFloats("Rules", padRules());
            automata.Dispatch(kernelAutomata, input.width / 8, input.height / 8, 1);

            aesthetic.SetTexture(kernelHeat, "Input", bufferB);
            aesthetic.SetTexture(kernelHeat, "Output", heat);
            aesthetic.Dispatch(kernelHeat, input.width / 8, input.height / 8, 1);

            for (int i = 0; i < 3; i++)
            {
                color1[i] = Mathf.Lerp(color1[i], targetColor1[i], 0.1f);
                color2[i] = Mathf.Lerp(color2[i], targetColor2[i], 0.1f);
            }

            aesthetic.SetFloats("Color1", color1);
            aesthetic.SetFloats("Color2", color2);
            aesthetic.SetTexture(kernelFancy, "Input", heat);
            aesthetic.SetTexture(kernelFancy, "Output", display);
            aesthetic.Dispatch(kernelFancy, (input.width * cellSize) / 8, (input.height * cellSize) / 8, 1);

            RenderTexture temp = bufferA;
            bufferA = bufferB;
            bufferB = temp;
        }
        frameCount++;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        Graphics.DrawTexture(new Rect(Screen.width / 2 - zoom * viewWidth * cellSize / 2, Screen.height / 2 - zoom * viewHeight * cellSize / 2, viewWidth * cellSize * zoom, viewHeight * cellSize * zoom), display);
    }
}
