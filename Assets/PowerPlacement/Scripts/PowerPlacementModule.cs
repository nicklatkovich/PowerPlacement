using System.Collections.Generic;
using System.Linq;
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
	public ShieldComponent ShieldPrefab;

	private CellComponent[][] _cellGrid;
	private PowerPlacementPuzzle _puzzle;
	private object[][] _objects;

	private void Start() {
		_puzzle = new PowerPlacementPuzzle();
		_cellGrid = new CellComponent[PowerPlacementPuzzle.SIZE][];
		_objects = new object[PowerPlacementPuzzle.SIZE][];
		InitGrid();
		for (int x = -1; x < PowerPlacementPuzzle.SIZE; x++) {
			if (x >= 0) _objects[x] = new object[PowerPlacementPuzzle.SIZE];
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
			_objects[pos.x][pos.y] = obj;
			UpdateColorsOnCross(pos);
			return;
		}
		if (cell.Type == PowerPlacementPuzzle.CellType.ENERGY) {
			cell.Type = PowerPlacementPuzzle.CellType.SHIELD;
			Destroy((_objects[pos.x][pos.y] as EnergyCellComponent).gameObject);
			ShieldComponent obj = Instantiate(ShieldPrefab);
			obj.transform.parent = GridContainer.transform;
			obj.transform.localPosition = new Vector3(pos.x * CELLS_INTERVAL, OBJECTS_HEIGHT, -pos.y * CELLS_INTERVAL);
			obj.transform.localScale = Vector3.one;
			obj.transform.localRotation = Quaternion.identity;
			_objects[pos.x][pos.y] = obj;
			UpdateColorsOnCross(pos);
			return;
		}
		if (cell.Type == PowerPlacementPuzzle.CellType.SHIELD) {
			cell.Type = PowerPlacementPuzzle.CellType.EMPTY;
			Destroy((_objects[pos.x][pos.y] as ShieldComponent).gameObject);
			_objects[pos.x][pos.y] = null;
			UpdateColorsOnCross(pos);
			return;
		}
	}

	private void UpdateColorsOnCross(Vector2Int crossPivot) {
		UpdateColorOnCell(crossPivot);
		foreach (Vector2Int dd in PowerPlacementPuzzle.DD) {
			Vector2Int p = crossPivot + dd;
			while (PowerPlacementPuzzle.IsOnGrid(p)) {
				UpdateColorOnCell(p);
				p += dd;
			}
		}
	}

	private void UpdateColorOnCell(Vector2Int pos) {
		PowerPlacementPuzzle.Cell cell = _puzzle.Grid[pos.x][pos.y];
		if (cell.Type == PowerPlacementPuzzle.CellType.EMPTY) return;
		if (cell.Type == PowerPlacementPuzzle.CellType.ENERGY) SetEnergyCellColor(pos);
		else if (cell.Type == PowerPlacementPuzzle.CellType.SHIELD) SetShieldCellColor(pos);
	}

	private Color SetShieldCellColor(Vector2Int pos) {
		ShieldComponent obj = _objects[pos.x][pos.y] as ShieldComponent;
		List<PowerPlacementPuzzle.Cell>[] lines = _puzzle.GetGridCrossLines(pos);
		if (lines.Any(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.SHIELD) > 1)) return obj.Color = Color.red;
		PowerPlacementPuzzle.Cell[] raycasts = _puzzle.MultiGridRaycast(pos);
		bool successfulBlocking = false;
		for (int d = 0; d < 4; d++) {
			PowerPlacementPuzzle.Cell cell = raycasts[d];
			if (cell == null || cell.Type != PowerPlacementPuzzle.CellType.ENERGY) continue;
			PowerPlacementPuzzle.Cell aCell = raycasts[(d + 2) % 4];
			if (aCell == null || aCell.Type != PowerPlacementPuzzle.CellType.RECEIVER) continue;
			if ((aCell.Flags & (1 << ((d + 2) % 4))) > 0) return obj.Color = Color.red;
			successfulBlocking = true;
		}
		if (successfulBlocking) return obj.Color = Color.green;
		return obj.Color = lines.All(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.ENERGY) == 1) ? Color.green : Color.white;
	}

	private Color SetEnergyCellColor(Vector2Int pos) {
		EnergyCellComponent obj = _objects[pos.x][pos.y] as EnergyCellComponent;
		List<PowerPlacementPuzzle.Cell>[] lines = _puzzle.GetGridCrossLines(pos);
		if (lines.Any(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.ENERGY) > 1)) return obj.Color = Color.red;
		PowerPlacementPuzzle.Cell[] raycasts = _puzzle.MultiGridRaycast(pos);
		for (int d = 0; d < 4; d++) {
			PowerPlacementPuzzle.Cell cell = raycasts[d];
			if (cell == null || cell.Type != PowerPlacementPuzzle.CellType.RECEIVER) continue;
			if ((cell.Flags & (1 << ((d + 2) % 4))) == 0) return obj.Color = Color.red;
		}
		if (raycasts.Any(cell => cell != null && cell.Type == PowerPlacementPuzzle.CellType.RECEIVER)) return obj.Color = Color.green;
		return obj.Color = lines.All(line => line.Count(cell => cell.Type == PowerPlacementPuzzle.CellType.SHIELD) == 1) ? Color.green : Color.white;
	}
}
