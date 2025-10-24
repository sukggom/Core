using System;
using System.Collections.Generic;
using System.Linq;

namespace UG.Framework
{
    public interface IMediatorSource
    {
        Type GetDestinationType();
    }

    public interface IMediatorDestination { }

    public interface IMediatorDestination<T> : IMediatorDestination
    {
        void OnMessage(IMediatorSource InSender, T InData);
    }

    public class UMediator
    {
        private Dictionary<Type, List<IMediatorDestination>> Mediators = new Dictionary<Type, List<IMediatorDestination>>();

        public void RegistMessage<T>() where T : IMediatorDestination
        {
            Type MessageType = typeof(T);
            if(Mediators.ContainsKey(MessageType))
            {
                ULogger.Error($"Mediator RegistMessage Error.");

                return;
            }

            Mediators.Add(MessageType, new List<IMediatorDestination>());
        }

        public void AddDestination<T>(T InTarget)
        {
            var Enumerator = Mediators.GetEnumerator();
            while(Enumerator.MoveNext())
            {
                if(Enumerator.Current.Key.IsAssignableFrom(InTarget.GetType()))
                {
                    Enumerator.Current.Value.Add(InTarget as IMediatorDestination);
                }
            }
        }

        public void RemoveDestination<T>(T InTarget)
        {
            var Enumerator = Mediators.GetEnumerator();
            while (Enumerator.MoveNext())
            {
                if (Enumerator.Current.Key.IsAssignableFrom(InTarget.GetType()))
                {
                    Enumerator.Current.Value.Remove(InTarget as IMediatorDestination);
                }
            }
        }

        public void SendEvent<T>(IMediatorSource InSource, T InData)
        {
            List<IMediatorDestination> Dests;
            if (!Mediators.TryGetValue(InSource.GetDestinationType(), out Dests))
            {
                ULogger.Error($"Mediator SendEvent Error.");
                return;
            }

            for (int i = 0; i < Dests.Count; ++i)
            {
                var Dest = Dests[i] as IMediatorDestination<T>;
                if (null != Dest)
                {
                    Dest.OnMessage(InSource, InData);
                }
            }
        }

        public void Release()
        {
            Mediators.Clear();
        }



    }
}
