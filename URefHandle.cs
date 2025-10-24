
namespace UG.Framework
{
    public class URefHandle
    {
        protected int Reference = 0;

        internal virtual void IncreaseRef()
        {
            Reference++;
        }

        internal virtual void DecreaseRef()
        {
            if (Reference <= 0)
            {
                ULogger.Error("RefCount <= 0");
            }

            Reference--;
        }

        public int GetCount()
        {
            return Reference;
        }
        public virtual bool IsZeroRef()
        {
            if (Reference == 0)
            {
                return true;
            }
            else
            {
                if (Reference < 0)
                {
                    ULogger.Error($"RefCount <= 0  {GetDebugName()}");
                }

                return false;
            }
        }

        protected virtual string GetDebugName()
        {
            return GetType().Name;
        }

        ~URefHandle()
        {
            if (Reference != 0)
            {
                ULogger.Error($"Reference count is not zero {Reference}. [{GetDebugName()}]");
            }
        }
    }


}