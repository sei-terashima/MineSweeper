using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DefaultExecutionOrder(-1)] // 他のスクリプトよりも先に実行されるように設定
public class Game : MonoBehaviour
{
    // ボードの幅、高さ、地雷の数
    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    private Board board;
    private CellGrid grid;
    private bool gameclear;
    private bool gameover;
    private bool generated;
    private bool isFlagMode = false; //フラグモード管理
    public Button flagButton; //UIフラグモード
    public Button resetButton; //UIフラグモード

    public Button openButton;  // 開くボタン
    public Button closeButton; // 閉じるボタン
    public GameObject resetPanel; // 表示するパネル
    private CanvasGroup canvasGroup; // UIのクリックを制御
    public GameObject gameClearPanel; // ゲームクリア画面
    public GameObject gameOverPanel;  // ゲームオーバー画面

    private void OnValidate()
    {
        // 地雷の数が範囲を超えないように制限
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60; // フレームレートを60に固定
        board = GetComponentInChildren<Board>(); // 子オブジェクトのBoardコンポーネントを取得
    }

    private void Start()
    {
        NewGame(); // 新しいゲームを開始
        flagButton.onClick.AddListener(ActivateFlagMode);
        resetButton.onClick.AddListener(NewGame); // リセットボタンを押したら新しいゲームを開始
        openButton.onClick.AddListener(OpenPanel);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    void OpenPanel()
    {
        resetPanel.SetActive(true); // パネルを表示
        Time.timeScale = 0f; // ゲームを停止
        isFlagMode = false; // フラグモードを一時的に有効にする
    }

    void ClosePanel()
    {
        resetPanel.SetActive(false); // パネルを非表示
        Time.timeScale = 1f; // ゲームを再開
    }

    public void ResetGame()
    {
        gameClearPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        resetPanel.SetActive(false);
        NewGame();
    }

    private void NewGame()
    {
        StopAllCoroutines(); // 進行中のコルーチンを全て停止

        // カメラの位置を盤面の中央に設定
        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);

        gameclear = false;
        gameover = false;
        generated = false;

        grid = new CellGrid(width, height); // 新しいグリッドを生成
        board.Draw(grid); // 盤面を描画
    }

    private void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return; // UIをクリックしている間は何もしない
        }

        // 各キーで新しいゲームを開始
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            NewGame();
            return;
        }

        if (!gameover || !gameclear)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (isFlagMode) // フラグモード時の動作
                {
                    Flag();
                    isFlagMode = false; // 1回でフラグモード解除
                }
                else
                {
                    Reveal(); // 通常のセルを開く
                }
            }
            else if (Input.GetMouseButtonDown(1))
            {
                Flag(); // 右クリックで旗を立てる
            }
        }
    }

    void ActivateFlagMode()
    {
        isFlagMode = true; // フラグモードを一時的に有効にする
    }

    private void Reveal()
    {
        if (TryGetCellAtMousePosition(out Cell cell))
        {
            if (!generated)
            {
                grid.GenerateMines(cell, mineCount); // 最初のクリック時に地雷を生成
                grid.GenerateNumbers(); // 数字を設定
                generated = true;
            }

            Reveal(cell);
        }
    }

    private void Reveal(Cell cell)
    {
        if (cell.revealed || cell.flagged) return; // 既に開いている or 旗付きならスキップ

        switch (cell.type)
        {
            case Cell.Type.Mine:
                Explode(cell); // 地雷ならゲームオーバー
                break;

            case Cell.Type.Empty:
                StartCoroutine(Flood(cell)); // 空白なら周囲を連続して開く
                CheckWinCondition();
                break;

            default:
                cell.revealed = true;
                CheckWinCondition();
                break;
        }

        board.Draw(grid);
    }

    private IEnumerator Flood(Cell cell)
    {
        if (gameclear || gameover || cell.revealed || cell.type == Cell.Type.Mine) yield break;

        cell.revealed = true;
        board.Draw(grid);

        yield return null;

        // 空白セルなら隣接セルを再帰的に開く
        if (cell.type == Cell.Type.Empty)
        {
            if (grid.TryGetCell(cell.position.x - 1, cell.position.y, out Cell left))
            {
                StartCoroutine(Flood(left));
            }
            if (grid.TryGetCell(cell.position.x + 1, cell.position.y, out Cell right))
            {
                StartCoroutine(Flood(right));
            }
            if (grid.TryGetCell(cell.position.x, cell.position.y - 1, out Cell down))
            {
                StartCoroutine(Flood(down));
            }
            if (grid.TryGetCell(cell.position.x, cell.position.y + 1, out Cell up))
            {
                StartCoroutine(Flood(up));
            }
        }
    }

    private void Flag()
    {
        if (!TryGetCellAtMousePosition(out Cell cell) || cell.revealed) return;

        cell.flagged = !cell.flagged; // 旗のオン・オフ
        board.Draw(grid);
    }



    private void Explode(Cell cell)
    {
        gameover = true;
        cell.exploded = true;
        cell.revealed = true;

        // 全ての地雷を表示
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cell = grid[x, y];
                if (cell.type == Cell.Type.Mine)
                {
                    cell.revealed = true;
                }
            }
        }

        gameOverPanel.SetActive(true);
    }

    private void CheckWinCondition()
    {
        // 全ての非地雷セルが開かれているかチェック
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                if (cell.type != Cell.Type.Mine && !cell.revealed)
                {
                    return; // まだ未開のセルがあるなら勝利ではない
                }
            }
        }

        gameclear = true;

        // すべての地雷に旗を立てる
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                if (cell.type == Cell.Type.Mine)
                {
                    cell.flagged = true;
                }
            }
        }

        // ゲームクリアパネルを表示
        gameClearPanel.SetActive(true);
    }

    private bool TryGetCellAtMousePosition(out Cell cell)
    {
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }
}
