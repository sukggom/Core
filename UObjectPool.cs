using System.Collections.Generic;

namespace UG.Framework
{

    public class UObjectPool<T> : IPool where T : class, IPoolObject
    {
        public interface IAllocator
        {
            public T Alloc();
            public void Release();
        }

        protected Queue<T> Objects = new Queue<T>();
        protected IAllocator Allocator = null;
        protected int AllocCount = 1;
        protected int TotalCount = 0;

        protected System.Action<T> RecycleAction = null;

#if UG_DEBUG_OBJECTPOOL
        private Dictionary<T, string> DebugTrace = new Dictionary<T, string>();
#endif
        public UObjectPool(int InAllocCount = 1)
        {
            AllocCount = InAllocCount;
        }

        public virtual void Free(IPoolObject InObject)
        {
            if (InObject is T)
            {
                this.Free(InObject as T);
            }
            else
            {
                ULogger.Error($"ObjectPool Free Error. bad type. {typeof(T)}");
            }
        }

        public virtual void Release()
        {
            if(TotalCount != Objects.Count)
            {
                ULogger.Error($"ObjectPool Count Error. TotalCount: {TotalCount}, CurrentCount: {Objects.Count}");
            }

            var ObjectEnumerator = Objects.GetEnumerator();
            while(ObjectEnumerator.MoveNext())
            {
                ObjectEnumerator.Current.OnLastRelease();
            }

            Objects.Clear();

            TotalCount = 0;

#if UG_DEBUG_OBJECTPOOL
            ShowDebugTrace();
#endif

            Allocator?.Release();
            Allocator = null;
        }

        public void SetAllocator(IAllocator InAllocator)
        {
            Allocator = InAllocator;
        }

        public void SetRecycleAction(System.Action<T> InRecycleAction)
        {
            RecycleAction = InRecycleAction;
        }
        public virtual T Alloc()
        {
            if(Objects.Count <= 0)
            {
                if( false == AddCount(AllocCount))
                {
                    return null;
                }
            }

            T Object = Objects.Dequeue();

            Object.OnAlloc();
#if UG_DEBUG_OBJECTPOOL
            DebugTrace.Add(Object, UnityEngine.StackTraceUtility.ExtractStackTrace());
#endif
            
#if UG_DEBUG_OBJECTS_COUNT
            UObjectCounterProfiler.DrawStateInfo(string.Format("[Pooling {0}]",typeof(T).ToString()), TotalCount);
#endif
            
            return Object;
        }

        public virtual void Free(T InObject)
        {
            InObject.OnFree();

            Objects.Enqueue(InObject);

#if UG_DEBUG_OBJECTPOOL
            DebugTrace.Remove(InObject);
#endif
#if UG_DEBUG_OBJECTS_COUNT
            UObjectCounterProfiler.DrawStateInfo(string.Format("[Pooling {0}]",typeof(T).ToString()), TotalCount);
#endif
        }

        public bool AddCount(int InCount)
        {
            for (int i = 0; i < InCount; ++i)
            {
                T Object = null == Allocator 
                    ? System.Activator.CreateInstance<T>()
                    : Allocator.Alloc();

                if (null == Object)
                {
                    ULogger.Error($"UObjectPool Alloca failed. Type: {typeof(T).FullName}");

                    return false;
                }

                Object.OnFirstCreate();

                Objects.Enqueue(Object);

                TotalCount += InCount;

            }

            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Objects.GetEnumerator();
        }

#if UG_DEBUG_OBJECTPOOL

        private void ShowDebugTrace()
        {
            if (DebugTrace.Count > 0)
            {
                string TraceType = typeof(T).FullName;

                ULogger.Error($"ObjectPool DebugTrace Error. [{TraceType}]");
                var Enumerator = DebugTrace.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    ULogger.Error($"-- DebugTrace TraceType: [{TraceType}], StackTrace: [{Enumerator.Current.Value}]");
                }

                DebugTrace.Clear();
            }
        }

     
     
#endif

    }
}
