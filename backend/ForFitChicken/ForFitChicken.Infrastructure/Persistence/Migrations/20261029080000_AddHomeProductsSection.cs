using ForFitChicken.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ForFitChicken.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ForFitChickenDbContext))]
[Migration("20261029080000_AddHomeProductsSection")]
public sealed class AddHomeProductsSection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint("CK_SiteContent_Section", "SiteContent");
        migrationBuilder.AddCheckConstraint("CK_SiteContent_Section", "SiteContent", "[SectionKey] IN ('HomeHero', 'Story', 'Usage', 'Faq', 'Contact', 'SiteSettings', 'HomeProducts')");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // A rollback with HomeProducts data deliberately fails rather than deleting content.
        migrationBuilder.DropCheckConstraint("CK_SiteContent_Section", "SiteContent");
        migrationBuilder.AddCheckConstraint("CK_SiteContent_Section", "SiteContent", "[SectionKey] IN ('HomeHero', 'Story', 'Usage', 'Faq', 'Contact', 'SiteSettings')");
    }
}
