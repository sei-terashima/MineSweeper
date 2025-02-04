using UnityEngine;

public class CellGrid
{
    // セルの2次元配列（グリッド）
    private readonly Cell[,] cells;

    // グリッドの幅（列数）
    public int Width => cells.GetLength(0);
    // グリッドの高さ（行数）
    public int Height => cells.GetLength(1);

    // 指定した座標のセルを取得するインデクサ
    public Cell this[int x, int y] => cells[x, y];

    // コンストラクタ: 指定された幅と高さでグリッドを初期化
    public CellGrid(int width, int height)
    {
        cells = new Cell[width, height];

        // 各セルを初期化し、座標を設定
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new Cell
                {
                    position = new Vector3Int(x, y, 0),
                    type = Cell.Type.Empty
                };
            }
        }
    }

    // 地雷をランダムに配置する（開始セルの周囲には配置しない）
    public void GenerateMines(Cell startingCell, int amount)
    {
        int width = Width;
        int height = Height;

        for (int i = 0; i < amount; i++)
        {
            int x = Random.Range(0, width);
            int y = Random.Range(0, height);

            Cell cell = cells[x, y];

            // 既に地雷があるか、開始セルの隣接セルなら再選択
            while (cell.type == Cell.Type.Mine || IsAdjacent(startingCell, cell))
            {
                x++;
                if (x >= width)
                {
                    x = 0;
                    y++;
                    if (y >= height)
                    {
                        y = 0;
                    }
                }
                cell = cells[x, y];
            }

            cell.type = Cell.Type.Mine;
        }
    }

    // 数字セルを生成（各セルの隣接する地雷の数を計算）
    public void GenerateNumbers()
    {
        int width = Width;
        int height = Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = cells[x, y];

                // 地雷セルはスキップ
                if (cell.type == Cell.Type.Mine)
                {
                    continue;
                }

                // 隣接する地雷の数をカウント
                cell.number = CountAdjacentMines(cell);
                // 地雷が隣接していれば数字セル、それ以外は空白セル
                cell.type = cell.number > 0 ? Cell.Type.Number : Cell.Type.Empty;
            }
        }
    }

    // 指定セルの隣接する地雷の数をカウント
    public int CountAdjacentMines(Cell cell)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0)
                {
                    continue;
                }

                int x = cell.position.x + adjacentX;
                int y = cell.position.y + adjacentY;

                // 隣接セルが地雷ならカウント
                if (TryGetCell(x, y, out Cell adjacent) && adjacent.type == Cell.Type.Mine)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // 指定セルの隣接する旗の数をカウント
    public int CountAdjacentFlags(Cell cell)
    {
        int count = 0;

        for (int adjacentX = -1; adjacentX <= 1; adjacentX++)
        {
            for (int adjacentY = -1; adjacentY <= 1; adjacentY++)
            {
                if (adjacentX == 0 && adjacentY == 0)
                {
                    continue;
                }

                int x = cell.position.x + adjacentX;
                int y = cell.position.y + adjacentY;

                // 隣接セルが旗付きならカウント
                if (TryGetCell(x, y, out Cell adjacent) && !adjacent.revealed && adjacent.flagged)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // 指定座標のセルを取得（範囲外ならnullを返す）
    public Cell GetCell(int x, int y)
    {
        if (InBounds(x, y))
        {
            return cells[x, y];
        }
        else
        {
            return null;
        }
    }

    // 指定座標のセルを取得（取得成功ならtrueを返す）
    public bool TryGetCell(int x, int y, out Cell cell)
    {
        cell = GetCell(x, y);
        return cell != null;
    }

    // 指定座標がグリッドの範囲内か確認
    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    // 2つのセルが隣接しているか判定
    public bool IsAdjacent(Cell a, Cell b)
    {
        return Mathf.Abs(a.position.x - b.position.x) <= 1 &&
               Mathf.Abs(a.position.y - b.position.y) <= 1;
    }
}
