using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;

public static class Util
{
    public static T GetOrAddComponent<T>(GameObject go) where T : UnityEngine.Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
            component = go.AddComponent<T>();

        return component;
    }

    public static GameObject FindChild(GameObject go, string name = null, bool recursive = false)
    {
        Transform transform = FindChild<Transform>(go,name,recursive);
        if (transform == null)
            return null;

        return transform.gameObject;
    }

    public static T FindChild<T>(GameObject go, string name = null, bool recursive = false) where T : UnityEngine.Object
    {
        if (go == null)
            return null;

        if (recursive == false)
        {
            for (int i = 0; i < go.transform.childCount; ++i)
            {
                Transform transform = go.transform.GetChild(i);
                if (string.IsNullOrEmpty(name) || transform.name == name)
                {
                    T component = transform.GetComponent<T>();
                    if (component != null)
                        return component;
                }
            }
        }
        else
        {
            foreach (T component in go.GetComponentsInChildren<T>(true))
            {
                if (string.IsNullOrEmpty(name) || component.name == name)
                    return component;
            }
        }

        return null;
    }

    public static T Parse<T>(string stringData)
    {
        return (T)Enum.Parse(typeof(T), stringData);
    }

    public static string RemovePrefix(string str, string prefix)
    {
        // 문자열이 null이거나 비어있을 경우 그대로 반환
        if (string.IsNullOrEmpty(str))
            return str;

        // 접두사가 문자열의 시작과 일치하는지 확인하고, 일치하면 해당 부분을 제거한 문자열 반환
        if (str.StartsWith(prefix))
            return str.Substring(prefix.Length);

        // 일치하지 않으면 원래 문자열 그대로 반환
        return str;
    }

    public static float CalculatePercent(int left, int right)
    {
        if (left > right)
            return (float)right / (float)left;
        else
            return (float)left / (float)right;
    }

    public static string SummaryOfNumbers(int number)
    {
        return SummaryOfNumbers($"{number}");
    }

    public static string SummaryOfNumbers(ulong number)
    {
        return SummaryOfNumbers($"{number}");
    }

    public static string SummaryOfNumbers(string number)
    {
        char[] unitAlphabet = new char[6] {'A','B','C','D','E','F'};
        int unit = 0;
        while (number.Length > 6)
        {
            unit++;
            number = number.Substring(0, number.Length - 3);
        }
        if (number.Length > 3)
        {
            int newInt = int.Parse(number);

            float retF = newInt/1000f;
            retF = Mathf.Floor(retF * 10) * 0.1f;
            return retF.ToString("0.0") + unitAlphabet[unit];
        }
        else
        {
            return number;
        }
    }

    //public static T GetRandomWeightedSelect<T>(List<T> options) where T : IRandomWeighted
    //{
    //    int totalWeight = 0;
    //    foreach (var option in options)
    //    {
    //        totalWeight += option.Weight;
    //    }

    //    int randomWeight = UnityEngine.Random.Range(0, totalWeight);
    //    int currentWeight = 0;

    //    foreach (var option in options)
    //    {
    //        if (option.Weight == 0)
    //            continue;

    //        currentWeight += option.Weight;
    //        if (randomWeight < currentWeight)
    //        {
    //            return option;
    //        }
    //    }

    //    Debug.LogWarning($"Failed Random Weighte Select : {typeof(T)}");
    //    return default; // 이론적으로는 여기 도달하지 않아야 함
    //}
}
