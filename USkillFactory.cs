using System;
using System.Collections;
using System.Collections.Generic;
using UG.Framework;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class USkillNameAttribute : Attribute
{
    public string SkillName { get; }
    public USkillNameAttribute(string InName) => SkillName = InName;
}


public static class USkillFactory
{
    private static readonly Dictionary<string, Func<UBaseSkill>> ctorMap = new Dictionary<string, Func<UBaseSkill>>();
    private static bool Init = false;

    public static void Initialize()
    {
        if (Init)
        {
            return;
        }

        Init = true;

        foreach (var t in typeof(UBaseSkill).Assembly.GetTypes())
        {
            if (t.IsAbstract || !typeof(UBaseSkill).IsAssignableFrom(t))
            {
                continue;
            }

            var Attr = (USkillNameAttribute)Attribute.GetCustomAttribute(t, typeof(USkillNameAttribute));

            if (Attr == null)
            {
                ULogger.Error($"SkillFactory Attr Null {t.FullName}");
                continue;
            }

            string Key = Attr.SkillName;
            if (string.IsNullOrWhiteSpace(Key))
            {
                ULogger.Error($"SkillFactory: empty SkillName on type={t.FullName}");
                continue;
            }

            var ctor = t.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
            {
                ULogger.Error($"SkillFactory SkillName Invalid : {Key}");
                continue;
            }

            Func<UBaseSkill> Factory = () => ctor.Invoke(null) as UBaseSkill;
            if (!ctorMap.ContainsKey(Key))
            {
                ctorMap.Add(Key, Factory);
            }
        }
    }

    public static bool Create(string InKey, out UBaseSkill skill)
    {
        if (!Init)
        {
            Initialize();
        }

        //있는것만 만들거임
        if (ctorMap.TryGetValue(InKey, out var outFactory))
        {
            skill = outFactory();
            return true;
        }

        ULogger.Error($"SkillFactory Create is Fail {InKey}");

        skill = null;
        return false;
    }
}
