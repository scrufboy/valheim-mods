using System;
using LivingVillageCore;
class WorkBudgetTests
{
    static int checks;
    static void Check(bool value){if(!value)throw new Exception("budget check "+checks);checks++;}
    static void Main()
    {
        var budget=new WorkBudget(100);Check(budget.Spend(20));Check(budget.Current==80);
        Check(!budget.Spend(81));Check(budget.Current==80);Check(!budget.Spend(-1));
        budget.Rest(1);Check(budget.Current==88);budget.Rest(100);Check(budget.Current==100);
        budget.Rest(-5);Check(budget.Current==100);Check(new WorkBudget(-1).Current==0);
        Check(new WorkBudget(200).Current==100);Console.WriteLine(checks+" work-budget tests passed");
    }
}
