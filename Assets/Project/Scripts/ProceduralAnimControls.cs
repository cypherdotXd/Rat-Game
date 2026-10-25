using UnityEngine;
using UnityEngine.Serialization;

public class ProceduralAnimControls : MonoBehaviour
{
	[SerializeField] private Transform _target;
	[SerializeField] private Transform _mesh;
	[SerializeField] private float _followThreshold = 8f;

	private Quaternion _currentRotation;
	private Quaternion _initialLocalRotation;

	private void Awake()
	{
		if (!_mesh) _mesh = transform;
		if (!_target && transform.parent) _target = transform.parent;

		_initialLocalRotation = _mesh.localRotation;
		_currentRotation = _mesh.rotation;
	}

	private void OnEnable()
	{
		if (_target && _mesh)
		{
			_currentRotation = _target.rotation * _initialLocalRotation;
			_mesh.rotation = _currentRotation;
		}
	}

	private void LateUpdate()
	{
		if (!_mesh || !_target) return;

		var targetRotation = _target.rotation * _initialLocalRotation;
		_currentRotation = Quaternion.Slerp(_currentRotation, targetRotation, _followThreshold * Time.deltaTime);
		_mesh.rotation = _currentRotation;
	}
}
