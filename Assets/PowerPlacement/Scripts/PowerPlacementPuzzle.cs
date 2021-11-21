using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PowerPlacementPuzzle {
	public static readonly Vector2Int[] DD = new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };
	public enum CellType { EMPTY, RECEIVER, SHIELD, ENERGY }
	public enum ReceiverFlag { RIGHT = 1 << 1, UP = 1 << 2, LEFT = 1 << 3, DOWN = 1 << 4 }
	public enum EmptyFlag { SHIELD = 1 << 1, ENERGY = 1 << 2 }

	public sealed class Cell {
		public Vector2Int Position;
		public CellType Type;
		public int Flags;
		public Cell(Vector2Int position, CellType type, int flags = 0) {
			Position = position;
			Type = type;
			Flags = flags;
		}
	}

	public const int SIZE = 7;

	public Cell[][] Solution;
	public Cell[][] Grid;
	public HashSet<Cell> Receivers;

	public List<Cell>[] GetGridCrossLines(Vector2Int pos) {
		return Enumerable.Range(0, 2).Select(d => GetGridLine(pos, d)).ToArray();
	}

	public List<Cell> GetGridLine(Vector2Int pos, int d) {
		int aD = (d + 2) % 4;
		Vector2Int pIt = pos + DD[aD];
		while (IsOnGrid(pIt)) pIt += DD[aD];
		pIt += DD[d];
		List<Cell> result = new List<Cell>();
		while (IsOnGrid(pIt)) {
			result.Add(Grid[pIt.x][pIt.y]);
			pIt += DD[d];
		}
		return result;
	}

	public Cell[] MultiGridRaycast(Vector2Int from) {
		return Enumerable.Range(0, 4).Select(d => GridRaycast(from, d)).ToArray();
	}

	public Cell GridRaycast(Vector2Int from, int direction) {
		Vector2Int pIt = from + DD[direction];
		while (IsOnGrid(pIt)) {
			Cell cell = Grid[pIt.x][pIt.y];
			if (cell.Type != CellType.EMPTY) return cell;
			pIt += DD[direction];
		}
		return null;
	}

	public PowerPlacementPuzzle() {
		Solution = new Cell[SIZE][];
		InitGrid();
		foreach (Cell receiver in Receivers) {
			for (int d = 0; d < 4; d++) {
				Vector2Int pos = receiver.Position + DD[d];
				bool active = false;
				while (IsOnGrid(pos) && !active) {
					if (Solution[pos.x][pos.y].Type == CellType.ENERGY) active = true;
					else if (Solution[pos.x][pos.y].Type != CellType.EMPTY) break;
					pos += DD[d];
				}
				if (!active) continue;
				receiver.Flags |= 1 << d;
			}
		}
		Grid = new Cell[SIZE][];
		for (int x = 0; x < SIZE; x++) {
			Grid[x] = new Cell[SIZE];
			for (int y = 0; y < SIZE; y++) {
				Cell solutionCell = Solution[x][y];
				if (solutionCell.Type == CellType.RECEIVER) Grid[x][y] = solutionCell;
				else Grid[x][y] = new Cell(new Vector2Int(x, y), CellType.EMPTY);
			}
		}
	}

	private void InitGrid() {
		HashSet<Vector2Int> emptyCells = new HashSet<Vector2Int>();
		for (int x = 0; x < SIZE; x++) {
			Solution[x] = new Cell[SIZE];
			for (int y = 0; y < SIZE; y++) {
				Vector2Int position = new Vector2Int(x, y);
				Solution[x][y] = new Cell(position, CellType.EMPTY, (int)(EmptyFlag.ENERGY | EmptyFlag.SHIELD));
				emptyCells.Add(position);
			}
		}
		int[] energyY = Enumerable.Range(0, SIZE).OrderBy(_ => Random.Range(0f, 1f)).ToArray();
		int[] shieldY = Enumerable.Range(0, SIZE).OrderBy(_ => Random.Range(0f, 1f)).ToArray();
		for (int x = 0; x < SIZE; x++) {
			if (energyY[x] != shieldY[x]) continue;
			int j = Random.Range(0, SIZE - 1);
			if (j >= x) j += 1;
			int y = energyY[x];
			energyY[x] = energyY[j];
			energyY[j] = y;
		}
		for (int x = 0; x < SIZE; x++) {
			Cell shieldCell = Solution[x][shieldY[x]];
			shieldCell.Type = CellType.SHIELD;
			emptyCells.Remove(shieldCell.Position);
			Cell energyCell = Solution[x][energyY[x]];
			energyCell.Type = CellType.ENERGY;
			emptyCells.Remove(energyCell.Position);
		}
		Receivers = new HashSet<Cell>();
		for (int i = 0; i < Random.Range(7, 13); i++) {
			Vector2Int pos = emptyCells.PickRandom();
			Cell cell = Solution[pos.x][pos.y];
			cell.Type = CellType.RECEIVER;
			cell.Flags = 0;
			Receivers.Add(cell);
		}
	}

	public static bool IsOnGrid(Vector2Int position) {
		return position.x >= 0 && position.x < SIZE && position.y >= 0 && position.y < SIZE;
	}
}
