// Copyright (c) 2018 Jakob Schmid
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE."

using Anywhen;

// empty     queue   queue     queue      pop
// [ | | ]   [x| | ] [x|x| ]   [x|x|x]    [ |x|x]
//  B         F B     F   B     B          B F  
//  F                           F
public class EventQueue
{

    public struct QueuedEvent
    {
        public double EventTime;
        public NoteEvent NoteEvent;

        public void Set(NoteEvent noteEvent, double eventTime)
        {
            NoteEvent = noteEvent;
            EventTime = eventTime;
        }
        
    }

    /// State
    public QueuedEvent[] events;

    private int _back = 0;
    private int _front = 0;
    private int _size = 0;
    private int _capacity = -1;
    private object _mutexLock = new object();

    public EventQueue(int capacity)
    {
        events = new QueuedEvent[capacity];
        this._capacity = capacity;
    }

    public bool Enqueue(NoteEvent noteEvent, double eventTime)
    {
        bool didEnqueue = false;
        lock (_mutexLock)
        {
            if (_size < _capacity)
            {
                events[_back].Set(noteEvent, eventTime);
                _back = (_back + 1) % _capacity;
                _size++;
                didEnqueue = true;
            }
        }

        return didEnqueue;
    }



    public void Dequeue()
    {
        lock (_mutexLock)
        {
            if (_size > 0)
            {
                _front = (_front + 1) % _capacity;
                --_size;
            }
        }
    }

    public bool GetFront(ref QueuedEvent result)
    {
        if (_size == 0)
            return false;
        result = events[_front];
        return true;
    }

    public bool GetFrontAndDequeue(ref QueuedEvent result)
    {
        if (_size == 0)
            return false;

        lock (_mutexLock)
        {
            result = events[_front];
            _front = (_front + 1) % _capacity;
            --_size;
        }

        return true;
    }

    public void Clear()
    {
        _front = 0;
        _back = 0;
        _size = 0;
    }

    public bool IsEmpty => _size == 0;

    public int GetSize() => _size;
}