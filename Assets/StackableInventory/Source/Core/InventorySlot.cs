using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single slot inside an inventory.
/// </summary>
/// <typeparam name="T"></typeparam>
public class InventorySlot<T>
{
    // Event that will be called every time this slot changes.
    public Action OnSlotChange;

    #region Getters/Setters

    /// <summary>
    /// The type of item that is stored in this slot.
    /// </summary>
    public T Item { get; private set; }

    /// <summary>
    /// The amount of the item in the slot.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// The maximum amount of items that can be stored in this slot.
    /// Use InventorySlot.Resize() to change the capacity.
    /// </summary>
    public int MaxQuantity { get; private set; }

    /// <summary>
    /// Is the slot empty or occupied by an item?
    /// </summary>
    public bool IsEmpty
    {
        get { return Item == null; }
    }

    /// <summary>
    /// Is the inventory slot considered full, as in there is no more room for more items?
    /// </summary>
    public bool IsFull
    {
        get
        {
            if (IsEmpty) return false;
            return Quantity >= MaxQuantity;
        }
    }

    /// <summary>
    /// How much free space is left in the slot? Will return max if the slot is empty.
    /// </summary>
    public int FreeSpace
    {
        get
        {
            if (IsEmpty) return MaxQuantity;
            return MaxQuantity - Quantity;
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Create an empty inventory slot.
    /// </summary>
    public InventorySlot()
    {
        
    }

    /// <summary>
    /// Create an inventory slot with a specific item and quantity.
    /// </summary>
    public InventorySlot(T item, int quantity, int maxQuantity)
    {
        SetItem(item, quantity, maxQuantity);
    }

    #endregion

    #region Slot modification

    /// <summary>
    /// Replace the item in the slot with a new item.
    /// </summary>
    /// <param name="newItem"></param>
    /// <param name="quantity"></param>
    public void SetItem(T newItem, int quantity, int maxQuantity)
    {
        Item = newItem;
        Quantity = quantity;
        MaxQuantity = MaxQuantity;

        OnSlotChange?.Invoke();
    }

    /// <summary>
    /// Decreases the quantity of the item in this slot by the specified amount.
    /// </summary>
    /// <param name="amount"></param>
    public void DecreaseQuantity(int amount)
    {
        Quantity -= amount;

        // If quantity reaches 0 it should be cleared.
        if (Quantity <= 0) Clear();
        else OnSlotChange?.Invoke(); // <-- Only else since Clear will also invoke the event.
    }

    /// <summary>
    /// Clears the slot and converts it into an empty one.
    /// </summary>
    public void Clear()
    {
        Item = default(T);
        Quantity = 0;
        MaxQuantity = 0;

        OnSlotChange?.Invoke();
    }

    /// <summary>
    /// Resizes the capacity of the slot and updates the quantity accordingly.
    /// </summary>
    /// <param name="newCapacity"></param>
    public void Resize(int newCapacity)
    {
        MaxQuantity = newCapacity;

        // If the new capacity is smaller than the current quantity, the quantity should be reduced.
        if (Quantity > MaxQuantity) Quantity = MaxQuantity;

        OnSlotChange?.Invoke();
    }

    /// <summary>
    /// Increases the quantity of the item in this slow by a specificed amount.
    /// Will clamp to max quantity.
    /// </summary>
    /// <param name="amount"></param>
    public void IncreaseQuantity(int amount)
    {
        Quantity = Mathf.Clamp(Quantity + amount, 0, MaxQuantity);
        OnSlotChange?.Invoke();
    }

    #endregion

    #region Quantity checks

    /// <summary>
    /// Determines if the slot has room for a specific amount of items.
    /// Does not account for if slot is empty or not.
    /// </summary>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool HasCapacityFor(int amount)
    {
        return (Quantity + amount) <= MaxQuantity;
    }

    /// <summary>
    /// Checks how many items the inventory has room for based on a desired amount of items to add.
    /// Returns the amount of items possible to add.
    /// </summary>
    /// <param name="desired"></param>
    /// <returns></returns>
    public int GetPossibleQuantityToAdd(int desired)
    {
        if (IsFull) return 0;

        int possible = desired;
        if (Quantity + desired > MaxQuantity) possible = MaxQuantity - Quantity;
        return possible;
    }

    #endregion
}
