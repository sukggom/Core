using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;


namespace UG.Framework
{
    public class UInterface : UManager<UInterface>
    {
        private Dictionary<int, UUILayer> UILayers = new Dictionary<int, UUILayer>();
        private GameObject UIRoot = null;

        private Dictionary<UUIHandler, IUIController> Controllers = new Dictionary<UUIHandler, IUIController>();

        private bool BlockChecker = false;
        private float BlockDeltaTime = 0f;
        private readonly float BlockTimeMax = 10f;

        public override void Initialize()
        {
            UIRoot = new GameObject("UIRoot");
            UIRoot.AddComponent<UUIInputModule>();
            UIRoot.AddComponent<BaseInput>();

            GameObject.DontDestroyOnLoad(UIRoot);
        }

        public override void UnInitialize()
        {
            ClearUIObjects();

            GameObject.Destroy(UIRoot);
        }

        public override void Update(float InDeltaTime)
        {
            if (BlockChecker)
            {
                BlockDeltaTime += InDeltaTime;
                if(BlockDeltaTime > BlockTimeMax)
                {
                    EventBlock(false);
                    BlockChecker = false;
                    BlockDeltaTime = 0f;
                }
            }
        }
        public override void ChangingScene()
        {
            ClearPoolObject();
        }
        public override void ChangedScene()
        {
        }

        public void EventBlock(bool IsBlock)
        {
            if (IsBlock)
            {
                BlockChecker = true;
                BlockDeltaTime = 0f;
            }

            foreach(var Data in UILayers)
            {
                Data.Value.SetBlock(IsBlock);
            }
        }

        public void RegistLayer(int InDepth, System.Action<UUILayer> InSettingDelegate = null)
        {
            if (UILayers.ContainsKey(InDepth))
            {
                ULogger.Error($"CreateLayer Depth Error. {InDepth}");
            }
            else
            {
                UUILayer Layer = new UUILayer(UIRoot, InDepth);

                UILayers.Add(InDepth, Layer);

                InSettingDelegate?.Invoke(Layer);
            }
        }

        public UUIHandler Show<T>(int InDepth) where T : IUIController, new()
        {
            UUILayer Layer = GetLayer(InDepth);
            if (null == Layer)
            {
                ULogger.Error($"LoadUI Layer Error. Depth: {InDepth}");

                return UUIHandler.Invalid;
            }

            T Controller = default;
            System.Type targetType = typeof(T);

            foreach (KeyValuePair<UUIHandler,IUIController> kvPair in Controllers)
            {
                if (kvPair.Value.GetType() == targetType)
                {
                    Controller = (T)kvPair.Value;
                    break;
                }
            }

            if (Controller == null)
            {
                Controller = new T();
                if (false == Controller.Create(InDepth, DestroyController))
                {
                    ULogger.Error($"LoadUI Controller Create Error.");

                    Controller.DestroyGameObject();

                    return UUIHandler.Invalid;
                }

                UUIHandler Handle = new UUIHandler();
                Controller.SetHandler(Handle);

                Controllers.Add(Handle, Controller);

                Layer.Add(Controller);

                UFramework.GetMediator().AddDestination(Controller);
          
                return Handle;
            }
            else
            {
                if (Controller.GetDepth() != InDepth)
                {
                    UUILayer OldLayer = GetLayer(Controller.GetDepth());
                    if (OldLayer != null)
                    {
                        OldLayer.Remove(Controller);
                        Controllers.Remove(Controller.GetHandler());
                        UFramework.GetMediator().RemoveDestination(Controller);
                    }
                    
                    UUIHandler Handle = new UUIHandler();
                    
                    Controller.SetHandler(Handle);
                
                    Controllers.Add(Handle, Controller);
                
                    Layer.Add(Controller);
                }

                UUIViewBehaviour viewBehaviour = Controller.GetViewBehaviour();
                if (viewBehaviour != null)
                {
                    if (viewBehaviour.GetChildCount() > 0)
                    {
                        viewBehaviour.ClearChilds();    
                    }
                }
                
                UFramework.GetMediator().AddDestination(Controller);
          
                return Controller.GetHandler();
            }
        }
      
        public void SingleCast<T>(UUIHandler InHandler, T InParameter)
        {
            IUIController TmpController;
            if(!Controllers.TryGetValue(InHandler, out TmpController))
            {
                ULogger.Error($"SingleCast Handler Error. ");
                return;
            }

            UUIMessageController<T> Controller = TmpController as UUIMessageController<T>;
            if(null == Controller)
            {
                ULogger.Error($"SingleCast TypeCast Error.");

                return;
            }

            Controller.OnUIMessage(InParameter);
        }

        /// <summary>
        /// BroadCast 를 통해서 다른 Controller를 Show 하거나 생성 또는 삭제가 발생할때 사용하세요.
        /// </summary>
        public void BroadCast_CreateController<T>(T InParameter)
        {
            List<UUIMessageController<T>> KeyList = null;
            var Enumerator = Controllers.GetEnumerator();
            while (Enumerator.MoveNext())
            {
                if (Enumerator.Current.Value is UUIMessageController<T>)
                {
                    UUIMessageController<T> Controller = Enumerator.Current.Value as UUIMessageController<T>;
                    if (KeyList == null)
                    {
                        KeyList = new List<UUIMessageController<T>>();
                    }

                    KeyList.Add(Controller);
                }
            }

            if(null != KeyList)
            {
                for (int i = 0; i < KeyList.Count; ++i)
                {
                    KeyList[i].OnUIMessage(InParameter);
                }
                KeyList.Clear();
            }
            KeyList = null;
        }
        
        public void BroadCast<MT>(MT InParameter)
        {
            var Enumerator = Controllers.GetEnumerator();
            while (Enumerator.MoveNext())
            {
                if (Enumerator.Current.Value is UUIMessageController<MT>)
                {
                    UUIMessageController<MT> Controller = Enumerator.Current.Value as UUIMessageController<MT>;
                    if (null != Controller)
                    {
                        Controller.OnUIMessage(InParameter);
                    }
                }
            }
        }

        public bool IsShowController<T>() where T : IUIController
        {
            var Enumerator = Controllers.GetEnumerator();
            while (Enumerator.MoveNext())
            {
                if (Enumerator.Current.Value.GetType() == typeof(T))
                {
                    return Enumerator.Current.Value.IsVisibleCheck();
                }
            }

            return false;
        }

        private void ClearPoolObject()
        {
            var UIList = Controllers.Keys.ToArray();
            if (null != UIList)
            {
                for (int i = UIList.Length - 1; i >= 0; i--)
                {
                    Controllers[UIList[i]].ChangingScene();
                }
            }
        }

        private void DestroyController(IUIController InController)
        {
            UFramework.GetMediator().RemoveDestination(InController);

            UUILayer Layer = GetLayer(InController.GetDepth());
            if(null == Layer)
            {
                ULogger.Error($"DestroyModel Error. null Layer");

                return;
            }

            Layer.Remove(InController);
            Controllers.Remove(InController.GetHandler());
            //SingleController.Remove(InController.GetType());

            InController.DestroyGameObject();
        }

        private UUILayer GetLayer(int InDepth)
        {
            UUILayer Layer;

            if (UILayers.TryGetValue(InDepth, out Layer))
            {
                return Layer;
            }

            return null;
        }

        internal void ClearUIObjects()
        {
            //UniqueController.Clear();

            var Enumerator = UILayers.GetEnumerator();
            while(Enumerator.MoveNext())
            {
                var Controllers = Enumerator.Current.Value.GetControllers();

                for (int i = 0; i < Controllers.Count; ++i)
                {
                    Controllers[i].DestroyGameObject();
                }

                Enumerator.Current.Value.ClearBehaviours();
            }

            UILayers.Clear();
        }
    }
}
