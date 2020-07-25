using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BrushCursor : MonoBehaviour
{
    [SerializeField] private UPaintGUI _upaint = null;
    [SerializeField] private CameraManager _upaintCamera = null;
    [SerializeField] private ColorPicker _colorPicker = null;
    [SerializeField] private Sprite _brushSprite = null;
    [SerializeField] private Sprite _eraserSprite = null;
    [SerializeField] private Sprite _fillSprite = null;
    [SerializeField] private Sprite _moveSprite = null;
    [SerializeField] private Vector2 _defaultCursorSize = default;

    private RectTransform _rectTransform;
    private Image _image;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    private bool _imageEnabled = false;
    private bool _applyThickness = false;
    private int _thickness = -1;
    private bool _applyColor = false;
    private Sprite _currentSprite;

    void Update()
    {
        UpdateData();
        UpdateView();
    }

    private void LateUpdate()
    {
        _rectTransform.position = Input.mousePosition;
    }

    private void UpdateData()
    {
        if (_upaint.IsPickingColor)
        {
            _imageEnabled = false;
        }
        else if (_upaint.CurrentBrush != null)
        {
            if (_upaint.CurrentBrush is CCC.UPaint.FreeLineBrush freelineBrush)
            {
                _currentSprite = _brushSprite;
                _applyColor = true;
                _applyThickness = true;
                _thickness = freelineBrush.Thickness;
            }

            if (_upaint.CurrentBrush is CCC.UPaint.EraseBrush eraser)
            {
                _currentSprite = _eraserSprite;
                _applyColor = false;
                _applyThickness = true;
                _thickness = eraser.Thickness;
            }

            if (_upaint.CurrentBrush is CCC.UPaint.FillBrush)
            {
                _currentSprite = _fillSprite;
                _applyColor = true;
                _applyThickness = false;
            }

            if (_upaint.CurrentBrush is CCC.UPaint.MoveBrush)
            {
                _currentSprite = _moveSprite;
                _applyColor = false;
                _applyThickness = false;
            }

            _imageEnabled = true;
        }
        else
        {
            _imageEnabled = false;
        }
    }

    private void UpdateView()
    {
        _image.enabled = _imageEnabled;

        if (_applyThickness)
        {
            Vector2 pixelSizeRatio = (_upaint.GetComponent<RectTransform>().rect.size / _upaint.Resolution) * _upaintCamera.ZoomFactor;
            _rectTransform.sizeDelta = pixelSizeRatio * _thickness;
        }
        else
        {
            _rectTransform.sizeDelta = _defaultCursorSize;
        }

        if (_applyColor)
        {
            if (_colorPicker.gameObject.activeInHierarchy)
            {
                _image.color = _colorPicker.CurrentColor;
            }
            else
            {
                _image.color = _upaint.PaintColor;
            }
        }
        else
        {
            _image.color = Color.white;
        }

        _image.sprite = _currentSprite;
    }
}
