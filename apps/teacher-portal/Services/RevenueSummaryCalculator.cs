using StepGo.PricingRules;
using StepGo.TeacherPortal.Models;
using StepGo.UI.Status;

namespace StepGo.TeacherPortal.Services;

public sealed record RevenueSummary(int OrderCount, decimal GrossRevenue, decimal TotalCost, decimal NetAmount);

/// <summary>frontend-teacher-portal spec: "本期收入總覽（報名筆數、總營收毛額、總扣除成本、應收淨額）".</summary>
public static class RevenueSummaryCalculator
{
    public static RevenueSummary Calculate(IEnumerable<Order> orders)
    {
        var paid = orders.Where(o => o.PaymentStatus == PaymentStatus.Paid).ToList();

        var grossRevenue = paid.Sum(o => o.Amount);
        var totalCost = paid.Sum(o =>
        {
            var breakdown = PricingCalculator.Calculate(o.Amount, o.PaymentMethod);
            return breakdown.GatewayFee + breakdown.PlatformFee + breakdown.PayoutTransferFee;
        });

        return new RevenueSummary(paid.Count, grossRevenue, totalCost, grossRevenue - totalCost);
    }
}
