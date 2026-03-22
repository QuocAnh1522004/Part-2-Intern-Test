using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GameManager;
using static NormalItem;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board m_board;

    private GameManager m_gameManager;

    private bool m_isDragging;

    private Camera m_cam;

    private Collider2D m_hitCollider;

    private GameSettings m_gameSettings;

    private List<Cell> m_potentialMatch;

    private float m_timeAfterFill;

    private bool m_hintIsShown;

    private bool m_gameOver;

    private bool m_isClicking = false;

    private float m_clickTimer = 1f;

    private eLevelMode m_levelMode;

    private bool m_playUntilWin = false;

    private bool m_playUntilLose = false;

    private float m_autoPlayMovesCooldown;

    private float m_autoplayTimer = 999f;

    private eNormalType m_selectedType;

    private int m_currentAutoIndex = 0;

    private bool m_isAutoPlaying = false;

    private List<NormalItem> m_itemList = new List<NormalItem>();

    private Func<List<NormalItem>> m_getItemsStrategy;

    private bool m_isTimerMode = false;


    public Board GetBoard()
    {
        return m_board;
    }
    public void StartGame(GameManager gameManager, GameSettings gameSettings, eLevelMode levelMode)
    {
        m_gameManager = gameManager;

        m_gameSettings = gameSettings;

        m_gameManager.StateChangedAction += OnGameStateChange;

        m_cam = Camera.main;

        m_board = new Board(this.transform, gameSettings);

        m_levelMode = levelMode;

        m_autoPlayMovesCooldown = m_gameSettings.LevelAutoPlayTimer;

        Fill();

        switch (m_levelMode)
        {
            case eLevelMode.AUTO_PLAY:
                m_playUntilWin = true;
                m_getItemsStrategy = () => m_board.GetSortedItems();
                break;

            case eLevelMode.AUTO_LOSE:
                m_playUntilLose = true;
                m_getItemsStrategy = () => m_board.GetRandomItem();
                break;
            case eLevelMode.TIMER:
                m_isTimerMode =true;

                break;
        }

    }
    private void Fill()
    {
        m_board.FillBoard();
        //FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                BottomBar.Instance.ClearListData();
                m_itemList.Clear();
                m_getItemsStrategy = null;
                m_gameOver = true;         
                m_isAutoPlaying = false;
                m_playUntilWin = false;
                m_isTimerMode=false;
                break;
        }
    }


    public void Update()
    {
        if (m_playUntilWin || m_playUntilLose)
        {
            HandleAutoPlay();
            return;
        }

        if (m_gameOver || IsBusy) return;

        HandleInput();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 worldPos = m_cam.ScreenToWorldPoint(Input.mousePosition);
            Collider2D hit = Physics2D.OverlapPoint(worldPos);

            if (hit != null)
            {
                ItemView itemView = hit.GetComponent<ItemView>();
                if (itemView != null)
                {
                    OnItemClicked(itemView.item);
                    Debug.Log("Item view");
                    return;
                }

                Cell cell = hit.GetComponent<Cell>();
                if (cell != null && !cell.IsEmpty)
                {
                    OnCellClicked(cell);
                    Debug.Log("Cell");
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }
    }

    private void OnItemClicked(Item item)
    {
        if (IsBusy) return;
        if (!m_isTimerMode) return;

        NormalItem normal = item as NormalItem;
        if (normal == null) return;

        // Only allow if item is in hotbar
        if (BottomBar.Instance.GetListData().Contains(item))
        {
            ReturnItemToBoard(normal);
        }
    }
    private void HandleAutoPlay()
    {
       // if (IsBusy) return;       
        if (!m_isAutoPlaying)
            StartAutoPlay();
        if (m_itemList == null || m_itemList.Count == 0)
        {
            m_itemList = m_getItemsStrategy?.Invoke();
            m_currentAutoIndex = 0;

            if (m_itemList == null || m_itemList.Count == 0)
                return;
        }

        if (m_autoplayTimer >= m_autoPlayMovesCooldown)
        {
            m_autoplayTimer = 0f;

            if (m_currentAutoIndex >= m_itemList.Count)
            {
                m_currentAutoIndex = 0;
            }

            if (m_currentAutoIndex < 0 || m_currentAutoIndex >= m_itemList.Count)
                return;

            NormalItem nextItem = m_itemList[m_currentAutoIndex];

            if (nextItem == null || nextItem.Cell == null || nextItem.Cell.IsEmpty)
            {
                m_currentAutoIndex++;
                return;
            }

            OnCellClicked(nextItem.Cell);
            m_currentAutoIndex++;
        }
        else
        {
            m_autoplayTimer += Time.deltaTime;
        }
    }

    public void StartAutoPlay()
    {
        m_isAutoPlaying = true;
        m_itemList = m_getItemsStrategy?.Invoke() ?? new List<NormalItem>();
        m_currentAutoIndex = 0;
        m_autoplayTimer = 0f;
    }

    private void ResetRayCast()
    {
        //   m_isDragging = false;
        m_hitCollider = null;
        m_isClicking = false;
    }

    void OnCellClicked(Cell cell)
    {
        if (IsBusy) return;
        IsBusy = true;
        if (!cell.IsClickable) return;
        NormalItem normalItem = cell.Item as NormalItem;
        if (normalItem == null) return;
        if (!m_isTimerMode && BottomBar.Instance.IsFull()) return;
       // IsBusy = true;
        cell.IsClickable = false;
        var item = cell.Item;
        BottomBar.Instance.AddItem(normalItem);

        if (item != null)
            item.SetSortingLayerHigher();

        var seq = m_board.MoveItemToHotBar();

        seq.OnComplete(() =>
        {
            BottomBar.Instance.CheckMatchAndCollapse();
            if (item != null && item.View != null)
            {
                item.SetSortingLayerLower();
            }
            if (m_isTimerMode)
            {
                AddColliderToItem(item);
            }
            cell.Free();

            // lose
            if (!m_isTimerMode && BottomBar.Instance.IsFull())
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);
            }
            // win
            else if (m_board.IsBoardEmpty())
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_WIN);
            }
            m_board.MoveAllItemToCorrectSlots();          
             IsBusy = false;
        });
    }
    void AddColliderToItem(Item item)
    {
        if (item.View == null) return;

        var col = item.View.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = item.View.gameObject.AddComponent<BoxCollider2D>();
        }

        col.size = new Vector2(1f, 1f);
        col.offset = Vector2.zero;
    }

    void RemoveColliderFromItem(Item item)
    {
        var col = item.View?.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            GameObject.Destroy(col);
        }
    }
    public void ReturnItemToBoard(NormalItem item)
    {
        if (item == null || IsBusy) return;

        Cell emptyCell = GetFirstEmptyCell();
        if (emptyCell == null) return;

        IsBusy = true;

        if (item.View != null)
        {
            item.View.DOKill(true);
        }

        BottomBar.Instance.RemoveItem(item);

        emptyCell.Assign(item);
        item.SetViewRoot(transform);

        item.SetViewPosition(emptyCell.transform.position);

        item.Cell.IsClickable = true;

        RemoveColliderFromItem(item);
        m_board.MoveAllItemToCorrectSlots();

        IsBusy = false;
    }

    private Cell GetFirstEmptyCell()
    {
        for (int x = 0; x < m_board.GetBoardSizeX(); x++)
        {
            for (int y = 0; y < m_board.GetBoardSizeY(); y++)
            {
                var cell = m_board.GetCell(x, y);
                if (cell.IsEmpty)
                    return cell;
            }
        }
        return null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < m_gameSettings.MatchesMin)
            {
                m_board.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count > 0)
            {
                IsBusy = false;

                m_timeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = m_board.GetHorizontalMatches(cell);
        if (listHor.Count < m_gameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = m_board.GetVerticalMatches(cell);
        if (listVert.Count < m_gameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if (matches.Count > m_gameSettings.MatchesMin)
        {
            m_board.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        m_board.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        m_board.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
        {
            cell.StopHintAnimation();
        }

        m_potentialMatch.Clear();
    }

    void OnDestroy()
    {
        if (m_gameManager != null)
            m_gameManager.StateChangedAction -= OnGameStateChange;
    }
}
