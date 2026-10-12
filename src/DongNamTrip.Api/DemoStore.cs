using DongNamTrip.Contracts;

namespace DongNamTrip.Api;
// Thay lớp này bằng repository SQL Server khi thống nhất ERD. Không dùng cho production.
public sealed class DemoStore
{
    private readonly object gate = new();
    private readonly List<Destination> destinations = [new(1, "Bưu điện Trung tâm Sài Gòn", "TP. Hồ Chí Minh", "Văn hóa", "Khu vực trung tâm TP. Hồ Chí Minh", "Một điểm dừng để khám phá kiến trúc, gửi một tấm bưu thiếp và bắt đầu hành trình qua những góc phố Sài Gòn.", 60, "city"), new(2, "Núi Bà Đen", "Tây Ninh", "Thiên nhiên", "Khu du lịch Núi Bà Đen, Tây Ninh", "Dành thời gian ngắm cảnh núi non và khám phá không gian văn hóa. Hoạt động cụ thể sẽ được nhân viên xác minh khi lập tour.", 180, "mountain"), new(3, "Mũi Nghinh Phong", "Vũng Tàu", "Biển", "Khu vực Mũi Nghinh Phong, Vũng Tàu", "Một khoảng trời thoáng rộng giữa núi và biển, phù hợp cho một buổi dạo chơi và lưu lại những bức ảnh của chuyến đi.", 90, "coast"), new(4, "Vườn quốc gia Cát Tiên", "Đồng Nai", "Thiên nhiên", "Khu vực Nam Cát Tiên, Đồng Nai", "Khám phá không gian rừng xanh và tìm hiểu thiên nhiên. Tuyến tham quan và dịch vụ cần được xác minh trước chuyến đi.", 240, "forest"), new(5, "Làng sơn mài Tương Bình Hiệp", "Bình Dương", "Văn hóa", "Khu vực Tương Bình Hiệp, Bình Dương", "Tìm hiểu những lớp màu và bàn tay người thợ trong nghề sơn mài. Lịch đón khách cần được xác nhận với cơ sở.", 90, "village"), new(6, "Chợ Bến Thành", "TP. Hồ Chí Minh", "Ẩm thực", "Khu vực chợ Bến Thành, TP. Hồ Chí Minh", "Dạo quanh khu chợ quen thuộc và tìm hiểu hương vị địa phương. Chi phí ăn uống tùy vào lựa chọn thực tế.", 90, "city")];
    private readonly List<Tour> tours = [new(1, "Sài Gòn: những góc phố thân quen", "TP. Hồ Chí Minh", "Kết hợp", "1 ngày", 690000, "Kiến trúc, câu chuyện phố thị và hương vị địa phương trong một ngày đi thật chậm.", "city", [1, 6]), new(2, "Tây Ninh: hẹn nhau trên tầng mây", "Tây Ninh", "Thiên nhiên", "1 ngày", 890000, "Rời phố một ngày để tìm về không gian xanh dưới chân núi Bà Đen.", "mountain", [2]), new(3, "Vũng Tàu: một ngày cùng biển", "Vũng Tàu", "Biển", "1 ngày", 790000, "Hít thở gió biển, dạo Mũi Nghinh Phong và tận hưởng một ngày nghỉ thảnh thơi.", "coast", [3]), new(4, "Cát Tiên: đi giữa màu xanh", "Đồng Nai", "Thiên nhiên", "2 ngày 1 đêm", 1690000, "Một hành trình chậm giữa rừng xanh, dành thời gian lắng nghe và tìm hiểu thiên nhiên.", "forest", [4]), new(5, "Bình Dương: sắc màu làng nghề", "Bình Dương", "Văn hóa", "1 ngày", 590000, "Gặp những câu chuyện thủ công qua sắc màu sơn mài Tương Bình Hiệp.", "village", [5])];
    private readonly List<Itinerary> itineraries = [];
    public List<Destination> Destinations()
    {
        lock (gate)
            return[..destinations];
    }

    public List<Tour> Tours()
    {
        lock (gate)
            return tours.Select(t => t with { DestinationIds = [..t.DestinationIds] }).ToList();
    }

    public Destination? Save(int? id, DestinationDraft d)
    {
        lock (gate)
        {
            var index = id is null ? -1 : destinations.FindIndex(x => x.Id == id);
            if (id is not null && index < 0)
                return null;
            var item = new Destination(id ?? destinations.Max(x => x.Id) + 1, d.Name.Trim(), d.Region, d.Category, d.Address.Trim(), d.Description.Trim(), d.VisitMinutes, CatalogOptions.IllustrationFor(d.Region));
            if (index < 0)
                destinations.Add(item);
            else
                destinations[index] = item;
            return item;
        }
    }

    public List<Itinerary> Itineraries(int userId)
    {
        lock (gate)
            return itineraries.Where(x => x.UserId == userId).Select(x => x with { DestinationIds = [..x.DestinationIds] }).ToList();
    }

    public Itinerary? SaveItinerary(int userId, ItineraryDraft d)
    {
        lock (gate)
        {
            if (d.DestinationIds.Any(id => destinations.All(x => x.Id != id)))
                return null;
            var item = new Itinerary(itineraries.Count + 1, userId, d.Name.Trim(), d.StartDate, d.Guests, [..d.DestinationIds]);
            itineraries.Add(item);
            return item;
        }
    }
}
