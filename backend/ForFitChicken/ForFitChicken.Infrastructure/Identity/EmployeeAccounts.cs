using System.Security.Cryptography;
using System.Text;
using ForFitChicken.Application.Authentication;
using ForFitChicken.Application.Commerce;
using ForFitChicken.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace ForFitChicken.Infrastructure.Identity;
public sealed class EmployeeAccounts(ForFitChickenDbContext db,UserManager<ApplicationUser> users):IEmployeeAccounts {
 private IQueryable<ApplicationUser> Operators()=>db.Users.Where(u=>db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="OPERATOR"))&&!db.UserRoles.Any(ur=>ur.UserId==u.Id&&db.Roles.Any(r=>r.Id==ur.RoleId&&r.NormalizedName=="OWNER")));
 private static string Revision(ApplicationUser u)=>Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(u.ConcurrencyStamp??"")));
 private static EmployeeView View(ApplicationUser u)=>new(u.Id,u.UserName!,u.DisplayName,u.IsActive,u.LockoutEnabled&&u.LockoutEnd>DateTimeOffset.UtcNow,Revision(u));
 public async Task<Page<EmployeeView>> ListAsync(string? search,bool? active,int page,int size,CancellationToken token){var q=Operators();if(active.HasValue)q=q.Where(u=>u.IsActive==active.Value);if(!string.IsNullOrWhiteSpace(search)){var term=search.Trim();q=q.Where(u=>u.UserName!.Contains(term)||u.DisplayName.Contains(term));}var total=await q.CountAsync(token);var rows=await q.OrderBy(u=>u.UserName).ThenBy(u=>u.Id).Skip((page-1)*size).Take(size).ToArrayAsync(token);return new(rows.Select(View).ToArray(),total,page,size);}
 public async Task<EmployeeView?> GetAsync(long id,CancellationToken token){var u=await Operators().SingleOrDefaultAsync(u=>u.Id==id,token);return u is null?null:View(u);}
 private static void Ensure(IdentityResult result){if(result.Succeeded)return;var codes=result.Errors.Select(e=>e.Code).ToArray();if(codes.Contains("ConcurrencyFailure"))throw new BusinessException(409,"VERSION_CONFLICT","Dữ liệu đã thay đổi, tải lại trước khi lưu.");if(codes.Contains("DuplicateUserName"))throw new BusinessException(409,"USERNAME_EXISTS","Tên đăng nhập đã tồn tại.");throw new BusinessException(400,"EMPLOYEE_VALIDATION_FAILED","Không lưu được tài khoản. Kiểm tra tên đăng nhập và mật khẩu 12–128 ký tự có chữ hoa, chữ thường, số, ký tự đặc biệt. Mã: "+string.Join(", ",codes));}
 public async Task<EmployeeView> CreateAsync(EmployeeCreate input,CancellationToken token){token.ThrowIfCancellationRequested();var u=new ApplicationUser{UserName=input.UserName.Trim(),DisplayName=input.DisplayName.Trim(),IsActive=true,LockoutEnabled=true};Ensure(await users.CreateAsync(u,input.Password));Ensure(await users.AddToRoleAsync(u,AdminRoles.Operator));return View(u);}
 public async Task<EmployeeView> ChangeAsync(long id,string version,string? name,bool? active,string? password,CancellationToken token){var u=await Operators().SingleOrDefaultAsync(u=>u.Id==id,token)??throw new BusinessException(404,"EMPLOYEE_NOT_FOUND","Chỉ quản lý tài khoản Operator, không quản lý Owner.");if(Revision(u)!=version)throw new BusinessException(409,"VERSION_CONFLICT","Dữ liệu đã thay đổi, tải lại trước khi lưu.");
 if(password is not null){foreach(var validator in users.PasswordValidators)Ensure(await validator.ValidateAsync(users,u,password));u.PasswordHash=users.PasswordHasher.HashPassword(u,password);}
 if(name is not null)u.DisplayName=name.Trim();if(active.HasValue){u.IsActive=active.Value;if(active.Value){u.LockoutEnd=null;u.AccessFailedCount=0;}}
 if(password is not null||active.HasValue)u.SecurityStamp=Guid.NewGuid().ToString();Ensure(await users.UpdateAsync(u));return View(u);}
}
