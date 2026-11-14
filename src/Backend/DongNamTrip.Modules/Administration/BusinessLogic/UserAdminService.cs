using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using DongNamTrip.Contracts;
using DongNamTrip.Modules.Persistence.Entities;
using Microsoft.AspNetCore.Identity;

namespace DongNamTrip.Modules.Administration;

/// <summary>Quản lý người dùng và vai trò (UC24, SCR19). Mật khẩu tạm chỉ trả một lần, không ghi nhật ký.</summary>
public sealed class UserAdminService(UserAdminRepository repository, AuditRepository audit, SqlSessionStore sessions, IPasswordHasher<Taikhoan> hasher, IAdministrationApi administration)
{
    private const string AccountType = "TAIKHOAN";
    private static readonly OpResult<AdminUser> Busy = Op.Conflict("Đang có thay đổi quản trị khác, hãy thử lại sau ít giây.");
    private static readonly string[] KnownRoles = [.. AppRoles.Staff, AppRoles.Customer];

    public Task<List<string>> RolesAsync(CancellationToken ct) => repository.RolesAsync(ct);
    public Task<List<AdminAccount>> AccountsAsync(string? query, string? role, CancellationToken ct) => repository.AccountsAsync(query, role, ct);

    public async Task<OpResult<AdminAccountPage>> GroupedAsync(string? group, string? query, string? role, string? status, int? page, CancellationToken ct)
    {
        if (group is not ("staff" or "customer") || status is not (null or "" or AccountStatus.Active or AccountStatus.Locked) || page is < 1 or > 1000000)
            return Op.Invalid("Bộ lọc hoặc trang không hợp lệ.");
        return Op.Ok(await repository.AccountPageAsync(group, query, role, status, page ?? 1, ct));
    }

    public async Task<PagedResult<AdminUser>> ListAsync(UserFilter filter, CancellationToken ct)
    {
        var page = Math.Clamp(filter.Page ?? 1, 1, Limits.MaxPage);
        var size = Math.Clamp(filter.Size ?? 20, 1, Limits.MaxPageSize);
        var (rows, total) = await repository.ListAsync(filter, (page - 1) * size, size, ct);
        return new PagedResult<AdminUser>(rows, total, page, size);
    }

    public async Task<AdminUserDetail?> GetAsync(int id, CancellationToken ct)
    {
        var user = await repository.GetAsync(id, ct);
        return user is null ? null : new AdminUserDetail(user, sessions.ForAccount(id, "").Count, await audit.RecentByActorAsync(id, 10, ct));
    }

    public async Task<OpResult<StaffCreated>> CreateStaffAsync(StaffForm form, int actor, CancellationToken ct)
    {
        var email = (form.Email ?? "").Trim().ToLowerInvariant();
        var name = (form.FullName ?? "").Trim();
        var phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim();
        if (email.Length is 0 or > 250 || !new EmailAddressAttribute().IsValid(email) || email.Any(character => character > 127)) return Op.Invalid("Email không đúng định dạng.");
        if (name.Length < Limits.NameMin || name.Length > Limits.NameMax) return Op.Invalid($"Họ tên từ {Limits.NameMin} đến {Limits.NameMax} ký tự.");
        if (phone is not null && (phone.Length > Limits.PhoneMax || !phone.All(character => character is >= '0' and <= '9'))) return Op.Invalid("Số điện thoại chỉ gồm chữ số.");
        var roles = Normalize(form.Roles);
        if (roles is null) return Op.Invalid("Cần ít nhất một vai trò hợp lệ.");
        if (await repository.MissingRolesAsync(roles, ct) is { Count: > 0 } absent) return Op.Conflict("Bảng VAITRO thiếu vai trò: " + string.Join(", ", absent) + "; hãy chạy lại script CSDL.");
        if (await repository.EmailExistsAsync(email, ct)) return Op.Conflict("Email đã được đăng ký.");

        var password = RandomPassword();
        var account = new Taikhoan { HoTen = name, Email = email, SoDienThoai = phone, TrangThai = AccountStatus.Active };
        account.MatKhauBam = hasher.HashPassword(account, password);
        int id;
        await using (var transaction = await repository.BeginAsync(ct))
        {
            if (await repository.AddAsync(account, roles, ct) is not { } added) return Op.Conflict("Email đã được đăng ký.");
            id = added;
            await administration.RecordAsync(new AuditEntry(actor, "Tạo tài khoản nhân viên", AccountType, id, null, null, new { Id = id, Email = email, Roles = roles }), ct);
            await transaction.CommitAsync(ct);
        }
        return Op.Ok(new StaffCreated((await repository.GetAsync(id, ct))!, password));
    }

    public async Task<OpResult<AdminUser>> SetRolesAsync(int id, IReadOnlyList<string> requested, int actor, CancellationToken ct, IReadOnlyList<string>? expectedRoles = null)
    {
        var roles = Normalize(requested);
        if (roles is null) return Op.Invalid("Cần ít nhất một vai trò hợp lệ.");
        if (await repository.MissingRolesAsync(roles, ct) is { Count: > 0 } absent) return Op.Conflict("Bảng VAITRO thiếu vai trò: " + string.Join(", ", absent) + "; hãy chạy lại script CSDL.");
        await using (var transaction = await repository.BeginAsync(ct))
        {
            if (!await repository.LockAdminChangesAsync(ct)) return Busy;
            var before = await repository.GetAsync(id, ct);
            if (before is null) return Op.NotFound("Không tìm thấy tài khoản.");
            if (expectedRoles is not null && !before.Roles.Order(StringComparer.Ordinal).SequenceEqual(expectedRoles.Order(StringComparer.Ordinal)))
                return Op.Conflict("Vai trò đã thay đổi; hãy tải lại.");
            if (before.Roles.Contains(AppRoles.Admin) && !roles.Contains(AppRoles.Admin))
            {
                if (actor == id) return Op.Conflict("Bạn không thể tự gỡ vai trò Admin của chính mình.");
                if (AdminSafety.IsLastActiveAdmin(before.Status, before.Roles, await repository.CountAdminsAsync(ct))) return Op.Conflict("Không thể gỡ Admin cuối cùng của hệ thống.");
            }
            await repository.ReplaceRolesAsync(id, roles, ct);
            await administration.RecordAsync(new AuditEntry(actor, "Đổi vai trò", AccountType, id, null, new { before.Roles }, new { Roles = roles }), ct);
            await transaction.CommitAsync(ct);
        }
        sessions.RemoveAllForAccount(id);
        return Op.Ok((await repository.GetAsync(id, ct))!);
    }

    public async Task<OpResult<AdminUser>> SetStatusAsync(int id, string status, string? reason, int actor, CancellationToken ct, string? expectedStatus = null)
    {
        if (!AccountStatus.All.Contains(status)) return Op.Invalid("Trạng thái chỉ nhận Active hoặc Locked.");
        await using (var transaction = await repository.BeginAsync(ct))
        {
            if (!await repository.LockAdminChangesAsync(ct)) return Busy;
            var before = await repository.GetAsync(id, ct);
            if (before is null) return Op.NotFound("Không tìm thấy tài khoản.");
            if (expectedStatus is not null && before.Status != expectedStatus) return Op.Conflict("Trạng thái đã thay đổi; hãy tải lại.");
            if (status == AccountStatus.Locked && actor == id) return Op.Conflict("Bạn không thể tự khóa tài khoản của chính mình.");
            if (before.Status == status) return Op.Ok(before);
            if (status == AccountStatus.Locked && before.Roles.Contains(AppRoles.Admin) && AdminSafety.IsLastActiveAdmin(before.Status, before.Roles, await repository.CountAdminsAsync(ct)))
                return Op.Conflict("Không thể khóa Admin cuối cùng của hệ thống.");
            await repository.SetStatusAsync(id, status, ct);
            await administration.RecordAsync(new AuditEntry(actor, status == AccountStatus.Locked ? "Khóa tài khoản" : "Mở khóa tài khoản", AccountType, id,
                string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(), new { before.Status }, new { Status = status }), ct);
            await transaction.CommitAsync(ct);
        }
        if (status == AccountStatus.Locked) sessions.RemoveAllForAccount(id);
        return Op.Ok((await repository.GetAsync(id, ct))!);
    }

    public async Task<OpResult<TempPassword>> ResetPasswordAsync(int id, int actor, CancellationToken ct)
    {
        var account = await repository.AccountAsync(id, ct);
        if (account is null) return Op.NotFound("Không tìm thấy tài khoản.");
        var password = RandomPassword();
        await using (var transaction = await repository.BeginAsync(ct))
        {
            await repository.SetPasswordHashAsync(id, hasher.HashPassword(account, password), ct);
            await administration.RecordAsync(new AuditEntry(actor, "Đặt lại mật khẩu", AccountType, id), ct); // không ghi mật khẩu
            await transaction.CommitAsync(ct);
        }
        sessions.RemoveAllForAccount(id);
        return Op.Ok(new TempPassword(id, password));
    }

    private static List<string>? Normalize(IReadOnlyList<string>? roles)
    {
        var distinct = (roles ?? []).Select(role => role?.Trim() ?? "").Distinct(StringComparer.Ordinal).ToList();
        return distinct.Count == 0 || distinct.Any(role => !KnownRoles.Contains(role)) ? null : distinct;
    }

    /// <summary>12 ký tự ngẫu nhiên, đủ chữ hoa, chữ thường, chữ số và ký hiệu.</summary>
    private static string RandomPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ", lower = "abcdefghijkmnpqrstuvwxyz", digits = "23456789", symbols = "!@#$%*?";
        var all = upper + lower + digits + symbols;
        var chars = new List<char> { Pick(upper), Pick(lower), Pick(digits), Pick(symbols) };
        while (chars.Count < 12) chars.Add(Pick(all));
        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());

        static char Pick(string set) => set[RandomNumberGenerator.GetInt32(set.Length)];
    }
}
