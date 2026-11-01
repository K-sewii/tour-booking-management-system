-- Dữ liệu mẫu cho danh mục (6 điểm đến, 5 tour). Chỉ để chạy thử; nhóm sẽ thay bằng dữ liệu thật.
-- Idempotent: bỏ qua dòng đã có (so theo tên). Trạng thái hiển thị: Published (Draft / Published / Inactive).
SET NOCOUNT ON;

DECLARE @places TABLE (Seq INT IDENTITY(1,1), Ten NVARCHAR(120), KhuVuc NVARCHAR(50), LoaiHinh NVARCHAR(100), DiaChi NVARCHAR(250), MoTa NVARCHAR(3000), Phut INT);
INSERT INTO @places (Ten, KhuVuc, LoaiHinh, DiaChi, MoTa, Phut) VALUES
(N'Bưu điện Trung tâm Sài Gòn', N'TP. Hồ Chí Minh', N'Văn hóa', N'Khu vực trung tâm TP. Hồ Chí Minh', N'Một điểm dừng để khám phá kiến trúc, gửi một tấm bưu thiếp và bắt đầu hành trình qua những góc phố Sài Gòn.', 60),
(N'Núi Bà Đen', N'Tây Ninh', N'Thiên nhiên', N'Khu du lịch Núi Bà Đen, Tây Ninh', N'Dành thời gian ngắm cảnh núi non và khám phá không gian văn hóa. Hoạt động cụ thể sẽ được nhân viên xác minh khi lập tour.', 180),
(N'Mũi Nghinh Phong', N'Vũng Tàu', N'Biển', N'Khu vực Mũi Nghinh Phong, Vũng Tàu', N'Một khoảng trời thoáng rộng giữa núi và biển, phù hợp cho một buổi dạo chơi và lưu lại những bức ảnh của chuyến đi.', 90),
(N'Vườn quốc gia Cát Tiên', N'Đồng Nai', N'Thiên nhiên', N'Khu vực Nam Cát Tiên, Đồng Nai', N'Khám phá không gian rừng xanh và tìm hiểu thiên nhiên. Tuyến tham quan và dịch vụ cần được xác minh trước chuyến đi.', 240),
(N'Làng sơn mài Tương Bình Hiệp', N'Bình Dương', N'Văn hóa', N'Khu vực Tương Bình Hiệp, Bình Dương', N'Tìm hiểu những lớp màu và bàn tay người thợ trong nghề sơn mài. Lịch đón khách cần được xác nhận với cơ sở.', 90),
(N'Chợ Bến Thành', N'TP. Hồ Chí Minh', N'Ẩm thực', N'Khu vực chợ Bến Thành, TP. Hồ Chí Minh', N'Dạo quanh khu chợ quen thuộc và tìm hiểu hương vị địa phương. Chi phí ăn uống tùy vào lựa chọn thực tế.', 90);

INSERT INTO DIADIEM (TenDD, KhuVuc, LoaiHinh, DiaChi, MoTa, ThoiLuongPhut, TrangThai)
SELECT p.Ten, p.KhuVuc, p.LoaiHinh, p.DiaChi, p.MoTa, p.Phut, 'Published'
FROM @places p WHERE NOT EXISTS (SELECT 1 FROM DIADIEM d WHERE d.TenDD = p.Ten)
ORDER BY p.Seq; -- ORDER BY: id tăng theo thứ tự khai báo

DECLARE @tours TABLE (Seq INT IDENTITY(1,1), Ten NVARCHAR(200), KhuVuc NVARCHAR(50), LoaiHinh NVARCHAR(100), ThoiLuong NVARCHAR(50), Gia DECIMAL(18,0), TomTat NVARCHAR(1000));
INSERT INTO @tours (Ten, KhuVuc, LoaiHinh, ThoiLuong, Gia, TomTat) VALUES
(N'Sài Gòn: những góc phố thân quen', N'TP. Hồ Chí Minh', N'Kết hợp', N'1 ngày', 690000, N'Kiến trúc, câu chuyện phố thị và hương vị địa phương trong một ngày đi thật chậm.'),
(N'Tây Ninh: hẹn nhau trên tầng mây', N'Tây Ninh', N'Thiên nhiên', N'1 ngày', 890000, N'Rời phố một ngày để tìm về không gian xanh dưới chân núi Bà Đen.'),
(N'Vũng Tàu: một ngày cùng biển', N'Vũng Tàu', N'Biển', N'1 ngày', 790000, N'Hít thở gió biển, dạo Mũi Nghinh Phong và tận hưởng một ngày nghỉ thảnh thơi.'),
(N'Cát Tiên: đi giữa màu xanh', N'Đồng Nai', N'Thiên nhiên', N'2 ngày 1 đêm', 1690000, N'Một hành trình chậm giữa rừng xanh, dành thời gian lắng nghe và tìm hiểu thiên nhiên.'),
(N'Bình Dương: sắc màu làng nghề', N'Bình Dương', N'Văn hóa', N'1 ngày', 590000, N'Gặp những câu chuyện thủ công qua sắc màu sơn mài Tương Bình Hiệp.');

INSERT INTO TOUR (TenTour, KhuVuc, LoaiHinh, ThoiLuong, GiaThamKhao, TomTat, TrangThai)
SELECT t.Ten, t.KhuVuc, t.LoaiHinh, t.ThoiLuong, t.Gia, t.TomTat, 'Published'
FROM @tours t WHERE NOT EXISTS (SELECT 1 FROM TOUR x WHERE x.TenTour = t.Ten)
ORDER BY t.Seq;

DECLARE @links TABLE (Tour NVARCHAR(200), Place NVARCHAR(120), ThuTu INT);
INSERT INTO @links VALUES
(N'Sài Gòn: những góc phố thân quen', N'Bưu điện Trung tâm Sài Gòn', 1),
(N'Sài Gòn: những góc phố thân quen', N'Chợ Bến Thành', 2),
(N'Tây Ninh: hẹn nhau trên tầng mây', N'Núi Bà Đen', 1),
(N'Vũng Tàu: một ngày cùng biển', N'Mũi Nghinh Phong', 1),
(N'Cát Tiên: đi giữa màu xanh', N'Vườn quốc gia Cát Tiên', 1),
(N'Bình Dương: sắc màu làng nghề', N'Làng sơn mài Tương Bình Hiệp', 1);

INSERT INTO TOUR_DIADIEM (MaTour, MaDD, NgayThu, ThuTu)
SELECT t.MaTour, d.MaDD, 1, l.ThuTu
FROM @links l
JOIN TOUR t ON t.TenTour = l.Tour
JOIN DIADIEM d ON d.TenDD = l.Place
WHERE NOT EXISTS (SELECT 1 FROM TOUR_DIADIEM x WHERE x.MaTour = t.MaTour AND x.MaDD = d.MaDD);
