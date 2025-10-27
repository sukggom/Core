using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UG.Framework
{
    public interface IUIController
    {
        internal void SetHandler(UUIHandler InHandler);
        internal UUIHandler GetHandler();
        internal bool Create(int InDepth, System.Action<IUIController> InDestroyer);
        internal void DestroyGameObject();
        internal UUIViewBehaviour GetViewBehaviour();
        public int GetDepth();
        public void ChangingScene();
        public bool IsVisibleCheck();
    }

    public struct UNoneMessage { }

    public interface UUIMessageController<Message> : IUIController
    {
        public void OnUIMessage(Message InParameter);
    }

    public abstract class UUIController<Behaviour, Message> : UUIMessageController<Message> where Behaviour : UUIViewBehaviour
    {
        private Behaviour ViewBehaviour = null;

        private int Depth = UUILayer.Invalid;
        private Action<IUIController> Destroyer = null;

        private UUIHandler Handler = UUIHandler.Invalid;
        protected abstract void Initialize(UUIViewBehaviour InView);
        protected abstract void UnInitialize();
        protected abstract UAssetID GetBehaviourAssetID();

        public virtual void OnUIMessage(Message InParameter) //Message가 없을수도 있음 
        {

        }

        public virtual bool IsVisibleCheck()
        {
            return true;
        }

        public int GetDepth()
        {
            return Depth;
        }

        public void DestroyUI()
        {
            Destroyer(this);
        }

        public void ChangingScene()
        {
            Destroyer(this);
        }

        protected Behaviour View()
        {
            return ViewBehaviour;
        }

        void IUIController.SetHandler(UUIHandler InHandler)
        {
            Handler = InHandler;
        }

        UUIHandler IUIController.GetHandler()
        {
            return Handler;
        }

        void IUIController.DestroyGameObject()
        {
            UnInitialize();

            ViewBehaviour?.DestroyGameObject();
            ViewBehaviour = null;
        }

        bool IUIController.Create(int InDepth, System.Action<IUIController> InDestroyer)
        {
            Depth = InDepth;
            Destroyer = InDestroyer;

            if (null != ViewBehaviour)
            {
                ULogger.Error($"null != ViewBehaviour Error.");
                return false;
            }

            UAssetID AssetID = GetBehaviourAssetID();

            if (!UAssetID.CheckValid(AssetID))
            {
                ULogger.Error($"InvalidAssetID Error. Path: {AssetID}");

                return false;
            }

            ViewBehaviour = UResources.Manager.AssetInstantiate<Behaviour>(AssetID);
            if (null == ViewBehaviour)
            {
                ULogger.Error($"CreateBehaviour Error. Path: {AssetID}");

                return false;
            }

            Initialize(ViewBehaviour);

            return true;
        }

        UUIViewBehaviour IUIController.GetViewBehaviour()
        {
            return ViewBehaviour;
        }
    }
}
