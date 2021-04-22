using System;
using System.Runtime.InteropServices;

namespace TeleporterPlugin.Structs {
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct Vector<T> where T : unmanaged {
        public T* First;
        public T* Last;
        public T* End;

        public nint Size() {
            if (First == null || Last == null)
                return 0;

            return ((nint)Last - (nint)First) / sizeof(T);
        }

        public nint Capacity() {
            if (End == null || First == null)
                return 0;

            return ((nint)End - (nint)First) / sizeof(T);
        }

        public T Get(nint index) {
            if (index >= Size())
                throw new IndexOutOfRangeException($"Index out of Range: {index}");

            return First[index];
        }
    }
}