using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GrothDev.Inventory
{
    /// <summary>
    /// This is a generic inventory system that can be used for any type of game where items are stacked, like in Minecraft.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Inventory<T>
    {
        #region Private variables

        private List<InventorySlot<T>> _slots = new List<InventorySlot<T>>();

        private Dictionary<T, int> _itemList = new Dictionary<T, int>();

        #endregion

        #region Getters/Setters

        /// <summary>
        /// The amount of slots the inventory has capacity for.
        /// Use Inventory.SetCapacity() to resize.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// The capacity of slots when items are added to the inventory.
        /// Similar to capacity of the inventory itself, the value can be changed with
        /// Inventory.SetSlotCapacity().
        /// </summary>
        public int SlotCapacity { get; private set; }

        /// <summary>
        /// All items that are in the inventory and their total quantity.
        /// </summary>
        public Dictionary<T, int> ItemList
        {
            get { return _itemList; }
        }

        /// <summary>
        /// Are all the slots in the inventory taken?
        /// </summary>
        public bool IsFull
        {
            get
            {
                int fullSlots = 0;
                _slots.ForEach(slot =>
                {
                    if (slot.IsFull) fullSlots++;
                });
                return fullSlots == Capacity;
            }
        }

        /// <summary>
        /// Does the inventory have at least one empty slot?
        /// </summary>
        public bool HasEmptySlot
        {
            get
            {
                bool emptySlot = false;
                _slots.ForEach(slot =>
                {
                    if (slot.IsEmpty) emptySlot = true;
                });
                return emptySlot;
            }
        }

        /// <summary>
        /// How many slots are currently not occupied?
        /// </summary>
        public int FreeSlots
        {
            get { return Capacity - _slots.Count; }
        }

        /// <summary>
        /// How many slots has an item in it. Note that this does not mean the slot itself is full.
        /// </summary>
        public int TakenSlots
        {
            get { return _slots.Count; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new inventory with the given capacity.
        /// </summary>
        /// <param name="capacity"></param>
        public Inventory(int capacity, int slotCapacity)
        {
            Capacity = capacity;
            SlotCapacity = slotCapacity;
            CreateEmptySlots();
        }

        #endregion

        #region Adding

        /// <summary>
        /// Attempts to add an item to the inventory.
        /// TODO: Split this into overridable sub-methods to more easily allow for custom inventory logic.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        public virtual void AddItem(T item, int quantity = 1)
        {
            if (!HasEmptySlot)
            {
                Debug.Log("Inventory is full!");
                return;
            }

            InventorySlot<T> slotToUse = GetNotFullSlotWithItem(item);
            if (slotToUse == null)
            {
                // If we couldn't find a slot with the item, we'll try to find an empty slot.
                // This should not be null, since we check if the inventory is full at the start of the method.
                slotToUse = GetNextEmptySlot();
            }

            // Since the slot might be empty, we resize it to have the expected MaxCapacity
            // to prevent GetPossibleQuantityToAdd() returning 0.
            if (slotToUse.IsEmpty)
                slotToUse.Resize(SlotCapacity);

            // We will need to ensure that the slot can support the amount of items we want to add.
            // In cases where this is not possible, this method will be called recursively.
            int amountToAdd = slotToUse.GetPossibleQuantityToAdd(quantity);

            if (slotToUse.IsEmpty) slotToUse.SetItem(item, amountToAdd, SlotCapacity);
            else slotToUse.IncreaseQuantity(amountToAdd);

            UpdateTotalQuantity(item, amountToAdd);

            if (amountToAdd < quantity)
            {
                // If we couldn't add all the items, we'll call this method again with the remaining amount.
                AddItem(item, quantity - amountToAdd);
            }
        }

        /// <summary>
        /// Checks if it is possible to add the given amount of items to the inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public bool CanAdd(T item, int quantity)
        {
            if (IsFull) return false;

            InventorySlot<T> slotToUse = GetNotFullSlotWithItem(item);
            if (slotToUse == null)
            {
                // If we couldn't find a slot with the item, we'll try to find an empty slot.
                // This should not be null, since we check if the inventory is full at the start of the method.
                slotToUse = GetNextEmptySlot();
            }

            // We will need to ensure that the slot can support the amount of items we want to add.
            // In cases where this is not possible, this method will be called recursively.
            int amountToAdd = slotToUse.GetPossibleQuantityToAdd(quantity);

            if (amountToAdd < quantity)
            {
                // If we couldn't add all the items, we'll call this method again with the remaining amount.
                return CanAdd(item, quantity - amountToAdd);
            }

            return true;
        }

        /// <summary>
        /// Calculates how many of an item can be added to the inventory and returns it as
        /// an integer.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="desiredQuantity"></param>
        /// <returns></returns>
        public int AmountPossibleToAdd(T item, int desiredQuantity)
        {
            if (IsFull) return 0;

            InventorySlot<T> slotToUse = GetNotFullSlotWithItem(item);
            if (slotToUse == null)
            {
                // If we couldn't find a slot with the item, we'll try to find an empty slot.
                // This should not be null, since we check if the inventory is full at the start of the method.
                slotToUse = GetNextEmptySlot();
            }

            // We will need to ensure that the slot can support the amount of items we want to add.
            // In cases where this is not possible, this method will be called recursively.
            int amountToAdd = slotToUse.GetPossibleQuantityToAdd(desiredQuantity);

            if (amountToAdd < desiredQuantity)
            {
                // If we couldn't add all the items, we'll call this method again with the remaining amount.
                return amountToAdd + AmountPossibleToAdd(item, desiredQuantity - amountToAdd);
            }

            return amountToAdd;
        }

        #endregion

        #region Removal

        /// <summary>
        /// Attempts to remove a specified amount of items from the inventory.
        /// In cases where more items are removed than there are in the inventory, it will
        /// clamp the amount to the maximum possible.
        /// 
        /// Removal will work backwards, removing from later slots first.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="amount"></param>
        public void RemoveItem(T item, int amount)
        {
            if (!HasItem(item))
                return;

            List<InventorySlot<T>> validSlots = GetSlotsWithItem(item);


            // We can first do a lazy check if the amount to remove is greater than the total quantity.
            // If so, we just remove all items.
            if (amount >= GetTotalQuantity(item))
            {
                validSlots.ForEach(slot => slot.Clear());
                UpdateTotalQuantity(item, -GetTotalQuantity(item));
                return;
            }

            // Amount is less than total quantity, so we will have to start removing from back of
            // list until we have removed the desired amount.
            validSlots.Reverse();
            int amountRemoved = 0;
            foreach (InventorySlot<T> slot in validSlots)
            {
                int possibleToRemove = slot.Quantity;
                if (amountRemoved + possibleToRemove > amount)
                {
                    // If we are removing more than we can, we will clamp the amount to the maximum possible.
                    possibleToRemove = amount - amountRemoved;
                }

                slot.DecreaseQuantity(possibleToRemove);

                amountRemoved += possibleToRemove;

                if (amountRemoved >= amount)
                    break;
            }

            UpdateTotalQuantity(item, -amountRemoved);
        }

        #endregion

        #region Slot retrieval

        /// <summary>
        /// Returns all the slots in the inventory.
        /// </summary>
        /// <returns></returns>
        public List<InventorySlot<T>> GetAllSlots()
        {
            return _slots;
        }

        /// <summary>
        /// Returns all slots that contains a type of item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public List<InventorySlot<T>> GetSlotsWithItem(T item)
        {
            List<InventorySlot<T>> slots = new List<InventorySlot<T>>();
            foreach (InventorySlot<T> slot in _slots)
            {
                if (slot.IsEmpty)
                    continue;

                if (slot.Item.Equals(item))
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        /// <summary>
        /// Returns all empty slots.
        /// </summary>
        /// <returns></returns>
        public List<InventorySlot<T>> GetEmptySlots()
        {
            List<InventorySlot<T>> slots = new List<InventorySlot<T>>();
            foreach (InventorySlot<T> slot in _slots)
            {
                if (slot.IsEmpty)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        /// <summary>
        /// Returns all slots that has an item in it or not, disregarding if it is full or not.
        /// </summary>
        /// <returns></returns>
        public List<InventorySlot<T>> GetTakenSlots()
        {
            List<InventorySlot<T>> slots = new List<InventorySlot<T>>();
            foreach (InventorySlot<T> slot in _slots)
            {
                if (!slot.IsEmpty)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        /// <summary>
        /// Returns all the slots that are at max capacity.
        /// </summary>
        /// <returns></returns>
        public List<InventorySlot<T>> GetFullSlots()
        {
            List<InventorySlot<T>> slots = new List<InventorySlot<T>>();
            foreach (InventorySlot<T> slot in _slots)
            {
                if (slot.IsFull)
                {
                    slots.Add(slot);
                }
            }

            return slots;
        }

        #endregion

        #region Item information

        /// <summary>
        /// Checks if a specified item is in the inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool HasItem(T item)
        {
            return GetTotalQuantity(item) > 0;
        }

        /// <summary>
        /// Returns the total quantity of a certain item in the inventory.
        /// Will return -1 if the item is not in the inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int GetTotalQuantity(T item)
        {
            if (_itemList.ContainsKey(item))
            {
                return _itemList[item];
            }

            return -1;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Modifies the capacity of the inventory. Will remove all slots outside the new capacity.
        /// </summary>
        /// <param name="capacity"></param>
        protected void SetCapacity(int capacity)
        {
            Capacity = capacity;

            // Remove all slots that are outside the new capacity.
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i >= Capacity)
                {
                    _slots.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Modifies the default capacity of slots for items in the inventory.
        /// Will call the Resize method for all slots.
        /// </summary>
        /// <param name="capacity"></param>
        protected void SetSlotCapacity(int capacity)
        {
            SlotCapacity = capacity;

            // Resize all slots to the new capacity.
            foreach (InventorySlot<T> slot in _slots)
            {
                slot.Resize(capacity);
            }
        }

        /// <summary>
        /// Find the next slot that does not contain an item. In the case of a full inventory, this will return null.
        /// To avoid having to account for a null value, check Inventory.IsFull before calling this method.
        /// </summary>
        /// <returns></returns>
        protected InventorySlot<T> GetNextEmptySlot()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    return _slots[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the next slot in the inventory that contains the given item.
        /// Returns null if the item is not found.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected InventorySlot<T> GetSlotWithItem(T item)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                    continue;

                if (_slots[i].Item.Equals(item))
                {
                    return _slots[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Same as GetSlotWithItem(), except for additional check that the slot is not full.
        /// This is used when adding items to the inventory.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected InventorySlot<T> GetNotFullSlotWithItem(T item)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                    continue;

                if (_slots[i].Item.Equals(item) && !_slots[i].IsFull)
                {
                    return _slots[i];
                }
            }

            return null;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Fills the inventory with empty slots. The purpose of this is to ensure that the inventory
        /// always has slots.
        /// </summary>
        private void CreateEmptySlots()
        {
            _slots.Clear();
            for (int i = 0; i < Capacity; i++)
            {
                _slots.Add(new InventorySlot<T>());
            }
        }

        /// <summary>
        /// To keep a dictionary of all items and their total quantity in a more efficient manner
        /// than calculating it each time it is needed, we will update the dictionary when items are added or removed.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="change"></param>
        private void UpdateTotalQuantity(T item, int change)
        {
            if (_itemList.ContainsKey(item))
            {
                _itemList[item] += change;

                // We can remove the item from the dictionary if the quantity is 0.
                if (_itemList[item] == 0)
                {
                    _itemList.Remove(item);
                }
            }
            else
            {
                _itemList.Add(item, change);
            }
        }

        #endregion
    }
}
