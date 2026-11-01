using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using DongNamTrip.Api.Data;
using DongNamTrip.Api.Data.Entities;
using DongNamTrip.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.Options;

namespace DongNamTrip.Api;

public sealed class SqlAuthenticationOptions
{
    public const string SectionName = "Authentication";
    public int SessionLifetimeSeconds { get; init; } = 14_400;
}

public sealed class SqlSessionStore(IOptions<SqlAuthenticationOptions> options)
{
    private readonly ConcurrentDictionary<string, SessionRecord> sessions = new();
    private readonly TimeSpan lifetime = GetLifetime(options.Value.SessionLifetimeSeconds);

    public string Create(int accountId)
    {
        RemoveExpired();
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        sessions[token] = new SessionRecord(accountId, Convert.ToHexString(RandomNumberGenerator.GetBytes(12)), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.Add(lifetime));
        return token;
    }

    public bool TryGetAccountId(string token, out int accountId)
    {
        accountId = default;
        if (!sessions.TryGetValue(token, out var session))
            return false;
        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            sessions.TryRemove(token, out _);
            return false;
        }

        accountId = session.AccountId;
        return true;
    }

    public void Remove(string token) => sessions.TryRemove(token, out _);

    public IReadOnlyList<AccountSession> ForAccount(int accountId, string currentToken)
    {
        RemoveExpired();
        return sessions.Where(pair => pair.Value.AccountId == accountId)
            .Select(pair => new AccountSession(pair.Value.Id, pair.Value.CreatedAt, pair.Value.ExpiresAt, pair.Key == currentToken))
            .OrderByDescending(item => item.CreatedAt).ToArray();
    }

    public bool RemoveForAccount(int accountId, string id)
    {
        foreach (var pair in sessions)
            if (pair.Value.AccountId == accountId && pair.Value.Id == id)
                return sessions.TryRemove(pair.Key, out _);
        return false;
    }

    public void RemoveAllForAccount(int accountId)
    {
        foreach (var pair in sessions)
            if (pair.Value.AccountId == accountId)
                sessions.TryRemove(pair.Key, out _);
    }

    private void RemoveExpired()
    {
        foreach (var session in sessions)
            if (session.Value.ExpiresAt <= DateTimeOffset.UtcNow)
                sessions.TryRemove(session.Key, out _);
    }

    private static TimeSpan GetLifetime(int seconds) => TimeSpan.FromSeconds(seconds is >= 1 and <= 86_400 ? seconds : 14_400);

    private sealed record SessionRecord(int AccountId, string Id, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
}

public sealed class SqlAuthService(
    DongNamTripDbContext db,
    IPasswordHasher<Taikhoan> passwordHasher,
    SqlSessionStore sessions)
{
    public async Task<PasswordChangeStatus> ChangePasswordAsync(int accountId, ChangePasswordRequest input, CancellationToken cancellationToken)
    {
        if (!Validator.TryValidateObject(input, new ValidationContext(input), [], true))
            return PasswordChangeStatus.Invalid;
        var account = await db.Taikhoan.SingleOrDefaultAsync(item => item.MaTk == accountId, cancellationToken);
        if (account is null || !IsActive(account))
            return PasswordChangeStatus.Unavailable;
        if (passwordHasher.VerifyHashedPassword(account, account.MatKhauBam, input.CurrentPassword) == PasswordVerificationResult.Failed)
            return PasswordChangeStatus.IncorrectCurrent;
        account.MatKhauBam = passwordHasher.HashPassword(account, input.NewPassword);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        var preference = await db.AccountPreferences.AsNoTracking().SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (preference?.SecurityNotices != false)
        {
            db.AccountNotices.Add(new AccountNotice { AccountId = accountId, Title = "Mật khẩu đã được đổi", Body = "Mật khẩu tài khoản vừa được thay đổi. Tất cả phiên đăng nhập đã kết thúc.", CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        sessions.RemoveAllForAccount(accountId);
        return PasswordChangeStatus.Changed;
    }
    public async Task<LoginResponse?> LoginAsync(LoginRequest input, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(input.Email);
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(input.Password))
            return null;

        var account = await LoadAccountByEmailAsync(email, cancellationToken);
        if (account is null || !IsActive(account))
            return null;

        var verification = passwordHasher.VerifyHashedPassword(account, account.MatKhauBam, input.Password);
        if (verification == PasswordVerificationResult.Failed)
            return null;

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.MatKhauBam = passwordHasher.HashPassword(account, input.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        return new LoginResponse(sessions.Create(account.MaTk), ToUser(account));
    }

    public async Task<RegistrationResult> RegisterAsync(RegisterRequest input, CancellationToken cancellationToken)
    {
        if (!IsValidRegistration(input, out var name, out var email, out var phoneNumber))
            return RegistrationResult.Invalid;

        if (await db.Taikhoan.AsNoTracking().AnyAsync(account => account.Email == email, cancellationToken))
            return RegistrationResult.Duplicate;

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var customerRole = await db.Vaitro.SingleOrDefaultAsync(role => role.TenVaiTro == AppRoles.Customer, cancellationToken);
            if (customerRole is null)
                return RegistrationResult.ConfigurationError;

            var account = new Taikhoan
            {
                HoTen = name,
                Email = email,
                SoDienThoai = phoneNumber,
                TrangThai = "Active"
            };
            account.MatKhauBam = passwordHasher.HashPassword(account, input.Password);

            db.Taikhoan.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            db.TaikhoanVaitro.Add(new TaikhoanVaitro { MaTk = account.MaTk, MaVaiTro = customerRole.MaVaiTro });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var user = new AuthenticatedUser(account.MaTk, account.HoTen, account.Email, [customerRole.TenVaiTro]);
            return RegistrationResult.Created(new LoginResponse(sessions.Create(account.MaTk), user));
        }
        catch (DbUpdateException exception) when (IsUniqueEmailViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return RegistrationResult.Duplicate;
        }
    }

    public async Task<AuthenticatedUser?> GetActiveUserAsync(int accountId, CancellationToken cancellationToken)
    {
        var account = await LoadAccountByIdAsync(accountId, cancellationToken);
        return account is not null && IsActive(account) ? ToUser(account) : null;
    }

    public async Task<AccountProfile?> GetProfileAsync(int accountId, CancellationToken cancellationToken)
    {
        var account = await db.Taikhoan.AsNoTracking().SingleOrDefaultAsync(account => account.MaTk == accountId, cancellationToken);
        return account is not null && IsActive(account) ? ToProfile(account) : null;
    }

    public async Task<ProfileUpdateResult> UpdateProfileAsync(int accountId, UpdateProfileRequest input, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var account = await db.Taikhoan.SingleOrDefaultAsync(item => item.MaTk == accountId, cancellationToken);
            if (account is null || !IsActive(account))
                return ProfileUpdateResult.Unavailable;

            input.Normalize();
            if (input.ValidateForExistingPhone(account.SoDienThoai).Count > 0)
                return ProfileUpdateResult.Invalid;
            var name = input.Name;
            var email = input.Email;
            var phoneNumber = input.PhoneNumber;

            if (!string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase)
                && await db.Taikhoan.AsNoTracking().AnyAsync(item => item.Email == email, cancellationToken))
                return ProfileUpdateResult.Duplicate;

            account.HoTen = name;
            account.Email = email;
            account.SoDienThoai = string.Equals(phoneNumber, account.SoDienThoai?.Trim(), StringComparison.Ordinal)
                ? account.SoDienThoai : phoneNumber;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProfileUpdateResult.Updated(ToProfile(account));
        }
        catch (DbUpdateException exception) when (IsUniqueEmailViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProfileUpdateResult.Duplicate;
        }
    }

    public static string NormalizeEmail(string? email) => email?.Trim().ToLowerInvariant() ?? "";

    private async Task<Taikhoan?> LoadAccountByEmailAsync(string email, CancellationToken cancellationToken) =>
        await Accounts().SingleOrDefaultAsync(account => account.Email == email, cancellationToken);

    private async Task<Taikhoan?> LoadAccountByIdAsync(int accountId, CancellationToken cancellationToken) =>
        await Accounts().SingleOrDefaultAsync(account => account.MaTk == accountId, cancellationToken);

    private IQueryable<Taikhoan> Accounts() => db.Taikhoan
        .Include(account => account.TaikhoanVaitro)
        .ThenInclude(assignment => assignment.MaVaiTroNavigation);

    private static bool IsActive(Taikhoan account) => string.Equals(account.TrangThai, "Active", StringComparison.OrdinalIgnoreCase);

    private static AuthenticatedUser ToUser(Taikhoan account) => new(
        account.MaTk,
        account.HoTen,
        account.Email,
        account.TaikhoanVaitro
            .Select(assignment => assignment.MaVaiTroNavigation.TenVaiTro)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray());

    private static AccountProfile ToProfile(Taikhoan account) => new(account.MaTk, account.HoTen, account.Email, account.SoDienThoai);

    private static bool IsValidRegistration(RegisterRequest input, out string name, out string email, out string? phoneNumber)
    {
        name = input.Name?.Trim() ?? "";
        email = NormalizeEmail(input.Email);
        phoneNumber = string.IsNullOrWhiteSpace(input.PhoneNumber) ? null : input.PhoneNumber.Trim();
        var password = input.Password ?? "";
        var confirmation = input.ConfirmPassword ?? "";
        return name.Length is >= 1 and <= 100
            && email.Length is > 0 and <= 254
            && email.All(character => character <= 127)
            && new EmailAddressAttribute().IsValid(email)
            && (phoneNumber is null || phoneNumber.Length <= 15 && phoneNumber.All(character => character is >= '0' and <= '9'))
            && password.Length is >= 8 and <= 128
            && password.Any(char.IsLetter)
            && password.Any(char.IsDigit)
            && password == confirmation
            && input.TermsAccepted;
    }

    private static bool IsUniqueEmailViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException sqlException && sqlException.Number is 2601 or 2627;
}

public sealed record ProfileUpdateResult(AccountProfile? Profile, ProfileUpdateStatus Status)
{
    public static ProfileUpdateResult Updated(AccountProfile profile) => new(profile, ProfileUpdateStatus.Updated);
    public static ProfileUpdateResult Duplicate { get; } = new(null, ProfileUpdateStatus.Duplicate);
    public static ProfileUpdateResult Invalid { get; } = new(null, ProfileUpdateStatus.Invalid);
    public static ProfileUpdateResult Unavailable { get; } = new(null, ProfileUpdateStatus.Unavailable);
}

public enum ProfileUpdateStatus
{
    Updated,
    Duplicate,
    Invalid,
    Unavailable
}

public enum PasswordChangeStatus { Changed, Invalid, IncorrectCurrent, Unavailable }

public sealed record RegistrationResult(LoginResponse? Session, RegistrationStatus Status)
{
    public static RegistrationResult Created(LoginResponse session) => new(session, RegistrationStatus.Created);
    public static RegistrationResult Duplicate { get; } = new(null, RegistrationStatus.Duplicate);
    public static RegistrationResult Invalid { get; } = new(null, RegistrationStatus.Invalid);
    public static RegistrationResult ConfigurationError { get; } = new(null, RegistrationStatus.ConfigurationError);
}

public enum RegistrationStatus
{
    Created,
    Duplicate,
    Invalid,
    ConfigurationError
}

public sealed class SqlAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    SqlSessionStore sessions,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header[7..].Trim();
        if (string.IsNullOrWhiteSpace(token) || !sessions.TryGetAccountId(token, out var accountId))
            return AuthenticateResult.Fail("Invalid or expired session.");

        using var scope = scopeFactory.CreateScope();
        var authentication = scope.ServiceProvider.GetRequiredService<SqlAuthService>();
        var user = await authentication.GetActiveUserAsync(accountId, Context.RequestAborted);
        if (user is null)
        {
            sessions.Remove(token);
            return AuthenticateResult.Fail("Account is unavailable.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email)
        };
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}

public sealed class DevelopmentAdminBootstrapper(
    IServiceScopeFactory scopeFactory,
    IPasswordHasher<Taikhoan> passwordHasher,
    IConfiguration configuration,
    ILogger<DevelopmentAdminBootstrapper> logger)
{
    public async Task EnsureCreatedAsync(IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("BootstrapAdmin:Enabled"))
            return;

        var email = SqlAuthService.NormalizeEmail(configuration["BootstrapAdmin:Email"]);
        var password = configuration["BootstrapAdmin:Password"];
        var name = configuration["BootstrapAdmin:Name"]?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 8 || !new EmailAddressAttribute().IsValid(email))
        {
            logger.LogWarning("Development admin bootstrap is enabled but its required settings are incomplete.");
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DongNamTripDbContext>();
        if (await db.Taikhoan.AsNoTracking().AnyAsync(account => account.Email == email, cancellationToken))
            return;

        var adminRole = await db.Vaitro.SingleOrDefaultAsync(role => role.TenVaiTro == AppRoles.Admin, cancellationToken);
        if (adminRole is null)
        {
            logger.LogWarning("Development admin bootstrap could not find the Admin role.");
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var account = new Taikhoan
            {
                HoTen = string.IsNullOrWhiteSpace(name) ? "Development administrator" : name,
                Email = email,
                TrangThai = "Active"
            };
            account.MatKhauBam = passwordHasher.HashPassword(account, password);
            db.Taikhoan.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            db.TaikhoanVaitro.Add(new TaikhoanVaitro { MaTk = account.MaTk, MaVaiTro = adminRole.MaVaiTro });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Created the configured development admin account.");
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(cancellationToken);
        }
    }
}

public sealed class DevelopmentTestAccountsBootstrapper(
    IServiceScopeFactory scopeFactory,
    IPasswordHasher<Taikhoan> passwordHasher,
    IConfiguration configuration,
    ILogger<DevelopmentTestAccountsBootstrapper> logger)
{
    private const string SectionName = "DevelopmentTestAccounts";

    private static readonly TestAccount[] Accounts =
    [
        new("admin", "admin@dongnamtrip.test", "Test Admin", AppRoles.Admin),
        new("operator", "operator@dongnamtrip.test", "Test Operator", AppRoles.Operator),
        new("editor", "editor@dongnamtrip.test", "Test Editor", AppRoles.Editor),
        new("finance", "finance@dongnamtrip.test", "Test Finance", AppRoles.Finance),
        new("publisher", "publisher@dongnamtrip.test", "Test Publisher", AppRoles.Publisher),
        new("customer1", "customer1@dongnamtrip.test", "Test Customer 1", AppRoles.Customer),
        new("customer2", "customer2@dongnamtrip.test", "Test Customer 2", AppRoles.Customer)
    ];

    public async Task EnsureCreatedAsync(IHostEnvironment environment, CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>($"{SectionName}:Enabled"))
            return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DongNamTripDbContext>();
        var emails = Accounts.Select(account => account.Email).ToArray();
        var existingEmails = (await db.Taikhoan.AsNoTracking()
                .Where(account => emails.Contains(account.Email))
                .Select(account => account.Email)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var account in Accounts.Where(account => existingEmails.Contains(account.Email)))
            logger.LogWarning("Development test account {Email} already exists and was left unchanged.", account.Email);

        var accountsToCreate = Accounts.Where(account => !existingEmails.Contains(account.Email)).ToArray();
        if (accountsToCreate.Length == 0)
            return;

        var passwords = ResolvePasswords(accountsToCreate);
        if (passwords is null)
            return;

        var roles = await db.Vaitro.AsNoTracking()
            .Where(role => Accounts.Select(account => account.Role).Contains(role.TenVaiTro))
            .ToDictionaryAsync(role => role.TenVaiTro, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var missingRoles = Accounts.Select(account => account.Role).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(role => !roles.ContainsKey(role)).ToArray();
        if (missingRoles.Length != 0)
        {
            logger.LogWarning("Development test-account seed could not find the required role records.");
            return;
        }

        foreach (var account in accountsToCreate)
            await CreateIfMissingAsync(db, account, roles[account.Role], passwords[account.Key], cancellationToken);
    }

    private Dictionary<string, string>? ResolvePasswords(IEnumerable<TestAccount> accountsToCreate)
    {
        var passwords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var generated = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var account in accountsToCreate)
        {
            var key = PasswordKey(account);
            var password = configuration[key];
            if (string.IsNullOrWhiteSpace(password))
            {
                password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
                generated[key] = password;
            }

            passwords[account.Key] = password;
        }

        if (generated.Count == 0)
            return passwords;

        if (!TryPersistUserSecrets(generated))
        {
            logger.LogWarning("Development test-account seed did not create accounts because generated passwords could not be saved to User Secrets.");
            return null;
        }

        logger.LogInformation("Generated and stored passwords for new development test accounts in User Secrets.");
        return passwords;
    }

    private async Task CreateIfMissingAsync(
        DongNamTripDbContext db,
        TestAccount specification,
        Vaitro role,
        string password,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            if (await db.Taikhoan.AsNoTracking().AnyAsync(account => account.Email == specification.Email, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogWarning("Development test account {Email} already exists and was left unchanged.", specification.Email);
                return;
            }

            var account = new Taikhoan
            {
                HoTen = specification.Name,
                Email = specification.Email,
                TrangThai = "Active"
            };
            account.MatKhauBam = passwordHasher.HashPassword(account, password);
            db.Taikhoan.Add(account);
            await db.SaveChangesAsync(cancellationToken);
            db.TaikhoanVaitro.Add(new TaikhoanVaitro { MaTk = account.MaTk, MaVaiTro = role.MaVaiTro });
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Created configured development test account {Email}.", specification.Email);
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogWarning("Development test account {Email} was created concurrently and was left unchanged.", specification.Email);
        }
    }

    private bool TryPersistUserSecrets(IReadOnlyDictionary<string, string> values)
    {
        try
        {
            var userSecretsId = Assembly.GetEntryAssembly()?.GetCustomAttribute<UserSecretsIdAttribute>()?.UserSecretsId;
            if (string.IsNullOrWhiteSpace(userSecretsId))
                return false;

            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft",
                "UserSecrets",
                userSecretsId);
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "secrets.json");
            JsonObject root;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                root = string.IsNullOrWhiteSpace(json) ? new JsonObject() : JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            foreach (var (key, value) in values)
                SetJsonValue(root, key.Split(':'), value);

            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, path, true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            logger.LogWarning(exception, "Could not persist generated development test-account passwords to User Secrets.");
            return false;
        }
    }

    private static void SetJsonValue(JsonObject root, IReadOnlyList<string> segments, string value)
    {
        var current = root;
        for (var index = 0; index < segments.Count - 1; index++)
        {
            if (current[segments[index]] is not JsonObject next)
            {
                next = new JsonObject();
                current[segments[index]] = next;
            }

            current = next;
        }

        current[segments[^1]] = value;
    }

    private static string PasswordKey(TestAccount account) => $"{SectionName}:Passwords:{account.Key}";

    private sealed record TestAccount(string Key, string Email, string Name, string Role);
}
