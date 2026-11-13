using DongNamTrip.Contracts;

namespace DongNamTrip.Modules.Quotation;

/// <summary>Quy tắc thuần của báo giá (QT06): tổng tiền và kiểm tra hợp lệ. Tiền là decimal VND, làm tròn 0 chữ số.</summary>
public static class QuoteCalculator
{
    /// <summary>Cộng Quantity x UnitPrice của các hạng mục chưa nằm trong giá trọn gói; hạng mục trọn gói hiển thị nhưng không cộng lần hai.</summary>
    public static decimal Total(IEnumerable<QuoteItemForm> items) =>
        Math.Round(items.Where(item => !item.IncludedInPackage).Sum(item => item.Quantity * item.UnitPrice), 0, MidpointRounding.AwayFromZero);

    /// <summary>Trả các lỗi theo tên trường; rỗng nghĩa là hợp lệ.</summary>
    public static Dictionary<string, string[]> Validate(QuoteForm form, DateTime now)
    {
        var errors = new Dictionary<string, string[]>();
        var items = form.Items ?? [];
        if (items.Count == 0) errors[nameof(form.Items)] = ["Cần ít nhất một hạng mục."];
        else
        {
            var itemErrors = new List<string>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (string.IsNullOrWhiteSpace(item.Description) || item.Description.Length > 500) itemErrors.Add($"Hạng mục {i + 1}: mô tả 1–500 ký tự.");
                if (string.IsNullOrWhiteSpace(item.Unit) || item.Unit.Length > 50) itemErrors.Add($"Hạng mục {i + 1}: đơn vị tính 1–50 ký tự.");
                if (item.Quantity <= 0) itemErrors.Add($"Hạng mục {i + 1}: số lượng phải lớn hơn 0.");
                if (item.UnitPrice < 0) itemErrors.Add($"Hạng mục {i + 1}: đơn giá không được âm.");
            }
            if (itemErrors.Count > 0) errors[nameof(form.Items)] = [.. itemErrors];
        }
        if (form.ValidUntil <= now) errors[nameof(form.ValidUntil)] = ["Hạn hiệu lực phải sau thời điểm hiện tại."];
        var total = Total(items);
        if (form.Deposit < 0 || form.Deposit > total) errors[nameof(form.Deposit)] = ["Khoản cần thu phải từ 0 đến tổng giá."];
        if (string.IsNullOrWhiteSpace(form.CancelPolicy) || form.CancelPolicy.Length > 2000) errors[nameof(form.CancelPolicy)] = ["Điều kiện hủy 1–2000 ký tự."];
        return errors;
    }
}
