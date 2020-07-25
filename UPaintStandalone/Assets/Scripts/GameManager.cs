using SFB;
using System;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

public class GameManager : MonoBehaviour
{
    private enum BrushType
    {
        Brush,
        Eraser,
        Move,
        Fill
    }

    [SerializeField] private UPaintGUI _upaint = null;
    [SerializeField] private RectTransform _drawArea = null;
    [SerializeField] private CameraManager _cameraManager = null;
    [SerializeField] private ColorPicker _colorPicker = null;

    [Header("Settings")]
    [SerializeField] private TMP_InputField _resolutionXInputField = null;
    [SerializeField] private TMP_InputField _resolutionYInputField = null;
    [SerializeField] private Button _applyResolutionButton = null;
    [SerializeField] private TMP_InputField _saveLocationInputField = null;
    [SerializeField] private Button _saveLocationPickButton = null;
    [SerializeField] private Button _openFileLocationButton = null;
    [SerializeField] private TMP_InputField _fileNameInputField = null;
    [SerializeField] private GameObject _overwriteWarningGameObject = null;
    [SerializeField] private Button _exportButton = null;
    [SerializeField] private Animator _animator = null;
    [SerializeField] private TextMeshProUGUI _failedExportText = null;
    [SerializeField] private Button _importButton = null;

    [Header("Toolbar")]
    [SerializeField] private Slider _brushSize = null;
    [SerializeField] private TextMeshProUGUI _brushSizeText = null;
    [SerializeField] private Slider _brushGradient = null;
    [SerializeField] private TextMeshProUGUI _brushGradientText = null;
    [SerializeField] private Button _brushButtonBrush = null;
    [SerializeField] private Button _brushButtonEraser = null;
    [SerializeField] private Button _brushButtonMove = null;
    [SerializeField] private Button _brushButtonFill = null;
    [SerializeField] private Button _undoButton = null;
    [SerializeField] private Button _redoButton = null;
    [SerializeField] private Button _colorPickerButton = null;
    [SerializeField] private Image _colorPickerImage = null;
    [SerializeField] private Button _colorPickerConfirmButton = null;
    [SerializeField] private Button _colorPickerConfirm2Button = null;
    [SerializeField] private Button _colorPickerCancelButton = null;
    [SerializeField] private GameObject _colorToolbarContainer = null;
    [SerializeField] private GameObject _brushSizeToolbarContainer = null;
    [SerializeField] private GameObject _brushGradientToolbarContainer = null;

    private Vector2 _lastMousePosition;
    private BrushType _brushType;
    private DirtyValue<Vector2Int> _upaintResolution;

    private void Awake()
    {
        // settings
        _applyResolutionButton.onClick.AddListener(ApplyResolution);
        _saveLocationPickButton.onClick.AddListener(PickSaveLocation);
        _exportButton.onClick.AddListener(Export);
        _openFileLocationButton.onClick.AddListener(OpenFileLocation);
        _importButton.onClick.AddListener(Import);

        // toolbar
        _brushSize.onValueChanged.AddListener(OnBrushSizeSet);
        OnBrushSizeSet(_brushSize.value);
        _brushGradient.onValueChanged.AddListener(OnBrushGradientSet);
        OnBrushGradientSet(_brushGradient.value);

        _brushButtonBrush.onClick.AddListener(() => SetBrushType(BrushType.Brush));
        _brushButtonEraser.onClick.AddListener(() => SetBrushType(BrushType.Eraser));
        _brushButtonMove.onClick.AddListener(() => SetBrushType(BrushType.Move));
        _brushButtonFill.onClick.AddListener(() => SetBrushType(BrushType.Fill));

        _undoButton.onClick.AddListener(_upaint.Undo);
        _redoButton.onClick.AddListener(_upaint.Redo);

        _colorPickerButton.onClick.AddListener(PickColor);
        _colorPicker.gameObject.SetActive(false);

        _colorPickerCancelButton.onClick.AddListener(() => _colorPicker.gameObject.SetActive(false));
        _colorPickerConfirmButton.onClick.AddListener(() => { _colorPicker.gameObject.SetActive(false); SetColor(_colorPicker.CurrentColor); });
        _colorPickerConfirm2Button.onClick.AddListener(() => { _colorPicker.gameObject.SetActive(false); SetColor(_colorPicker.CurrentColor); });

        SetBrushType(BrushType.Brush);

        LoadPlayerPrefs();
        ApplyResolution();
    }

    private void OnApplicationQuit()
    {
        SavePlayerPrefs();
    }

    private void Update()
    {
        _exportButton.interactable = _saveLocationInputField.text.Length > 0 && _fileNameInputField.text.Length > 0;
        _overwriteWarningGameObject.SetActive(File.Exists(GetExportPath()));

        bool isInputFieldFocused = EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>()?.isFocused == true;

        if (!isInputFieldFocused)
        {
            HandleCameraMouseDragMove();
            HandleUndoRedo();
            HandleMouseWheel();
            HandleWASDMovement();
            HandleBrushPicking();
        }

        UpdateDrawArea();

        _lastMousePosition = Input.mousePosition;

        _undoButton.interactable = _upaint.CanUndo();
        _redoButton.interactable = _upaint.CanRedo();
    }

    private void UpdateDrawArea()
    {
        _upaintResolution.Set(_upaint.Resolution);
        if (_upaintResolution.ClearDirty())
        {
            _drawArea.sizeDelta = new Vector2(_upaintResolution.Get().x, _upaintResolution.Get().y);
            _cameraManager.SetCameraSize(Mathf.Max(_drawArea.sizeDelta.x, _drawArea.sizeDelta.y) * 1.1f, 0);
            _cameraManager.MoveTo(_upaint.transform.position);
        }
    }

    private void HandleBrushPicking()
    {
        if (Input.GetKey(KeyCode.LeftControl))
            return;

        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Alpha1))
            SetBrushType(BrushType.Brush);
        
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Alpha2))
            SetBrushType(BrushType.Eraser);
        
        if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.Alpha3))
            SetBrushType(BrushType.Fill);
        
        if (Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Alpha4))
            SetBrushType(BrushType.Move);
    }

    private void HandleCameraMouseDragMove()
    {
        if (Input.GetKey(KeyCode.Space) || Input.GetMouseButton(2))
        {
            _cameraManager.Move((_lastMousePosition - (Vector2)Input.mousePosition) / _cameraManager.ZoomFactor);
        }
    }

    private void HandleMouseWheel()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            _brushSize.value += Input.GetAxisRaw("Mouse ScrollWheel") * 6 * (_brushSize.value * 0.2f);
        }
        else
        {
            _cameraManager.AdjustCameraSize(Input.GetAxisRaw("Mouse ScrollWheel") * _cameraManager.CurrentSize * -1.5f);
        }
    }

    private void HandleUndoRedo()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                _upaint.Redo();
            }
            else
            {
                _upaint.Undo();
            }
        }

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
        {
            _upaint.Redo();
        }
    }

    private void HandleWASDMovement()
    {
        Vector2 move = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            move += Vector2.up;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            move += Vector2.left;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            move += Vector2.down;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            move += Vector2.right;

        if (move != Vector2.zero)
            move.Normalize();

        _cameraManager.Move(move * Time.deltaTime * _cameraManager.CurrentSize * 1);
    }

    private void ApplyResolution()
    {
        if (!int.TryParse(_resolutionXInputField.text, out int x))
            return;
        if (!int.TryParse(_resolutionYInputField.text, out int y))
            return;
        if (x <= 0 || y <= 0)
            return;

        _upaint.Initialize(new Vector2Int(x, y), FilterMode.Point, 50);
    }

    private void PickSaveLocation()
    {
        string[] selectedFolders = StandaloneFileBrowser.OpenFolderPanel("Select Save Location", _saveLocationInputField.text, false);
        if (selectedFolders.Length != 1)
        {
            return;
        }

        _saveLocationInputField.text = selectedFolders[0];
    }

    private void OpenFileLocation()
    {
        if (string.IsNullOrEmpty(_saveLocationInputField.text))
            return;

        Process.Start("explorer.exe", _saveLocationInputField.text);
    }

    private void Export()
    {
        try
        {
            File.WriteAllBytes(GetExportPath(), _upaint.ExportToPNG());

            _animator.SetTrigger("export successful");
        }
        catch (Exception e)
        {
            _failedExportText.text = "Failed Export: " + e.Message;
            _animator.SetTrigger("export failed");
        }
    }

    private void Import()
    {
        ExtensionFilter[] allowedExtensions = new ExtensionFilter[]
        {
            new ExtensionFilter("Images", "png", "jpg")
        };
        string[] selectedFiles = StandaloneFileBrowser.OpenFilePanel("Select File", _saveLocationInputField.text, allowedExtensions, false);
        if (selectedFiles.Length != 1)
        {
            return;
        }

        byte[] bytes = File.ReadAllBytes(selectedFiles[0]);

        Texture2D texture = new Texture2D(1, 1);
        texture.LoadImage(bytes);
        
        _upaint.ImportImage(texture);
    }

    private string GetExportPath()
    {
        string fullPath = _saveLocationInputField.text;

        fullPath = fullPath.Replace('/', '\\');

        if (!fullPath.EndsWith("\\"))
            fullPath += "\\";

        fullPath += _fileNameInputField.text;

        if (!fullPath.EndsWith(".png"))
            fullPath += ".png";
        return fullPath;
    }

    private void SetBrushType(BrushType brushType)
    {
        _brushType = brushType;

        _brushButtonBrush.targetGraphic.color = brushType == BrushType.Brush ? new Color(0.8f, 1, 0.8f) : Color.white;
        _brushButtonEraser.targetGraphic.color = brushType == BrushType.Eraser ? new Color(0.8f, 1, 0.8f) : Color.white;
        _brushButtonMove.targetGraphic.color = brushType == BrushType.Move ? new Color(0.8f, 1, 0.8f) : Color.white;
        _brushButtonFill.targetGraphic.color = brushType == BrushType.Fill ? new Color(0.8f, 1, 0.8f) : Color.white;

        _colorToolbarContainer.SetActive(brushType == BrushType.Brush || brushType == BrushType.Fill);
        _brushSizeToolbarContainer.SetActive(brushType == BrushType.Brush || brushType == BrushType.Eraser);
        _brushGradientToolbarContainer.SetActive(brushType == BrushType.Brush || brushType == BrushType.Eraser);

        ApplyBrush();
    }

    private void OnBrushSizeSet(float value)
    {
        _brushSizeText.text = "Brush Size: " + Mathf.RoundToInt(value);
        ApplyBrush();
    }

    private void OnBrushGradientSet(float value)
    {
        _brushGradientText.text = "Brush Gradient: " + (Mathf.Round(value * 100f) / 100f);
        ApplyBrush();
    }

    private void ApplyBrush()
    {
        switch (_brushType)
        {
            case BrushType.Brush:
                _upaint.SetDefaultBrush(Mathf.RoundToInt(_brushSize.value), _brushGradient.value);
                break;
            case BrushType.Eraser:
                _upaint.SetEraserBrush(Mathf.RoundToInt(_brushSize.value));
                break;
            case BrushType.Move:
                _upaint.SetMoveBrush();
                break;
            case BrushType.Fill:
                _upaint.SetFillBrush();
                break;
        }
    }

    private void PickColor()
    {
        _colorPicker.gameObject.SetActive(true);
        _colorPicker.CurrentColor = _colorPickerImage.color;
    }

    private void SetColor(Color color)
    {
        _colorPickerImage.color = color;
        _upaint.PaintColor = color;
    }

    private void LoadPlayerPrefs()
    {
        // Settings

        // fbessette: Activating/deactiving the gameobjects fixes a text mesh pro bug where the text mesh position is not properly updated
        _resolutionXInputField.gameObject.SetActive(false);
        _resolutionXInputField.text = PlayerPrefs.GetInt("res-x", 512).ToString();
        _resolutionXInputField.gameObject.SetActive(true);

        _resolutionYInputField.gameObject.SetActive(false);
        _resolutionYInputField.text = PlayerPrefs.GetInt("res-y", 512).ToString();
        _resolutionYInputField.gameObject.SetActive(true);

        _saveLocationInputField.gameObject.SetActive(false);
        _saveLocationInputField.text = PlayerPrefs.GetString("savelocation");
        _saveLocationInputField.gameObject.SetActive(true);

        _fileNameInputField.gameObject.SetActive(false);
        _fileNameInputField.text = PlayerPrefs.GetString("filename", "myExport");
        _fileNameInputField.gameObject.SetActive(true);


        // Toolbar
        _brushSize.value = PlayerPrefs.GetFloat("brushsize", 5);
        _brushGradient.value = PlayerPrefs.GetFloat("brushgradient", 0.1f);

        Color32 color = new Color32()
        {
            r = (byte)PlayerPrefs.GetInt("color-r", 255),
            g = (byte)PlayerPrefs.GetInt("color-g", 255),
            b = (byte)PlayerPrefs.GetInt("color-b", 255),
            a = (byte)PlayerPrefs.GetInt("color-a", 255),
        };
        SetColor(color);
    }

    private void SavePlayerPrefs()
    {
        // Settings
        if (int.TryParse(_resolutionXInputField.text, out int x))
            PlayerPrefs.SetInt("res-x", x);
        if (int.TryParse(_resolutionYInputField.text, out int y))
            PlayerPrefs.SetInt("res-y", y);
        PlayerPrefs.SetString("savelocation", _saveLocationInputField.text);
        PlayerPrefs.SetString("filename", _fileNameInputField.text);

        // Toolbar
        PlayerPrefs.SetFloat("brushsize", _brushSize.value);
        PlayerPrefs.SetFloat("brushgradient", _brushGradient.value);

        Color32 color = _colorPickerImage.color;
        PlayerPrefs.SetInt("color-r", color.r);
        PlayerPrefs.SetInt("color-g", color.g);
        PlayerPrefs.SetInt("color-b", color.b);
        PlayerPrefs.SetInt("color-a", color.a);

        PlayerPrefs.Save();
    }
}
