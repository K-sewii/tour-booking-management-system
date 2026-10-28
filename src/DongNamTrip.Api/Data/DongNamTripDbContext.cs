using System;
using System.Collections.Generic;
using DongNamTrip.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DongNamTrip.Api.Data;

public partial class DongNamTripDbContext : DbContext
{
    public DongNamTripDbContext(DbContextOptions<DongNamTripDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Bandich> Bandich { get; set; }

    public virtual DbSet<Baogia> Baogia { get; set; }

    public virtual DbSet<Diadiem> Diadiem { get; set; }

    public virtual DbSet<Dondat> Dondat { get; set; }

    public virtual DbSet<Giaodichtien> Giaodichtien { get; set; }

    public virtual DbSet<Hangmucbaogia> Hangmucbaogia { get; set; }

    public virtual DbSet<Hoatdong> Hoatdong { get; set; }

    public virtual DbSet<Lichkhoihanh> Lichkhoihanh { get; set; }

    public virtual DbSet<Lichtrinh> Lichtrinh { get; set; }

    public virtual DbSet<LichtrinhDiadiem> LichtrinhDiadiem { get; set; }

    public virtual DbSet<LichtrinhSuat> LichtrinhSuat { get; set; }

    public virtual DbSet<Ngonngu> Ngonngu { get; set; }

    public virtual DbSet<Nhatkythaotac> Nhatkythaotac { get; set; }

    public virtual DbSet<Phienbanlichtrinh> Phienbanlichtrinh { get; set; }

    public virtual DbSet<Quyennghe> Quyennghe { get; set; }

    public virtual DbSet<Suat> Suat { get; set; }

    public virtual DbSet<Taikhoan> Taikhoan { get; set; }

    public virtual DbSet<TaikhoanVaitro> TaikhoanVaitro { get; set; }

    public virtual DbSet<Tepaudio> Tepaudio { get; set; }

    public virtual DbSet<Thuyetminh> Thuyetminh { get; set; }

    public virtual DbSet<Tour> Tour { get; set; }

    public virtual DbSet<TourDiadiem> TourDiadiem { get; set; }

    public virtual DbSet<Vaitro> Vaitro { get; set; }

    public virtual DbSet<Xacminhdichvu> Xacminhdichvu { get; set; }

    public virtual DbSet<Yeucautourrieng> Yeucautourrieng { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bandich>(entity =>
        {
            entity.HasKey(e => e.MaBd);

            entity.ToTable("BANDICH", tb => tb.HasComment("Lưu nội dung theo ngôn ngữ cho một phiên bản thuyết minh gốc."));

            entity.HasIndex(e => e.MaNn, "IX_BANDICH_MaNN");

            entity.HasIndex(e => new { e.MaTm, e.MaNn }, "UQ_BANDICH_MaTM_MaNN").IsUnique();

            entity.Property(e => e.MaBd).HasColumnName("MaBD");
            entity.Property(e => e.MaNn)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNN");
            entity.Property(e => e.MaTm).HasColumnName("MaTM");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft", "DF_BANDICH_TrangThai");

            entity.HasOne(d => d.MaNnNavigation).WithMany(p => p.Bandich)
                .HasForeignKey(d => d.MaNn)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BANDICH_MaNN");

            entity.HasOne(d => d.MaTmNavigation).WithMany(p => p.Bandich)
                .HasForeignKey(d => d.MaTm)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BANDICH_MaTM");
        });

        modelBuilder.Entity<Baogia>(entity =>
        {
            entity.HasKey(e => e.MaBg);

            entity.ToTable("BAOGIA", tb => tb.HasComment("Lưu giá và điều kiện gắn với yêu cầu cùng phiên bản lịch trình được báo giá."));

            entity.HasIndex(e => e.MaPb, "IX_BAOGIA_MaPB");

            entity.HasIndex(e => e.MaYc, "IX_BAOGIA_MaYC");

            entity.HasIndex(e => new { e.TrangThai, e.HanHieuLuc }, "IX_BAOGIA_TrangThai_HanHieuLuc");

            entity.Property(e => e.MaBg).HasColumnName("MaBG");
            entity.Property(e => e.DieuKienHuy).HasMaxLength(2000);
            entity.Property(e => e.KhoanCanThu).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.MaPb).HasColumnName("MaPB");
            entity.Property(e => e.MaYc).HasColumnName("MaYC");
            entity.Property(e => e.TongGia).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft", "DF_BAOGIA_TrangThai");

            entity.HasOne(d => d.MaPbNavigation).WithMany(p => p.Baogia)
                .HasForeignKey(d => d.MaPb)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BAOGIA_MaPB");

            entity.HasOne(d => d.MaYcNavigation).WithMany(p => p.Baogia)
                .HasForeignKey(d => d.MaYc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BAOGIA_MaYC");
        });

        modelBuilder.Entity<Diadiem>(entity =>
        {
            entity.HasKey(e => e.MaDd);

            entity.ToTable("DIADIEM", tb => tb.HasComment("Lưu thông tin địa điểm, vị trí, thời lượng và trạng thái công bố."));

            entity.Property(e => e.MaDd).HasColumnName("MaDD");
            entity.Property(e => e.AnhMinhHoa).HasMaxLength(500);
            entity.Property(e => e.DiaChi).HasMaxLength(250);
            entity.Property(e => e.KhuVuc).HasMaxLength(50);
            entity.Property(e => e.KinhDo).HasColumnType("decimal(9, 6)");
            entity.Property(e => e.LoaiHinh).HasMaxLength(100);
            entity.Property(e => e.MoTa).HasMaxLength(3000);
            entity.Property(e => e.TenDd)
                .HasMaxLength(120)
                .HasColumnName("TenDD");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft", "DF_DIADIEM_TrangThai");
            entity.Property(e => e.ViDo).HasColumnType("decimal(9, 6)");
        });

        modelBuilder.Entity<Dondat>(entity =>
        {
            entity.HasKey(e => e.MaDon);

            entity.ToTable("DONDAT", tb => tb.HasComment("Lưu đơn tour có sẵn hoặc tour riêng cùng giá và điều kiện đã chốt."));

            entity.HasIndex(e => e.MaLkh, "IX_DONDAT_MaLKH");

            entity.HasIndex(e => e.MaTk, "IX_DONDAT_MaTK");

            entity.HasIndex(e => new { e.TrangThai, e.HanGiuCho }, "IX_DONDAT_TrangThai_HanGiuCho");

            entity.HasIndex(e => e.MaBg, "UX_DONDAT_MaBG_KhongRong")
                .IsUnique()
                .HasFilter("([MaBG] IS NOT NULL)");

            entity.Property(e => e.DieuKienHuy).HasMaxLength(2000);
            entity.Property(e => e.GiaChot).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.LyDoHuy).HasMaxLength(1000);
            entity.Property(e => e.MaBg).HasColumnName("MaBG");
            entity.Property(e => e.MaLkh).HasColumnName("MaLKH");
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.NgayDat).HasDefaultValueSql("(sysutcdatetime())", "DF_DONDAT_NgayDat");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending", "DF_DONDAT_TrangThai");

            entity.HasOne(d => d.MaBgNavigation).WithOne(p => p.Dondat)
                .HasForeignKey<Dondat>(d => d.MaBg)
                .HasConstraintName("FK_DONDAT_MaBG");

            entity.HasOne(d => d.MaLkhNavigation).WithMany(p => p.Dondat)
                .HasForeignKey(d => d.MaLkh)
                .HasConstraintName("FK_DONDAT_MaLKH");

            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.Dondat)
                .HasForeignKey(d => d.MaTk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DONDAT_MaTK");
        });

        modelBuilder.Entity<Giaodichtien>(entity =>
        {
            entity.HasKey(e => e.MaGd);

            entity.ToTable("GIAODICHTIEN", tb => tb.HasComment("Lưu các khoản thu và hoàn tiền của đơn, kèm người ghi nhận và mã đối soát."));

            entity.HasIndex(e => e.MaDon, "IX_GIAODICHTIEN_MaDon");

            entity.HasIndex(e => e.MaTkghiNhan, "IX_GIAODICHTIEN_MaTKGhiNhan");

            entity.HasIndex(e => e.MaThamChieu, "UQ_GIAODICHTIEN_MaThamChieu").IsUnique();

            entity.Property(e => e.MaGd).HasColumnName("MaGD");
            entity.Property(e => e.LoaiGd)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("LoaiGD");
            entity.Property(e => e.MaThamChieu)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.MaTkghiNhan).HasColumnName("MaTKGhiNhan");
            entity.Property(e => e.SoTien).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.ThoiDiem).HasDefaultValueSql("(sysutcdatetime())", "DF_GIAODICHTIEN_ThoiDiem");

            entity.HasOne(d => d.MaDonNavigation).WithMany(p => p.Giaodichtien)
                .HasForeignKey(d => d.MaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GIAODICHTIEN_MaDon");

            entity.HasOne(d => d.MaTkghiNhanNavigation).WithMany(p => p.Giaodichtien)
                .HasForeignKey(d => d.MaTkghiNhan)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_GIAODICHTIEN_MaTKGhiNhan");
        });

        modelBuilder.Entity<Hangmucbaogia>(entity =>
        {
            entity.HasKey(e => e.MaHm);

            entity.ToTable("HANGMUCBAOGIA", tb => tb.HasComment("Lưu từng dịch vụ, số lượng và đơn giá cấu thành báo giá."));

            entity.HasIndex(e => e.MaBg, "IX_HANGMUCBAOGIA_MaBG");

            entity.Property(e => e.MaHm).HasColumnName("MaHM");
            entity.Property(e => e.DonGia).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.DonViTinh).HasMaxLength(50);
            entity.Property(e => e.MaBg).HasColumnName("MaBG");
            entity.Property(e => e.MoTa).HasMaxLength(500);

            entity.HasOne(d => d.MaBgNavigation).WithMany(p => p.Hangmucbaogia)
                .HasForeignKey(d => d.MaBg)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HANGMUCBAOGIA_MaBG");
        });

        modelBuilder.Entity<Hoatdong>(entity =>
        {
            entity.HasKey(e => e.MaHd);

            entity.ToTable("HOATDONG", tb => tb.HasComment("Lưu hoạt động tại địa điểm, điều kiện tham gia và phí tham khảo."));

            entity.HasIndex(e => e.MaDd, "IX_HOATDONG_MaDD");

            entity.Property(e => e.MaHd).HasColumnName("MaHD");
            entity.Property(e => e.DieuKien).HasMaxLength(500);
            entity.Property(e => e.MaDd).HasColumnName("MaDD");
            entity.Property(e => e.MoTa).HasMaxLength(2000);
            entity.Property(e => e.PhiThamKhao).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TenHd)
                .HasMaxLength(150)
                .HasColumnName("TenHD");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft", "DF_HOATDONG_TrangThai");

            entity.HasOne(d => d.MaDdNavigation).WithMany(p => p.Hoatdong)
                .HasForeignKey(d => d.MaDd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HOATDONG_MaDD");
        });

        modelBuilder.Entity<Lichkhoihanh>(entity =>
        {
            entity.HasKey(e => e.MaLkh);

            entity.ToTable("LICHKHOIHANH", tb => tb.HasComment("Lưu từng đợt tổ chức tour, giá, sức chứa và số chỗ đã giữ."));

            entity.HasIndex(e => e.MaTour, "IX_LICHKHOIHANH_MaTour");

            entity.Property(e => e.MaLkh).HasColumnName("MaLKH");
            entity.Property(e => e.Gia).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft", "DF_LICHKHOIHANH_TrangThai");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.Lichkhoihanh)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHKHOIHANH_MaTour");
        });

        modelBuilder.Entity<Lichtrinh>(entity =>
        {
            entity.HasKey(e => e.MaLt);

            entity.ToTable("LICHTRINH", tb => tb.HasComment("Lưu kế hoạch chuyến đi hiện tại và tài khoản sở hữu."));

            entity.HasIndex(e => e.MaTk, "IX_LICHTRINH_MaTK");

            entity.Property(e => e.MaLt).HasColumnName("MaLT");
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.NganSach).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.NgayCapNhat).HasDefaultValueSql("(sysutcdatetime())", "DF_LICHTRINH_NgayCapNhat");
            entity.Property(e => e.NoiXuatPhat).HasMaxLength(250);
            entity.Property(e => e.PhuongTien).HasMaxLength(50);
            entity.Property(e => e.TenLt)
                .HasMaxLength(100)
                .HasColumnName("TenLT");

            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.Lichtrinh)
                .HasForeignKey(d => d.MaTk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHTRINH_MaTK");
        });

        modelBuilder.Entity<LichtrinhDiadiem>(entity =>
        {
            entity.HasKey(e => new { e.MaLt, e.MaDd });

            entity.ToTable("LICHTRINH_DIADIEM", tb => tb.HasComment("Lưu các điểm dự kiến tham quan và thứ tự của lịch trình cá nhân."));

            entity.HasIndex(e => e.MaDd, "IX_LICHTRINH_DIADIEM_MaDD");

            entity.HasIndex(e => new { e.MaLt, e.NgayThu, e.ThuTu }, "UQ_LICHTRINH_DIADIEM_MaLT_NgayThu_ThuTu").IsUnique();

            entity.Property(e => e.MaLt).HasColumnName("MaLT");
            entity.Property(e => e.MaDd).HasColumnName("MaDD");
            entity.Property(e => e.GhiChu).HasMaxLength(500);

            entity.HasOne(d => d.MaDdNavigation).WithMany(p => p.LichtrinhDiadiem)
                .HasForeignKey(d => d.MaDd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHTRINH_DIADIEM_MaDD");

            entity.HasOne(d => d.MaLtNavigation).WithMany(p => p.LichtrinhDiadiem)
                .HasForeignKey(d => d.MaLt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHTRINH_DIADIEM_MaLT");
        });

        modelBuilder.Entity<LichtrinhSuat>(entity =>
        {
            entity.HasKey(e => new { e.MaLt, e.MaSuat });

            entity.ToTable("LICHTRINH_SUAT", tb => tb.HasComment("Lưu các suất hoạt động khách mong muốn tham gia tại từng địa điểm."));

            entity.HasIndex(e => e.MaDd, "IX_LICHTRINH_SUAT_MaDD");

            entity.HasIndex(e => e.MaSuat, "IX_LICHTRINH_SUAT_MaSuat");

            entity.Property(e => e.MaLt).HasColumnName("MaLT");
            entity.Property(e => e.GhiChu).HasMaxLength(500);
            entity.Property(e => e.MaDd).HasColumnName("MaDD");

            entity.HasOne(d => d.MaDdNavigation).WithMany(p => p.LichtrinhSuat)
                .HasForeignKey(d => d.MaDd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHTRINH_SUAT_MaDD");

            entity.HasOne(d => d.MaLtNavigation).WithMany(p => p.LichtrinhSuat)
                .HasForeignKey(d => d.MaLt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHTRINH_SUAT_MaLT");

            entity.HasOne(d => d.MaSuatNavigation).WithMany(p => p.LichtrinhSuat)
                .HasForeignKey(d => d.MaSuat)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHTRINH_SUAT_MaSuat");

            entity.HasOne(d => d.LichtrinhDiadiem).WithMany(p => p.LichtrinhSuat)
                .HasForeignKey(d => new { d.MaLt, d.MaDd })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LICHTRINH_SUAT_DiemTrongLichTrinh");
        });

        modelBuilder.Entity<Ngonngu>(entity =>
        {
            entity.HasKey(e => e.MaNn);

            entity.ToTable("NGONNGU", tb => tb.HasComment("Lưu mã và tên ngôn ngữ được hỗ trợ cho bản dịch."));

            entity.Property(e => e.MaNn)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("MaNN");
            entity.Property(e => e.TenNn)
                .HasMaxLength(50)
                .HasColumnName("TenNN");
        });

        modelBuilder.Entity<Nhatkythaotac>(entity =>
        {
            entity.HasKey(e => e.MaNk);

            entity.ToTable("NHATKYTHAOTAC", tb => tb.HasComment("Lưu người thực hiện, thời điểm, đối tượng, lý do và dữ liệu thay đổi để đáp ứng UC24 và BR13."));

            entity.HasIndex(e => new { e.LoaiDoiTuong, e.MaDoiTuong, e.ThoiDiem }, "IX_NHATKYTHAOTAC_DoiTuong");

            entity.HasIndex(e => e.MaTk, "IX_NHATKYTHAOTAC_MaTK");

            entity.HasIndex(e => e.ThoiDiem, "IX_NHATKYTHAOTAC_ThoiDiem");

            entity.Property(e => e.MaNk).HasColumnName("MaNK");
            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(45)
                .IsUnicode(false)
                .HasColumnName("DiaChiIP");
            entity.Property(e => e.HanhDong).HasMaxLength(100);
            entity.Property(e => e.LoaiDoiTuong).HasMaxLength(100);
            entity.Property(e => e.LyDo).HasMaxLength(2000);
            entity.Property(e => e.MaDoiTuong).HasMaxLength(128);
            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.ThoiDiem).HasDefaultValueSql("(sysutcdatetime())", "DF_NHATKYTHAOTAC_ThoiDiem");

            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.Nhatkythaotac)
                .HasForeignKey(d => d.MaTk)
                .HasConstraintName("FK_NHATKYTHAOTAC_MaTK");
        });

        modelBuilder.Entity<Phienbanlichtrinh>(entity =>
        {
            entity.HasKey(e => e.MaPb);

            entity.ToTable("PHIENBANLICHTRINH", tb => tb.HasComment("Lưu bản chụp lịch trình tại thời điểm gửi yêu cầu hoặc chốt phương án."));

            entity.HasIndex(e => new { e.MaLt, e.SoPhienBan }, "UQ_PHIENBANLICHTRINH_MaLT_SoPhienBan").IsUnique();

            entity.Property(e => e.MaPb).HasColumnName("MaPB");
            entity.Property(e => e.MaLt).HasColumnName("MaLT");
            entity.Property(e => e.NoiDungJson).HasColumnName("NoiDungJSON");
            entity.Property(e => e.ThoiDiemTao).HasDefaultValueSql("(sysutcdatetime())", "DF_PHIENBANLICHTRINH_ThoiDiemTao");

            entity.HasOne(d => d.MaLtNavigation).WithMany(p => p.Phienbanlichtrinh)
                .HasForeignKey(d => d.MaLt)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PHIENBANLICHTRINH_MaLT");
        });

        modelBuilder.Entity<Quyennghe>(entity =>
        {
            entity.HasKey(e => e.MaQn);

            entity.ToTable("QUYENNGHE", tb => tb.HasComment("Lưu quyền nghe theo tài khoản, đơn đã xác nhận và khoảng hiệu lực."));

            entity.HasIndex(e => e.MaDon, "IX_QUYENNGHE_MaDon");

            entity.HasIndex(e => e.MaTk, "IX_QUYENNGHE_MaTK");

            entity.HasIndex(e => new { e.MaTk, e.HieuLucTu, e.HieuLucDen }, "IX_QUYENNGHE_TaiKhoan_HieuLuc");

            entity.Property(e => e.MaQn).HasColumnName("MaQN");
            entity.Property(e => e.MaTk).HasColumnName("MaTK");

            entity.HasOne(d => d.MaDonNavigation).WithMany(p => p.Quyennghe)
                .HasForeignKey(d => d.MaDon)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUYENNGHE_MaDon");

            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.Quyennghe)
                .HasForeignKey(d => d.MaTk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_QUYENNGHE_MaTK");
        });

        modelBuilder.Entity<Suat>(entity =>
        {
            entity.HasKey(e => e.MaSuat);

            entity.ToTable("SUAT", tb => tb.HasComment("Lưu ngày giờ tổ chức và trạng thái công bố của hoạt động."));

            entity.HasIndex(e => new { e.MaHd, e.Ngay, e.GioBatDau }, "UQ_SUAT_MaHD_Ngay_GioBatDau").IsUnique();

            entity.Property(e => e.MaHd).HasColumnName("MaHD");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Published", "DF_SUAT_TrangThai");

            entity.HasOne(d => d.MaHdNavigation).WithMany(p => p.Suat)
                .HasForeignKey(d => d.MaHd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SUAT_MaHD");
        });

        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.HasKey(e => e.MaTk);

            entity.ToTable("TAIKHOAN", tb => tb.HasComment("Lưu thông tin đăng nhập, liên hệ và trạng thái tài khoản."));

            entity.HasIndex(e => e.Email, "UQ_TAIKHOAN_Email").IsUnique();

            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.Email)
                .HasMaxLength(254)
                .IsUnicode(false)
                .UseCollation("Latin1_General_100_CI_AS");
            entity.Property(e => e.HoTen).HasMaxLength(100);
            entity.Property(e => e.MatKhauBam)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.NgayTao).HasDefaultValueSql("(sysutcdatetime())", "DF_TAIKHOAN_NgayTao");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Active", "DF_TAIKHOAN_TrangThai");
        });

        modelBuilder.Entity<TaikhoanVaitro>(entity =>
        {
            entity.HasKey(e => new { e.MaTk, e.MaVaiTro });

            entity.ToTable("TAIKHOAN_VAITRO", tb => tb.HasComment("Lưu việc gán một hoặc nhiều vai trò cho tài khoản."));

            entity.HasIndex(e => e.MaVaiTro, "IX_TAIKHOAN_VAITRO_MaVaiTro");

            entity.Property(e => e.MaTk).HasColumnName("MaTK");
            entity.Property(e => e.NgayGan).HasDefaultValueSql("(sysutcdatetime())", "DF_TAIKHOAN_VAITRO_NgayGan");

            entity.HasOne(d => d.MaTkNavigation).WithMany(p => p.TaikhoanVaitro)
                .HasForeignKey(d => d.MaTk)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TAIKHOAN_VAITRO_MaTK");

            entity.HasOne(d => d.MaVaiTroNavigation).WithMany(p => p.TaikhoanVaitro)
                .HasForeignKey(d => d.MaVaiTro)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TAIKHOAN_VAITRO_MaVaiTro");
        });

        modelBuilder.Entity<Tepaudio>(entity =>
        {
            entity.HasKey(e => e.MaTep);

            entity.ToTable("TEPAUDIO", tb => tb.HasComment("Lưu tệp giọng đọc gắn với bản dịch và trạng thái công bố."));

            entity.HasIndex(e => e.MaBd, "IX_TEPAUDIO_MaBD");

            entity.Property(e => e.DuongDan).HasMaxLength(500);
            entity.Property(e => e.GiongDoc)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MaBd).HasColumnName("MaBD");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending", "DF_TEPAUDIO_TrangThai");

            entity.HasOne(d => d.MaBdNavigation).WithMany(p => p.Tepaudio)
                .HasForeignKey(d => d.MaBd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TEPAUDIO_MaBD");
        });

        modelBuilder.Entity<Thuyetminh>(entity =>
        {
            entity.HasKey(e => e.MaTm);

            entity.ToTable("THUYETMINH", tb => tb.HasComment("Lưu nội dung gốc và phiên bản thuyết minh của địa điểm."));

            entity.HasIndex(e => e.MaDd, "IX_THUYETMINH_MaDD");

            entity.Property(e => e.MaTm).HasColumnName("MaTM");
            entity.Property(e => e.MaDd).HasColumnName("MaDD");
            entity.Property(e => e.TieuDe).HasMaxLength(200);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft", "DF_THUYETMINH_TrangThai");

            entity.HasOne(d => d.MaDdNavigation).WithMany(p => p.Thuyetminh)
                .HasForeignKey(d => d.MaDd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_THUYETMINH_MaDD");
        });

        modelBuilder.Entity<Tour>(entity =>
        {
            entity.HasKey(e => e.MaTour);

            entity.ToTable("TOUR", tb => tb.HasComment("Lưu chương trình tour mẫu, giá tham khảo và điều kiện hủy."));

            entity.Property(e => e.DieuKienHuy).HasMaxLength(2000);
            entity.Property(e => e.GiaThamKhao).HasColumnType("decimal(18, 0)");
            entity.Property(e => e.KhuVuc).HasMaxLength(50);
            entity.Property(e => e.LoaiHinh).HasMaxLength(100);
            entity.Property(e => e.TenTour).HasMaxLength(150);
            entity.Property(e => e.ThoiLuong).HasMaxLength(50);
            entity.Property(e => e.TomTat).HasMaxLength(1000);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Draft", "DF_TOUR_TrangThai");
        });

        modelBuilder.Entity<TourDiadiem>(entity =>
        {
            entity.HasKey(e => new { e.MaTour, e.MaDd });

            entity.ToTable("TOUR_DIADIEM", tb => tb.HasComment("Liên kết tour với địa điểm, ngày thứ và thứ tự tham quan."));

            entity.HasIndex(e => e.MaDd, "IX_TOUR_DIADIEM_MaDD");

            entity.HasIndex(e => new { e.MaTour, e.NgayThu, e.ThuTu }, "UQ_TOUR_DIADIEM_MaTour_NgayThu_ThuTu").IsUnique();

            entity.Property(e => e.MaDd).HasColumnName("MaDD");

            entity.HasOne(d => d.MaDdNavigation).WithMany(p => p.TourDiadiem)
                .HasForeignKey(d => d.MaDd)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOUR_DIADIEM_MaDD");

            entity.HasOne(d => d.MaTourNavigation).WithMany(p => p.TourDiadiem)
                .HasForeignKey(d => d.MaTour)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TOUR_DIADIEM_MaTour");
        });

        modelBuilder.Entity<Vaitro>(entity =>
        {
            entity.HasKey(e => e.MaVaiTro);

            entity.ToTable("VAITRO", tb => tb.HasComment("Lưu danh mục vai trò và ý nghĩa của từng vai trò."));

            entity.HasIndex(e => e.TenVaiTro, "UQ_VAITRO_TenVaiTro").IsUnique();

            entity.Property(e => e.MoTa).HasMaxLength(250);
            entity.Property(e => e.TenVaiTro).HasMaxLength(50);
        });

        modelBuilder.Entity<Xacminhdichvu>(entity =>
        {
            entity.HasKey(e => e.MaXm);

            entity.ToTable("XACMINHDICHVU", tb => tb.HasComment("Lưu kết quả kiểm tra từng dịch vụ với đơn vị cung cấp trước khi báo giá."));

            entity.HasIndex(e => e.MaYc, "IX_XACMINHDICHVU_MaYC");

            entity.Property(e => e.MaXm).HasColumnName("MaXM");
            entity.Property(e => e.DonViCungCap).HasMaxLength(250);
            entity.Property(e => e.GhiChu).HasMaxLength(1000);
            entity.Property(e => e.HangMuc).HasMaxLength(250);
            entity.Property(e => e.KetQua)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MaYc).HasColumnName("MaYC");
            entity.Property(e => e.ThoiDiem).HasDefaultValueSql("(sysutcdatetime())", "DF_XACMINHDICHVU_ThoiDiem");

            entity.HasOne(d => d.MaYcNavigation).WithMany(p => p.Xacminhdichvu)
                .HasForeignKey(d => d.MaYc)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_XACMINHDICHVU_MaYC");
        });

        modelBuilder.Entity<Yeucautourrieng>(entity =>
        {
            entity.HasKey(e => e.MaYc);

            entity.ToTable("YEUCAUTOURRIENG", tb => tb.HasComment("Lưu nhu cầu tổ chức chuyến đi dựa trên một phiên bản lịch trình."));

            entity.HasIndex(e => e.MaPb, "IX_YEUCAUTOURRIENG_MaPB");

            entity.Property(e => e.MaYc).HasColumnName("MaYC");
            entity.Property(e => e.LienHe).HasMaxLength(250);
            entity.Property(e => e.MaPb).HasColumnName("MaPB");
            entity.Property(e => e.NgayGui).HasDefaultValueSql("(sysutcdatetime())", "DF_YEUCAUTOURRIENG_NgayGui");
            entity.Property(e => e.NhuCauDichVu).HasMaxLength(2000);
            entity.Property(e => e.TrangThai)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Submitted", "DF_YEUCAUTOURRIENG_TrangThai");

            entity.HasOne(d => d.MaPbNavigation).WithMany(p => p.Yeucautourrieng)
                .HasForeignKey(d => d.MaPb)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_YEUCAUTOURRIENG_MaPB");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
