# Ảnh địa danh DongNamTrip

Cập nhật ngày 19/11/2026. Ảnh được đóng gói tại `src/Frontend/DongNamTrip.Web/wwwroot/images/destinations/`, không bị `.gitignore` loại bỏ và được Web SDK đưa vào bản publish/Docker. Cần commit thư mục ảnh này cùng mã nguồn. Không phụ thuộc folder ảnh đầu vào hay máy chủ ảnh bên ngoài khi chạy.

## Ghép ảnh với dữ liệu

`src/Shared/DongNamTrip.Contracts/CatalogPhotos.cs` ghép ảnh theo **tên địa danh + khu vực**, không theo ID database. Tên được chuẩn hóa hoa/thường, dấu và dấu câu. Địa danh mới chưa có ảnh dùng SVG dự phòng theo khu vực.

Các DTO địa điểm, chi tiết, quản trị, tour và khu vực có thêm `imageUrl`; trường `illustration` cũ được giữ nguyên để tương thích. Ảnh tour lấy từ địa điểm đã công bố đầu tiên có ảnh trong lịch trình tour, theo ngày/thứ tự; nếu chưa có thì dùng ảnh đại diện khu vực. Không thay đổi schema, bản ghi hay trạng thái công bố trong database.

SPA và MVC sử dụng ảnh ở danh sách/chi tiết địa điểm, danh sách/chi tiết tour, khu vực trang chủ và danh sách lập lịch trình. Ảnh nền trang chủ dùng Bưu điện Trung tâm Sài Gòn.

## Hai ảnh đầu vào được thay thế

- `bd02-chua-ba-thien-hau.webp`: hình đầu vào không khớp cổng chùa Bà tại số 4 Nguyễn Du, Thủ Dầu Một. Thay bằng `bd02-chua-ba-thien-hau.jpg` từ [Trung tâm Xúc tiến du lịch Bình Dương](https://dulichbinhduong.org.vn/du-lich/chua-ba-thien-hau/ct), ảnh “Cổng chùa Bà”. [Tệp nguồn](https://dulichbinhduong.org.vn/storage/travels/June2020/c%E1%BB%95ng%20ch%C3%B9a%20b%C3%A0.jpg).
- `vt08-cho-ba-ria.webp`: ảnh ghép có cả biển “Chợ Vũng Tàu”. Thay bằng `vt08-cho-ba-ria.jpg`, ảnh toàn cảnh Trung tâm thương mại Bà Rịa của **Nguyễn Văn Khởi**, bài [Những phiên chợ xuyên Tết ở phía Đông Nam TP.HCM — Pháp Luật TP.HCM/Tuổi Trẻ](https://tuoitre.vn/plo/nhung-phien-cho-xuyen-tet-o-phia-dong-nam-tphcm-109900705.htm). [Tệp nguồn](https://cdn2.tuoitre.vn/plo/image/Uploaded/hoanggiang/2026_03_23/cho-1-2226-633.jpg).

Hai tệp thay thế được giữ nguyên nội dung, không dùng AI dựng địa danh. 28 ảnh còn lại được sao chép nguyên tệp người dùng cung cấp; dấu nguồn/watermark có sẵn được giữ lại. Folder gốc không bị xóa hoặc sửa.

## Đối chiếu hình ảnh

Đối chiếu tên và khu vực với danh mục SQL và hình ảnh nhận diện: kiến trúc Dinh Độc Lập/Bưu điện/Chợ Bến Thành; Phật nằm Hội Khánh; cổng chùa Bà; nội thất nhà Trần Văn Hổ; tượng Phật Bà trên núi Bà Đen; hai tháp của Tòa Thánh; bảng tên Cát Tiên/Vườn Xoài; hải đăng, tượng Chúa và tháp Tam Thắng. Các ảnh cảnh quan không có bảng tên được đối chiếu cảnh quan theo nguồn du lịch, không suy ra tọa độ hay thời điểm chụp từ ảnh.

Nguồn trực tuyến đã dùng gồm [Du lịch Bình Dương](https://dulichbinhduong.org.vn/du-lich/tham-quan-nhung-ngoi-nha-co-o-binh-duong/ct), [Ban quản lý núi Bà Đen](https://khudulichnuibaden.tayninh.gov.vn/gioi-thieu-nui-ba-den), [Lò Gò – Xa Mát](https://dulich.tayninh.gov.vn/tin-tuc/vuon-quoc-gia-lo-go-xa-mat-diem-den-sinh-thai-hap-dan-cua-du-lich-tay-ninh-1619), [Hồ Dầu Tiếng](https://dulich.tayninh.gov.vn/tin-tuc/hanh-trinh-san-hoang-hon-ho-dau-tieng-ban-giao-huong-anh-sang-giua-long-bien-ho-1594), [Chùa Gò Kén](https://www.vietnamairlines.com/de/vi/plan-book/travel/travel-guide/chua-go-ken), [Cổng thông tin Đồng Nai](https://dongnai.gov.vn/vi/wcag/Chuong-trinh-Ke-hoach-phat-trien-KTXH/du-lich-dong-nai-nhung-diem-den-tranh-nong-hap-dan-2324.html), [Vietnam Tourism — Vũng Tàu](https://vietnam.travel/things-to-do/vung-tau-sightseeing-wonderland/), [TTXVN — tháp Tam Thắng](https://vietnam.vnanet.vn/vietnamese/tin-tuc/thap-tam-thang-bieu-tuong-moi-niem-tu-hao-cua-tp-ho-chi-minh-414516.html), [Giác Ngộ — Hội Khánh](https://giacngo.vn/anh-chua-hoi-khanh-di-tich-lich-su-van-hoa-cap-quoc-gia-voi-kien-truc-tieu-bieu-o-nam-bo-post75222.html), [Visit Vũng Tàu — Bình Châu](https://visitvungtau.travel/vi/suoi-khoang-nong-binh-chau-mang-den-ky-nghi-duong-tron-ven) và [Hồ Cốc](https://visitvungtau.travel/vi/bien-ho-coc-4288).

## Danh sách ảnh đóng gói

28 tệp WebP từ folder đầu vào và 2 tệp JPEG thay thế; tổng khoảng 8,24 MB.

| Khu vực | Địa danh | Tệp |
| --- | --- | --- |
| TP. Hồ Chí Minh | Dinh Độc Lập | `hcm01-dinh-doc-lap.webp` |
| TP. Hồ Chí Minh | Bảo tàng Chứng tích Chiến tranh | `hcm02-bao-tang-chung-tich-chien-tranh.webp` |
| TP. Hồ Chí Minh | Bưu điện Trung tâm Sài Gòn | `hcm03-buu-dien-trung-tam-sai-gon.webp` |
| TP. Hồ Chí Minh | Chợ Bến Thành | `hcm04-cho-ben-thanh.webp` |
| TP. Hồ Chí Minh | Địa đạo Củ Chi | `hcm05-dia-dao-cu-chi.webp` |
| Tây Ninh | Núi Bà Đen | `tn01-nui-ba-den.webp` |
| Tây Ninh | Tòa Thánh Cao Đài Tây Ninh | `tn02-toa-thanh-cao-dai.webp` |
| Tây Ninh | Hồ Dầu Tiếng | `tn03-ho-dau-tieng.webp` |
| Tây Ninh | Vườn quốc gia Lò Gò - Xa Mát | `tn04-lo-go-xa-mat.webp` |
| Tây Ninh | Chùa Gò Kén | `tn05-chua-go-ken.webp` |
| Vũng Tàu | Bãi Sau Vũng Tàu | `vt01-bai-sau-vung-tau.webp` |
| Vũng Tàu | Bạch Dinh | `vt02-bach-dinh.webp` |
| Vũng Tàu | Hải đăng Vũng Tàu | `vt03-hai-dang-vung-tau.webp` |
| Vũng Tàu | Tượng Chúa Kitô Vua Vũng Tàu | `vt04-tuong-chua-kito-vua.webp` |
| Vũng Tàu | Mũi Nghinh Phong | `vt05-mui-nghinh-phong.webp` |
| Vũng Tàu | Tháp Tam Thắng | `vt06-thap-tam-thang.webp` |
| Vũng Tàu | Chùa Đại Tòng Lâm | `vt07-chua-dai-tong-lam.webp` |
| Vũng Tàu | Chợ Bà Rịa | `vt08-cho-ba-ria.jpg` |
| Vũng Tàu | Suối khoáng nóng Bình Châu | `vt09-suoi-khoang-nong-binh-chau.webp` |
| Vũng Tàu | Biển Hồ Cốc | `vt10-bien-ho-coc.webp` |
| Đồng Nai | Khu du lịch Bửu Long | `dn01-khu-du-lich-buu-long.webp` |
| Đồng Nai | Văn miếu Trấn Biên | `dn02-van-mieu-tran-bien.webp` |
| Đồng Nai | Vườn quốc gia Cát Tiên | `dn03-vuon-quoc-gia-cat-tien.webp` |
| Đồng Nai | Khu du lịch thác Giang Điền | `dn04-thac-giang-dien.webp` |
| Đồng Nai | Khu du lịch Vườn Xoài | `dn05-khu-du-lich-vuon-xoai.webp` |
| Bình Dương | Chùa Hội Khánh | `bd01-chua-hoi-khanh.webp` |
| Bình Dương | Chùa Bà Thiên Hậu Thủ Dầu Một | `bd02-chua-ba-thien-hau.jpg` |
| Bình Dương | Nhà cổ Trần Văn Hổ | `bd03-nha-co-tran-van-ho.webp` |
| Bình Dương | Làng sơn mài Tương Bình Hiệp | `bd04-lang-son-mai-tuong-binh-hiep.webp` |
| Bình Dương | Khu du lịch Đại Nam | `bd05-khu-du-lich-dai-nam.webp` |
