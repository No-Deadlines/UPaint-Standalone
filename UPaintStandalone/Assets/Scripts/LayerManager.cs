using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngineX;

public class LayerManager : MonoBehaviour
{
    [SerializeField] private Button _addButton = null;
    [SerializeField] private Button _buttonPrefab = null;
    [SerializeField] private Transform _layerButtonContainer = null;
    [SerializeField] private UPaintGUI _upaint = null;
    [SerializeField] private Sprite _visibleSprite = null;
    [SerializeField] private Sprite _hiddenSprite = null;

    private List<Button> _layerButtons = new List<Button>();

    void Start()
    {
        _addButton.onClick.AddListener(() => _upaint.AddLayer());
        _upaint.AddLayer();
    }

    private void Update()
    {
        int i = 0;
        for (; i < _upaint.LayerCount; i++)
        {
            if (i >= _layerButtons.Count)
            {
                int index = i;
                var newLayerButton = Instantiate(_buttonPrefab, _layerButtonContainer);

                newLayerButton.onClick.AddListener(() => _upaint.CurrentLayerIndex = index);
                newLayerButton.transform.Find("X Button").GetComponent<Button>().onClick.AddListener(() => _upaint.RemoveLayer(index));
                newLayerButton.transform.Find("Up Button").GetComponent<Button>().onClick.AddListener(() => _upaint.MoveLayerUp(index));
                newLayerButton.transform.Find("Down Button").GetComponent<Button>().onClick.AddListener(() => _upaint.MoveLayerDown(index));
                newLayerButton.transform.Find("Visibility Button").GetComponent<Button>().onClick.AddListener(() => _upaint.SetLayerVisible(index, !_upaint.IsLayerVisible(index)));
                newLayerButton.transform.SetSiblingIndex(2);

                _layerButtons.Add(newLayerButton);
            }

            _layerButtons[i].gameObject.SetActive(true);
            _layerButtons[i].transform.Find("X Button").GetComponent<Button>().interactable = _upaint.LayerCount > 1;
            _layerButtons[i].transform.Find("Up Button").GetComponent<Button>().interactable = i < _upaint.LayerCount - 1;
            _layerButtons[i].transform.Find("Down Button").GetComponent<Button>().interactable = i > 0;
            _layerButtons[i].transform.Find("Selected Frame").gameObject.SetActive(_upaint.CurrentLayerIndex == i);
            _layerButtons[i].transform.Find("Visibility Button").Find("Visibility Image").GetComponent<Image>().sprite = _upaint.IsLayerVisible(i) ? _visibleSprite : _hiddenSprite;
            _layerButtons[i].GetComponent<RawImage>().texture = _upaint.GetLayerTexture(i);
        }

        for (int r = _layerButtons.Count - 1; r >= i; r--)
        {
            _layerButtons[r].gameObject.SetActive(false);
        }
    }
}