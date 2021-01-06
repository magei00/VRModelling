using System;
using System.Runtime.InteropServices;

namespace Assets.GEL
{
    public class IntVector : IIntVector
    {
        
        public IntPtr[] _intVector;

        public IntVector(int size)
        {
            _intVector = IntVector_new(size);
        }

        public IntVector(IntPtr[] intVector)
        {
            _intVector = intVector;
        }

        ~IntVector()
        {
            IntVector_delete(_intVector);
        }
        
        public int Get(int index)
        {
            return IntVector_get(_intVector, index);
        }

        public int Size()
        {
            return IntVector_size(_intVector);
        }

        public IntPtr[] GetVector()
        {
            return _intVector;
        }
        /*
        public int[] ToIntArray()
        {
            int len = _intVector.Length;
            int[] result = new int[len];

            for(int i =0; i < len; i++)
            {
                result[i] = (int)_intVector.GetValue(i);
            }

            return result;
        }*/
    }
}