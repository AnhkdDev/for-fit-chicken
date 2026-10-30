using System.ComponentModel.DataAnnotations;
using ForFitChicken.Application.Abstractions.Persistence;
using ForFitChicken.Application.Commerce;
namespace ForFitChicken.Application.Authentication;
public sealed record EmployeeView(long Id,string UserName,string DisplayName,bool IsActive,bool IsTemporarilyLocked,string Version);
public sealed record EmployeeCreate([Required,StringLength(100),RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{2,99}$")] string UserName,[Required,StringLength(150)] string DisplayName,[Required,StringLength(128,MinimumLength=12)] string Password);
public sealed record EmployeeEdit([Required] string Version,[Required,StringLength(150)] string DisplayName);
public sealed record EmployeeStatus([Required] string Version,bool IsActive);
public sealed record EmployeePassword([Required] string Version,[Required,StringLength(128,MinimumLength=12)] string Password);
public interface IEmployeeAccounts {
 Task<Page<EmployeeView>> ListAsync(string? search,bool? active,int page,int size,CancellationToken token);
 Task<EmployeeView?> GetAsync(long id,CancellationToken token);
 Task<EmployeeView> CreateAsync(EmployeeCreate input,CancellationToken token);
 Task<EmployeeView> ChangeAsync(long id,string version,string? name,bool? active,string? password,CancellationToken token);
}
public sealed class EmployeeService(IEmployeeAccounts accounts,ICommerceStore store,IUnitOfWork unit,TimeProvider clock):BusinessService(store,unit,clock){
 public Task<Page<EmployeeView>> ListAsync(string? search,bool? active,int page,int size,CancellationToken token)=>accounts.ListAsync(search,active,Number(page),Size(size),token);
 public async Task<EmployeeView> GetAsync(long id,CancellationToken token)=>await accounts.GetAsync(id,token)??throw new BusinessException(404,"EMPLOYEE_NOT_FOUND","Không tìm thấy tài khoản Operator có thể quản lý.");
 public Task<EmployeeView> CreateAsync(EmployeeCreate input,long actor,CancellationToken token)=>Write(async()=>{Validate(input);Check(!string.IsNullOrWhiteSpace(input.DisplayName),"INVALID_INPUT","Tên hiển thị bắt buộc.",400);var result=await accounts.CreateAsync(input,token);Audit(actor,"EmployeeCreated","Employee",result.Id);return result;},token);
 public Task<EmployeeView> EditAsync(long id,EmployeeEdit input,long actor,CancellationToken token){Validate(input);Check(!string.IsNullOrWhiteSpace(input.DisplayName),"INVALID_INPUT","Tên hiển thị bắt buộc.",400);return Change(id,input.Version,input.DisplayName,null,null,actor,"EmployeeNameChanged",token);}
 public Task<EmployeeView> StatusAsync(long id,EmployeeStatus input,long actor,CancellationToken token){Validate(input);return Change(id,input.Version,null,input.IsActive,null,actor,input.IsActive?"EmployeeEnabled":"EmployeeDisabled",token);}
 public Task<EmployeeView> PasswordAsync(long id,EmployeePassword input,long actor,CancellationToken token){Validate(input);return Change(id,input.Version,null,null,input.Password,actor,"EmployeePasswordReset",token);}
 private Task<EmployeeView> Change(long id,string version,string? name,bool? active,string? password,long actor,string audit,CancellationToken token)=>Write(async()=>{Check(id!=actor,"SELF_CHANGE_FORBIDDEN","Không quản lý chính tài khoản Owner tại API nhân viên.",400);var result=await accounts.ChangeAsync(id,version,name,active,password,token);Audit(actor,audit,"Employee",id);return result;},token);
}
