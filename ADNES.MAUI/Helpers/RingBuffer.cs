namespace ADNES.MAUI.Helpers
{
    /// <summary>
    ///     This is a ring buffer class which allows the user to create a set number of instances of an object to
    ///     be used in circular order. Calling "GetNext" will return a reference to the next instance in the ring buffer.
    ///     When the ring buffer reaches the end of the buffer, it resets back to instance zero.
    ///
    ///     We use this class to reduce GC on things that are frequently used/created such as SKBitmaps, etc. 
    /// </summary>
    public class RingBuffer<T>
    {
        /// <summary>
        ///     Internal buffer that holds the instances of the object
        /// </summary>
        private readonly T[] _buffer;

        /// <summary>
        ///    Current index of the ring buffer
        /// </summary>
        private int _currentIndex;

        /// <summary>
        ///     Returns the current index of the ring buffer
        /// </summary>
        public int Index => _currentIndex;

        /// <summary>
        ///     Returns the length of the ring buffer
        /// </summary>
        public int Length => _buffer.Length;

        /// <summary>
        ///     Default constructor that initializes the ring buffer with a set size
        /// </summary>
        /// <param name="size"></param>
        /// <param name="initialValue"></param>
        public RingBuffer(int size, T initialValue)
        {
            _buffer = new T[size];

            //Set all instances to the initial value
            for (var i = 0; i < size; i++)
            {
                _buffer[i] = initialValue;
            }
        }

        /// <summary>
        ///     Returns the next instance in the ring buffer
        /// </summary>
        /// <returns></returns>
        public T GetNext()
        {
            var next = _buffer[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _buffer.Length;
            return next;
        }

        /// <summary>
        ///     Sets the value of the instance at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void Set(int index, T value)
        {
            _buffer[index] = value;
        }

        /// <summary>
        ///     Returns the instance at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T Get(int index)
        {
            return _buffer[index];
        }
    }
}
