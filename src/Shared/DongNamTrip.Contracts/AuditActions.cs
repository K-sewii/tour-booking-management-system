namespace DongNamTrip.Contracts;

public static class AuditActions
{
    public const string AudioFailed = "Tạo audio lỗi";
    public const string AudioCreated = "Tạo audio";
    public const string AudioPublished = "Công bố audio";
    public const string AudioUnpublished = "Ngừng công bố audio";
    public const string NarrationCreated = "Tạo thuyết minh";
    public const string NarrationEdited = "Sửa thuyết minh";
    public const string NarrationVersionCreated = "Tạo phiên bản thuyết minh";
    public const string TranslationAdded = "Thêm bản dịch";
    public const string TranslationEdited = "Sửa bản dịch";
    public const string ListenRightGranted = "Cấp quyền nghe";
    public const string ListenRightRevoked = "Thu hồi quyền nghe";
    public const string QuoteAccepted = "Khách chấp nhận báo giá";
    public const string QuoteChangeRequested = "Khách đề nghị đổi báo giá";
}
