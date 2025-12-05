using System.Collections.Generic;
using UnityEngine;

public static class SequenceEvaluator
{
    public enum SequenceType
    {
        None,
        Geometric,      // 等比
        Arithmetic,     // 等差
        Increasing,     // 递增
        Decreasing,     // 递减
        Odd,            // 奇数列
        Even,           // 偶数列
        Fibonacci       // 斐波那契
    }

    // 主判定函数
    public static List<SequenceType> Evaluate(List<int> numbers)
    {
        List<SequenceType> result = new List<SequenceType>();
        if (numbers.Count < 3) return result; // 至少3张

        if (IsArithmetic(numbers)) result.Add(SequenceType.Arithmetic);
        if (IsGeometric(numbers)) result.Add(SequenceType.Geometric);
        if (IsFibonacci(numbers)) result.Add(SequenceType.Fibonacci);
        
        // 奇偶性判定
        if (IsAllOdd(numbers)) result.Add(SequenceType.Odd);
        if (IsAllEven(numbers)) result.Add(SequenceType.Even);

        // 只有在不是等差/等比/斐波那契时，才单独列出递增/递减（或者你可以根据设计决定是否共存）
        // 这里假设它们是独立属性，可以共存
        if (IsIncreasing(numbers)) result.Add(SequenceType.Increasing);
        if (IsDecreasing(numbers)) result.Add(SequenceType.Decreasing);

        return result;
    }

    static bool IsArithmetic(List<int> nums)
    {
        int diff = nums[1] - nums[0];
        for (int i = 2; i < nums.Count; i++)
        {
            if (nums[i] - nums[i - 1] != diff) return false;
        }
        return true;
    }

    static bool IsGeometric(List<int> nums)
    {
        if (nums[0] == 0) return false; // 避免除零
        float ratio = (float)nums[1] / nums[0];
        // 简单处理：如果除不尽（非整数倍）通常在扑克里不算标准等比，除非你允许浮点
        // 这里假设必须是整数倍等比，比如 2, 4, 8
        if (nums[1] % nums[0] != 0) return false; 
        
        int intRatio = nums[1] / nums[0];
        
        for (int i = 2; i < nums.Count; i++)
        {
             if (nums[i - 1] == 0 || nums[i] % nums[i - 1] != 0) return false;
             if (nums[i] / nums[i - 1] != intRatio) return false;
        }
        return true;
    }

    static bool IsIncreasing(List<int> nums)
    {
        for (int i = 1; i < nums.Count; i++)
        {
            if (nums[i] <= nums[i - 1]) return false;
        }
        return true;
    }

    static bool IsDecreasing(List<int> nums)
    {
        for (int i = 1; i < nums.Count; i++)
        {
            if (nums[i] >= nums[i - 1]) return false;
        }
        return true;
    }

    static bool IsAllOdd(List<int> nums)
    {
        foreach (var n in nums) if (n % 2 == 0) return false;
        return true;
    }

    static bool IsAllEven(List<int> nums)
    {
        foreach (var n in nums) if (n % 2 != 0) return false;
        return true;
    }

    static bool IsFibonacci(List<int> nums)
    {
        // 斐波那契定义：F(n) = F(n-1) + F(n-2)
        // 比如 1, 2, 3, 5 或 2, 3, 5, 8
        for (int i = 2; i < nums.Count; i++)
        {
            if (nums[i] != nums[i - 1] + nums[i - 2]) return false;
        }
        return true;
    }
}