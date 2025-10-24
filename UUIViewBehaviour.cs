
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UG.Framework
{
    public abstract partial class UUIViewBehaviour : UManagedBehaviour
    {
        [Serializable]
        public class UChild
        {
            public Transform Parent;
            public string Path;
        }

        [SerializeField]
        private UUIEventMapper EventMap = new UUIEventMapper();

        [SerializeField]
        private UChild[] ChildSettings = null;

        private RectTransform UITransform = null;

        private List<UUIViewBehaviour> Childs = new List<UUIViewBehaviour>();

        private List<UnityEventBase> RestoreEvents = new List<UnityEventBase>();

        private List<UManagedBehaviour> AttachBehaviourList = new List<UManagedBehaviour>();

        protected override void OnUnityAwake()
        {
            EventMap.Initialize();

            UITransform = GetComponent<RectTransform>();
        }

        protected override void OnUnityDestroy()
        {
            EventMap.UnInitialize();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void UnInitialize()
        {
            ClearChilds();

            for(int i = 0; i < RestoreEvents.Count; ++i)
            {
                RestoreEvents[i].RemoveAllListeners();
            }

            RestoreEvents.Clear();


            for(int i =0; i < AttachBehaviourList.Count; ++i)
            {
                if(null != AttachBehaviourList[i])
                {
                    AttachBehaviourList[i].DestroyGameObject();
                }
            }

            AttachBehaviourList.Clear();

            base.UnInitialize();
        }

        public void ResetUITransform()
        {
            UITransform.anchoredPosition = Vector2.zero;
            UITransform.offsetMin = Vector2.zero;
            UITransform.offsetMax = Vector2.zero;
            UITransform.localScale = Vector3.one;
        }

        public void ClearChilds()
        {
            UUnityHelper.DestroyGameObjects(Childs);
        }

        public RectTransform GetRectTransform()
        {
            return UITransform;
        }



        public T CreateChild<T>(int InChildIndex , bool InManualControl = false) where T : UUIViewBehaviour
        {
            if(ChildSettings.IsNullOrEmpty())
            {
                ULogger.Error($"CreateChild Error. IsNullOrEmpty");

                return null;
            }

            var AssetID = new UAssetID(ChildSettings[InChildIndex].Path);
            if(!AssetID.IsValid())
            {
                ULogger.Error($"CreateChild Error. InvalidAssetID");

                return null;
            }

            var ChildBehaviour
                = UResources.Manager.AssetInstantiate<T>(AssetID);

            if (null == ChildBehaviour)
            {
                ULogger.Error($"CreateChild Error. Path: {AssetID}");
                return null;
            }

            ChildBehaviour.AttachParent(ChildSettings[InChildIndex].Parent);
            ChildBehaviour.ResetLocalTransform();

            if(InManualControl == false)
            {
                Childs.Add(ChildBehaviour);
            }
            else
            {
                ULogger.Log("CreateChild Manual Create On");
            }

            return ChildBehaviour;
        }

        public T GetChild<T>(int InIndex) where T : UUIViewBehaviour
        {
            if (InIndex >= Childs.Count)
            {
                return null;
            }

            return Childs[InIndex] as T;
        }

        public void RemoveChild(int InIndex)
        {
            if (InIndex >= Childs.Count)
            {
                return;
            }

            Childs[InIndex].DestroyGameObject();
            Childs.RemoveAt(InIndex);
        }

        public int GetChildCount()
        {
            return Childs.Count;
        }

        public sealed override void DestroyGameObject()
        {
            base.DestroyGameObject();
        }

        public void AttachBehaviour(UManagedBehaviour InBehaviour)
        {
            AttachBehaviourList.Add(InBehaviour);
        }


        //UIEvents
        public void OnButtonClickEvent(string InPath, UnityAction InCall)
        {
            Button Behaviour = EventMap.GetBehaviour<Button>(InPath);
            if (null == Behaviour)
            {
                ULogger.Error($"Regist UIEvent Error. Path: {InPath}");

                return;
            }

            Behaviour.onClick.AddListener(InCall);

            RestoreEvents.Add(Behaviour.onClick);
        }

        public void OnToggleValueChangedEvent(string InPath, UnityAction<bool> InCall)
        {
            Toggle Behaviour = EventMap.GetBehaviour<Toggle>(InPath);
            if (null == Behaviour)
            {
                ULogger.Error($"Regist UIEvent Error. Path: {InPath}");

                return;
            }

            Behaviour.onValueChanged.AddListener(InCall);

            RestoreEvents.Add(Behaviour.onValueChanged);
        }
        public void OnDropdownValueChangedEvent(string InPath, UnityAction<int> InCall)
        {
            TMP_Dropdown Behaviour = EventMap.GetBehaviour<TMP_Dropdown>(InPath);
            if (null == Behaviour)
            {
                ULogger.Error($"Regist UIEvent Error. Path: {InPath}");

                return;
            }

            Behaviour.onValueChanged.AddListener(InCall);

            RestoreEvents.Add(Behaviour.onValueChanged);
        }
        public void OnInputFieldValueChangedEvent(string InPath, UnityAction<string> InCall)
        {
            TMP_InputField Behaviour = EventMap.GetBehaviour<TMP_InputField>(InPath);
            if (null == Behaviour)
            {
                ULogger.Error($"Regist UIEvent Error. Path: {InPath}");

                return;
            }

            Behaviour.onValueChanged.AddListener(InCall);

            RestoreEvents.Add(Behaviour.onValueChanged);
        }
        public void OnInputFieldSubmitEvent(string InPath, UnityAction<string> InCall)
        {
            TMP_InputField Behaviour = EventMap.GetBehaviour<TMP_InputField>(InPath);
            if (null == Behaviour)
            {
                ULogger.Error($"Regist UIEvent Error. Path: {InPath}");

                return;
            }

            Behaviour.onSubmit.AddListener(InCall);

            RestoreEvents.Add(Behaviour.onSubmit);
        }

    }
}
