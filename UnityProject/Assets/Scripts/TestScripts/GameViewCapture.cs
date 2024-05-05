using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Debug = UnityEngine.Debug;

public class GameViewCapture : MonoBehaviour
{
    public Camera StreaminCamera;
    public bool StreamingOn = true;

    public Vector2 StreamingResolution = new(720, 405);
    public int StreamingFPS = 60;
    public int Quality = 100;

    public Renderer UnityEncodeRenderer;
    public Renderer LibJpegTurboEncodeRenderer;

    public TextMeshProUGUI ByUnityTiming;
    public TextMeshProUGUI ByLibJpegTurboTiming;

    private readonly Stopwatch _sw1 = new();
    private readonly Stopwatch _sw2 = new();
    private readonly List<long> byLibJpegTurboAverageValue = new();
    private readonly List<long> byUnityAverageValue = new();

    private float _lastCapture = -1f;

    private LibJpegTurboUnity.LJTCompressor _ljtCompressor;
    private RenderTexture streamingTexture;

    private void Awake()
    {
        if (StreaminCamera == null)
            StreaminCamera = GetComponent<Camera>();

        StreaminCamera.depth = -1;
        streamingTexture = new RenderTexture(XRSettings.enabled
            ? XRSettings.eyeTextureDesc
            : new RenderTextureDescriptor((int) StreamingResolution.x, (int) StreamingResolution.y,
                RenderTextureFormat.ARGB32));
        StreaminCamera.targetTexture = streamingTexture;

        _ljtCompressor = new LibJpegTurboUnity.LJTCompressor();
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        while (StreamingOn)
        {
            var delay = 1f / StreamingFPS;
            if (Time.time - _lastCapture > delay)
            {
                CaptureGameView();
                _lastCapture = Time.time;
            }
            else
            {
                yield return new WaitForEndOfFrame();
            }
        }
    }

    private void OnDestroy()
    {
        _ljtCompressor.Dispose();
    }

    private void CaptureGameView()
    {
        AsyncGPUReadback.Request(streamingTexture, 0, streamingTexture.graphicsFormat, RequestResult);
    }

    private void RequestResult(AsyncGPUReadbackRequest asyncGPUReadbackRequest)
    {
        byte[] output = null;
        if (asyncGPUReadbackRequest.hasError)
        {
            Debug.LogError("GPU readback has error!");
        }
        else
        {
            var requestData = asyncGPUReadbackRequest.GetData<byte>();

            output = new byte[requestData.Length];
            requestData.CopyTo(output);

            AssignTextureByUnity(output);

            AssignTextureByLibJpegTurbo(output);
        }
    }

    private void AssignTextureByUnity(byte[] data)
    {
        //Texture2D tex = new Texture2D(_renderTexture.width, _renderTexture.height);
        if (streamingTexture == null)
            return;
        _sw1.Restart();
        var encodedImage = ImageConversion.EncodeArrayToJPG(data, streamingTexture.graphicsFormat,
            (uint) streamingTexture.width, (uint) streamingTexture.height, 0, Quality);
        _sw1.Start();
        byUnityAverageValue.Add(_sw1.ElapsedMilliseconds);
        ByUnityTiming.text = "Unity: " + byUnityAverageValue.Average().ToString("F4");
        var tex2D = new Texture2D(streamingTexture.width, streamingTexture.height);
        tex2D.LoadImage(encodedImage);
        tex2D.Apply();
        UnityEncodeRenderer.material.mainTexture = tex2D;
    }

    private void AssignTextureByLibJpegTurbo(byte[] data)
    {
        if (streamingTexture == null)
            return;
        _sw2.Restart();
        var encodedImage = _ljtCompressor.EncodeJPG(data, streamingTexture.width, streamingTexture.height,
            data.Length / (streamingTexture.height * streamingTexture.width) == 3
                ? LibJpegTurboUnity.LJTPixelFormat.RGB
                : LibJpegTurboUnity.LJTPixelFormat.RGBA, Quality);
        _sw2.Stop();
        byLibJpegTurboAverageValue.Add(_sw2.ElapsedMilliseconds);
        ByLibJpegTurboTiming.text = "LibJpegTurbo: " + byLibJpegTurboAverageValue.Average().ToString("F4");

        //tex2D.LJTMatchResolution(ref tex2D, streamingTexture.width, streamingTexture.height);
        //tex2D.LJTLoadJPG(tex2D, encodedImage);
        //tex2D.Apply();

        var tex2D = new Texture2D(streamingTexture.width, streamingTexture.height);
        tex2D.LoadImage(encodedImage);
        tex2D.Apply();
        LibJpegTurboEncodeRenderer.material.mainTexture = tex2D;
    }
}