using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(logging =>
{
    // https://bit.ly/aspnetcore6-httplogging
    logging.LoggingFields = HttpLoggingFields.All;
    logging.MediaTypeOptions.AddText("application/javascript");
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
builder.Services.AddW3CLogging(option =>
{
    // https://bit.ly/aspnetcore6-w3clogger
    option.LoggingFields = W3CLoggingFields.All;
    option.FileSizeLimit = 5 * 1024 * 1024;
    option.RetainedFileCountLimit = 2;
    option.FileName = "Logging-W3C-UI";
    option.LogDirectory = Path.Combine(path, "logs");
    option.FlushInterval = TimeSpan.FromSeconds(2);
});

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseHttpLogging();
app.UseW3CLogging();

// Configure the HTTP request pipeline.
// if (!app.Environment.IsDevelopment())
// {
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
// }

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
