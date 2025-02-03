using UnityEngine;
using UnityEngine.Tilemaps;

// ボード（盤面）を管理するクラス
[RequireComponent(typeof(Tilemap))]
public class Board : MonoBehaviour
{
    public Tilemap tilemap { get; private set; } // タイルマップコンポーネント

    // タイルの種類（未公開、空、地雷、爆発、旗、数字）
    public Tile tileUnknown;
    public Tile tileEmpty;
    public Tile tileMine;
    public Tile tileExploded;
    public Tile tileFlag;
    public Tile tileNum1;
    public Tile tileNum2;
    public Tile tileNum3;
    public Tile tileNum4;
    public Tile tileNum5;
    public Tile tileNum6;
    public Tile tileNum7;
    public Tile tileNum8;

    private void Awake()
    {
        tilemap = GetComponent<Tilemap>(); // Tilemapコンポーネントを取得
    }

    // 盤面を描画するメソッド
    public void Draw(CellGrid grid)
    {
        int width = grid.Width;
        int height = grid.Height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                tilemap.SetTile(cell.position, GetTile(cell)); // セルの状態に応じたタイルを設定
            }
        }
    }

    // セルの状態に応じたタイルを取得
    private Tile GetTile(Cell cell)
    {
        if (cell.revealed)
        {
            return GetRevealedTile(cell); // 公開済みのセル
        }
        else if (cell.flagged)
        {
            return tileFlag; // 旗が立っているセル
        }
        else
        {
            return tileUnknown; // 未公開のセル
        }
    }

    // 公開済みのセルのタイルを取得
    private Tile GetRevealedTile(Cell cell)
    {
        switch (cell.type)
        {
            case Cell.Type.Empty: return tileEmpty; // 空のセル
            case Cell.Type.Mine: return cell.exploded ? tileExploded : tileMine; // 地雷セル（爆発した場合は爆発タイル）
            case Cell.Type.Number: return GetNumberTile(cell); // 数字セル
            default: return null;
        }
    }

    // 数字セルのタイルを取得
    private Tile GetNumberTile(Cell cell)
    {
        switch (cell.number)
        {
            case 1: return tileNum1;
            case 2: return tileNum2;
            case 3: return tileNum3;
            case 4: return tileNum4;
            case 5: return tileNum5;
            case 6: return tileNum6;
            case 7: return tileNum7;
            case 8: return tileNum8;
            default: return null;
        }
    }
}
