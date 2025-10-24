
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UG.Framework
{

    public class UResources : UManager<UResources>
    {
        private Dictionary<UBundleID, UBundle> Bundles = new Dictionary<UBundleID, UBundle>();
        private Dictionary<UAssetID, UAssetObject> AssetObjects = new Dictionary<UAssetID, UAssetObject>();
        
        private UBundleDatabase BundleDatabase = null;
        private AssetBundleManifest BundleManifest = null;

        private UAssetDestroyer ResourceDestroyer;
        private UBundleDestroyer BundleDestroyer;
        private UAssetCopyObjectDestroyer CopyObjectDestroyer;

        private Dictionary<UAssetID, UAssetPool> AssetPools = new Dictionary<UAssetID, UAssetPool>();

        
        
        private bool BundleSystem = false;
        public override void Initialize()
        {
            ResourceDestroyer = new UAssetDestroyer(this);
            BundleDestroyer = new UBundleDestroyer(this);
            CopyObjectDestroyer = new UAssetCopyObjectDestroyer(this);
        }
        public override void UnInitialize()
        {
            Clear();
        }
        public override void ChangingScene()
        {
        }

        public override void ChangedScene()
        {

        }

        public void Clear()
        {
            {
                var Enumerator = AssetPools.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    Enumerator.Current.Value.Release();
                }

                AssetPools.Clear();
            }

            {
                ResourceDestroyer.Clear();

                var Enumerator = AssetObjects.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    UAssetObject AssetObject = Enumerator.Current.Value;
                    
                    if (!AssetObject.IsZeroRef())
                    {
                        ULogger.Error($"UnInitialize: Asset Reference Count Error. Name: {AssetObject.GetAssetID()}[{AssetObject.GetCount()}], Bundle: {AssetObject.GetBundleID()?.GetBundleName() ?? ""}");
                    }
                }

                AssetObjects.Clear();

                Resources.UnloadUnusedAssets();
            }

            {

                BundleDestroyer.Update();

                List<UBundleID> RemoveBundles = new List<UBundleID>();

                var Enumerator = Bundles.Values.GetEnumerator();
                while (Enumerator.MoveNext())
                {
                    UBundle CurrentBundle = Enumerator.Current;

                    if (!CurrentBundle.IsZeroRef())
                    {
                        ULogger.Log($"UnInitialize: AssetBundleUnLoad Error. {CurrentBundle.GetBundleID().GetBundleName()}. Count: {CurrentBundle.GetCount()}");
                    }

                    RemoveBundles.Add(CurrentBundle.GetBundleID());
                }

                if (RemoveBundles.Count != 0)
                {
                    for (int RemoveBundleIndex = 0;
                        RemoveBundleIndex < RemoveBundles.Count;
                        ++RemoveBundleIndex)
                    {
                        UnloadBundle(RemoveBundles[RemoveBundleIndex]);
                    }

                    RemoveBundles.Clear();
                }

                Bundles.Clear();
            }
        }

        public override void Update(float InDeltaTime)
        {
            ResourceDestroyer.Update();
            BundleDestroyer.Update();
            CopyObjectDestroyer.Update();

        }


        //패치 타이밍 때문에 나눌수밖에 없을듯
        public void LoadBundleDatas()
        {
            BundleManifest = LoadManifest();
            BundleDatabase = LoadDatabase();

            BundleSystem = null != BundleManifest && null != BundleDatabase;
        }
        private UBundle FindBundle(UBundleID BundleID)
        {
            if (UBundleID.CheckValid(BundleID) == false)
            {
                return null;
            }

            if (Bundles.ContainsKey(BundleID) == true)
            {
                return Bundles[BundleID];
            }
            else
            {
                return null;
            }
        }

        private UBundle AddBundle(UBundleID BundleID)
        {
            if (UBundleID.CheckValid(BundleID) == false)
            {
                return null;
            }

            //if (InReleaseType == EReleaseType.Immidiately)
            //{
            //    HasImmidiatelyDestroyBundle = true;
            //}

            UBundle Bundle = FindBundle(BundleID);
            if (Bundle == null)
            {
                AssetBundle LoadedBundle = LoadAssetBundle(BundleID.GetBundleName());
                if(null == LoadedBundle)
                {
                    return null;
                }

#if UG_BUNDLE_LOG
                ULogger.Log($"AssetBundleLoad: {BundleID.GetBundleName()}");
#endif

                Bundle = new UBundle(BundleID, LoadedBundle, BundleDestroyer);
                 
                Bundles.Add(BundleID, Bundle);

                LoadDependencies(Bundle);
            }
            else
            {
                ULogger.Error("Already added bundle : " + BundleID.ToString());

                //Bundle.CheckBundleInfo(InReleaseType);
            }

            return Bundle;
        }
        public void UnloadBundle(UBundleID BundleID)
        {
            if (UBundleID.CheckValid(BundleID) == false)
            {
                return;
            }

            UBundle Bundle = FindBundle(BundleID);
            if (Bundle != null)
            {
#if UG_BUNDLE_LOG
                ULogger.Log($"AssetBundleUnLoad: {BundleID.GetBundleName()}");

                if (!Bundle.IsZeroRef())
                {
                    ULogger.Error($"Bundle ReferenceCount Error. {Bundle.GetCount()}");
                }
#endif
                
                Bundle.ReleaseBundle();

                Bundles.Remove(BundleID);
            }
        }

        private UBundle FindOrLoadBundle(UBundleID BundleID)
        {
            UBundle Bundle = FindBundle(BundleID);
            if (Bundle == null)
            {
                Bundle = AddBundle(BundleID);
            }

            return Bundle;
        }

        public bool LoadBundleByBundleName(string InBundleName)
        {
            UBundleID BundleID = BundleDatabase.GetBundleIDByBundleName(InBundleName);
            if (UBundleID.NullBundleID == BundleID)
            {
                ULogger.Error($"LoadBundle Error. BundleName: {InBundleName}");

                return false;
            }


            FindOrLoadBundle(BundleID);

            return true;
        }
        public UBundleRefObject LoadBundleFromAssetName(UAssetID InAssetID)
        {
            if (BundleSystem == false)
            {
                return null;
            }
                
            UBundleID BundleID = BundleDatabase.GetBundleIDByAssetName(InAssetID.GetName());

            if (BundleID != UBundleID.NullBundleID)
            {
                UBundle Bundle = null;
                if (Bundles.ContainsKey(BundleID))
                {
                    Bundle = FindBundle(BundleID);
                }
                else
                {
                    Bundle = AddBundle(BundleID);
                }

                if (null == Bundle)
                {
                    return null;
                }

                return new UBundleRefObject(Bundle);
            }
            else
            {
                return null;
            }
        }
        
        public UAssetRefObject Asset(UAssetID InAssetID)
        {
            UAssetObject AssetObject = LoadAsset(InAssetID);
            if (null != AssetObject)
            {
                //기본 리소스이기 때문에 이걸갖고 놀라면 이 레퍼런스를 기억해야한다. 
                //지워질때 레퍼카운터를 차감시킴
                UAssetRefObject RefObject = new UAssetRefObject(AssetObject);

                return RefObject;
            }

            return null;
        }

        public T AssetInstantiate<T>(UAssetID InAssetID) where T : UManagedBehaviour
        {
            UAssetObject AssetObject = LoadAsset(InAssetID);
            if (null == AssetObject)
            {
                return null;
            }

            T Template = null;
            if (UBehaviourUtility.IsPoolingObject<T>())
            {
                UAssetPool AssetPool = this.GetAssetPool(InAssetID, AssetObject);

                Template = AssetPool.Alloc() as T;
            }
            else
            {
                Object Asset = AssetObject.Instantiate();

                Template = FindTemplate<T>(Asset);
                if (null == Template)
                {
                    GameObject.DestroyImmediate(Asset);

                    ResourceDestroyer.Add(AssetObject);

                    return null;
                }

            }

            AssetObject.IncreaseRef();

            if (null != Template)
            {
                Template.Initialize();
                Template.SetAssetReference(new UAssetCopyObject(AssetObject, Template, CopyObjectDestroyer));
            }

            return Template;
        }

        public T AssetInstantiate<T>(UAssetID InAssetID, Vector3 InCenter) where T : UManagedBehaviour
        {
            UAssetObject AssetObject = LoadAsset(InAssetID);
            if (null == AssetObject)
            {
                return null;
            }

            T Template = null;
            if (UBehaviourUtility.IsPoolingObject<T>())
            {
                UAssetPool AssetPool = this.GetAssetPool(InAssetID, AssetObject);

                Template = AssetPool.Alloc(InCenter) as T;
            }
            else
            {
                Object Asset = AssetObject.Instantiate(InCenter);

                Template = FindTemplate<T>(Asset);
                if (null == Template)
                {
                    GameObject.DestroyImmediate(Asset);

                    ResourceDestroyer.Add(AssetObject);

                    return null;
                }

            }

            AssetObject.IncreaseRef();

            if (null != Template)
            {
                Template.Initialize();
                Template.SetAssetReference(new UAssetCopyObject(AssetObject, Template, CopyObjectDestroyer));
            }

            return Template;
        }


        public void PreloadAsssetPoolCount<T>(UAssetID InAssetID, int InCount)
        {
            if (!UBehaviourUtility.IsPoolingObject<T>())
            {
                ULogger.Error($"PreloadAsssetPoolCount Error. {InAssetID}");

                return;
            }

            UAssetObject AssetObject = LoadAsset(InAssetID);
            if (null == AssetObject)
            {
                ULogger.Error($"PreloadAsssetPoolCount LoadAsset Error. {InAssetID}");

                return;
            }

            UAssetPool AssetPool = this.GetAssetPool(InAssetID, AssetObject);
            AssetPool.AddSize(InCount);
        }

        private UAssetObject LoadAsset(UAssetID InAssetID)
        {
            if (UAssetID.CheckValid(InAssetID) == false)
            {   
                return null;
            }

            UAssetObject AssetObject = GetLoadedBundleResource(InAssetID);
            if (AssetObject != null)
            {
                return AssetObject;
            }
            
            if (BundleSystem)
            {
                UBundleID BundleID = BundleDatabase.GetBundleIDByAssetName(InAssetID.GetName());
                if (BundleID != UBundleID.NullBundleID)
                {
                    AssetObject = LoadBundleResource(InAssetID, BundleID);
                }
                else
                {
                    AssetObject = LoadResources(InAssetID);
                }
                
                return AssetObject;
            }
            else
            {
                AssetObject = LoadResources(InAssetID);
                return AssetObject;
            }
        }

        private UAssetObject GetLoadedBundleResource(UAssetID InAssetID)
        {
            UAssetObject AssetObject;
            if (AssetObjects.TryGetValue(InAssetID, out AssetObject))
            {
                ResourceDestroyer.CancelDestroyAssetObject(AssetObject);    
            }
            return AssetObject;
        }
        
        private UAssetObject LoadBundleResource(UAssetID InAssetID, UBundleID InBundleID)
        {
            UAssetObject AssetObject;
            
            if (string.IsNullOrEmpty(InBundleID.GetBundleName()))
            {
                ULogger.Error($"GameRecord Is null : {InAssetID.GetName()}");
                return null;
            }

            //기존에 로드된 리소스면 AssetObjects에서 걸림.
            UBundle Bundle = null;
            if (Bundles.ContainsKey(InBundleID))
            {
                Bundle = FindBundle(InBundleID);
            }
            else
            {
                Bundle = AddBundle(InBundleID);
            }
            
            UnityEngine.Object Resource = Bundle.LoadBaseAssetObject(InAssetID, ResourceDestroyer);

            AssetObject = new UAssetObject(InAssetID, Resource, ResourceDestroyer, InBundleID);

            //기존에 로드 안된 번들이거나 같은 번들이면 새로 RefCount를 할당
            Bundle.IncreaseRef();
            
            AssetObjects.Add(InAssetID, AssetObject);

            return AssetObject;
        }
        
        private UAssetObject LoadResources(UAssetID InAssetID)
        {
            UAssetObject AssetObject = null;
            
            string NoExtensionID = InAssetID.GetName().Split('.')[0];
            UnityEngine.Object Resource = UnityEngine.Resources.Load(NoExtensionID);

            if(null == Resource)
            {
                ULogger.Error($"Load fail Resource. {InAssetID}");

                return null;

            }
            
            if (AssetObjects.TryGetValue(InAssetID, out AssetObject))
            {
                UBundle Bundle = null;
                if (Bundles.ContainsKey(AssetObject.GetBundleID()))
                {
                    Bundle = FindBundle(AssetObject.GetBundleID());
                    Bundle.IncreaseRef();
                }

                return AssetObject;
            }
                
            AssetObject = new UAssetObject(InAssetID, Resource, ResourceDestroyer, UBundleID.NullBundleID);

            AssetObjects.Add(InAssetID, AssetObject);

            return AssetObject;
        }
        
        private T FindTemplate<T>(UnityEngine.Object InObject) where T : UManagedBehaviour
        {
            return UBehaviourUtility.FindTemplate<T>(InObject);   
        }

        private UAssetPool GetAssetPool(UAssetID InAssetID, UAssetObject InAssetObject)
        {
            UAssetPool Pool;
            if (!AssetPools.TryGetValue(InAssetID, out Pool))
            {
                Pool = new UAssetPool(InAssetID, this, 0);

                AssetPools.Add(InAssetID, Pool);
            }

            return Pool;
        }

        private void LoadDependencies(UBundle InBundle)
        {
            string[] Dependencies = BundleManifest.GetDirectDependencies(InBundle.GetBundleID().GetBundleName());
          
            for (int Index = 0; Index < Dependencies.Length; ++Index)
            {
                string DependencePath = Dependencies[Index].Replace('/', '\\');
                UBundleID DependBundleID = BundleDatabase.GetBundleIDByBundleName(DependencePath);
                if (DependBundleID != UBundleID.NullBundleID)
                {
#if UG_BUNDLE_LOG
                    ULogger.Log("LoadDependencies : " + Dependencies[Index]);
#endif

                    UBundle Bundle = FindOrLoadBundle(DependBundleID);

                    InBundle.AddBundleDependencies(Bundle);

                      
                }
                else
                {
                    ULogger.Error(
                        "DependBundleID == UBundleID.NullBundleID : " + Dependencies[Index]);
                }
            }
        }
        private AssetBundle LoadAssetBundle(string InBundleName)
        {
            string RootPath = UFramework.GetSettings().Bundle_RootPath();
            string AssetBundleFullPath = RootPath + InBundleName;
            
            AssetBundle Bundle = AssetBundle.LoadFromFile(AssetBundleFullPath);
            if (Bundle != null)
            {
                return Bundle;
            }
            else
            {
                ULogger.Error(
                    "AssetBundle.LoadFromFile() == null : " + AssetBundleFullPath);
            }

            return null;
        }

        internal void CallDestroyer_UnLoadAssetInstantiate(UAssetCopyObject InCopyObject)
        {
            UAssetObject OriginObject = InCopyObject.GetOriginObject();
            {
                UAssetPool AssetPool;

                UManagedBehaviour CopyedBehaviour = InCopyObject.GetCopyObject();

                if (AssetPools.TryGetValue(InCopyObject.GetAssetID(), out AssetPool))
                {
                    CopyedBehaviour.UnInitialize();

                    AssetPool.Free(CopyedBehaviour);
                }
                else
                {
                    CopyedBehaviour.UnInitialize();

                    UResources.DestroyObject(CopyedBehaviour);
                }
            }
            OriginObject.DecreaseRef();
        }
        
        internal void CallDestroyer_UnLoadAsset(UAssetObject InAssetObject)
        {
            if (InAssetObject.IsZeroRef()) //다음 프레임에서 다시 먼저 할당할 수도 있으니까 체크
            {
                AssetObjects.Remove(InAssetObject.GetAssetID());
                //이게 호출이 되야하는지 체크(번들일때)
                if (!InAssetObject.IsBuiltInResource())
                {
                    //UnloadAsset may only be used on individual assets and can not0 be used on GameObject's / Components / AssetBundles or GameManagers
                    UBundle Bundle = FindBundle(InAssetObject.GetBundleID());
           
                    if (Bundle != null)
                    {
                        Bundle.DecreaseRef();
                    }
                }
                else
                {
                    //이부분 문제가 있음. prefab이면 안되고 기본 자산이면 가능한부분
                    //Resources.UnloadAsset(InAssetObject.Asset());
                    //Object.Destroy(InAssetObject.Asset());

                    //일단 단일로 테스트할땐 문제 없었는데 후에 리소스가 많아지고 많이 언로드를 할때 렉이 걸리던지 한다면 모았다가 하던지 
                    //다른 방법을 찾아야 한다. 어차피 해제를 안시키면 유니티가 메모리가 모자라지면 불시에 청소하려 할거기 때문에 일단 이대로 진행
                    //prefab load -> prefab unload -> UnloadUnusedAssets 시 내부 링크된 리소스 즉시 지워짐
                    Resources.UnloadUnusedAssets();
                }
            }
        }
        internal void CallDestroyer_UnLoadBundle(UBundle InBundle)
        {
            if (InBundle.IsZeroRef())
            {
                UnloadBundle(InBundle.GetBundleID());
            }
        }

        private AssetBundleManifest LoadManifest()
        {
            IProjectSettings Settings = UFramework.GetSettings();

            string BundlePath = $"{UFramework.GetSettings().Bundle_RootPath()}{UFramework.GetSettings().Bundle_ManifestName()}";

            if (System.IO.File.Exists(BundlePath) == true)
            {
                AssetBundle AssetBundles = AssetBundle.LoadFromFile(BundlePath);
                AssetBundleManifest Manifest = AssetBundles.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

                if (Manifest == null)
                {
                    ULogger.Error("Invalid LoadedAssetBundleManifest");
                }

                AssetBundles.Unload(false);

                return Manifest;
            }
            else
            {
                ULogger.Error("Not using Bundle");
            }

            return null;
        }
        private UBundleDatabase LoadDatabase()
        {
            IProjectSettings Settings = UFramework.GetSettings();

            string BundleDatabasePath = $"{Settings.Bundle_RootPath()}{Settings.Bundle_DatabaseName()}";

            if (System.IO.File.Exists(BundleDatabasePath) == true)
            {
                return UFileSerializer<UBundleDatabase>.Load(BundleDatabasePath);
            }
            else
            {
                ULogger.Log($"BundleDatabase Load failed.");
            }

            return null;
        }

        public static void DestroyObject(UBaseBehaviour InBehaviour)
        {
            GameObject.DestroyImmediate(InBehaviour.GetGameObject());
        }

        public void UnUsed_ReleaseAssetPool(string InKeyString,  List<UAssetID> InIDList)
        {
            List<UAssetID> DeleteKeyList = new List<UAssetID>();
            foreach(var Data in AssetPools)
            {
                bool bUse = false;
                int Index = Data.Key.GetName().IndexOf(InKeyString);

                if (Index == 0)
                {
                    foreach(var ID in InIDList)
                    {
                        //있으면, 살려둠
                        if(ID == Data.Key)
                        {
                            bUse = true;
                            break;
                        }
                    }

                    if (!bUse)
                    {
                        DeleteKeyList.Add(Data.Key);
                    }
                }
            }

            foreach(var Data in DeleteKeyList)
            {
                ULogger.Warning($"Delete Key {Data.GetName()}");

                if(AssetPools.TryGetValue(Data, out UAssetPool CurrentAsset))
                {
                    CurrentAsset.Release();
                }

                AssetPools.Remove(Data);
            }
        }

#if UNITY_EDITOR
        internal Dictionary<UBundleID, UBundle> GetBundlesForEditor()
        {
            return Bundles;
        }

        internal Dictionary<UAssetID, UAssetObject> GetAssetObjectsForEditor()
        {
            return AssetObjects;
        }

        internal Dictionary<UAssetID, UAssetPool> GetAssetPoolForEditor()
        {
            return AssetPools;
        }
#endif

        //고민좀 해봐야함
        //이하 assetpool
        private T PoolAlloc<T>(UAssetID InAssetID) where T : UManagedBehaviour
        {
            UAssetObject AssetObject = LoadAsset(InAssetID);
            if (null == AssetObject)
            {
                return null;
            }

            Object Asset = AssetObject.Instantiate();

            T Template = null;
            Template = FindTemplate<T>(Asset);
            if (null == Template)
            {
                GameObject.DestroyImmediate(Asset);

                ResourceDestroyer.Add(AssetObject);

                return null;
            }

            //if (null != Template)
            //{
            //    Template.Initialize();
            //    Template.SetAssetReference(new UAssetCopyObject(AssetObject, Template, CopyObjectDestroyer));
            //}

            return Template;
        }

        public class UAllocator : UObjectPool<IPoolObject>.IAllocator
        {
            private UResources Resources = null;

            private UAssetID AssetID = UAssetID.Invalid;
            private UAssetRefObject AssetRefObject = null;
            public UAllocator(UAssetID InAssetID, UResources InResources)
            {
                AssetID = InAssetID;
                Resources = InResources;

                //유지시킴
                AssetRefObject = Resources.Asset(InAssetID);
                if (null == AssetRefObject)
                {
                    ULogger.Error($"Error null AssetRefObject. {InAssetID}");
                }

            }

            public void Release()
            {
                AssetRefObject?.Destroy();
                AssetRefObject = null;
            }

            public IPoolObject Alloc()
            {
                //UBaseBehaviour Value = Resources.AssetInstantiate<UBaseBehaviour>(AssetID);
                UManagedBehaviour Value = Resources.PoolAlloc<UManagedBehaviour>(AssetID);

                if (null == Value)
                {
                    return null;
                }

                if (Value is IPoolObject)
                {
                    Value.GetGameObject().SetActive(false);

                    GameObject.DontDestroyOnLoad(Value.GetGameObject());

                    return Value as IPoolObject;
                }
                else
                {
                    ULogger.Error($"PoolObject Type Mismatch. Path: {AssetID}");

                    Value.DestroyGameObject();

                    return null;
                }
            }
        }

      
    }
}
