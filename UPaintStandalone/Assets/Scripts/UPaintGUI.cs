﻿using UnityEngine;
using Unity.Mathematics;
using UnityEngineX;
using UnityEngine.UI;
using CCC.UPaint;
using System.IO;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UPaintGUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Color _paintColor = Color.white;
    [SerializeField] private Vector2Int _resolution = new Vector2Int(10, 10);
    [SerializeField] private FilterMode _textureFiltering = FilterMode.Point;
    [SerializeField] private int _historicCapacity = 10;
    internal EraseBrush EraseBrush = new EraseBrush();
    internal FreeLineBrush FreeLineBrush = new FreeLineBrush();
    internal FillBrush FillBrush = new FillBrush();
    internal MoveBrush MoveBrush = new MoveBrush();

    private bool _brushWasSet;
    private bool _pointerIn;
    private DirtyValue<bool> _brushPressed;
    private List<UPaintLayerGUI> _layers = new List<UPaintLayerGUI>();

    private UPaintLayerGUI CurrentLayer => CurrentLayerIndex >= 0 && CurrentLayerIndex < _layers.Count ? _layers[CurrentLayerIndex] : null;

    public Vector2Int Resolution => _resolution;
    public IUPaintBrush CurrentBrush { get; private set; }
    public bool IsPickingColor => _isColorPicking.Get();
    public Color PaintColor { get => _paintColor; set => _paintColor = value; }
    public FilterMode TextureFiltering => _textureFiltering;
    public int HistoryCapacity => _historicCapacity;
    public int CurrentLayerIndex { get; set; }
    public int LayerCount => _layers.Count;

    public void AddLayer()
    {
        UPaintLayerGUI newLayer = new GameObject("UPaintCanvasGUI (generated)", typeof(RectTransform), typeof(UPaintLayerGUI)).GetComponent<UPaintLayerGUI>();

        RectTransform rectTransform = newLayer.GetComponent<RectTransform>();
        rectTransform.SetParent(transform);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        _layers.Add(newLayer);

        newLayer.Initialize(_resolution, _textureFiltering, _historicCapacity);

        CurrentLayerIndex = _layers.Count - 1;
    }

    public Texture2D GetLayerTexture(int index)
    {
        if (index < 0 || index >= _layers.Count)
            return null;

        return _layers[index].RenderTexture;
    }

    public void RemoveLayer(int index)
    {
        if (index < 0 || index >= _layers.Count)
            return;

        Destroy(_layers[index].gameObject);
        _layers.RemoveAt(index);

        if (CurrentLayerIndex >= index)
        {
            CurrentLayerIndex--;
        }

        if (CurrentLayerIndex < 0)
            CurrentLayerIndex = 0;
    }

    public void MoveLayerUp(int index)
    {
        if (index < 0 || index >= _layers.Count - 1)
            return;

        var temp = _layers[index];
        _layers[index] = _layers[index + 1];
        _layers[index + 1] = temp;
        _layers[index].transform.SetSiblingIndex(index);

        if (CurrentLayerIndex == index)
            CurrentLayerIndex++;
        else if (CurrentLayerIndex == index + 1)
            CurrentLayerIndex--;
    }

    public void MoveLayerDown(int index)
    {
        if (index <= 0 || index >= _layers.Count)
            return;

        var temp = _layers[index];
        _layers[index] = _layers[index - 1];
        _layers[index - 1] = temp;
        _layers[index - 1].transform.SetSiblingIndex(index - 1);

        if (CurrentLayerIndex == index)
            CurrentLayerIndex--;
        else if (CurrentLayerIndex == index - 1)
            CurrentLayerIndex++;
    }

    public void SetLayerVisible(int index, bool visible)
    {
        if (index < 0 || index >= _layers.Count)
            return;

        _layers[index].gameObject.SetActive(visible);
    }

    public bool IsLayerVisible(int index)
    {
        if (index < 0 || index >= _layers.Count)
            return false;

        return _layers[index].gameObject.activeSelf;
    }

    /// <summary>
    /// This will clear the canvases
    /// </summary>
    public void Initialize(Vector2Int renderResolution, FilterMode textureFiltering, int historyCapacity)
    {
        // remove all existing layers
        while (_layers.Count > 1)
        {
            RemoveLayer(_layers.Count - 1);
        }

        for (int i = 0; i < _layers.Count; i++)
        {
            _layers[i].Initialize(renderResolution, textureFiltering, historyCapacity);
        }
        _resolution = renderResolution;
        _textureFiltering = textureFiltering;
        _historicCapacity = historyCapacity;

        CurrentLayerIndex = 0;
    }

    public void SetFillBrush(int extrusionCount = 0)
    {
        FillBrush.ExtrusionCount = extrusionCount;
        CurrentBrush = FillBrush;
        _brushWasSet = true;
    }

    public void SetMoveBrush()
    {
        CurrentBrush = MoveBrush;
        _brushWasSet = true;
    }

    public void SetEraserBrush(int thickness = 10)
    {
        EraseBrush.Thickness = thickness;
        CurrentBrush = EraseBrush;
        _brushWasSet = true;
    }

    public void SetDefaultBrush(int thickness = 10)
    {
        SetDefaultBrush(thickness, 0.1f * Mathf.Clamp01(thickness / 6f));
    }
    public void SetDefaultBrush(int thickness, float gradient01)
    {
        FreeLineBrush.Thickness = thickness;
        FreeLineBrush.Gradient01 = gradient01;
        CurrentBrush = FreeLineBrush;
        _brushWasSet = true;
    }

    public void SetCustomBrush(IUPaintBrush brush)
    {
        CurrentBrush = brush;
        _brushWasSet = true;
    }

    public bool CanRedo()
    {
        return CurrentLayer != null && CurrentLayer.CanRedo();
    }

    public void Redo()
    {
        CurrentLayer?.Redo();
    }

    public bool CanUndo()
    {
        return CurrentLayer != null && CurrentLayer.CanUndo();
    }

    public void Undo()
    {
        CurrentLayer?.Undo();
    }

    /// <summary>
    /// Export all visible layers to PNG data
    /// </summary>
    public byte[] ExportToPNG()
    {
        UPaintLayer[] layers = _layers.Where(l => l.gameObject.activeSelf).Select(l => l.Layer).ToArray();

        return ExportToPNGInternal(layers);
    }

    /// <summary>
    /// Export layers to PNG data
    /// </summary>
    public byte[] ExportToPNG(params int[] layerIndexes)
    {
        List<UPaintLayer> layers = new List<UPaintLayer>(layerIndexes.Length);
        for (int i = 0; i < _layers.Count; i++)
        {
            if (layerIndexes.Contains(i))
            {
                layers.Add(_layers[i].Layer);
            }
        }

        return ExportToPNGInternal(layers.ToArray());
    }

    public void ImportImage(Texture2D texture)
    {
        if (_layers.Count == 1 && _layers[0].IsEmptyEmpty)
        {
            Initialize(new Vector2Int(texture.width, texture.height), _textureFiltering, _historicCapacity);
        }
        else
        {
            AddLayer();
            CurrentLayerIndex = _layers.Count - 1;
        }

        ImportImage(texture, CurrentLayerIndex);
    }

    public void ImportImage(Texture2D texture, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= _layers.Count)
            return;

        _layers[layerIndex].Initialize(texture, _textureFiltering, _historicCapacity);
    }

    public void StartColorPicking() => StartColorPicking(Camera.main);

    public void StartColorPicking(Camera camera)
    {
        if (_isColorPicking.Get())
            return;

        _colorPickCamera = camera ?? throw new System.ArgumentNullException(nameof(camera));
        _beforeColorPickColor = PaintColor;
        _isColorPicking.Set(true);
        _brushPressed.Set(false);
    }

    public void CancelColorPicking()
    {
        if (!_isColorPicking.Get())
            return;

        _isColorPicking.Set(false);
        PaintColor = _beforeColorPickColor;
    }

    private void CompleteColorPicking()
    {
        if (!_isColorPicking.Get())
            return;

        _isColorPicking.Set(false);
    }

    private void Awake()
    {
        if (!_brushWasSet)
            SetDefaultBrush();
    }

    void Update()
    {
        if (CurrentLayer != null)
        {
            CurrentLayer.ApplyChangesIfPossible();
        }

        UpdateBrushPressedState();
        HandleBrushPress();

        HandleColorPicking();

        if (_isColorPicking.ClearDirty())
        {
            if (_isColorPicking.Get())
            {
                _colorPickCoroutine = StartCoroutine(UpdateColorPicking());
            }
            else if (_colorPickCoroutine != null)
            {
                StopCoroutine(_colorPickCoroutine);
                _colorPickCoroutine = null;
            }
        }
    }

    private void HandleColorPicking()
    {
        if (_isColorPicking.Get())
        {
            if (Input.GetMouseButtonDown(0))
            {
                CompleteColorPicking();
            }
            else if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelColorPicking();
            }
        }
    }

    private void UpdateBrushPressedState()
    {
        if (CurrentBrush != null && _pointerIn && !_isColorPicking.Get())
        {
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                _brushPressed.Set(true);
            }
        }

        // pointer up
        if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            _brushPressed.Set(false);
        }
    }

    private void HandleBrushPress()
    {
        if (_brushPressed.ClearDirty()) // on change
        {
            if (_brushPressed.Get())
            {
                // pointer down
                if (CurrentLayer != null)
                {
                    float2 pixelCoordinate = DisplayPositionToLayerCoordinate(Input.mousePosition);
                    CurrentLayer.PressBursh(CurrentBrush, pixelCoordinate, PaintColor, Input.GetMouseButtonDown(0) ? MouseButton.Left : MouseButton.Right);
                }
            }
            else
            {
                // pointer up
                if (CurrentLayer != null)
                {
                    float2 pixelCoordinate = DisplayPositionToLayerCoordinate(Input.mousePosition);

                    CurrentLayer.ReleaseBursh(CurrentBrush, pixelCoordinate, PaintColor, Input.GetMouseButtonUp(0) ? MouseButton.Left : MouseButton.Right);
                }
            }
        }
        else
        {
            // pointer hold
            if (_brushPressed.Get())
            {
                if (CurrentLayer != null)
                {
                    float2 pixelCoordinate = DisplayPositionToLayerCoordinate(Input.mousePosition);
                    CurrentLayer.HoldBursh(CurrentBrush, pixelCoordinate, PaintColor, Input.GetMouseButton(0) ? MouseButton.Left : MouseButton.Right);
                }
            }
        }
    }

    private float2 DisplayPositionToLayerCoordinate(Vector2 mousePosition)
    {
        Rect renderImageRect = CurrentLayer.GetComponent<RectTransform>().GetScreenRect();

        // get position in 'rect-space' (from (0,0) to (1,1))
        Vector2 rectSpacePosition = renderImageRect.GetPointInRectSpace(mousePosition);

        // scale position in 'pixel-space' (from (0,0) to (resX, resY))
        rectSpacePosition.Scale(CurrentLayer.Resolution);

        // offset position half a pixel to account for pixel center
        rectSpacePosition -= new Vector2(0.5f, 0.5f);

        return new float2(rectSpacePosition.x, rectSpacePosition.y);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _layers.Count; i++)
        {
            Destroy(_layers[i].gameObject);
        }
        FillBrush?.Dispose();
        MoveBrush?.Dispose();
    }

    private byte[] ExportToPNGInternal(UPaintLayer[] layers)
    {
        var texture = new Texture2D(_resolution.x, _resolution.y, TextureFormat.RGBA32, mipChain: false);
        texture.filterMode = _textureFiltering;
        texture.wrapMode = TextureWrapMode.Clamp;

        UPaintLayer.MergeLayers(texture, layers).Complete();

        byte[] result = texture.EncodeToPNG();

        Destroy(texture);

        return result;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerIn = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pointerIn = false;
    }

    private WaitForEndOfFrame _frameEnd = new WaitForEndOfFrame();
    private Texture2D _colorPickTexture;
    private Coroutine _colorPickCoroutine;
    private Camera _colorPickCamera;
    private Color _beforeColorPickColor;
    private DirtyValue<bool> _isColorPicking;

    public IEnumerator UpdateColorPicking()
    {
        while (true)
        {
            yield return _frameEnd;

            if (_colorPickTexture == null)
                _colorPickTexture = new Texture2D(1, 1, TextureFormat.RGB24, false);

            Rect viewRect = _colorPickCamera.pixelRect;

            Rect pickRect = new Rect(Input.mousePosition, Vector2.one);
            pickRect.x = Mathf.Clamp(pickRect.x, viewRect.xMin, viewRect.xMax - 1);
            pickRect.y = Mathf.Clamp(pickRect.y, viewRect.yMin, viewRect.yMax - 1);

            _colorPickTexture.ReadPixels(pickRect, 0, 0, false);
            _colorPickTexture.Apply(false);

            PaintColor = _colorPickTexture.GetPixel(0, 0);
        }
    }
}

internal static class RectExtensions
{
    public static Vector2 GetPointInRectSpace(this in Rect rect, Vector2 position)
    {
        return new Vector2()
        {
            x = (position.x - rect.x) / rect.width,
            y = (position.y - rect.y) / rect.height
        };
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UPaintGUI))]
public class UPaintGUIEditor : Editor
{
    private SerializedProperty _resolution;
    private SerializedProperty _textureFiltering;
    private SerializedProperty _historicCapacity;
    private SerializedProperty _paintColor;

    private void OnEnable()
    {
        _resolution = serializedObject.FindProperty("_resolution");
        _textureFiltering = serializedObject.FindProperty("_textureFiltering");
        _historicCapacity = serializedObject.FindProperty("_historicCapacity");
        _paintColor = serializedObject.FindProperty("_paintColor");
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var upaint = ((UPaintGUI)target);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(_resolution);
        EditorGUILayout.PropertyField(_textureFiltering);
        EditorGUILayout.PropertyField(_historicCapacity);

        if (Application.isPlaying)
        {
            if (GUILayout.Button("Reinitialize"))
            {
                upaint.Initialize(_resolution.vector2IntValue, (FilterMode)_textureFiltering.enumValueIndex, _historicCapacity.intValue);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.PropertyField(_paintColor);

        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Brush");

            int brushIndex;
            if (upaint.CurrentBrush == upaint.FreeLineBrush)
                brushIndex = 0;
            else if (upaint.CurrentBrush == upaint.FillBrush)
                brushIndex = 1;
            else if (upaint.CurrentBrush == upaint.MoveBrush)
                brushIndex = 2;
            else
                brushIndex = 3;

            int newBrushIndex = GUILayout.SelectionGrid(brushIndex, new string[] { "Default", "Fill", "Move", "Custom" }, 4);

            if (newBrushIndex != brushIndex)
            {
                if (newBrushIndex == 0)
                    upaint.SetDefaultBrush();
                else if (newBrushIndex == 1)
                    upaint.SetFillBrush();
                else if (newBrushIndex == 2)
                    upaint.SetMoveBrush();
                else
                    upaint.SetCustomBrush(null);
            }


            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            bool wasGUIEnabled = GUI.enabled;

            GUI.enabled = upaint.CanUndo();
            if (GUILayout.Button("Undo"))
            {
                upaint.Undo();
            }

            GUI.enabled = upaint.CanRedo();
            if (GUILayout.Button("Redo"))
            {
                upaint.Redo();
            }

            GUI.enabled = wasGUIEnabled;
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif