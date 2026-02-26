using System;
using System.Collections.Generic;
public class MinHeap<T>
{
    private List<(T item, float priority)> _heap = new List<(T, float)>();
    public int Count => _heap.Count;
    public bool IsEmpty => _heap.Count == 0;
    public void Enqueue(T item, float priority)
    {
        _heap.Add((item, priority));
        HeapifyUp(_heap.Count - 1);
    }
    public T Dequeue()
    {
        if (_heap.Count == 0)
            throw new InvalidOperationException("Heap is empty");
        T min = _heap[0].item;
        _heap[0] = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);
        if (_heap.Count > 0)
            HeapifyDown(0);
        return min;
    }
    public void Clear() => _heap.Clear();
    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_heap[parent].priority <= _heap[index].priority)
                break;
            Swap(index, parent);
            index = parent;
        }
    }
    private void HeapifyDown(int index)
    {
        int lastIndex = _heap.Count - 1;
        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int smallest = index;
            if (left <= lastIndex && _heap[left].priority < _heap[smallest].priority)
                smallest = left;
            if (right <= lastIndex && _heap[right].priority < _heap[smallest].priority)
                smallest = right;
            if (smallest == index)
                break;
            Swap(index, smallest);
            index = smallest;
        }
    }
    private void Swap(int i, int j)
    {
        var temp = _heap[i];
        _heap[i] = _heap[j];
        _heap[j] = temp;
    }
}
