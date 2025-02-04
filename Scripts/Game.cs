using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// スクリプトの実行順序を優先させる（他のスクリプトより先に実行）
[DefaultExecutionOrder(-1)]
public class Game : MonoBehaviour
{
    // ボードサイズと地雷の数
    public int width = 16;
    public int height = 16;
    public int mineCount = 32;

    // ゲームの管理用変数
    private Board board; // ボード管理
    private CellGrid grid; // グリッド管理
    private bool gameclear; // ゲームクリアフラグ
    private bool gameover; // ゲームオーバーフラグ
    private bool generated; // 地雷生成済みかのフラグ
    private bool isFlagMode = false; // フラグモードの状態管理

    // UI要素
    public Button flagButton; // フラグモード切替ボタン
    public Button resetButton; // リセットボタン
    public Button openButton;  // メニュー開くボタン
    public Button closeButton; // メニュー閉じるボタン
    public GameObject resetPanel; // リセットメニューのパネル
    private CanvasGroup canvasGroup; // UIのクリック制御
    public GameObject gameClearPanel; // ゲームクリア画面
    public GameObject gameOverPanel;  // ゲームオーバー画面

    // タイマー関連
    private float currentTime; // 経過時間
    private bool isTiming; // タイマーの動作状態
    public TextMeshProUGUI currentTimeText; // 現在の時間表示
    public TextMeshProUGUI bestTimeText; // ベストタイム表示


    private void OnValidate()
    {
        // 地雷の数が範囲を超えないように制限
        mineCount = Mathf.Clamp(mineCount, 0, width * height);
    }

    private void Awake()
    {
        // アプリケーションのフレームレートを60FPSに固定
        Application.targetFrameRate = 60;

        // ゲームボード（Boardコンポーネント）を子オブジェクトから取得
        board = GetComponentInChildren<Board>();
    }

    private void Start()
    {
        // ゲームの初期化処理を実行し、新しいゲームを開始
        NewGame();

        // フラグモードを有効にするボタンのクリックイベントを登録
        flagButton.onClick.AddListener(ActivateFlagMode);

        // リセットボタンを押したら新しいゲームを開始するように設定
        resetButton.onClick.AddListener(NewGame);

        // 設定パネルを開くボタンのクリックイベントを登録
        openButton.onClick.AddListener(OpenPanel);

        // 設定パネルを閉じるボタンが存在する場合、そのクリックイベントを登録
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        // 保存されたベストタイムをUIに表示
        DisplayBestTime();
    }

    void OpenPanel()
    {
        resetPanel.SetActive(true); // パネルを表示
        Time.timeScale = 0f; // ゲームを停止
        isFlagMode = false; // フラグモードを一時的に無効にする
    }

    void ClosePanel()
    {
        resetPanel.SetActive(false); // パネルを非表示
        Time.timeScale = 1f; // ゲームを再開
    }

    public void ResetGame()
    {
        Time.timeScale = 1f; // ゲームを再開

        // ゲームクリア画面を非表示にする
        gameClearPanel.SetActive(false);

        // ゲームオーバー画面を非表示にする
        gameOverPanel.SetActive(false);

        // リセット用のパネルを非表示にする
        resetPanel.SetActive(false);

        // 新しいゲームを開始（盤面のリセットとタイマーの再設定）
        NewGame();
    }


    private void NewGame()
    {
        StopAllCoroutines(); // 進行中のコルーチンを全て停止

        // ゲームボードとグリッドの状態をリセット
        grid = new CellGrid(width, height); // 新しいグリッドを生成
        board.Draw(grid); // 新しいグリッドを描画

        // ゲームの状態を初期化
        gameover = false;
        gameclear = false;
        generated = false; // 地雷生成フラグをリセット
        isFlagMode = false; // フラグモードを無効にする

        // タイマーをリセットし、再開
        currentTime = 0f;
        isTiming = true;

        // カメラの位置を盤面の中央に設定
        Camera.main.transform.position = new Vector3(width / 2f, height / 2f, -10f);
    }

    private void Update()
    {
        // UI上にポインタがある場合、処理をスキップ
        if (IsPointerOverUI()) return;

        // ゲームオーバーでもゲームクリアでもない場合に進行
        if (!gameover && !gameclear)
        {
            // ゲームが進行中なら、時間を加算
            if (isTiming)
            {
                currentTime += Time.deltaTime; // Time.deltaTimeでフレームごとの経過時間を加算
                currentTimeText.text = "TIME: " + FormatTime(currentTime); // フォーマットした時間を画面に表示
            }

            // 左クリック (0番ボタン) が押された場合
            if (Input.GetMouseButtonDown(0))
            {
                // フラグモードが有効な場合、フラグを立てる
                if (isFlagMode)
                {
                    Flag(); // フラグを立てる処理
                    isFlagMode = false; // フラグモードを解除
                }
                else
                {
                    Reveal(); // フラグモードでなければタイルを表示
                }
            }
            // 右クリック (1番ボタン) が押された場合
            else if (Input.GetMouseButtonDown(1))
            {
                Flag(); // 右クリックでフラグを立てる
            }
        }

        // 新しいゲームを開始するためのキー入力チェック
        // N、R、Return、Spaceのいずれかが押されたら新しいゲームを開始
        if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.R) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            NewGame(); // 新しいゲームを開始
        }
    }


    // フラグモードを有効にするメソッド
    void ActivateFlagMode()
    {
        isFlagMode = true; // フラグモードを一時的に有効にする
                           // このモードではユーザーが右クリックでセルにフラグを立てられるようになる
    }

    // セルを開く (表示) 処理
    private void Reveal()
    {
        // マウス位置にあるセルを取得
        if (TryGetCellAtMousePosition(out Cell cell))
        {
            // ゲーム開始前にまだ地雷が生成されていない場合
            if (!generated)
            {
                // 最初のクリック時に地雷を生成
                grid.GenerateMines(cell, mineCount); // 最初のクリック位置を避けて地雷を配置
                grid.GenerateNumbers(); // 各セルに隣接する地雷数を設定
                generated = true; // 地雷と数字の配置が完了したフラグを立てる
            }

            // 対象セルを開く処理
            Reveal(cell); // セルを表示または開放
        }
    }


    // セルを開く処理
    private void Reveal(Cell cell)
    {
        // 既に開かれているセルやフラグが立っているセルはスキップ
        if (cell.revealed || cell.flagged) return;

        // セルの種類に応じた処理を行う
        switch (cell.type)
        {
            // 地雷の場合、爆発させてゲームオーバー
            case Cell.Type.Mine:
                Explode(cell); // 地雷を踏んだ場合は爆発処理を実行し、ゲームオーバー
                break;

            // 空白セルの場合、周囲のセルを自動で開く
            case Cell.Type.Empty:
                StartCoroutine(Flood(cell)); // 空白セルの場合は周囲を順次開く処理をコルーチンで実行
                CheckWinCondition(); // 勝利条件をチェック
                break;

            // 数字セルの場合、そのセルを開放
            default:
                cell.revealed = true; // 数字セルはただ開けるだけ
                CheckWinCondition(); // 勝利条件をチェック
                break;
        }

        // ボードの描画を更新
        board.Draw(grid); // グリッド全体を再描画
    }


    // 空白セル周辺を再帰的に開くコルーチン
    private IEnumerator Flood(Cell cell)
    {
        // ゲームがクリアまたはオーバーしている、またはセルが既に開かれている、地雷の場合は何もしない
        if (gameclear || gameover || cell.revealed || cell.type == Cell.Type.Mine) yield break;

        // 現在のセルを開く
        cell.revealed = true;
        board.Draw(grid); // ボードを更新して変更を反映

        // コルーチンを1フレーム待機
        yield return null;

        // 空白セルの場合、隣接するセルを再帰的に開く
        if (cell.type == Cell.Type.Empty)
        {
            // 左のセルを開く（範囲外チェックあり）
            if (grid.TryGetCell(cell.position.x - 1, cell.position.y, out Cell left))
            {
                StartCoroutine(Flood(left)); // 左隣を再帰的に開く
            }
            // 右のセルを開く（範囲外チェックあり）
            if (grid.TryGetCell(cell.position.x + 1, cell.position.y, out Cell right))
            {
                StartCoroutine(Flood(right)); // 右隣を再帰的に開く
            }
            // 下のセルを開く（範囲外チェックあり）
            if (grid.TryGetCell(cell.position.x, cell.position.y - 1, out Cell down))
            {
                StartCoroutine(Flood(down)); // 下隣を再帰的に開く
            }
            // 上のセルを開く（範囲外チェックあり）
            if (grid.TryGetCell(cell.position.x, cell.position.y + 1, out Cell up))
            {
                StartCoroutine(Flood(up)); // 上隣を再帰的に開く
            }
        }
    }


    // セルにフラグを立てる処理
    private void Flag()
    {
        // マウス位置のセルを取得し、セルが既に開かれている場合は処理を中断
        if (!TryGetCellAtMousePosition(out Cell cell) || cell.revealed) return;

        // セルのフラグ状態を切り替える
        cell.flagged = !cell.flagged; // フラグが立っていれば外し、立っていなければ立てる
        board.Draw(grid); // ボードを再描画してフラグの状態を反映

        // フラグを立てた後に勝利条件をチェック
        CheckWinCondition(); // ゲームクリアの判定を行う
    }




    // 地雷が爆発したときの処理
    private void Explode(Cell cell)
    {
        // ゲームオーバーフラグを立て、タイマーを停止
        gameover = true; // ゲームオーバー状態に設定
        isTiming = false; // タイマーを停止

        // 爆発したセルを開放
        cell.exploded = true; // 爆発したセルの状態を設定
        cell.revealed = true; // 爆発したセルを表示

        // すべての地雷を表示
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cell = grid[x, y]; // グリッド内のセルを取得
                if (cell.type == Cell.Type.Mine) // 地雷セルなら
                {
                    cell.revealed = true; // 地雷を表示
                }
            }
        }

        // ゲームオーバーパネルを表示
        gameOverPanel.SetActive(true); // ゲームオーバーのUIを表示
    }


    // ゲームの勝利条件をチェックする処理
    private void CheckWinCondition()
    {
        // 全ての非地雷セルが開かれているかを確認
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                // 地雷でないセルが未開の場合、勝利ではない
                if (cell.type != Cell.Type.Mine && !cell.revealed)
                {
                    return; // 未開のセルがあれば、ゲームはまだクリアではない
                }
            }
        }

        // すべての非地雷セルが開かれている場合、ゲームクリア
        gameclear = true; // ゲームクリア状態に設定
        isTiming = false; // タイマーを停止

        // 最速クリアタイムを保存
        SaveBestTime(); // ベストタイムを保存する処理

        // すべての地雷にフラグを立てる
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Cell cell = grid[x, y];
                // 地雷セルにフラグを立てる
                if (cell.type == Cell.Type.Mine)
                {
                    cell.flagged = true;
                }
            }
        }

        // ゲームクリアパネルを表示
        gameClearPanel.SetActive(true); // ゲームクリアのUIを表示
    }


    // ベストタイムを保存する処理
    private void SaveBestTime()
    {
        // まだベストタイムが保存されていない場合、または現在のタイムがベストタイムよりも短い場合
        if (!PlayerPrefs.HasKey("BestTime") || currentTime < PlayerPrefs.GetFloat("BestTime"))
        {
            // 現在のタイムをベストタイムとして保存
            PlayerPrefs.SetFloat("BestTime", currentTime); // PlayerPrefsにベストタイムを保存
            PlayerPrefs.Save(); // 保存されたデータをディスクに書き込む
            DisplayBestTime(); // ベストタイムを表示する
        }
    }

    // ベストタイムを画面に表示する処理
    private void DisplayBestTime()
    {
        // ベストタイムが保存されている場合
        if (PlayerPrefs.HasKey("BestTime"))
        {
            // 保存されたベストタイムを取得
            float bestTime = PlayerPrefs.GetFloat("BestTime");
            // フォーマットしたベストタイムを表示
            bestTimeText.text = "BEST TIME: " + FormatTime(bestTime);
        }
        else
        {
            // ベストタイムが保存されていない場合はデフォルト表示
            bestTimeText.text = "BEST TIME: --:--"; // ベストタイムが未設定の場合の表示
        }
    }


    // 秒数を「MM:SS」形式でフォーマットする処理
    private string FormatTime(float time)
    {
        // 分と秒を計算（整数値）
        int minutes = Mathf.FloorToInt(time / 60); // 1分を60秒で割って分を算出
        int seconds = Mathf.FloorToInt(time % 60); // 秒は60で割った余りを使用

        // 分と秒を「00:00」形式で文字列にフォーマット
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // ベストタイムをリセット
    public void ResetBestTime()
    {
            PlayerPrefs.DeleteKey("BestTime");
            PlayerPrefs.Save();
            DisplayBestTime();
    }

    // マウス位置にあるセルを取得する処理
    private bool TryGetCellAtMousePosition(out Cell cell)
    {
        // マウス位置をワールド座標に変換
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        // ワールド座標をタイル座標に変換
        Vector3Int cellPosition = board.tilemap.WorldToCell(worldPosition);

        // 変換したセル座標を使ってセルを取得
        return grid.TryGetCell(cellPosition.x, cellPosition.y, out cell);
    }

    // UI上にポインターがあるかを判定する処理
    private bool IsPointerOverUI()
    {
        // マウス操作の場合、ポインタがUIの上にあるかを判定
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true; // ポインタがUI上にある場合
        }

        // タップ操作の場合、タップした場所がUIの上かを判定
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0); // 最初のタッチ情報を取得
            return EventSystem.current.IsPointerOverGameObject(touch.fingerId); // タップ位置がUI上かを判定
        }

        return false; // UI上にポインタがない場合
    }
}
