using UnityEngine;

// セル（マス目）を表すクラス
public class Cell
{
    // セルの種類を定義する列挙型
    public enum Type
    {
        Empty,  // 何もない空のセル
        Mine,   // 地雷があるセル
        Number, // 数字が表示されるセル（周囲の地雷の数を示す）
    }

    public Vector3Int position; // セルの座標（3Dベクトルで表現）
    public Type type;           // セルの種類（地雷・空・数字）
    public int number;          // 数字セルの場合、周囲の地雷の数
    public bool revealed;       // プレイヤーに公開されたかどうか
    public bool flagged;        // プレイヤーが旗を立てたかどうか
    public bool exploded;       // 地雷が爆発したかどうか
}