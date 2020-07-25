using UnityEngine;
using UnityEngine.UI;
using CCC.UPaint;
using Unity.Mathematics;
using System.Linq.Expressions;
using System;
using Unity.Collections;
using UnityEngineX;
using Unity.Jobs;
using Unity.Burst;

public class UPaintLayerGUI : MonoBehaviour
{
    [SerializeField] private Vector2Int _resolution = new Vector2Int(10, 10);
    [SerializeField] private FilterMode _textureFiltering = FilterMode.Point;
    [SerializeField] private int _historicCapacity = 10;

    private RawImage _mainRenderImage;
    private RawImage _previewRenderImage;
    private Texture2D _mainRenderTexture;
    private Texture2D _previewRenderTexture;
    private UPaintLayer _layer;
    private bool _resetLayerOnUpdate;
    private Texture2D _initTexture;
    private bool _resetResolution;

    public Vector2Int Resolution => _resolution;
    public FilterMode TextureFiltering => _textureFiltering;
    public int HistoryCapacity => _historicCapacity;
    public Texture2D RenderTexture => _mainRenderTexture;
    public UPaintLayer Layer => _layer;
    public bool IsEmptyEmpty { get; private set; } = true;

    /// <summary>
    /// This will clear the layer
    /// </summary>
    public void Initialize(Vector2Int renderResolution, FilterMode textureFiltering, int historyCapacity)
    {
        _initTexture = null;
        _resolution = renderResolution;
        _textureFiltering = textureFiltering;
        _historicCapacity = historyCapacity;

        _resetResolution = true;
        _resetLayerOnUpdate = true;
    }
    /// <summary>
    /// This will clear the layer
    /// </summary>
    public void Initialize(Texture2D imageData, FilterMode textureFiltering, int historyCapacity)
    {
        _initTexture = imageData;
        _textureFiltering = textureFiltering;
        _historicCapacity = historyCapacity;

        _resetLayerOnUpdate = true;
    }

    public bool CanRedo()
    {
        return _layer != null && _layer.AvailableRedos > 0;
    }

    public void Redo()
    {
        IsEmptyEmpty = false;
        _layer?.Redo();
    }

    public bool CanUndo()
    {
        return _layer != null && _layer.AvailableUndos > 0;
    }

    public void Undo()
    {
        IsEmptyEmpty = false;
        _layer?.Undo();
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

        _resetLayerOnUpdate = true;
    }

    public void PressBursh<T>(T brush, float2 pixelCoordinate, Color color, MouseButton mouseButton) where T : IUPaintBrush
    {
        IsEmptyEmpty = false;
        _layer?.PressBursh(brush, pixelCoordinate, color, mouseButton);
    }
    public void HoldBursh<T>(T brush, float2 pixelCoordinate, Color color, MouseButton mouseButton) where T : IUPaintBrush
    {
        IsEmptyEmpty = false;
        _layer?.HoldBursh(brush, pixelCoordinate, color, mouseButton);
    }
    public void ReleaseBursh<T>(T brush, float2 pixelCoordinate, Color color, MouseButton mouseButton) where T : IUPaintBrush
    {
        IsEmptyEmpty = false;
        _layer?.ReleaseBursh(brush, pixelCoordinate, color, mouseButton);
    }

    public void ApplyChangesIfPossible() => _layer?.ApplyChangesIfPossible();

    void Update()
    {
        if (_resetLayerOnUpdate)
        {
            _resetLayerOnUpdate = false;
            ResetLayer();
        }
    }

    private void ResetLayer()
    {
        _layer?.Dispose();

        Color? initColor;

        if (_resetResolution)
        {
            _resetResolution = false;
            _mainRenderTexture.Resize(_resolution.x, _resolution.y);
        }

        if (_initTexture != null)
        {
            // Load data in new texture
            // copy colors to existing texture (this takes care of converting betweent the different texture formats)
            int importWidth = Mathf.Min(_initTexture.width, _resolution.x);
            int importHeight = Mathf.Min(_initTexture.height, _resolution.y);
            Color[] newPixels = _initTexture.GetPixels(0, 0, importWidth, importHeight);
            _mainRenderTexture.SetPixels(0, 0, importWidth, importHeight, newPixels);

            _initTexture = null;
            initColor = null;
            IsEmptyEmpty = false;
        }
        else
        {
            IsEmptyEmpty = true;
            initColor = new Color(255, 255, 255, 0);
        }

        _previewRenderTexture.Resize(_resolution.x, _resolution.y);
        _mainRenderTexture.filterMode = _textureFiltering;
        _previewRenderTexture.filterMode = _textureFiltering;

        // setup UPaint layer
        _layer = new UPaintLayer(_mainRenderTexture, _previewRenderTexture, _historicCapacity, initColor);

    }

    private void OnDestroy()
    {
        _layer?.Dispose();

        Destroy(_mainRenderImage);
        Destroy(_previewRenderTexture);
    }
}