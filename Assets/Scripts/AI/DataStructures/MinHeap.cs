using System;
using System.Collections.Generic;

namespace AI.DataStructures
{
    public class MinHeap<T>
    {
        private List<(T item, float priority)> heap = new List<(T, float)>();
        public int Count => heap.Count;
        public bool IsEmpty => heap.Count == 0;
        
        public void Enqueue(T item, float priority)
        {
            heap.Add((item, priority));
            HeapifyUp(heap.Count - 1);
        }
        
        public T Dequeue()
        {
            if (heap.Count == 0)
                throw new InvalidOperationException("Heap is empty");
            T min = heap[0].item;
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count > 0)
                HeapifyDown(0);
            return min;
        }
        
        public void Clear() => heap.Clear();
        
        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (heap[parent].priority <= heap[index].priority)
                    break;
                Swap(index, parent);
                index = parent;
            }
        }
        
        private void HeapifyDown(int index)
        {
            int lastIndex = heap.Count - 1;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;
                if (left <= lastIndex && heap[left].priority < heap[smallest].priority)
                    smallest = left;
                if (right <= lastIndex && heap[right].priority < heap[smallest].priority)
                    smallest = right;
                if (smallest == index)
                    break;
                Swap(index, smallest);
                index = smallest;
            }
        }
        
        private void Swap(int i, int j)
        {
            var temp = heap[i];
            heap[i] = heap[j];
            heap[j] = temp;
        }
    }
}
