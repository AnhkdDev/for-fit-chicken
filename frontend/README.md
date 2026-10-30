# ForFit Chicken frontend

React + TypeScript + Vite, code sản phẩm ở đây; reference ignored nằm ngoài frontend.
Đọc `../docs/HANDOFF.md` và `../docs/frontend-progress.md` trước khi làm tiếp.

## Chạy trên Windows
Yêu cầu Node 24 LTS (đã kiểm tra 24.13.1), npm, .NET SDK đã có của backend.

```powershell
cd D:\TaiLieu\UcGaNauCham\frontend
npm ci
.\scripts\setup-https.ps1
# Chỉ nếu development certificate chưa được tin cậy, tự xác nhận hộp thoại:
dotnet dev-certs https --trust
npm run dev
```

Mở https://localhost:5173. Trong cửa sổ khác chạy đúng một backend HTTPS theo README backend:
`dotnet run --project ForFitChicken.API --launch-profile https` từ backend/ForFitChicken.
Frontend proxy `/api` về https://localhost:7010, giữ cùng origin phía browser. Không trộn 127.0.0.1 và localhost.
Khi chưa có certificate, Vite chạy HTTP chỉ để xem thiết kế; nút login bị chặn vì cookie Secure cần HTTPS.
Không tắt Secure/SameSite/CSRF để làm login hoạt động qua HTTP. `secure:false` chỉ áp dụng proxy dev tới
certificate localhost của API, không phải cấu hình reverse proxy production.

`.certs/` có private key development, Git ignored, không đưa lên Git/ảnh chụp/chat hoặc máy chủ production.
Không dùng biến `VITE_*` để chứa R2 keys, bot token, connection string hay password: chúng được đưa vào browser.
Frontend hiện không cần file `.env` hoặc bất kỳ khóa R2/Telegram nào.

## Đăng nhập quản trị thật
1. Chạy API HTTPS 7010 bằng Visual Studio hoặc dotnet (chỉ một instance), và frontend `npm run dev` tại 5173.
2. Mở **https://localhost:5173/quan-tri**. Dùng Owner đã tạo bằng bootstrap, với mật khẩu tự chọn trước đây;
   không có tài khoản/mật khẩu mặc định. Không chạy lại bootstrap để reset Owner đã có.
3. Không dùng 5174: đó là UI fixture có Owner giả lập, không phản ánh database thật. Khách mua không cần tài khoản.
4. Nếu báo kết nối máy chủ: kiểm tra API đang chạy và đúng HTTPS 7010. Nếu báo cần HTTPS: chạy
   scripts/setup-https.ps1 rồi khởi động lại Vite; certificate chưa tin cậy thì người dùng tự chạy
   `dotnet dev-certs https --trust` và xác nhận hộp thoại. Không bỏ qua browser security warning.
5. Nếu báo sai thông tin: kiểm tra username/password đã chọn, không gửi password qua chat. Sau 5 lần sai
   Identity có thể khóa 15 phút; không thử liên tục. Chưa có UI quên mật khẩu/tạo Operator.

## Trang & dữ liệu
- `/`, `/san-pham`, `/san-pham/:id`, `/cau-chuyen`, `/diem-ban`, `/gio-hang`, `/dat-hang`.
- `/quan-tri`: login Owner/Operator, dashboard và các module theo backend.
- Catalog công khai lấy dữ liệu thật, không tự chèn sản phẩm/giá/review giả để lấp khoảng trống.
- Giỏ chỉ lưu variantId/quantity trong localStorage. Không lưu password, cookie, thông tin khách hoặc token auth.
- Checkout COD hai bước. Giữ Idempotency-Key cho retry cùng payload trong cùng trang; không tự retry POST.
- CMS: lưu nháp rồi Owner công bố. HomeHero asset đầu là banner; bỏ trống dùng ảnh reference tạm.
- Cụm câu chuyện + dải sản phẩm Home: StoryShowcase/QuickBuy. Bấm sản phẩm mở popup chọn SKU/số lượng,
  thêm vào giỏ rồi tiếp tục COD cũ; không gửi đơn ngay từ popup. Chỉ sản phẩm public được trưng bày.
- Ảnh `public/reference-asin-hero.webp` chỉ để dựng bố cục nội bộ theo yêu cầu. Thay bằng ảnh có quyền sử dụng
  trước khi công khai. Không dùng nội dung/sản phẩm của A Sín như sản phẩm ForFit.
- Màn hình thao tác nâng cao còn thiếu: xem checklist trong frontend-progress, không coi UI đã bao phủ mọi API.

## Build / deployment
`npm run build` chạy strict TypeScript rồi xuất `dist/`. `npm run preview` chỉ xem build, không dùng production.
`npm test` chạy 5 kiểm tra quy tắc giỏ bằng node:test (Node 24), không cần backend/database.
`npm run dev:fixtures` mở https://localhost:5174 cho UI test. Có watermark đỏ, Owner giả lập,
không tài khoản/mật khẩu mặc định trong database. Middleware chặn toàn bộ `/api`, không forward backend.
Chỉ dùng để duyệt thiết kế/kiểm tra flow; không dùng kết quả đó để khẳng định SQL/R2/Telegram đã hoạt động.
Build production không chứa fixture plugin/data. Tắt fixture server sau kiểm tra.
Production phục vụ dist qua cùng domain HTTPS với `/api` reverse proxy sang ASP.NET.
Nginx SPA: `location / { try_files $uri $uri/ /index.html; }`; `/api/` phải proxy, không fallback HTML.
Giữ giới hạn multipart 6 MiB theo backend runbook. Tránh cache `/api/admin` và `/api/orders`.
Không phục vụ thư mục source, `.certs`, User Secrets hoặc env của backend từ web root.
Google Fonts hiện được tải ngoài cho font chữ; có system fallback. Có thể self-host font trước production.

## Cấu trúc
`src/lib`: DTO, fetch/CSRF, public store/cart. `src/components`: UI dùng chung.
`src/pages/storefront.tsx`: public routes. `src/pages/admin.tsx`: session/admin modules.
`src/pages/operations.tsx`: kho/gym. `src/styles.css`: tokens, tablet <=1100, mobile <=767, nhỏ <=360.
Tách thêm theo module khi mở rộng; không gọi adapter R2 trực tiếp từ browser.

## Admin theo tác vụ (29/10/2026)
- Sản phẩm có trang thêm/chỉnh riêng, 4 bước; tên tự gợi slug, nội dung dài thu gọn; SKU tạo/sửa, ảnh chọn từ thư viện Owner hoặc upload Staff.
- Đơn có tab theo công việc, form tạo đơn Facebook/Zalo/Phone/Other, chọn SKU theo tên; sửa recipient trước xác nhận. Trên điện thoại danh sách đơn/sản phẩm thành thẻ.
- CMS có chọn/xếp ảnh, preview nội dung, Lưu nháp và công bố riêng. Thay đổi chưa lưu không được publish.
- Admin dùng CSS độc lập src/admin.css, storefront giữ concept C. Chi tiết và giới hạn kiểm chứng ở ../docs/admin-workflows.md.
- npm test hiện gồm 7 tests: cart và Vietnamese product slug. UI fixtures vẫn không ghi backend thật.

