﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public int id;

    private BoardManager board;
    private SpriteRenderer render;
    private bool isSelected = false;

    private static readonly Color selectedColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color normalColor = Color.white;

    private static TileController previousSelected = null;

    private static readonly float moveDuration = 0.5f;
    private static readonly Vector2[] adjacentDirection = new Vector2[]
    {
        Vector2.up, Vector2.down, Vector2.left, Vector2.right
    };

    public bool IsDestroyed
    {
        get; private set;
    }

    private void Awake()
    {
        board = BoardManager.Instance;
        render = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        IsDestroyed = false;
    }

    public void ChangeId(int id, int x, int y)
    {
        render.sprite = board.tileTypes[id];
        this.id = id;

        name = "TILE_" + id + "(" + x + "," + y + ")";
    }

    private void OnMouseDown()
    {
        // Non Selectable conditions
        if (render.sprite == null || board.IsAnimating)
        {
            return;
        }


        // Already selected this tile?
        if (isSelected)
        {
            Deselect();
        }
        else
        {
            // if nothing selected yet
            if (previousSelected == null)
            {
                Select();
            }

            else
            {
                // is this an adjacent tiles?
                if (GetAllAdjacentTiles().Contains(previousSelected))
                {
                    TileController otherTile = previousSelected;
                    previousSelected.Deselect();

                    // swap tile
                    SwapTile(otherTile, () => {
                        if (board.GetAllMatches().Count > 0)
                        {
                            Debug.Log("A MATCH!");
                        }
                        else
                        {
                            SwapTile(otherTile,  () => { board.IsAnimating = false; });
                        }
                    });
                }
                // if not adjacent then change selected
                else
                {
                    previousSelected.Deselect();
                    Select();
                }
            }
        }
    }

    public void SwapTilePosition(TileController otherTile, System.Action onCompleted = null)
    {
        StartCoroutine(board.SwapTilePosition(this, otherTile, onCompleted));
    }

    #region Select & Deselect

    private void Select()
    {
        isSelected = true;
        render.color = selectedColor;
        previousSelected = this;
    }

    private void Deselect()
    {
        isSelected = false;
        render.color = normalColor;
        previousSelected = null;
    }

    #endregion

    #region Swapping & Moving

    public void SwapTile(TileController otherTile, System.Action onCompleted = null)
    {
        StartCoroutine(board.SwapTilePosition(this, otherTile, onCompleted));
    }

    public IEnumerator MoveTilePosition(Vector2 targetPosition, System.Action onCompleted)
    {
        Vector2 startPosition = transform.position;
        float duration = moveDuration;
        float time = 0.0f;

        yield return new WaitForEndOfFrame();

        while (time < duration)
        {
            transform.position = Vector2.Lerp(startPosition, targetPosition, time / duration);
            time += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
        transform.position = targetPosition;
        onCompleted?.Invoke();
    }

    #endregion


    #region Adjacent

    private TileController GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, render.size.x);

        if (hit)
        {
            return hit.collider.GetComponent<TileController>();
        }
        return null; 
    }

    public List<TileController> GetAllAdjacentTiles()
    {
        List<TileController> adjacentTiles = new List<TileController>();

        for (int i = 0; i < adjacentDirection.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirection[i]));
        }
        return adjacentTiles;
    }

    #endregion

    #region Check Match

    private List<TileController> GetMatch(Vector2 castDir)
    {
        List<TileController> matchingTiles = new List<TileController>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, render.size.x);

        while (hit)
        {
            TileController otherTile = hit.collider.GetComponent<TileController>();
            if (otherTile.id != id || otherTile.IsDestroyed)
            {
                break;
            }
            matchingTiles.Add(otherTile);
            hit = Physics2D.Raycast(otherTile.transform.position, castDir, render.size.x);
        }

        return matchingTiles;
    }

    private List<TileController> GetOneLineMatch(Vector2[] paths)
    {
        List<TileController> matchingTiles = new List<TileController>();

        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(GetMatch(paths[i]));
        }

        if (matchingTiles.Count >= 2)
        {
            return matchingTiles;
        }

        return null;
    }

    public List<TileController> GetAllMatches()
    {
        if (IsDestroyed)
        {
            return null;
        }

        List<TileController> matchingTiles = new List<TileController>();

        List<TileController> horizontalMatchingTiles = GetOneLineMatch(new Vector2[2] { Vector2.up, Vector2.down });
        List<TileController> verticalMatchingTiles = GetOneLineMatch(new Vector2[2] { Vector2.left, Vector2.right });

        if (horizontalMatchingTiles != null)
        {
            matchingTiles.AddRange(horizontalMatchingTiles);
        }

        if (verticalMatchingTiles != null)
        {
            matchingTiles.AddRange(verticalMatchingTiles);
        }

        if (matchingTiles != null && matchingTiles.Count >= 2)
        {
            matchingTiles.Add(this);
        }

        return matchingTiles;
    }

    #endregion

}
