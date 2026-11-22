using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartGymBooking.Data;
using SmartGymBooking.SampleData;

var mode = args.SingleOrDefault() ?? "--preview";
if (mode is not ("--preview" or "--apply" or "--verify" or "--restore-packages" or "--preview-extra" or "--apply-extra" or "--verify-extra"))
    throw new ArgumentException("Usage: SampleData --preview | --apply | --verify | --restore-packages | --preview-extra | --apply-extra | --verify-extra");

var root = new DirectoryInfo(Directory.GetCurrentDirectory());
while (root != null && !File.Exists(Path.Combine(root.FullName, "SmartGymBooking.csproj"))) root = root.Parent;
if (root == null) throw new InvalidOperationException("Run this tool from the repository root or a subdirectory.");

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = root.FullName });
await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")).Options);
var seeder = new SampleDataSeeder(db);
var result = mode switch
{
    "--preview-extra" => await seeder.AdditionalAsync(apply: false),
    "--apply-extra" => await seeder.AdditionalAsync(apply: true),
    "--verify-extra" => await seeder.VerifyAdditionalAsync(),
    "--apply" => await seeder.ApplyAsync(),
    "--verify" => await seeder.VerifyAsync(),
    "--restore-packages" => await seeder.RestoreOriginalPackagesAsync(Path.Combine(root.FullName, "obj", "package-restore")),
    _ => await seeder.PreviewAsync()
};
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
{
    WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
});
if (mode == "--apply" && JsonSerializer.SerializeToElement(result).GetProperty("Mode").GetString() == "Applied")
{
    var folder = Path.Combine(root.FullName, "obj", "sample-data");
    Directory.CreateDirectory(folder);
    await File.WriteAllTextAsync(Path.Combine(folder, "last-result.json"), json);
}
Console.WriteLine(json);
