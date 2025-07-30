🌿 EcoConnect Hanoi – Kết Nối Cộng Đồng Vì Môi Trường Hà Nội
1. 📝 Giới thiệu
EcoConnect Hanoi là một nền tảng trực tuyến đa chức năng nhằm thúc đẩy các hoạt động sống xanh trong cộng đồng dân cư tại Hà Nội. Ứng dụng hỗ trợ phân loại rác, cập nhật lịch thu gom, tìm địa điểm tái chế, chia sẻ/tặng đồ dùng cũ, và khuyến khích người dùng thông qua hệ thống thử thách và điểm thưởng.

2. 🧱 Nền tảng & Công nghệ
Back-end: ASP.NET Core Web API (C#)

Front-end: ASP.NET Core MVC / Razor Pages (giả định Web App)

Cơ sở dữ liệu: Microsoft SQL Server

Ngôn ngữ chính: C#

3. 🌐 Chức năng chính
3.1 Quản lý tài khoản người dùng
Đăng ký/Đăng nhập: Email, mật khẩu (được băm), họ tên, quận/phường.

Xác thực: Email, khôi phục mật khẩu.

Hồ sơ cá nhân: Eco-Points, lịch sử hoạt động, thử thách đã tham gia.

Vai trò:

User: Xem thông tin, đăng tin, tham gia thử thách.

Admin: Quản lý nội dung, kiểm duyệt, thống kê, thử thách.

3.2 Hướng dẫn phân loại & tái chế
Phân loại rác: Hình ảnh/video trực quan theo quy định Hà Nội.

Lịch thu gom rác: Theo địa chỉ người dùng.

Địa điểm tái chế: Địa chỉ, loại rác nhận, giờ hoạt động.

3.3 Chợ đồ cũ ảo – Cho/Tặng/Trao đổi
Đăng tin: Hình thức (cho tặng / trao đổi), danh mục, ảnh, mô tả.

Tìm kiếm: Theo tên, danh mục, quận/phường, hình thức.

Liên hệ: Nhắn tin ẩn danh hoặc thông tin công khai.

Quản lý tin: Chỉnh sửa, xoá, đánh dấu hoàn thành.

3.4 Gamification – Thử thách sống xanh (Tuỳ chọn)
Tạo thử thách: Theo tuần/tháng hoặc sự kiện đặc biệt.

Ghi nhận: Qua báo cáo người dùng hoặc hành động trong hệ thống.

Điểm thưởng (Eco-Points): Gắn với hành động tích cực.

Bảng xếp hạng & đổi điểm: Khuyến khích cạnh tranh lành mạnh.

3.5 Quản trị hệ thống (Admin)
Quản lý nội dung: Hướng dẫn, lịch thu gom, địa điểm.

Danh mục: Rác, đồ cũ, khu vực...

Người dùng: Phân quyền, quản lý.

Tin đăng: Kiểm duyệt để tránh rác trá hình.

Thử thách & điểm: Theo dõi và cập nhật hệ thống gamification.

4. 🗃️ Cấu trúc cơ sở dữ liệu (Gợi ý - SQL Server)
Người dùng
sql
Sao chép
Chỉnh sửa
Users(UserId, Email, PasswordHash, FullName, District, Ward, EcoPoints)
Phân loại & hướng dẫn tái chế
sql
Sao chép
Chỉnh sửa
RecyclingCategories(CategoryId, Name, Description)
RecyclingGuides(GuideId, CategoryId, Content, ImageUrl)
Lịch và địa điểm thu gom
sql
Sao chép
Chỉnh sửa
CollectionSchedules(ScheduleId, District, Ward, WasteType, CollectionDayOfWeek, CollectionTime)
CollectionPoints(PointId, Name, Address, District, Ward, Latitude, Longitude, AcceptedCategoryIds, OperatingHours)
Chợ đồ cũ
sql
Sao chép
Chỉnh sửa
ItemCategories(ItemCategoryId, Name)

CommunityItems(
  ItemId, OwnerUserId, Title, Description, ItemCategoryId,
  Condition, ListingType, ExchangeWishes, PreferredLocation,
  Status, CreatedAt
)

ItemImages(ImageId, ItemId, ImageUrl)
ItemClaims(ClaimId, ItemId, InterestedUserId, ClaimTime, Status)
Gamification
sql
Sao chép
Chỉnh sửa
Challenges(ChallengeId, Title, Description, StartDate, EndDate, PointsAwarded)
UserChallenges(UserId, ChallengeId, CompletionDate)
UserPointsLog(UserId, ActionType, Points, Timestamp)
5. ⚠️ Ghi chú quan trọng
Cập nhật dữ liệu: Nội dung phân loại, lịch, địa điểm cần chính xác và thường xuyên được cập nhật.

Giới hạn ban đầu: Nên triển khai thử nghiệm tại một quận/phường cụ thể.

Tương tác đơn giản: Giao diện cần thân thiện, dễ sử dụng với mọi đối tượng.

Đo lường hiệu quả: Thống kê số lượng món đồ đã cho, thử thách đã hoàn thành, v.v.

Hợp tác cộng đồng: Có thể kết nối với tổ chức môi trường, địa phương, doanh nghiệp xanh.

6. 💡 Tổng kết
EcoConnect Hanoi là dự án ứng dụng công nghệ để giải quyết bài toán môi trường và kết nối cộng đồng một cách thực tế và sáng tạo. Kết hợp giữa:

Cung cấp thông tin (Chuyển đổi số)

Chia sẻ cộng đồng (Kinh tế tuần hoàn)

Khuyến khích hành vi (Gamification)

