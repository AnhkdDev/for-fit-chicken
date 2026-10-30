using ForFitChicken.API.Authentication;
using ForFitChicken.API.RateLimiting;
using ForFitChicken.Application.Authentication;
using ForFitChicken.Application.Commerce;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace ForFitChicken.API.Controllers;
[ApiController,Route("api/admin/employees"),Authorize(Policy=AdminPolicies.Owner),EnableRateLimiting(RateLimitPolicies.Admin),ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class EmployeesController(EmployeeService employees):StaffController {
 [HttpGet] public Task<Page<EmployeeView>> List(string? search=null,bool? active=null,int page=1,int size=20,CancellationToken token=default)=>employees.ListAsync(search,active,page,size,token);
 [HttpGet("{id:long}")] public Task<EmployeeView> Get(long id,CancellationToken token)=>employees.GetAsync(id,token);
 [HttpPost] public async Task<ActionResult<EmployeeView>> Create(EmployeeCreate input,CancellationToken token){var result=await employees.CreateAsync(input,Actor,token);return CreatedAtAction(nameof(Get),new{id=result.Id},result);}
 [HttpPut("{id:long}")] public Task<EmployeeView> Edit(long id,EmployeeEdit input,CancellationToken token)=>employees.EditAsync(id,input,Actor,token);
 [HttpPut("{id:long}/status")] public Task<EmployeeView> Status(long id,EmployeeStatus input,CancellationToken token)=>employees.StatusAsync(id,input,Actor,token);
 [HttpPost("{id:long}/password")] public Task<EmployeeView> Password(long id,EmployeePassword input,CancellationToken token)=>employees.PasswordAsync(id,input,Actor,token);
}
