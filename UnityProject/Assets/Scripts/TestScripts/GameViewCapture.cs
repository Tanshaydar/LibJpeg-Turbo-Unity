using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LibJpegTurboUnity;
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

    public TextMeshProUGUI ByUnityEncoderTiming;
    public TextMeshProUGUI ByUnityEncoderTimingV2;
    public TextMeshProUGUI ByLibJpegTurboEncoderTiming;
    public TextMeshProUGUI ByLibJpegTurboEncoderTimingV2;

    private Stopwatch _sw1 = new();
    private double _timeKeeperUnity;
    private Stopwatch _sw2 = new();
    private double _timeKeeperLibJpegTurbo;
    private readonly FixedSizedQueue<long> _byLibJpegTurboEncoderAverageValue = new FixedSizedQueue<long>(250);
    private readonly FixedSizedQueue<double> _byLibJpegTurboEncoderAverageValueV2 = new FixedSizedQueue<double>(250);
    private readonly FixedSizedQueue<long> _byUnityEncoderAverageValue = new FixedSizedQueue<long>(250);
    private readonly FixedSizedQueue<double> _byUnityEncoderAverageValueV2 = new FixedSizedQueue<double>(250);

    private float _lastCapture = -1f;

    private LJTCompressor _ljtCompressor;
    private LJTDecompressor _ljtDecomporessor;

    private RenderTexture streamingTexture;
    private Texture2D _byUnityTex2D;
    private Texture2D _byLibJpegTurboTex2D;

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

        _ljtCompressor = new LJTCompressor();
        _ljtDecomporessor = new LJTDecompressor();
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
        _ljtDecomporessor.Dispose();
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
        {
            return;
        }

        _sw1.Restart();
        _timeKeeperUnity = Time.realtimeSinceStartupAsDouble;
        byte[] encodedImageUnity = ImageConversion.EncodeArrayToJPG(data, streamingTexture.graphicsFormat,
            (uint) streamingTexture.width, (uint) streamingTexture.height, 0, Quality);
        _sw1.Stop();
        _byUnityEncoderAverageValue.Enqueue(_sw1.ElapsedMilliseconds);
        _byUnityEncoderAverageValueV2.Enqueue(Time.realtimeSinceStartupAsDouble - _timeKeeperUnity);
        ByUnityEncoderTiming.text = "Unity Enc: " + _byUnityEncoderAverageValue.Average().ToString("F8");
        ByUnityEncoderTimingV2.text = "Unity Enc: " + _byUnityEncoderAverageValueV2.Average().ToString("F8");

        _byUnityTex2D = new Texture2D(streamingTexture.width, streamingTexture.height);
        _byUnityTex2D.LoadImage(encodedImageUnity);
        _byUnityTex2D.Apply();
        UnityEncodeRenderer.material.mainTexture = _byUnityTex2D;
    }

    private void AssignTextureByLibJpegTurbo(byte[] data)
    {
        if (streamingTexture == null)
        {
            return;
        }

        LJTPixelFormat pixelFormat = data.Length / (streamingTexture.height * streamingTexture.width) == 3
            ? LJTPixelFormat.RGB
            : LJTPixelFormat.RGBA;

        _sw2.Restart();
        _timeKeeperLibJpegTurbo = Time.realtimeSinceStartupAsDouble;
        byte[] encodedImageLibJpegTurbo =
            _ljtCompressor.EncodeJPG(data, streamingTexture.width, streamingTexture.height, pixelFormat, Quality);
        _sw2.Stop();
        _byLibJpegTurboEncoderAverageValue.Enqueue(_sw2.ElapsedMilliseconds);
        _byLibJpegTurboEncoderAverageValueV2.Enqueue(Time.realtimeSinceStartupAsDouble - _timeKeeperLibJpegTurbo);
        ByLibJpegTurboEncoderTiming.text =
            "LibJpegTurbo Enc: " + _byLibJpegTurboEncoderAverageValue.Average().ToString("F8");
        ByLibJpegTurboEncoderTimingV2.text= "LibJpegTurbo Enc: " + _byLibJpegTurboEncoderAverageValueV2.Average().ToString("F8");

        _byLibJpegTurboTex2D = new Texture2D(streamingTexture.width, streamingTexture.height);
        _byLibJpegTurboTex2D.LoadImage(encodedImageLibJpegTurbo);
        _byLibJpegTurboTex2D.Apply();
        LibJpegTurboEncodeRenderer.material.mainTexture = _byLibJpegTurboTex2D;
    }
}

public class FixedSizedQueue<T> : Queue<T>
{
    private readonly int maxQueueSize;
    private readonly object syncRoot = new object();

    public FixedSizedQueue(int maxQueueSize)
    {
        this.maxQueueSize = maxQueueSize;
    }

    public new void Enqueue(T item)
    {
        lock (syncRoot)
        {
            base.Enqueue(item);
            if (Count > maxQueueSize)
                Dequeue(); // Throw away
        }
    }
}