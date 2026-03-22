using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Progress;

public class BottomBar : MonoBehaviour
{
    [SerializeField] private List<SlotData> m_slotPositions;
    [HideInInspector] public int moveToSlotIndex;
    public static BottomBar Instance;
    private List<Item> m_slots = new List<Item>();
    private int m_capacity = 5;

    private void Awake()
    {
        Instance = this;
    }
    public bool IsFull()
    {
        return m_slots.Count >= m_capacity;
    }
    public void AddItem(Item item)
    {
        if (IsFull()) return;
        var normalItem = item as NormalItem;
        if (normalItem == null) return;
        int insertIndex = SearchForInsertIndex(normalItem);
        if (insertIndex == -1)
        {
            m_slots.Add(normalItem);
            moveToSlotIndex = m_slots.Count - 1;
        }
        else
        {
            moveToSlotIndex = insertIndex + 1;
            m_slots.Insert(moveToSlotIndex, item);         
        }
    }

    public void RemoveItem(NormalItem item)
    {
        if (item == null) return;

        int index = m_slots.IndexOf(item);
        if (index == -1) return;
        m_slots.RemoveAt(index);

        //if (item.View != null)
        //{
        //    item.View.DOKill();
        //}

        //Re-arrange remaining items visually
        for (int i = 0; i < m_slots.Count; i++)
        {
            var slotItem = m_slots[i];
            if (slotItem?.View == null) continue;

            Transform target = SearchForSlotTransform(i);
            if (target != null)
            {
                slotItem.View.DOMove(target.position, 0.25f);
            }
        }
    }

    public int SearchForInsertIndex(NormalItem normalItem)
    {
        int insertIndex = -1;

        for (int i = 0; i < m_slots.Count; i++)
        {
            var indexedItem = m_slots[i] as NormalItem;
            if (indexedItem != null && indexedItem.ItemType == normalItem.ItemType)
            {
                insertIndex = i;
            }
        }

        return insertIndex;
    }

    public Transform SearchForSlotTransform(int searchIndex)
    {
        foreach (var slot in m_slotPositions)
        {
            if (slot.value == searchIndex) return slot.transform;
        }
        return null;
    }

    public void CheckMatchAndCollapse()
    {
        for (int i = 0; i < m_slots.Count; i++)
        {
            var current = m_slots[i] as NormalItem;
            if (current == null) continue;

            int count = 1;

            for (int j = i + 1; j < m_slots.Count; j++)
            {
                if (count >= 3) break;
                var next = m_slots[j] as NormalItem;
                if (next != null && next.ItemType == current.ItemType)
                {
                    count++;
                }
                else break;
            }

            if (count >= 3)
            {
                for (int k = 0; k < 3; k++)
                {
                    m_slots[i + k].ExplodeView(); 
                }
                m_slots.RemoveRange(i, 3);
               
                return;
            }
        }
    }

    public int GetTotalItemInHotbar()
    {

        return m_slots.Count;
    }

    public List<Item> GetListData()
    {
        return m_slots;
    }

    public void ClearListData()
    {
        m_slots.Clear();
    }

}
[System.Serializable]
public class SlotData
{
    public Transform transform;
    public int value;
}
