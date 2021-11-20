using System.Collections.Generic;
using UnityEngine;
using KeepCoding;

public class PowerPlacementModule : ModuleScript {
	public const float CELLS_INTERVAL = 0.018f;
	public const float OBJECTS_HEIGHT = 0.002f;

	public GameObject GridContainer;
	public KMSelectable Selectable;
	public CellComponent CellPrefab;
	public CellBorderComponent CellBorderPrefab;
	public ReceiverComponent ReceiverPrefab;
	public EnergyCellComponent EnergyCellPrefab;

	private CellComponent[][] _cellGrid;
	private PowerPlacementPuzzle _puzzle;

	private void Start() {
		_puzzle = new PowerPlacementPuzzle();
		_cellGrid = new CellComponent[PowerPlacementPuzzle.SIZE][];
		InitGrid();
		for (int x = -1; x < PowerPlacementPuzzle.SIZE; x++) {
			for (int z = -1; z < PowerPlacementPuzzle.SIZE; z++) {
				if (x >= 0) {
					CellBorderComponent border = CreateCellBorder(x, z + 0.5f, 90);
					if (z >= 0) _cellGrid[x][z].Borders.Add(border);
					if (z + 1 < PowerPlacementPuzzle.SIZE) _cellGrid[x][z + 1].Borders.Add(border);
				}
				if (z >= 0) {
					CellBorderComponent border = CreateCellBorder(x + 0.5f, z, 0);
					if (x >= 0) _cellGrid[x][z].Borders.Add(border);
					if (x + 1 < PowerPlacementPuzzle.SIZE) _cellGrid[x + 1][z].Borders.Add(border);
				}
			}
		}
	}

	private void InitGrid() {
		List<KMSelectable> selectables = new List<KMSelectable>();
		for (int x = 0; x < PowerPlacementPuzzle.SIZE; x++) {
			_cellGrid[x] = new CellComponent[PowerPlacementPuzzle.SIZE];
			for (int z = 0; z < PowerPlacementPuzzle.SIZE; z++) {
				CellComponent cell = Instantiate(CellPrefab);
				cell.transform.parent = GridContainer.transform;
				cell.transform.localPosition = new Vector3(x * CELLS_INTERVAL, 0, -z * CELLS_INTERVAL);
				cell.transform.localScale = Vector3.one;
				cell.transform.localRotation = Quaternion.identity;
				if (_puzzle.Solution[x][z].Type != PowerPlacementPuzzle.CellType.RECEIVER) {
					cell.Selectable.Parent = Selectable;
					Vector2Int pos = new Vector2Int(x, z);
					cell.Selectable.OnInteract += () => { OnCellPressed(pos); return false; };
					selectables.Add(cell.Selectable);
				} else {
					ReceiverComponent receiver = Instantiate(ReceiverPrefab);
					receiver.transform.parent = GridContainer.transform;
					receiver.transform.localPosition = new Vector3(x * CELLS_INTERVAL, OBJECTS_HEIGHT, -z * CELLS_INTERVAL);
					receiver.transform.localScale = Vector3.one;
					receiver.transform.localRotation = Quaternion.identity;
					if (_puzzle.Solution[x][z].Flags == 0) {
						receiver.Center.material.color = Color.green;
						receiver.Outline.material.color = Color.green;
					}
					for (int d = 0; d < 4; d++) {
						if ((_puzzle.Solution[x][z].Flags & (1 << d)) > 0) continue;
						receiver.Sides[d].gameObject.SetActive(false);
					}
				}
				_cellGrid[x][z] = cell;
			}
		}
		Selectable.Children = selectables.ToArray();
		Selectable.UpdateChildren();
	}

	private CellBorderComponent CreateCellBorder(float x, float z, float angle) {
		CellBorderComponent result = Instantiate(CellBorderPrefab);
		result.transform.parent = GridContainer.transform;
		result.transform.localPosition = new Vector3(x * CELLS_INTERVAL, 0, -z * CELLS_INTERVAL);
		result.transform.localScale = Vector3.one;
		result.transform.localRotation = Quaternion.Euler(0, angle, 0);
		return result;
	}

	private void OnCellPressed(Vector2Int pos) {
		PowerPlacementPuzzle.Cell cell = _puzzle.Grid[pos.x][pos.y];
		if (cell.Type == PowerPlacementPuzzle.CellType.EMPTY) {
			cell.Type = PowerPlacementPuzzle.CellType.ENERGY;
			EnergyCellComponent obj = Instantiate(EnergyCellPrefab);
			obj.transform.parent = GridContainer.transform;
			obj.transform.localPosition = new Vector3(pos.x * CELLS_INTERVAL, OBJECTS_HEIGHT, -pos.y * CELLS_INTERVAL);
			obj.transform.localScale = Vector3.one;
			obj.transform.localRotation = Quaternion.identity;
			obj.Renderer.material.color = Color.green;
		}
	}
}
