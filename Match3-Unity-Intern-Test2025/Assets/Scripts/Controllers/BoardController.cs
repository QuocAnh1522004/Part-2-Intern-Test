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

    private List<NormalItem> m_ItemsList = new List<NormalItem>();

    private bool m_isAutoPlaying = false;

    private Func<List<NormalItem>> m_getItemsStrategy;

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
                break;
            case eLevelMode.AUTO_LOSE:
                m_playUntilLose = true;
                break;
            default:
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
                m_gameOver = true;
                BottomBar.Instance.ClearListData();
                m_isAutoPlaying = false;
                m_playUntilWin = false;
                break;
        }
    }


    public void Update()
    {
        if (m_playUntilWin)
        {
           if(!m_isAutoPlaying) StartAutoPlay();
            if (m_ItemsList == null || m_ItemsList.Count == 0)
                return;
            if (m_autoplayTimer >= m_autoPlayMovesCooldown)
            {
                m_autoplayTimer = 0f;
                if (m_currentAutoIndex >= m_ItemsList.Count)
                {
                    m_currentAutoIndex = 0;
                    m_ItemsList = m_board.GetSortedItems();
                }
                NormalItem nextItem = m_ItemsList[m_currentAutoIndex];
                OnCellClicked(nextItem.Cell);
                m_currentAutoIndex++;
            }
            else
            {
                m_autoplayTimer += Time.deltaTime;
            }

        }
        else if (m_playUntilLose)
        {
            if (m_autoplayTimer > m_autoPlayMovesCooldown)
            {

            }
            else
            {
                m_autoplayTimer += Time.deltaTime;
            }
        }
        else
        {
            if (m_gameOver) return;
            if (IsBusy) return;
            else
            {
                if (Input.GetMouseButtonDown(0) && !m_isClicking)
                {
                    m_clickTimer += Time.deltaTime;
                    m_isClicking = true;
                    var hit = Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

                    if (hit.collider != null)
                    {
                        Cell cell = hit.collider.GetComponent<Cell>();
                        if (cell == null) Debug.Log("cell is null");
                        if (cell != null && !cell.IsEmpty)
                        {
                            OnCellClicked(cell);
                        }
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    ResetRayCast();
                }
            }
        }


    }

    public void StartAutoPlay()
    {
        m_isAutoPlaying = true;
        m_ItemsList = m_board.GetSortedItems();
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
        if (!cell.IsClickable) return;

        NormalItem normalItem = cell.Item as NormalItem;
        if (normalItem == null) return;

        if (BottomBar.Instance.IsFull()) return;

        cell.IsClickable = false;
        var item = cell.Item;
        BottomBar.Instance.AddItem(normalItem);
        item.SetSortingLayerHigher();

        var seq = m_board.MoveItemToHotBar();
        seq.OnComplete(() =>
        {
            BottomBar.Instance.CheckMatchAndCollapse();
            m_board.MoveAllItemToCorrectSlots();
            if (item != null)
                item.SetSortingLayerLower();
            cell.Free();
            //lost
            if (BottomBar.Instance.IsFull())
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_OVER);
            }
            else if (m_board.IsBoardEmpty())
            {
                m_gameManager.SetState(GameManager.eStateGame.GAME_WIN);
            }
        });
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
}
