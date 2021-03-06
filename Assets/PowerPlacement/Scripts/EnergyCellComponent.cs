using System.Collections.Generic;
using UnityEngine;

public class EnergyCellComponent : MonoBehaviour {
	public Renderer Renderer;
	public ConnectionComponent[] Connections = new ConnectionComponent[4];

	private Color _color = Color.white;
	public Color Color { get { return _color; } set { if (_color == value) return; _color = value; UpdateColor(); } }

	private void Start() {
		UpdateColor();
	}

	private void UpdateColor() {
		Renderer.material.color = Color;
	}
}
