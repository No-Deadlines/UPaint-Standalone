using UnityEngine;
using Unity.Mathematics;
using UnityEngineX;
using UnityEngine.UI;
using CCC.UPaint;
using System.IO;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;

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
    private bool _brushPressed;
    private List<UPaintLayerGUI> _layers = new List<UPaintLayerGUI>();

    public Vector2Int Resolution => _resolution;
    public IUPaintBrush CurrentBrush { get; private set; }
    public Color PaintColor { get => _paintColor; set => _paintColor = value; }
    public FilterMode TextureFiltering => _textureFiltering;
    public int HistoryCapacity => _historicCapacity;

    public int CurrentLayerIndex { get; set; }
    public UPaintLayerGUI CurrentLayer => CurrentLayerIndex >= 0 && CurrentLayerIndex < _layers.Count ? _layers[CurrentLayerIndex] : null;
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

        if (CurrentLayerIndex == _layers.Count || CurrentLayerIndex == index)
            CurrentLayerIndex--;
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
    /// This will clear the canvas
    /// </summary>
    public void Initialize(Vector2Int renderResolution, FilterMode textureFiltering, int historyCapacity)
    {
        for (int i = 0; i < _layers.Count; i++)
        {
            _layers[i].Initialize(renderResolution, textureFiltering, historyCapacity);
        }
        _resolution = renderResolution;
        _textureFiltering = textureFiltering;
        _historicCapacity = historyCapacity;
    }

    public void SetFillBrush(int extrusionCount = 1)
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

    public byte[] ExportToPNG()
    {
        var texture = new Texture2D(_resolution.x, _resolution.y, TextureFormat.RGBA32, mipChain: false);
        texture.filterMode = _textureFiltering;
        texture.wrapMode = TextureWrapMode.Clamp;

        UPaintCanvas[] canvases = _layers.Where(l => l.gameObject.activeSelf).Select(l => l.Canvas).ToArray();

        UPaintCanvas.MergeCanvases(texture, canvases).Complete();

        byte[] result = texture.EncodeToPNG();

        Destroy(texture);

        return result;
    }

    public void ImportFromImage(byte[] imageData)
    {
        while (LayerCount > 1)
        {
            RemoveLayer(LayerCount - 1);
        }

        if (LayerCount == 0)
            AddLayer();

        _layers[0].Initialize(imageData, _textureFiltering, _historicCapacity);
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

        if (CurrentBrush != null && _pointerIn)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (CurrentLayer != null)
                {
                    float2 pixelCoordinate = DisplayPositionToLayerCoordinate(Input.mousePosition);
                    CurrentLayer.PressBursh(CurrentBrush, pixelCoordinate, PaintColor);
                }
                _brushPressed = true;
            }
        }

        if (_brushPressed)
        {
            // pointer hold
            if (Input.GetMouseButton(0))
            {
                if (CurrentLayer != null)
                {
                    float2 pixelCoordinate = DisplayPositionToLayerCoordinate(Input.mousePosition);
                    CurrentLayer.HoldBursh(CurrentBrush, pixelCoordinate, PaintColor);
                }
            }

            // pointer up
            if (Input.GetMouseButtonUp(0))
            {
                if (CurrentLayer != null)
                {
                    float2 pixelCoordinate = DisplayPositionToLayerCoordinate(Input.mousePosition);

                    CurrentLayer.ReleaseBursh(CurrentBrush, pixelCoordinate, PaintColor);
                }
                _brushPressed = false;
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

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerIn = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pointerIn = false;
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