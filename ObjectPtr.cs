using System.Collections.Generic;


namespace UG.Framework
{
    public interface IUObjectPointer
    {
        void OnRemove();
    }

    public struct FObjectPtr<TValue> where TValue : class, System.IDisposable
    {
        private class TObjectPtr
        {
            private TValue Value;

            public void Set(TValue InValue)
            {
                Clear();

                Value = InValue;
            }

            public TValue Get()
            {
                return Value;
            }

            public bool IsValid()
            {
                return Value != null;
            }

            ~TObjectPtr()
            {
                if (Value != null)
                {
                    ULogger.Error($"Object[{Value.ToString()}] is not being deleted, please delete.");

                    Clear();
                }
            }

            private void Clear()
            {
                if (Value != null)
                {
                    Value.Dispose();
                    Value = null;
                }
            }
        }

        private TObjectPtr Ptr;

        public TValue Value
        {
            get
            {
                if (Ptr != null)
                {
                    return Ptr.Get();
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsValid()
        {
            if (Ptr == null)
            {
                return false;
            }
            else
            {
                return Ptr.IsValid();
            }
        }

        public void Clear()
        {
            if (Ptr != null)
            {
                Ptr.Set(null);
            }
        }

        public void Set(TValue InValue)
        {
            if (InValue == null)
            {
                Clear();
            }
            else
            {
                if (Ptr == null)
                {
                    Ptr = new TObjectPtr();
                }

                Ptr.Set(InValue);
            }
        }
    }

    public interface IUPointer
    {
        public void AddObjectPointer(IUObjectPointer InObjectPointer);

        public void RemoveObjectPointer(IUObjectPointer InObjectPointer);
    }

    public struct UObjectPointer
    {
        private List<IUObjectPointer> ObjectPointers;

        public void Add(IUObjectPointer InObjectPointer)
        {
            if (ObjectPointers == null)
            {
                ObjectPointers = new List<IUObjectPointer>();
            }

            if (!ObjectPointers.Contains(InObjectPointer))
            {
                ObjectPointers.Add(InObjectPointer);
            }
            else
            {
                ULogger.Error($"Unregistered IUObjectPointer. [{GetType().Name}]");
            }
        }

        public void Remove(IUObjectPointer InObjectPointer)
        {
            if (ObjectPointers != null && ObjectPointers.Contains(InObjectPointer))
            {
                ObjectPointers.Remove(InObjectPointer);
            }
            else
            {
                ULogger.Error($"Already registered IUObjectPointer. [{GetType().Name}]");
            }
        }

        public void Clear()
        {
            if (ObjectPointers != null)
            {
                for (int i = 0; i < ObjectPointers.Count; i++)
                {
                    ObjectPointers[i].OnRemove();
                }

                ObjectPointers.Clear();
            }
        }
    }

    public struct FWeakPtr<TValue> where TValue : class, IUPointer
    {

        private class TObjectPtr : IUObjectPointer
        {
            private TValue Value;
            void IUObjectPointer.OnRemove()
            {
                if (Value != null)
                {
                    Value = null;
                }
            }
            ~TObjectPtr()
            {
                if (Value != null)
                {
                    ULogger.Error($"Object[{Value.ToString()}] is not being deleted, please delete.");

                    Clear();
                }
            }

            public void Set(TValue InValue)
            {
                Clear();

                Value = InValue;

                if (Value != null)
                {
                    Value.AddObjectPointer(this);
                }
            }

            public TValue Get()
            {
                return Value;
            }

            public bool IsValid()
            {
                return Value != null;
            }

            public void Clear()
            {
                if (Value != null)
                {
                    Value.RemoveObjectPointer(this);

                    Value = null;
                }
            }
        }

        private TObjectPtr Ptr;
        public TValue Value
        {
            get
            {
                if (Ptr != null)
                {
                    return Ptr.Get();
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsValid()
        {
            if (Ptr == null)
            {
                return false;
            }
            else
            {
                return Ptr.IsValid();
            }
        }

        public void Clear()
        {
            if (Ptr != null)
            {
                Ptr.Set(null);
            }
        }

        public void Set(TValue InValue)
        {
            if (InValue == null)
            {
                Clear();
            }
            else
            {
                if (Ptr == null)
                {
                    Ptr = new TObjectPtr();
                }

                Ptr.Set(InValue);
            }
        }
    }
}
