using UnityEngine;
using UnityEngine.UI;
using CCC.UPaint;
using Unity.Mathematics;
using System.Linq.Expressions;

public class UPaintLayerGUI : MonoBehaviour
{
    [SerializeField] private Vector2Int _resolution = new Vector2Int(10, 10);
    [SerializeField] private FilterMode _textureFiltering = FilterMode.Point;
    [SerializeField] private int _historicCapacity = 10;

    private RawImage _mainRenderImage;
    private RawImage _previewRenderImage;
    private Texture2D _mainRenderTexture;
    private Texture2D _previewRenderTexture;
    private UPaintCanvas _canvas;
    private bool _resetCanvasOnUpdate;
    private byte[] _dataToUseOnReset;

    public Vector2Int Resolution => _resolution;
    public FilterMode TextureFiltering => _textureFiltering;
    public int HistoryCapacity => _historicCapacity;
    public Texture2D RenderTexture => _mainRenderTexture;
    public UPaintCanvas Canvas => _canvas;

    /// <summary>
    /// This will clear the canvas
    /// </summary>
    public void Initialize(Vector2Int renderResolution, FilterMode textureFiltering, int historyCapacity)
    {
        _dataToUseOnReset = null;
        _resolution = renderResolution;
        _textureFiltering = textureFiltering;
        _historicCapacity = historyCapacity;

        _resetCanvasOnUpdate = true;
    }
    /// <summary>
    /// This will clear the canvas
    /// </summary>
    public void Initialize(byte[] imageData, FilterMode textureFiltering, int historyCapacity)
    {
        _dataToUseOnReset = imageData;
        _textureFiltering = textureFiltering;
        _historicCapacity = historyCapacity;

        _resetCanvasOnUpdate = true;
    }

    public bool CanRedo()
    {
        return _canvas != null && _canvas.AvailableRedos > 0;
    }

    public void Redo()
    {
        _canvas?.Redo();
    }

    public bool CanUndo()
    {
        return _canvas != null && _canvas.AvailableUndos > 0;
    }

    public void Undo()
    {
        _canvas?.Undo();
    }

    private void Awake()
    {
        RawImage SpawnRawImage(string name)
        {
            var rawImage = new GameObject(name, typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            var rectTransform = rawImage.rectTransform;
            rectTransform.SetParent(transform);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            return rawImage;
        }

        Texture2D CreateTexture(string name)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            texture.name = name;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = _textureFiltering;
            return texture;
        }

        _mainRenderTexture = CreateTexture("Main Render Texture (generated)");
        _previewRenderTexture = CreateTexture("Preview Render Texture (generated)");

        _mainRenderImage = SpawnRawImage("Main Render Image (generated)");
        _mainRenderImage.texture = _mainRenderTexture;
        _previewRenderImage = SpawnRawImage("Preview Render Image (generated)");
        _previewRenderImage.texture = _previewRenderTexture;

        _resetCanvasOnUpdate = true;
    }

    public void PressBursh<T>(T brush, float2 pixelCoordinate, Color color) where T : IUPaintBrush
    {
        _canvas?.PressBursh(brush, pixelCoordinate, color);
    }
    public void HoldBursh<T>(T brush, float2 pixelCoordinate, Color color) where T : IUPaintBrush
    {
        _canvas?.HoldBursh(brush, pixelCoordinate, color);
    }
    public void ReleaseBursh<T>(T brush, float2 pixelCoordinate, Color color) where T : IUPaintBrush
    {
        _canvas?.ReleaseBursh(brush, pixelCoordinate, color);
    }

    public void ApplyChangesIfPossible() => _canvas?.ApplyChangesIfPossible();

    void Update()
    {
        if (_resetCanvasOnUpdate)
        {
            _resetCanvasOnUpdate = false;
            ResetCanvas();
        }
    }

    private void ResetCanvas()
    {
        _canvas?.Dispose();

        Color? initColor;

        if (_dataToUseOnReset != null)
        {
            _mainRenderTexture.LoadImage(_dataToUseOnReset);
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            tex.LoadImage(_dataToUseOnReset);
            var x = new GameObject("test").AddComponent<RawImage>();
            x.texture = tex;
            _resolution = new Vector2Int(_mainRenderTexture.width, _mainRenderTexture.height);
            _dataToUseOnReset = null;
            initColor = null;
        }
        else
        {
            initColor = new Color(255, 255, 255, 0);
            _mainRenderTexture.Resize(_resolution.x, _resolution.y);
        }

        _previewRenderTexture.Resize(_resolution.x, _resolution.y);
        _mainRenderTexture.filterMode = _textureFiltering;
        _previewRenderTexture.filterMode = _textureFiltering;

        // setup UPaint canvas
        _canvas = new UPaintCanvas(_mainRenderTexture, _previewRenderTexture, _historicCapacity, initColor);

    }

    private void OnDestroy()
    {
        _canvas?.Dispose();

        Destroy(_mainRenderImage);
        Destroy(_previewRenderTexture);
    }
}