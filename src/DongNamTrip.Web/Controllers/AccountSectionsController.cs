using System.Net;
using DongNamTrip.Client;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DongNamTrip.Web.Controllers;

[Authorize]
public class AccountSectionsController(TripApiClient api) : Controller
{
    private string Token => User.FindFirst("api_token")!.Value;

    [HttpGet("Account/Security")]
    public async Task<IActionResult> Security()
    {
        try
        {
            ViewBag.Sessions = await api.SessionsAsync(Token);
            return View(new ChangePasswordRequest());
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Security"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            ViewBag.LoadError = "Chưa tải được phiên đăng nhập. Vui lòng thử lại.";
            return View(new ChangePasswordRequest());
        }
    }

    [HttpPost("Account/Security")]
    public async Task<IActionResult> Security(ChangePasswordRequest input)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await api.ChangePasswordAsync(input, Token);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                TempData["PasswordChanged"] = "Đã đổi mật khẩu. Tất cả phiên đăng nhập đã kết thúc; vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Account/Security" });
            }
            catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Security"); }
            catch (HttpRequestException error) when ((int?)error.StatusCode == 422) { ModelState.AddModelError(nameof(input.CurrentPassword), "Mật khẩu hiện tại không đúng."); }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { ModelState.AddModelError("", "Chưa lưu được mật khẩu. Vui lòng thử lại."); }
        }
        input.CurrentPassword = input.NewPassword = input.ConfirmNewPassword = "";
        ClearPasswordValue(nameof(input.CurrentPassword));
        ClearPasswordValue(nameof(input.NewPassword));
        ClearPasswordValue(nameof(input.ConfirmNewPassword));
        try { ViewBag.Sessions = await api.SessionsAsync(Token); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { ViewBag.LoadError = "Chưa tải được phiên đăng nhập."; }
        return View(input);
    }

    [HttpPost("Account/Security/Revoke/{id}")]
    public async Task<IActionResult> Revoke(string id)
    {
        try
        {
            var current = (await api.SessionsAsync(Token)).FirstOrDefault(item => item.Id == id)?.IsCurrent == true;
            await api.RevokeSessionAsync(id, Token);
            if (current)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account", new { returnUrl = "/Account/Security" });
            }
            TempData["SessionStatus"] = "Đã kết thúc phiên đã chọn.";
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Security"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { TempData["SessionStatus"] = "Chưa kết thúc được phiên. Vui lòng thử lại."; }
        return RedirectToAction(nameof(Security));
    }

    [HttpGet("Account/Payments")]
    [HttpGet("Account/Payments/{id:int}")]
    public async Task<IActionResult> Payments(int? id, string? status)
    {
        try
        {
            var all = await api.PaymentsAsync(Token);
            ViewBag.Choice = await api.PaymentChoiceAsync(Token);
            ViewBag.Status = status;
            ViewBag.Selected = id is null ? null : all.FirstOrDefault(item => item.Id == id);
            if (id is not null && ViewBag.Selected is null) return NotFound();
            return View(all.Where(item => string.IsNullOrEmpty(status) || item.Status == status).ToList());
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Payments"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            ViewBag.LoadError = "Chưa tải được dữ liệu đơn và giao dịch. Vui lòng thử lại.";
            return View(new List<PaymentEntry>());
        }
    }

    [HttpPost("Account/Payments/Choice")]
    public async Task<IActionResult> SavePaymentChoice(string? methodCode)
    {
        if (methodCode is not ("bank_transfer" or "cash"))
            return BadRequest(new { message = "Vui lòng chọn một phương thức ưu tiên." });
        try
        {
            var choice = await api.UpdatePaymentChoiceAsync(new(methodCode), Token);
            return Json(new { choice.MethodCode, label = PaymentChoiceLabel(choice.MethodCode!), message = "Đã lưu lựa chọn ưu tiên. Chưa có khoản thanh toán nào được thực hiện." });
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new { loginUrl = Url.Action("Login", "Account", new { returnUrl = "/Account/Payments" }) });
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return StatusCode(503, new { message = "Chưa lưu được lựa chọn. Vui lòng thử lại." });
        }
    }

    private static string PaymentChoiceLabel(string code) => code == "bank_transfer" ? "Ưu tiên chuyển khoản ngân hàng" : "Ưu tiên trao đổi về tiền mặt";

    [HttpGet("Account/Favorites")]
    public async Task<IActionResult> Favorites(string? tab)
    {
        ViewBag.Tab = tab is "search" or "viewed" ? tab : "favorites";
        try { return View(await api.CollectionsAsync(Token)); }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Favorites"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            ViewBag.LoadError = "Chưa tải được danh sách. Vui lòng thử lại.";
            return View(new AccountCollections([], [], []));
        }
    }

    [HttpPost("Account/Favorites/{kind}/{id:int}")]
    public async Task<IActionResult> Favorite(string kind, int id, bool save, string? returnUrl)
    {
        try
        {
            if (save) await api.SaveFavoriteAsync(kind, id, Token);
            else await api.DeleteFavoriteAsync(kind, id, Token);
            TempData["CollectionStatus"] = save ? "Đã lưu vào yêu thích." : "Đã bỏ khỏi yêu thích.";
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Favorites"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { TempData["CollectionStatus"] = "Chưa cập nhật được yêu thích. Vui lòng thử lại."; }
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction(nameof(Favorites));
    }

    [HttpPost("Account/Favorites/Activity/{id:long}")]
    public async Task<IActionResult> DeleteActivity(long id, string tab)
    {
        try { await api.DeleteActivityAsync(id, Token); TempData["CollectionStatus"] = "Đã xóa mục lịch sử."; }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Favorites"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { TempData["CollectionStatus"] = "Chưa xóa được mục. Vui lòng thử lại."; }
        return RedirectToAction(nameof(Favorites), new { tab });
    }

    [HttpPost("Account/Favorites/Clear")]
    public async Task<IActionResult> ClearActivity(string tab)
    {
        try { await api.ClearActivityAsync(tab == "search" ? "search" : "viewed", Token); TempData["CollectionStatus"] = "Đã xóa lịch sử đã chọn."; }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Favorites"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { TempData["CollectionStatus"] = "Chưa xóa được lịch sử. Vui lòng thử lại."; }
        return RedirectToAction(nameof(Favorites), new { tab });
    }

    [HttpGet("Account/Notices")]
    public async Task<IActionResult> Notices()
    {
        try { return View(new AccountNoticesPage(await api.NoticesAsync(Token), await api.PreferencesAsync(Token))); }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Notices"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            ViewBag.LoadError = "Chưa tải được thông báo. Vui lòng thử lại.";
            return View(new AccountNoticesPage([], new(true)));
        }
    }

    [HttpPost("Account/Notices/{id:long}/Read")]
    public async Task<IActionResult> ReadNotice(long id)
    {
        try { await api.ReadNoticeAsync(id, Token); TempData["NoticeStatus"] = "Đã đánh dấu đã đọc."; }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Notices"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { TempData["NoticeStatus"] = "Chưa cập nhật được thông báo. Vui lòng thử lại."; }
        return RedirectToAction(nameof(Notices));
    }

    [HttpPost("Account/Notices/Preferences")]
    public async Task<IActionResult> Preferences(bool securityNotices)
    {
        try { await api.UpdatePreferencesAsync(new(securityNotices), Token); TempData["NoticeStatus"] = "Đã lưu tùy chọn thông báo trong ứng dụng."; }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized) { return await Expired("Notices"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException) { TempData["NoticeStatus"] = "Chưa lưu được tùy chọn. Vui lòng thử lại."; }
        return RedirectToAction(nameof(Notices));
    }

    private async Task<IActionResult> Expired(string action)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account", new { returnUrl = "/Account/" + action });
    }

    private void ClearPasswordValue(string key)
    {
        if (!ModelState.TryGetValue(key, out var state)) return;
        var errors = state.Errors.Select(error => error.ErrorMessage).ToArray();
        ModelState.Remove(key);
        foreach (var error in errors) ModelState.AddModelError(key, error);
    }
}
