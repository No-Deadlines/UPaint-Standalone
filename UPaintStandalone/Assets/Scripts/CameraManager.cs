using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private float _targetCameraSize = 1;
    private float _lerpCamSizeSource = 1;
    private float _lerpCamSizeTime = 0;
    private float _lerpCamSizeDuration = 0;
    private Camera _camera;

    public float CurrentSize { get; private set; } = 1;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    public void Move(Vector2 move)
    {
        transform.position += new Vector3(move.x, move.y, 0);
    }

    public void MoveTo(Vector2 destination)
    {
        transform.position = new Vector3(destination.x, destination.y, -1);
    }

    public float ZoomFactor => Screen.height / (_camera.orthographicSize * 2);

    public void AdjustCameraSize(float amount)
    {
        _targetCameraSize += amount;
    }

    public void SetCameraSize(float destination, float duration)
    {
        _lerpCamSizeSource = CurrentSize;
        _lerpCamSizeDuration = duration;
        _lerpCamSizeTime = 0;

        _targetCameraSize = destination;
    }

    private void LateUpdate()
    {
        _lerpCamSizeTime += Time.deltaTime;

        CurrentSize = Mathf.Lerp(_lerpCamSizeSource, _targetCameraSize, (_lerpCamSizeDuration > 0 ? _lerpCamSizeTime / _lerpCamSizeDuration : 1));

        _camera.orthographicSize = CurrentSize / 2;
    }
}
